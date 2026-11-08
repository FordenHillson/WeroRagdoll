using UnityEngine;

namespace WeroRagdoll.Core.Euphoria.Behaviors
{
    /// <summary>
    /// Cover head with short-reach arm springs (no long yank that stretches limbs).
    /// </summary>
    public sealed class ProtectHeadBehavior : IEuphoriaBehavior
    {
        public string Name => "ProtectHead";
        private WoundAssist _assist;

        public bool Wants(EuphoriaContext ctx)
        {
            if (!Plugin.Cfg.EuphoriaEnabled.Value)
                return false;
            if (ctx.State != WeroState.ActiveStruggle && ctx.State != WeroState.Stagger && ctx.State != WeroState.LimpDeath)
                return false;
            return ctx.LastZone == HitZone.Head || ctx.LastZone == HitZone.Torso || ctx.Lethal;
        }

        public void Enter(EuphoriaContext ctx)
        {
            if (ctx.Actor == null)
                return;

            _assist = WoundAssist.Ensure(ctx.Actor);
            Rigidbody head = ctx.Body(HumanBodyBones.Head);
            Vector3 hp = head != null ? head.worldCenterOfMass : ctx.Actor.CenterPosition() + Vector3.up * 0.7f;
            _assist?.BeginWound(HitZone.Head, hp, brace: false);

            if (!LimbMotor.IsRagdolled(ctx.Actor))
            {
                try { ctx.Actor.ForceApplyAnimatorForTime(0.35f); } catch { /* ignore */ }
            }
        }

        public void Tick(EuphoriaContext ctx, float dt)
        {
            LimbMotor.LoosenForBehavior(ctx.Actor, HitZone.Head, armMul: 0.4f, legMul: 0.65f, torsoMul: 0.55f);

            Rigidbody head = ctx.Body(HumanBodyBones.Head);
            if (head == null)
                return;

            Vector3 target = head.worldCenterOfMass + Vector3.up * 0.04f;
            _assist?.BeginWound(HitZone.Head, target, brace: false);

            float s = Mathf.Min(Plugin.Cfg.EuphoriaProtectHeadForce.Value, 16f);
            LimbMotor.SpringBody(ctx.Body(HumanBodyBones.LeftHand), target, s, s * 1.4f);
            LimbMotor.SpringBody(ctx.Body(HumanBodyBones.RightHand), target, s * 0.95f, s * 1.4f);
            LimbMotor.SpringBody(ctx.Body(HumanBodyBones.LeftLowerArm), target, s * 0.4f, s);
            LimbMotor.SpringBody(ctx.Body(HumanBodyBones.RightLowerArm), target, s * 0.4f, s);
        }

        public void Exit(EuphoriaContext ctx) { }
    }
}
