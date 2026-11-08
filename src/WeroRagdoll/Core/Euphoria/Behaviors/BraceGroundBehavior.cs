using UnityEngine;

namespace WeroRagdoll.Core.Euphoria.Behaviors
{
    /// <summary>
    /// Plant hands toward ground with short-reach springs (no Impulse yank).
    /// </summary>
    public sealed class BraceGroundBehavior : IEuphoriaBehavior
    {
        public string Name => "BraceGround";
        private Vector3 _bracePoint;
        private WoundAssist _assist;

        public bool Wants(EuphoriaContext ctx)
        {
            if (!Plugin.Cfg.EuphoriaEnabled.Value)
                return false;
            if (ctx.State != WeroState.ActiveStruggle && ctx.State != WeroState.Stagger && ctx.State != WeroState.LimpDeath)
                return false;
            if (ctx.Actor == null)
                return false;
            return ctx.Actor.fallenOver || LimbMotor.IsRagdolled(ctx.Actor);
        }

        public void Enter(EuphoriaContext ctx)
        {
            UpdateBracePoint(ctx);
            _assist = WoundAssist.Ensure(ctx.Actor);
            _assist?.BeginWound(ctx.LastZone, _bracePoint, brace: true);
            _assist?.SetBrace(true);
        }

        public void Tick(EuphoriaContext ctx, float dt)
        {
            LimbMotor.LoosenForBehavior(ctx.Actor, ctx.LastZone, armMul: 0.42f, legMul: 0.6f, torsoMul: 0.6f);
            UpdateBracePoint(ctx);
            _assist?.BeginWound(ctx.LastZone, _bracePoint, brace: true);
            _assist?.SetBrace(true);

            float s = Mathf.Min(Plugin.Cfg.EuphoriaBraceForce.Value, 18f);
            bool preferRight = ctx.LastZone != HitZone.ArmR;
            HumanBodyBones primary = preferRight ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand;
            HumanBodyBones secondary = preferRight ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;

            LimbMotor.SpringBody(ctx.Body(primary), _bracePoint, s, s * 1.3f);
            LimbMotor.SpringBody(ctx.Body(secondary), _bracePoint + Vector3.right * 0.15f, s * 0.65f, s);
            LimbMotor.SpringBody(
                ctx.Body(preferRight ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm),
                _bracePoint + Vector3.up * 0.12f, s * 0.35f, s * 0.9f);
        }

        public void Exit(EuphoriaContext ctx) { }

        private void UpdateBracePoint(EuphoriaContext ctx)
        {
            Vector3 origin = ctx.Actor.CenterPosition() + Vector3.up * 0.2f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                _bracePoint = hit.point + hit.normal * 0.06f;
            else
                _bracePoint = origin + Vector3.down * 1.2f;

            // Keep brace target near the hand so we never pull across the body.
            Rigidbody hand = ctx.Body(HumanBodyBones.RightHand) ?? ctx.Body(HumanBodyBones.LeftHand);
            if (hand != null)
            {
                Vector3 to = _bracePoint - hand.worldCenterOfMass;
                if (to.sqrMagnitude > 0.55f * 0.55f)
                    _bracePoint = hand.worldCenterOfMass + to.normalized * 0.55f;
            }

            if (ctx.LastZone == HitZone.LegL)
                _bracePoint += Vector3.right * 0.12f;
            else if (ctx.LastZone == HitZone.LegR)
                _bracePoint += Vector3.left * 0.12f;
        }
    }
}
