using UnityEngine;

namespace WeroRagdoll.Core.Euphoria.Behaviors
{
    /// <summary>
    /// When balance tips, nudge the downhill foot/hip opposite to fall direction (Euphoria-ish step).
    /// </summary>
    public sealed class CompensatoryStepBehavior : IEuphoriaBehavior
    {
        public string Name => "CompensatoryStep";
        private float _cooldown;

        public bool Wants(EuphoriaContext ctx)
        {
            if (!Plugin.Cfg.EuphoriaEnabled.Value)
                return false;
            if (ctx.State != WeroState.ActiveStruggle && ctx.State != WeroState.Stagger)
                return false;
            if (ctx.Actor == null || ctx.Driver == null)
                return false;
            return ctx.Driver.Muscle.CoreBalance < Plugin.Cfg.CoreBalanceSoftFloor.Value;
        }

        public void Enter(EuphoriaContext ctx)
        {
            _cooldown = 0f;
        }

        public void Tick(EuphoriaContext ctx, float dt)
        {
            _cooldown -= dt;
            if (_cooldown > 0f)
                return;
            _cooldown = Plugin.Cfg.EuphoriaStepInterval.Value;

            Rigidbody hips = ctx.HipBody();
            if (hips == null)
                return;

            Vector3 fallDir = hips.velocity;
            fallDir.y = 0f;
            if (fallDir.sqrMagnitude < 0.05f)
                fallDir = -ctx.ForceDir;
            fallDir.y = 0f;
            if (fallDir.sqrMagnitude < 0.001f)
                return;
            fallDir.Normalize();

            // Step opposite to fall.
            Vector3 step = -fallDir * Plugin.Cfg.EuphoriaStepForce.Value;
            bool leftLead = Vector3.Dot(fallDir, hips.transform.right) > 0f;
            Rigidbody foot = ctx.Body(leftLead ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot)
                             ?? ctx.Body(leftLead ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
            if (foot != null)
                foot.AddForce(step + Vector3.up * (Plugin.Cfg.EuphoriaStepForce.Value * 0.15f), ForceMode.Impulse);
            hips.AddForce(step * 0.35f, ForceMode.Impulse);
        }

        public void Exit(EuphoriaContext ctx) { }
    }
}
