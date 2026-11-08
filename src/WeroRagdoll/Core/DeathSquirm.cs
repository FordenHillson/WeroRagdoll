using UnityEngine;

namespace WeroRagdoll.Core
{
    /// <summary>
    /// Death: stay loose so the kill impulse reads, then light squirm, then limp.
    /// High spring on death was fighting Impulse realism.
    /// </summary>
    public sealed class DeathSquirm : MonoBehaviour
    {
        private enum Phase
        {
            None,
            Ballistic, // loose body, impulse dominates
            Squirm,
            FadeLimp
        }

        private Actor _actor;
        private MuscleModel _muscle;
        private Phase _phase = Phase.None;
        private float _timer;
        private float _pulse;
        private HitZone _killZone = HitZone.Torso;
        private Vector3 _lastForceDir = Vector3.forward;

        public bool IsRunning => _phase != Phase.None;

        public static DeathSquirm Ensure(Actor actor)
        {
            if (actor == null)
                return null;
            var d = actor.GetComponent<DeathSquirm>();
            if (d == null)
                d = actor.gameObject.AddComponent<DeathSquirm>();
            d._actor = actor;
            return d;
        }

        public void Begin(Actor actor, MuscleModel muscle, HitZone zone, Vector3 forceDir)
        {
            if (!Plugin.Cfg.DeathSquirmEnabled.Value)
            {
                DriveApplier.ApplyLimp(actor);
                return;
            }

            _actor = actor;
            _muscle = muscle;
            _killZone = zone;
            _lastForceDir = forceDir.sqrMagnitude > 0.001f ? forceDir.normalized : Vector3.forward;
            _phase = Phase.Ballistic;
            _timer = Plugin.Cfg.DeathTenseSeconds.Value;
            _pulse = 0f;
            if (_muscle != null)
                _muscle.CollapseAll(0.9f);

            // Stay limp-ish so knockback is visible (no strong living spring).
            DriveApplier.ApplyLimp(_actor);
        }

        public void Stop()
        {
            _phase = Phase.None;
        }

        private void FixedUpdate()
        {
            if (_phase == Phase.None || _actor == null)
                return;

            float dt = Time.fixedDeltaTime;
            _timer -= dt;
            _pulse += dt;

            switch (_phase)
            {
                case Phase.Ballistic:
                    DriveApplier.ApplyLimp(_actor);
                    if (_timer <= 0f)
                    {
                        _phase = Phase.Squirm;
                        _timer = Plugin.Cfg.DeathSquirmSeconds.Value;
                    }
                    break;

                case Phase.Squirm:
                    // Very light residual tension only — do not overpower physics.
                    DriveApplier.ApplyLiving(_actor, _muscle, 0.12f);
                    if (_pulse >= Plugin.Cfg.DeathSquirmPulse.Value)
                    {
                        _pulse = 0f;
                        Pulse();
                    }
                    if (_timer <= 0f)
                    {
                        _phase = Phase.FadeLimp;
                        _timer = Plugin.Cfg.DeathFadeSeconds.Value;
                    }
                    break;

                case Phase.FadeLimp:
                    DriveApplier.ApplyLimp(_actor);
                    if (_timer <= 0f)
                    {
                        DriveApplier.ApplyLimp(_actor);
                        _phase = Phase.None;
                    }
                    break;
            }
        }

        private void Pulse()
        {
            if (_actor == null || _actor.ragdoll == null || !_actor.ragdoll.IsRagdoll())
                return;

            float strength = Plugin.Cfg.DeathSquirmImpulse.Value;
            Vector3 side = Vector3.Cross(_lastForceDir, Vector3.up);
            if (side.sqrMagnitude < 0.001f)
                side = Vector3.right;
            side.Normalize();

            float zoneMul = _killZone == HitZone.Head ? 0.45f : 1f;
            Vector3 force = (_lastForceDir * 0.55f + side * Random.Range(-1f, 1f) + Vector3.up * Random.Range(-0.15f, 0.25f))
                            * strength * zoneMul;
            force.y = Mathf.Clamp(force.y, -18f, 22f);

            try
            {
                Transform hips = _actor.ragdoll.HumanBoneTransformRagdoll(HumanBodyBones.Hips);
                Rigidbody rb = hips != null ? hips.GetComponent<Rigidbody>() : null;
                if (rb != null)
                    rb.AddForce(force, ForceMode.Impulse);
                else
                    _actor.ApplyRigidbodyForce(force);
            }
            catch { /* ignore */ }
        }
    }
}
