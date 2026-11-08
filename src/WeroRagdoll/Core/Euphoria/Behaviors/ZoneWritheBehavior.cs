using UnityEngine;

namespace WeroRagdoll.Core.Euphoria.Behaviors
{
    /// <summary>
    /// Zone thrash with small clamped kicks — large VelocityChange stretches limbs.
    /// </summary>
    public sealed class ZoneWritheBehavior : IEuphoriaBehavior
    {
        public string Name => "ZoneWrithe";
        private float _pulse;

        public bool Wants(EuphoriaContext ctx)
        {
            if (!Plugin.Cfg.EuphoriaEnabled.Value)
                return false;
            return ctx.State == WeroState.ActiveStruggle || ctx.State == WeroState.LimpDeath;
        }

        public void Enter(EuphoriaContext ctx)
        {
            _pulse = 0f;
            if (ctx.Driver != null)
                ctx.Driver.OnImpact(ctx.LastZone, 0.55f);
        }

        public void Tick(EuphoriaContext ctx, float dt)
        {
            LimbMotor.LoosenForBehavior(ctx.Actor, ctx.LastZone, armMul: 0.45f, legMul: 0.42f, torsoMul: 0.55f);

            _pulse += dt;
            float interval = Plugin.Cfg.EuphoriaWritheInterval.Value;
            if (_pulse < interval)
                return;
            _pulse = 0f;

            float f = Mathf.Min(Plugin.Cfg.EuphoriaWritheForce.Value, 8f);
            Vector3 side = Vector3.Cross(ctx.ForceDir, Vector3.up);
            if (side.sqrMagnitude < 0.001f)
                side = Vector3.right;
            side.Normalize();

            switch (ctx.LastZone)
            {
                case HitZone.LegL:
                    Kick(ctx, HumanBodyBones.LeftFoot, (side * 0.7f + Vector3.up * 0.25f) * f);
                    Kick(ctx, HumanBodyBones.LeftLowerLeg, (side * 0.45f + Vector3.up * 0.15f) * f * 0.55f);
                    break;
                case HitZone.LegR:
                    Kick(ctx, HumanBodyBones.RightFoot, (-side * 0.7f + Vector3.up * 0.25f) * f);
                    Kick(ctx, HumanBodyBones.RightLowerLeg, (-side * 0.45f + Vector3.up * 0.15f) * f * 0.55f);
                    break;
                case HitZone.ArmL:
                    Kick(ctx, HumanBodyBones.LeftHand, (side * 0.65f + Vector3.up * 0.35f) * f);
                    Kick(ctx, HumanBodyBones.LeftLowerArm, (side * 0.4f + Vector3.up * 0.25f) * f * 0.5f);
                    break;
                case HitZone.ArmR:
                    Kick(ctx, HumanBodyBones.RightHand, (-side * 0.65f + Vector3.up * 0.35f) * f);
                    Kick(ctx, HumanBodyBones.RightLowerArm, (-side * 0.4f + Vector3.up * 0.25f) * f * 0.5f);
                    break;
                case HitZone.Head:
                    Kick(ctx, HumanBodyBones.Head, (ctx.ForceDir * 0.2f + side * Random.Range(-0.35f, 0.35f)) * f * 0.35f);
                    break;
                default:
                    Kick(ctx, HumanBodyBones.Chest, (ctx.ForceDir * 0.25f + Vector3.up * 0.12f + side * Random.Range(-0.3f, 0.3f)) * f * 0.45f);
                    Rigidbody hips = ctx.HipBody();
                    if (hips != null)
                        hips.AddTorque(side * f * Random.Range(-0.25f, 0.25f), ForceMode.VelocityChange);
                    break;
            }
        }

        public void Exit(EuphoriaContext ctx) { }

        private static void Kick(EuphoriaContext ctx, HumanBodyBones bone, Vector3 impulse)
        {
            Rigidbody rb = ctx.Body(bone);
            if (rb == null)
                return;
            LimbMotor.ImpulseBody(rb, impulse);
        }
    }
}
