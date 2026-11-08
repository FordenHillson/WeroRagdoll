using UnityEngine;

namespace WeroRagdoll.Core
{
    public static class HitClassifier
    {
        public static HitZone Classify(Actor actor, DamageInfo info)
        {
            if (actor == null)
                return HitZone.Unknown;

            Vector3 point = info.point;
            if (point == Vector3.zero)
                point = actor.CenterPosition();

            Transform head = Bone(actor, HumanBodyBones.Head);
            Transform hips = Bone(actor, HumanBodyBones.Hips);
            Transform chest = Bone(actor, HumanBodyBones.Chest) ?? Bone(actor, HumanBodyBones.Spine);
            Transform armL = Bone(actor, HumanBodyBones.LeftUpperArm) ?? Bone(actor, HumanBodyBones.LeftLowerArm);
            Transform armR = Bone(actor, HumanBodyBones.RightUpperArm) ?? Bone(actor, HumanBodyBones.RightLowerArm);
            Transform legL = Bone(actor, HumanBodyBones.LeftUpperLeg) ?? Bone(actor, HumanBodyBones.LeftLowerLeg);
            Transform legR = Bone(actor, HumanBodyBones.RightUpperLeg) ?? Bone(actor, HumanBodyBones.RightLowerLeg);

            float best = float.MaxValue;
            HitZone zone = HitZone.Torso;

            Consider(ref best, ref zone, HitZone.Head, head, point, 0.85f);
            Consider(ref best, ref zone, HitZone.Torso, chest, point, 1.05f);
            Consider(ref best, ref zone, HitZone.Torso, hips, point, 1.1f);
            Consider(ref best, ref zone, HitZone.ArmL, armL, point, 0.95f);
            Consider(ref best, ref zone, HitZone.ArmR, armR, point, 0.95f);
            Consider(ref best, ref zone, HitZone.LegL, legL, point, 1f);
            Consider(ref best, ref zone, HitZone.LegR, legR, point, 1f);

            if (info.isCriticalHit && zone != HitZone.Head)
            {
                // Crits often mean head/neck — bias if close enough to head.
                if (head != null && (point - head.position).sqrMagnitude < 0.55f * 0.55f)
                    zone = HitZone.Head;
            }

            return zone;
        }

        public static float ImpactScore(DamageInfo info, HitZone zone)
        {
            float score = info.healthDamage * 0.65f + info.balanceDamage * 0.85f;
            score += info.impactForce.magnitude * 0.02f;
            if (info.isCriticalHit)
                score *= 1.35f;
            if (info.isSplashDamage)
                score *= 1.15f;
            if (zone == HitZone.Head)
                score *= Plugin.Cfg.HeadImpactBias.Value;
            return score;
        }

        public static ReactionTier Tier(float impact, float coreBalance, bool lethal)
        {
            if (lethal)
                return ReactionTier.Lethal;

            var cfg = Plugin.Cfg;
            if (coreBalance <= cfg.CoreBalanceHardCeil.Value || impact >= cfg.HardImpact.Value)
                return ReactionTier.Hard;
            if (impact >= cfg.MediumImpact.Value || coreBalance < cfg.CoreBalanceSoftFloor.Value)
                return ReactionTier.Medium;
            if (impact >= cfg.SoftImpact.Value)
                return ReactionTier.Soft;
            return ReactionTier.None;
        }

        private static void Consider(ref float best, ref HitZone zone, HitZone candidate, Transform bone, Vector3 point, float weight)
        {
            if (bone == null)
                return;
            float d = (point - bone.position).sqrMagnitude * weight * weight;
            if (d < best)
            {
                best = d;
                zone = candidate;
            }
        }

        private static Transform Bone(Actor actor, HumanBodyBones bone)
        {
            if (actor == null || actor.ragdoll == null)
                return null;
            try
            {
                return actor.ragdoll.HumanBoneTransformActive(bone);
            }
            catch
            {
                return null;
            }
        }
    }
}
