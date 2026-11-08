using HarmonyLib;
using UnityEngine;

namespace WeroRagdoll.Core.Euphoria
{
    /// <summary>
    /// Softens selected limb drives enough for readable reactions, then applies
    /// clamped springs/impulses. Caps are intentional — uncapped VelocityChange
    /// stretches ConfigurableJoints into rubber-band limbs.
    /// </summary>
    public static class LimbMotor
    {
        private const float MaxImpulseDv = 3.2f;
        private const float MaxSpringAccel = 26f;
        private const float MaxReach = 0.7f;

        private static readonly AccessTools.FieldRef<ActiveRaggy, ConfigurableJoint[]> JointsRef =
            AccessTools.FieldRefAccess<ActiveRaggy, ConfigurableJoint[]>("joints");

        public static void LoosenForBehavior(Actor actor, HitZone focus, float armMul, float legMul, float torsoMul)
        {
            if (actor == null || actor.ragdoll == null)
                return;

            ConfigurableJoint[] joints;
            try { joints = JointsRef(actor.ragdoll); }
            catch { return; }
            if (joints == null)
                return;

            float baseSpring = Plugin.Cfg.MuscleSpring.Value * Plugin.Cfg.EuphoriaDriveMul.Value;
            float baseDamp = Plugin.Cfg.MuscleDamping.Value * 0.85f;

            for (int i = 0; i < joints.Length; i++)
            {
                ConfigurableJoint j = joints[i];
                if (j == null)
                    continue;
                string n = j.name.ToLowerInvariant();
                float mul = torsoMul;
                if (n.Contains("head") || n.Contains("neck"))
                    mul = focus == HitZone.Head ? 0.35f : 0.55f;
                else if (n.Contains("left") && (n.Contains("arm") || n.Contains("hand") || n.Contains("shoulder")))
                    mul = armMul;
                else if (n.Contains("right") && (n.Contains("arm") || n.Contains("hand") || n.Contains("shoulder")))
                    mul = armMul;
                else if (n.Contains("left") && (n.Contains("leg") || n.Contains("upleg") || n.Contains("thigh") || n.Contains("foot")))
                    mul = legMul;
                else if (n.Contains("right") && (n.Contains("leg") || n.Contains("upleg") || n.Contains("thigh") || n.Contains("foot")))
                    mul = legMul;
                else if (n.Contains("spine") || n.Contains("chest") || n.Contains("hip") || n.Contains("pelvis"))
                    mul = torsoMul;

                // Soften focus zone a little, but never crush cohesion (stretch).
                if (focus == HitZone.ArmL && n.Contains("left") && (n.Contains("arm") || n.Contains("hand") || n.Contains("shoulder")))
                    mul *= 0.7f;
                if (focus == HitZone.ArmR && n.Contains("right") && (n.Contains("arm") || n.Contains("hand") || n.Contains("shoulder")))
                    mul *= 0.7f;
                if (focus == HitZone.LegL && n.Contains("left") && (n.Contains("leg") || n.Contains("upleg") || n.Contains("thigh") || n.Contains("foot")))
                    mul *= 0.7f;
                if (focus == HitZone.LegR && n.Contains("right") && (n.Contains("leg") || n.Contains("upleg") || n.Contains("thigh") || n.Contains("foot")))
                    mul *= 0.7f;

                mul = Mathf.Clamp(mul, 0.28f, 1.2f);

                var drive = j.slerpDrive;
                drive.positionSpring = baseSpring * mul;
                drive.positionDamper = baseDamp * Mathf.Max(0.35f, mul);
                drive.maximumForce = 4500f;
                j.slerpDrive = drive;
                j.angularXDrive = drive;
                j.angularYZDrive = drive;
            }
        }

        public static void SpringBody(Rigidbody body, Vector3 target, float spring, float maxAccel)
        {
            if (body == null)
                return;

            Vector3 delta = target - body.worldCenterOfMass;
            float dist = delta.magnitude;
            if (dist < 0.02f)
                return;

            // Local reach only — long pulls tear limbs apart when joints are soft.
            if (dist > MaxReach)
                delta = delta * (MaxReach / dist);

            float useSpring = Mathf.Min(spring, 18f);
            Vector3 force = delta * useSpring - body.velocity * (useSpring * 0.18f);
            float cap = Mathf.Min(maxAccel, MaxSpringAccel);
            body.AddForce(Vector3.ClampMagnitude(force, cap), ForceMode.Acceleration);
        }

        public static void ImpulseBody(Rigidbody body, Vector3 impulse)
        {
            if (body == null)
                return;

            impulse = Vector3.ClampMagnitude(impulse, MaxImpulseDv);
            impulse.y = Mathf.Clamp(impulse.y, -2.2f, 2.8f);
            body.AddForce(impulse, ForceMode.VelocityChange);

            // Kill leftover runaway velocity that stretches connected joints.
            if (body.velocity.sqrMagnitude > 36f)
                body.velocity = Vector3.ClampMagnitude(body.velocity, 6f);
        }

        public static bool IsRagdolled(Actor actor)
        {
            try { return actor != null && actor.ragdoll != null && actor.ragdoll.IsRagdoll(); }
            catch { return false; }
        }
    }
}
