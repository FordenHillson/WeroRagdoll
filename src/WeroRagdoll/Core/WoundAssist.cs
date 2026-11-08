using HarmonyLib;
using UnityEngine;

namespace WeroRagdoll.Core
{
    /// <summary>
    /// Phase 2: wound clutch + ground brace.
    /// Animated: ActorIk hand targets. Ragdolled: spring forces on hand rigidbodies.
    /// </summary>
    public sealed class WoundAssist : MonoBehaviour
    {
        private static readonly AccessTools.FieldRef<Actor, ActorIk> IkRef =
            AccessTools.FieldRefAccess<Actor, ActorIk>("ik");

        private Actor _actor;
        private bool _active;
        private bool _brace;
        private HitZone _zone = HitZone.Torso;
        private Vector3 _woundWorld;
        private float _handSpring;
        private Transform _woundMarker;
        private Transform _braceMarker;
        private ActorIk _cachedIk;
        private bool _ikOwned;

        public bool IsActive => _active;

        public static WoundAssist Ensure(Actor actor)
        {
            if (actor == null)
                return null;
            var assist = actor.GetComponent<WoundAssist>();
            if (assist == null)
                assist = actor.gameObject.AddComponent<WoundAssist>();
            assist._actor = actor;
            return assist;
        }

        public void BeginWound(HitZone zone, Vector3 woundPoint, bool brace)
        {
            if (!Plugin.Cfg.WoundAssistEnabled.Value)
                return;

            if (_actor == null)
                _actor = GetComponent<Actor>() ?? GetComponentInParent<Actor>();

            _zone = zone;
            _woundWorld = woundPoint;
            _brace = brace;
            _handSpring = Plugin.Cfg.WoundHandSpring.Value;
            _active = true;
            EnsureMarkers();
            PlaceWoundMarker();
            TryStartAnimatedIk();
        }

        public void SetBrace(bool brace)
        {
            _brace = brace;
        }

        public void Stop()
        {
            _active = false;
            RestoreIk();
        }

        private void OnDestroy()
        {
            RestoreIk();
            if (_woundMarker != null)
                Destroy(_woundMarker.gameObject);
            if (_braceMarker != null)
                Destroy(_braceMarker.gameObject);
        }

        private void FixedUpdate()
        {
            if (!_active || _actor == null || !Plugin.Cfg.WoundAssistEnabled.Value)
                return;

            PlaceWoundMarker();
            if (_brace)
                PlaceBraceMarker();

            bool ragdolled = _actor.ragdoll != null && _actor.ragdoll.IsRagdoll();
            if (ragdolled)
                SpringHandsPhysics();
            else
                ReassertAnimatedIk();
        }

        private void EnsureMarkers()
        {
            if (_woundMarker == null)
            {
                var go = new GameObject("WeroWoundTarget");
                _woundMarker = go.transform;
            }
            if (_braceMarker == null)
            {
                var go = new GameObject("WeroBraceTarget");
                _braceMarker = go.transform;
            }
        }

        private void PlaceWoundMarker()
        {
            if (_woundMarker == null)
                return;
            // Slightly stick to zone bone if available so clutch follows body.
            Vector3 p = _woundWorld;
            try
            {
                if (_actor.ragdoll != null)
                {
                    HumanBodyBones bone = HumanBodyBones.Chest;
                    switch (_zone)
                    {
                        case HitZone.Head: bone = HumanBodyBones.Head; break;
                        case HitZone.ArmL: bone = HumanBodyBones.LeftUpperArm; break;
                        case HitZone.ArmR: bone = HumanBodyBones.RightUpperArm; break;
                        case HitZone.LegL: bone = HumanBodyBones.LeftUpperLeg; break;
                        case HitZone.LegR: bone = HumanBodyBones.RightUpperLeg; break;
                    }
                    Transform t = _actor.ragdoll.HumanBoneTransformActive(bone);
                    if (t != null)
                        p = Vector3.Lerp(t.position, _woundWorld, 0.35f);
                }
            }
            catch { /* ignore */ }

            _woundMarker.position = p;
        }

        private void PlaceBraceMarker()
        {
            if (_braceMarker == null || _actor == null)
                return;

            Vector3 origin = _actor.CenterPosition() + Vector3.up * 0.2f;
            Vector3 target = origin + Vector3.down * 1.4f;
            // Bias away from a disabled/wounded leg.
            if (_zone == HitZone.LegL)
                target += Vector3.right * 0.25f;
            else if (_zone == HitZone.LegR)
                target += Vector3.left * 0.25f;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                _braceMarker.position = hit.point + hit.normal * 0.04f;
            else
                _braceMarker.position = target;
        }

        private void SpringHandsPhysics()
        {
            Rigidbody handL = HandBody(HumanBodyBones.LeftHand);
            Rigidbody handR = HandBody(HumanBodyBones.RightHand);
            if (handL == null && handR == null)
                return;

            // Primary clutch hand opposite to wounded arm when possible.
            bool preferRight = _zone != HitZone.ArmR;
            Rigidbody clutch = preferRight ? handR : handL;
            Rigidbody other = preferRight ? handL : handR;
            if (clutch == null)
                clutch = other;

            float spring = _handSpring * Plugin.Cfg.WoundAssistStrength.Value;
            if (clutch != null)
                SpringToward(clutch, _woundMarker.position, spring);

            if (_brace && other != null && _braceMarker != null)
                SpringToward(other, _braceMarker.position, spring * 0.85f);
            else if (_brace && clutch != null && other == null && _braceMarker != null)
                SpringToward(clutch, Vector3.Lerp(_woundMarker.position, _braceMarker.position, 0.4f), spring * 0.7f);
        }

        private static void SpringToward(Rigidbody body, Vector3 target, float spring)
        {
            if (body == null)
                return;
            Vector3 delta = target - body.worldCenterOfMass;
            Vector3 force = delta * spring - body.velocity * (spring * 0.08f);
            force = Vector3.ClampMagnitude(force, spring * 1.8f);
            body.AddForce(force, ForceMode.Acceleration);
        }

        private Rigidbody HandBody(HumanBodyBones bone)
        {
            if (_actor == null || _actor.ragdoll == null)
                return null;
            try
            {
                Transform t = _actor.ragdoll.HumanBoneTransformRagdoll(bone);
                return t != null ? t.GetComponent<Rigidbody>() : null;
            }
            catch
            {
                return null;
            }
        }

        private void TryStartAnimatedIk()
        {
            _cachedIk = SafeIk();
            if (_cachedIk == null || _woundMarker == null)
                return;

            bool left = _zone != HitZone.ArmL;
            bool right = _zone != HitZone.ArmR;
            // Always clutch with at least one hand.
            if (!left && !right)
                right = true;

            try
            {
                if (left)
                    _cachedIk.handTargetL = _woundMarker;
                if (right)
                    _cachedIk.handTargetR = _brace ? _braceMarker : _woundMarker;
                _cachedIk.weight = Mathf.Clamp01(Plugin.Cfg.WoundIkWeight.Value);
                _cachedIk.SetHandIkEnabled(left, right);
                _ikOwned = true;
            }
            catch
            {
                _ikOwned = false;
            }
        }

        private void ReassertAnimatedIk()
        {
            if (!_ikOwned || _cachedIk == null)
            {
                TryStartAnimatedIk();
                return;
            }

            try
            {
                _cachedIk.weight = Mathf.Clamp01(Plugin.Cfg.WoundIkWeight.Value);
            }
            catch { /* ignore */ }
        }

        private void RestoreIk()
        {
            if (!_ikOwned || _cachedIk == null)
                return;
            try
            {
                _cachedIk.SetHandIkEnabled(false, false);
                _cachedIk.weight = 0f;
            }
            catch { /* ignore */ }
            _ikOwned = false;
        }

        private ActorIk SafeIk()
        {
            if (_actor == null)
                return null;
            try
            {
                return IkRef(_actor);
            }
            catch
            {
                return null;
            }
        }
    }
}
