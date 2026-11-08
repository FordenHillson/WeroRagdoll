using UnityEngine;

namespace WeroRagdoll.Core
{
    /// <summary>
    /// Per-zone muscle strength 0..1 with loose/gain lerp (W.E.R.O-inspired).
    /// Extensible later via a richer IMuscleDriver for NaturalMotion-like phase.
    /// </summary>
    public sealed class MuscleModel
    {
        public float Head = 1f;
        public float Torso = 1f;
        public float ArmL = 1f;
        public float ArmR = 1f;
        public float LegL = 1f;
        public float LegR = 1f;

        public float CoreBalance => (Torso * 0.35f) + (LegL * 0.325f) + (LegR * 0.325f);

        public float Get(HitZone zone)
        {
            switch (zone)
            {
                case HitZone.Head: return Head;
                case HitZone.Torso: return Torso;
                case HitZone.ArmL: return ArmL;
                case HitZone.ArmR: return ArmR;
                case HitZone.LegL: return LegL;
                case HitZone.LegR: return LegR;
                default: return Torso;
            }
        }

        public void Set(HitZone zone, float value)
        {
            value = Mathf.Clamp01(value);
            switch (zone)
            {
                case HitZone.Head: Head = value; break;
                case HitZone.Torso: Torso = value; break;
                case HitZone.ArmL: ArmL = value; break;
                case HitZone.ArmR: ArmR = value; break;
                case HitZone.LegL: LegL = value; break;
                case HitZone.LegR: LegR = value; break;
                default: Torso = value; break;
            }
        }

        public void ApplyImpact(HitZone zone, float impactNormalized)
        {
            float loose = Plugin.Cfg.LooseStrengthRate.Value * Mathf.Clamp01(impactNormalized);
            // Local limb takes the hit; neighbors soften a bit.
            Soften(zone, loose);
            Soften(HitZone.Torso, loose * 0.25f);
            if (zone == HitZone.LegL || zone == HitZone.LegR)
                Soften(zone == HitZone.LegL ? HitZone.LegR : HitZone.LegL, loose * 0.15f);
            if (zone == HitZone.ArmL || zone == HitZone.ArmR)
                Soften(HitZone.Torso, loose * 0.1f);
            if (zone == HitZone.Head)
                Soften(HitZone.Torso, loose * 0.35f);
        }

        public void TickRecover(float dt, float recoverTargetMul = 1f)
        {
            float gain = Plugin.Cfg.GainStrengthRate.Value * dt;
            Head = MoveToward(Head, 1f * recoverTargetMul, gain);
            Torso = MoveToward(Torso, 1f * recoverTargetMul, gain);
            ArmL = MoveToward(ArmL, 1f * recoverTargetMul, gain * 0.9f);
            ArmR = MoveToward(ArmR, 1f * recoverTargetMul, gain * 0.9f);
            LegL = MoveToward(LegL, 1f * recoverTargetMul, gain * 0.85f);
            LegR = MoveToward(LegR, 1f * recoverTargetMul, gain * 0.85f);
        }

        public void CollapseAll(float amount = 1f)
        {
            Soften(HitZone.Head, amount);
            Soften(HitZone.Torso, amount);
            Soften(HitZone.ArmL, amount);
            Soften(HitZone.ArmR, amount);
            Soften(HitZone.LegL, amount);
            Soften(HitZone.LegR, amount);
        }

        public bool ArmDisabled(bool right)
        {
            return (right ? ArmR : ArmL) < 0.22f;
        }

        public bool LegDisabled(bool right)
        {
            return (right ? LegR : LegL) < 0.22f;
        }

        private void Soften(HitZone zone, float amount)
        {
            Set(zone, Get(zone) - amount);
        }

        private static float MoveToward(float current, float target, float maxDelta)
        {
            if (current < target)
                return Mathf.Min(current + maxDelta, target);
            return Mathf.Max(current - maxDelta, target);
        }
    }
}
