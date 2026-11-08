using UnityEngine;

namespace WeroRagdoll.Core
{
    /// <summary>
    /// Builds hit impulses that feel more ballistic: along shot direction,
    /// zone-biased application point, capped vertical launch.
    /// </summary>
    public static class ImpulseBuilder
    {
        public static Vector3 BuildForce(DamageInfo info, HitZone zone, float scale)
        {
            Vector3 dir = info.direction;
            if (dir.sqrMagnitude < 0.0001f && info.impactForce.sqrMagnitude > 0.0001f)
                dir = info.impactForce.normalized;
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.forward;

            dir.Normalize();

            float mag = info.impactForce.magnitude;
            if (mag < 1f)
                mag = 90f + info.healthDamage * 2.5f + info.balanceDamage * 1.5f;

            // Zone character: head snaps more, legs trip more horizontal, torso pushes mass.
            float zoneMul = 1f;
            float upBias = 0.08f;
            switch (zone)
            {
                case HitZone.Head:
                    zoneMul = 0.75f;
                    upBias = 0.02f;
                    break;
                case HitZone.Torso:
                    zoneMul = 1.05f;
                    upBias = 0.06f;
                    break;
                case HitZone.ArmL:
                case HitZone.ArmR:
                    zoneMul = 0.7f;
                    upBias = 0.04f;
                    break;
                case HitZone.LegL:
                case HitZone.LegR:
                    zoneMul = 0.85f;
                    upBias = -0.02f; // trip down/forward feel
                    break;
            }

            if (info.isCriticalHit)
                zoneMul *= 1.2f;
            if (info.isSplashDamage)
            {
                zoneMul *= 1.35f;
                upBias += 0.12f;
            }

            Vector3 force = (dir + Vector3.up * upBias).normalized * mag * zoneMul;
            force *= Plugin.Cfg.ImpulseScale.Value * scale;

            float maxUp = Plugin.Cfg.MaxUpwardImpulse.Value;
            if (force.y > maxUp)
                force.y = maxUp;
            if (force.y < -maxUp)
                force.y = -maxUp;

            float maxHoriz = Plugin.Cfg.MaxHorizontalImpulse.Value;
            Vector3 flat = new Vector3(force.x, 0f, force.z);
            if (flat.magnitude > maxHoriz)
            {
                flat = flat.normalized * maxHoriz;
                force.x = flat.x;
                force.z = flat.z;
            }

            return force;
        }

        public static Vector3 PreferHitPoint(Actor actor, DamageInfo info, HitZone zone)
        {
            if (info.point.sqrMagnitude > 0.001f)
                return info.point;

            if (actor == null || actor.ragdoll == null)
                return actor != null ? actor.CenterPosition() : Vector3.zero;

            HumanBodyBones bone = HumanBodyBones.Chest;
            switch (zone)
            {
                case HitZone.Head: bone = HumanBodyBones.Head; break;
                case HitZone.ArmL: bone = HumanBodyBones.LeftUpperArm; break;
                case HitZone.ArmR: bone = HumanBodyBones.RightUpperArm; break;
                case HitZone.LegL: bone = HumanBodyBones.LeftUpperLeg; break;
                case HitZone.LegR: bone = HumanBodyBones.RightUpperLeg; break;
                default: bone = HumanBodyBones.Chest; break;
            }

            try
            {
                Transform t = actor.ragdoll.HumanBoneTransformActive(bone);
                if (t != null)
                    return t.position;
            }
            catch { /* ignore */ }

            return actor.CenterPosition();
        }
    }
}
