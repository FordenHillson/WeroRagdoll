using UnityEngine;
using WeroRagdoll.Core.Euphoria;

namespace WeroRagdoll.Core
{
    [DefaultExecutionOrder(50)]
    public sealed class WeroActorBrain : MonoBehaviour
    {
        public Actor Actor { get; private set; }
        public IMuscleDriver Driver { get; private set; } = new PidMuscleDriver();
        public MuscleModel Muscle => Driver.Muscle;
        public WeroState State { get; private set; } = WeroState.Normal;

        public bool BlockWeapon =>
            State == WeroState.ActiveStruggle
            || State == WeroState.GetUp
            || State == WeroState.LimpDeath
            || (State == WeroState.WoundDebuff && (Muscle.ArmDisabled(false) || Muscle.ArmDisabled(true)));

        public bool LegWoundActive =>
            State == WeroState.WoundDebuff && (Muscle.LegDisabled(false) || Muscle.LegDisabled(true));

        public float MoveSpeedMul =>
            LegWoundActive ? Plugin.Cfg.WoundLegSpeedMul.Value : 1f;

        /// <summary>
        /// Kill Actor.LateUpdate height-offset float during get-up settle.
        /// </summary>
        public bool SuppressHeightOffset =>
            State == WeroState.GetUp
            || State == WeroState.ActiveStruggle
            || (State == WeroState.WoundDebuff && _heightSyncTimer > 0f);

        private float _stateTimer;
        private float _impactCooldown;
        private float _settledTimer;
        private float _getUpElapsed;
        private float _heightSyncTimer;
        private bool _getUpStarted;
        private HitZone _lastZone = HitZone.Unknown;
        private bool _struggleSlotHeld;
        private bool _pendingLethal;
        private WoundAssist _wound;
        private DeathSquirm _squirm;
        private EuphoriaDirector _euphoria;
        private Vector3 _lastForceDir = Vector3.forward;

        public static WeroActorBrain Ensure(Actor actor)
        {
            if (actor == null)
                return null;
            var brain = actor.GetComponent<WeroActorBrain>();
            if (brain == null)
                brain = actor.gameObject.AddComponent<WeroActorBrain>();
            brain.Actor = actor;
            if (brain.Driver == null)
                brain.Driver = new PidMuscleDriver();
            brain._wound = WoundAssist.Ensure(actor);
            brain._squirm = DeathSquirm.Ensure(actor);
            brain._euphoria = EuphoriaDirector.Ensure(actor, brain);
            return brain;
        }

        private void OnDestroy()
        {
            ReleaseStruggleSlot();
            _wound?.Stop();
            _squirm?.Stop();
        }

        public void HandleDamage(DamageInfo info, bool wasDead)
        {
            if (!Plugin.Cfg.Enabled.Value || Actor == null || !Actor.aiControlled)
                return;
            if (Actor.dead && State == WeroState.LimpDeath)
                return;

            HitZone zone = HitClassifier.Classify(Actor, info);
            _lastZone = zone;
            float impact = HitClassifier.ImpactScore(info, zone);
            float impactN = Mathf.Clamp01(impact / Mathf.Max(1f, Plugin.Cfg.HardImpact.Value));
            Driver.OnImpact(zone, impactN);

            bool lethal = wasDead || Actor.dead || Actor.health <= 0f;
            ReactionTier tier = HitClassifier.Tier(impact, Muscle.CoreBalance, lethal);
            if (lethal)
                _pendingLethal = true;

            var lod = LodService.BandFor(Actor);
            if (lod == LodService.LodBand.Cheap && tier != ReactionTier.Lethal)
                return;

            switch (tier)
            {
                case ReactionTier.Soft:
                    EnterFlinch(info, zone);
                    break;
                case ReactionTier.Medium:
                    EnterStagger(info, zone);
                    break;
                case ReactionTier.Hard:
                    EnterStruggle(info, zone, lod);
                    break;
                case ReactionTier.Lethal:
                    EnterDeath(info, zone, lod);
                    break;
            }

            Vector3 wound = ImpulseBuilder.PreferHitPoint(Actor, info, zone);
            _euphoria = EuphoriaDirector.Ensure(Actor, this);
            _euphoria?.NotifyHit(zone, _lastForceDir, wound, lethal);

            _impactCooldown = 0.12f;
        }

        public void NotifyDied(DamageInfo info)
        {
            _pendingLethal = true;
            HitZone zone = _lastZone;
            if (zone == HitZone.Unknown)
                zone = HitClassifier.Classify(Actor, info);
            EnterDeath(info, zone, LodService.BandFor(Actor));
        }

        private void Update()
        {
            if (Actor == null || !Plugin.Cfg.Enabled.Value)
                return;

            float dt = Time.deltaTime;
            if (_impactCooldown > 0f)
                _impactCooldown -= dt;
            if (_heightSyncTimer > 0f)
                _heightSyncTimer -= dt;

            float recoverMul = 1f;
            if (State == WeroState.WoundDebuff)
                recoverMul = 0.55f;
            else if (State == WeroState.ActiveStruggle)
                recoverMul = 0.35f;
            else if (State == WeroState.LimpDeath)
                recoverMul = 0f;

            if (State != WeroState.LimpDeath && _impactCooldown <= 0f)
                Driver.TickRecover(dt, recoverMul);

            _stateTimer -= dt;
            switch (State)
            {
                case WeroState.Flinch:
                    if (_stateTimer <= 0f)
                    {
                        _wound?.Stop();
                        SetState(WeroState.Normal);
                    }
                    break;

                case WeroState.Stagger:
                    TickStagger();
                    break;

                case WeroState.ActiveStruggle:
                    TickStruggle();
                    break;

                case WeroState.GetUp:
                    TickGetUp();
                    break;

                case WeroState.WoundDebuff:
                    if (_heightSyncTimer > 0f)
                        ActorGetUp.TickGrounding(Actor);
                    if (_stateTimer <= 0f)
                    {
                        ActorGetUp.TickGrounding(Actor);
                        _wound?.Stop();
                        SetState(WeroState.Normal);
                    }
                    break;

                case WeroState.LimpDeath:
                    // DeathSquirm owns drive while running; otherwise keep limp.
                    if (_squirm == null || !_squirm.IsRunning)
                        Driver.ApplyLimp(Actor);
                    break;
            }
        }

        private void EnterFlinch(DamageInfo info, HitZone zone)
        {
            if (State == WeroState.ActiveStruggle || State == WeroState.LimpDeath || State == WeroState.GetUp)
            {
                if (State == WeroState.ActiveStruggle)
                {
                    Driver.ApplyLiving(Actor, 0.85f);
                    StartWound(info, zone, brace: false);
                }
                return;
            }

            SetState(WeroState.Flinch);
            _stateTimer = Plugin.Cfg.FlinchSeconds.Value;
            StartWound(info, zone, brace: false);
            try
            {
                Actor.ForceApplyAnimatorForTime(_stateTimer);
            }
            catch
            {
                // Soft flinch best-effort.
            }
        }

        private void EnterStagger(DamageInfo info, HitZone zone)
        {
            if (State == WeroState.ActiveStruggle || State == WeroState.LimpDeath)
                return;

            if (Muscle.CoreBalance <= Plugin.Cfg.CoreBalanceHardCeil.Value)
            {
                EnterStruggle(info, zone, LodService.BandFor(Actor));
                return;
            }

            SetState(WeroState.Stagger);
            _stateTimer = Plugin.Cfg.StaggerSeconds.Value;
            StartWound(info, zone, brace: true);

            bool shouldFall = Actor.fallenOver
                              || Muscle.LegDisabled(false)
                              || Muscle.LegDisabled(true)
                              || Muscle.CoreBalance < Plugin.Cfg.CoreBalanceSoftFloor.Value;
            if (shouldFall)
                Knock(info, zone, 0.55f);
            else
            {
                try { Actor.ForceApplyAnimatorForTime(Mathf.Min(0.45f, _stateTimer)); }
                catch { /* ignore */ }
            }
        }

        private void EnterStruggle(DamageInfo info, HitZone zone, LodService.LodBand lod)
        {
            if (State == WeroState.LimpDeath)
                return;

            if (lod == LodService.LodBand.Cheap)
            {
                Knock(info, zone, 0.8f);
                return;
            }

            if (!LodService.TryAcquireStruggle(Actor))
            {
                Knock(info, zone, 0.7f);
                SetState(WeroState.Stagger);
                _stateTimer = Plugin.Cfg.StaggerSeconds.Value;
                StartWound(info, zone, brace: true);
                return;
            }

            _struggleSlotHeld = true;
            SetState(WeroState.ActiveStruggle);
            _stateTimer = Plugin.Cfg.StruggleSeconds.Value;
            _settledTimer = 0f;
            Knock(info, zone, 1f);
            Driver.ApplyLiving(Actor, Plugin.Cfg.StruggleDriveMul.Value);
            StartWound(info, zone, brace: true);
            _euphoria?.NotifyHit(zone, _lastForceDir, ImpulseBuilder.PreferHitPoint(Actor, info, zone), false);
        }

        private void EnterDeath(DamageInfo info, HitZone zone, LodService.LodBand lod)
        {
            ReleaseStruggleSlot();
            SetState(WeroState.LimpDeath);
            _stateTimer = 999f;
            Muscle.CollapseAll(0.9f);

            // Always push a readable kill impulse, even if already ragdolled / one-shot weapons.
            Knock(info, zone, Plugin.Cfg.DeathImpulseScale.Value);
            Driver.ApplyLimp(Actor);

            StartWound(info, zone, brace: true);
            _wound?.SetBrace(true);

            if (lod != LodService.LodBand.Cheap && Plugin.Cfg.DeathSquirmEnabled.Value)
            {
                _squirm = DeathSquirm.Ensure(Actor);
                _squirm.Begin(Actor, Muscle, zone, _lastForceDir);
            }
            else
            {
                CancelInvoke(nameof(GoFullyLimp));
                Invoke(nameof(GoFullyLimp), 1.2f);
            }
        }

        private void GoFullyLimp()
        {
            if (State == WeroState.LimpDeath)
            {
                _wound?.Stop();
                Driver.ApplyLimp(Actor);
            }
        }

        private void TickStagger()
        {
            if (Actor.dead || _pendingLethal)
            {
                EnterDeath(DamageInfo.Default, _lastZone, LodService.BandFor(Actor));
                return;
            }

            if (Actor.fallenOver)
            {
                Driver.ApplyLiving(Actor, 0.75f);
                _wound?.SetBrace(true);
                if (Muscle.CoreBalance <= Plugin.Cfg.CoreBalanceHardCeil.Value || _stateTimer <= 0f)
                    EnterStruggle(DamageInfo.Default, _lastZone, LodService.BandFor(Actor));
                return;
            }

            if (_stateTimer <= 0f)
            {
                bool wounded = Muscle.ArmDisabled(false) || Muscle.ArmDisabled(true)
                               || Muscle.LegDisabled(false) || Muscle.LegDisabled(true);
                if (wounded)
                {
                    SetState(WeroState.WoundDebuff);
                    _stateTimer = Plugin.Cfg.DebuffSeconds.Value;
                }
                else
                {
                    _wound?.Stop();
                    SetState(WeroState.Normal);
                }
            }
        }

        private void TickStruggle()
        {
            // Soften whole-body drive so Euphoria limb motors can win against joints.
            float drive = Plugin.Cfg.StruggleDriveMul.Value;
            if (Plugin.Cfg.EuphoriaEnabled.Value)
                drive *= Plugin.Cfg.EuphoriaDriveMul.Value;
            Driver.ApplyLiving(Actor, drive);
            _wound?.SetBrace(true);

            if (Actor.dead || _pendingLethal)
            {
                EnterDeath(DamageInfo.Default, _lastZone, LodService.BandFor(Actor));
                return;
            }

            // Stay down for the full struggle budget first.
            if (_stateTimer > 0f)
            {
                _settledTimer = 0f;
                return;
            }

            if (!Plugin.Cfg.AutoGetUp.Value)
            {
                SetState(WeroState.WoundDebuff);
                _stateTimer = Plugin.Cfg.DebuffSeconds.Value;
                ReleaseStruggleSlot();
                return;
            }

            bool slow = true;
            try
            {
                if (Actor.ragdoll != null && Actor.ragdoll.IsRagdoll())
                    slow = Actor.ragdoll.Velocity().sqrMagnitude < 1.35f;
            }
            catch { /* ignore */ }

            if (slow)
                _settledTimer += Time.deltaTime;
            else
                _settledTimer = 0f;

            if (_settledTimer < Plugin.Cfg.MinGroundedBeforeGetUp.Value)
                return;

            BeginGetUp();
        }

        private void BeginGetUp()
        {
            SetState(WeroState.GetUp);
            _stateTimer = Plugin.Cfg.GetUpSeconds.Value;
            _getUpElapsed = 0f;
            _getUpStarted = false;
            _heightSyncTimer = 2.5f;
            _wound?.Stop();
            ActorGetUp.StartAnimated(Actor);
            _getUpStarted = true;
            ReleaseStruggleSlot();
        }

        private void TickGetUp()
        {
            if (Actor.dead || _pendingLethal)
            {
                EnterDeath(DamageInfo.Default, _lastZone, LodService.BandFor(Actor));
                return;
            }

            _getUpElapsed += Time.deltaTime;

            if (!_getUpStarted)
            {
                ActorGetUp.StartAnimated(Actor);
                _getUpStarted = true;
            }

            // Instant path finishes in the same call — settle grounding for a short beat.
            ActorGetUp.TickGrounding(Actor);

            if (ActorGetUp.IsFullyUpright(Actor) && _getUpElapsed >= 0.15f)
            {
                FinishGetUpSuccess();
                return;
            }

            if (_getUpElapsed >= Plugin.Cfg.GetUpFailsafeSeconds.Value)
            {
                ActorGetUp.FailsafeInstant(Actor);
                FinishGetUpSuccess();
            }
        }

        private void FinishGetUpSuccess()
        {
            ActorGetUp.SoftFinish(Actor);
            ActorGetUp.TickGrounding(Actor);
            _heightSyncTimer = 3f;
            _wound?.Stop();
            SetState(WeroState.WoundDebuff);
            _stateTimer = Plugin.Cfg.DebuffSeconds.Value;
        }

        private void StartWound(DamageInfo info, HitZone zone, bool brace)
        {
            _wound = WoundAssist.Ensure(Actor);
            Vector3 point = ImpulseBuilder.PreferHitPoint(Actor, info, zone);
            _wound?.BeginWound(zone, point, brace);
        }

        private void Knock(DamageInfo info, HitZone zone, float scale)
        {
            Vector3 force = ImpulseBuilder.BuildForce(info, zone, scale);
            _lastForceDir = force.sqrMagnitude > 0.001f ? force.normalized : info.direction;
            Vector3 point = ImpulseBuilder.PreferHitPoint(Actor, info, zone);

            try
            {
                if (Actor.ragdoll != null && Actor.ragdoll.IsRagdoll())
                {
                    // Apply at hit point for rotational realism when possible.
                    Transform hips = Actor.ragdoll.HumanBoneTransformRagdoll(HumanBodyBones.Hips);
                    Rigidbody rb = hips != null ? hips.GetComponent<Rigidbody>() : null;
                    if (rb != null)
                        rb.AddForceAtPosition(force, point, ForceMode.Impulse);
                    else
                        Actor.ApplyRigidbodyForce(force);
                }
                else
                {
                    Actor.KnockOver(force);
                }
            }
            catch
            {
                // Vanilla may refuse knock in some states.
            }
        }

        private void SetState(WeroState next)
        {
            if (State == next)
                return;
            State = next;
        }

        private void ReleaseStruggleSlot()
        {
            if (!_struggleSlotHeld)
                return;
            LodService.ReleaseStruggle(Actor);
            _struggleSlotHeld = false;
        }
    }
}
