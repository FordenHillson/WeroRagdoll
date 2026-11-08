using HarmonyLib;
using UnityEngine;

namespace WeroRagdoll.Core
{
    public static class DriveApplier
    {
        private static readonly AccessTools.FieldRef<ActiveRaggy, ConfigurableJoint[]> JointsRef =
            AccessTools.FieldRefAccess<ActiveRaggy, ConfigurableJoint[]>("joints");

        public static void ApplyLiving(Actor actor, MuscleModel muscle, float globalMul = 1f)
        {
            if (actor == null || actor.ragdoll == null || muscle == null)
                return;

            float spring = Plugin.Cfg.MuscleSpring.Value * globalMul;
            float damp = Plugin.Cfg.MuscleDamping.Value * globalMul;
            float core = Mathf.Lerp(0.25f, 1f, muscle.CoreBalance);
            actor.ragdoll.SetDrive(spring * core, damp * core);
            ScalePerJoint(actor.ragdoll, muscle, spring, damp);
        }

        public static void ApplyLimp(Actor actor)
        {
            if (actor == null || actor.ragdoll == null)
                return;
            actor.ragdoll.SetDrive(Plugin.Cfg.LimpSpring.Value, Plugin.Cfg.LimpDamping.Value);
        }

        public static void ApplyGetUp(Actor actor, MuscleModel muscle)
        {
            if (actor == null || actor.ragdoll == null || muscle == null)
                return;
            float spring = Plugin.Cfg.MuscleSpring.Value * 0.65f;
            float damp = Plugin.Cfg.MuscleDamping.Value * 0.9f;
            actor.ragdoll.SetDrive(spring * Mathf.Max(0.4f, muscle.CoreBalance), damp);
        }

        private static void ScalePerJoint(ActiveRaggy ragdoll, MuscleModel muscle, float spring, float damp)
        {
            ConfigurableJoint[] joints;
            try
            {
                joints = JointsRef(ragdoll);
            }
            catch
            {
                return;
            }

            if (joints == null)
                return;

            for (int i = 0; i < joints.Length; i++)
            {
                ConfigurableJoint joint = joints[i];
                if (joint == null)
                    continue;

                float zoneMul = ZoneMulForJoint(joint, muscle);
                var drive = joint.slerpDrive;
                drive.positionSpring = spring * zoneMul;
                drive.positionDamper = damp * zoneMul;
                drive.maximumForce = 5000f;
                joint.slerpDrive = drive;
                joint.angularXDrive = drive;
                joint.angularYZDrive = drive;
            }
        }

        private static float ZoneMulForJoint(ConfigurableJoint joint, MuscleModel muscle)
        {
            string n = joint.name.ToLowerInvariant();
            if (n.Contains("head") || n.Contains("neck"))
                return Mathf.Lerp(0.15f, 1f, muscle.Head);
            if (n.Contains("left") && (n.Contains("arm") || n.Contains("hand") || n.Contains("shoulder")))
                return Mathf.Lerp(0.1f, 1f, muscle.ArmL);
            if (n.Contains("right") && (n.Contains("arm") || n.Contains("hand") || n.Contains("shoulder")))
                return Mathf.Lerp(0.1f, 1f, muscle.ArmR);
            if (n.Contains("left") && (n.Contains("leg") || n.Contains("upleg") || n.Contains("thigh") || n.Contains("foot")))
                return Mathf.Lerp(0.08f, 1f, muscle.LegL);
            if (n.Contains("right") && (n.Contains("leg") || n.Contains("upleg") || n.Contains("thigh") || n.Contains("foot")))
                return Mathf.Lerp(0.08f, 1f, muscle.LegR);
            if (n.Contains("spine") || n.Contains("chest") || n.Contains("hip") || n.Contains("pelvis"))
                return Mathf.Lerp(0.2f, 1f, muscle.Torso);
            return Mathf.Lerp(0.25f, 1f, muscle.CoreBalance);
        }
    }
}
