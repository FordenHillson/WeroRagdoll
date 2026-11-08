using UnityEngine;

namespace WeroRagdoll.Core
{
    /// <summary>
    /// Applies synthetic hits to the nearest AI bot for Test UI buttons.
    /// </summary>
    public static class TestHarness
    {
        public static string LastStatus { get; set; } = "Ready";

        /// <summary>
        /// Keep AI bots alive while testing body-part / Euphoria reactions.
        /// Lethal Test Panel buttons still bypass this for one hit.
        /// </summary>
        public static bool ImmortalBots = true;

        private static int _allowLethalFrames;

        public static bool AllowLethalThisFrame => _allowLethalFrames > 0;

        public static void Tick()
        {
            if (_allowLethalFrames > 0)
                _allowLethalFrames--;

            if (!ImmortalBots)
                return;

            try
            {
                var list = ActorManager.instance != null ? ActorManager.instance.actors : null;
                if (list == null)
                    return;

                for (int i = 0; i < list.Count; i++)
                {
                    Actor a = list[i];
                    if (a == null || !a.aiControlled || a.dead)
                        continue;
                    if (a.health < a.maxHealth)
                        a.SetHealth(a.maxHealth);
                    if (a.balance < a.maxBalance * 0.35f)
                        a.balance = a.maxBalance;
                }
            }
            catch { /* ignore */ }
        }

        /// <summary>Called from Die prefix — restore and cancel death while immortal.</summary>
        public static bool ShouldBlockDeath(Actor actor)
        {
            if (!ImmortalBots || actor == null || !actor.aiControlled)
                return false;
            if (_allowLethalFrames > 0)
                return false;

            try
            {
                if (!actor.dead)
                    actor.SetHealth(actor.maxHealth);
            }
            catch { /* ignore */ }
            return true;
        }

        public static Actor FindTargetBot()
        {
            Actor player = null;
            try { player = ActorManager.instance != null ? ActorManager.instance.player : null; } catch { /* ignore */ }
            if (player == null)
            {
                try { player = LocalPlayer.actor; } catch { /* ignore */ }
            }

            Vector3 origin = player != null ? player.Position() : (Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            Actor best = null;
            float bestDist = float.MaxValue;

            try
            {
                var list = ActorManager.instance != null ? ActorManager.instance.actors : null;
                if (list == null)
                    return null;

                for (int i = 0; i < list.Count; i++)
                {
                    Actor a = list[i];
                    if (a == null || !a.aiControlled || a.dead)
                        continue;
                    float d = (a.Position() - origin).sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = a;
                    }
                }
            }
            catch { /* ignore */ }

            return best;
        }

        public static void EnsureTestRangeOn()
        {
            if (!TestRange.Active)
                TestRange.SetActive(true);
        }

        public static bool ApplyHit(HitZone zone, float healthDamage, float balanceDamage, float impulseMag, bool critical, bool lethal)
        {
            EnsureTestRangeOn();
            Actor bot = FindTargetBot();
            if (bot == null)
            {
                LastStatus = "No living AI bot found";
                return false;
            }

            Vector3 point = ZonePoint(bot, zone);
            Vector3 from = Camera.main != null ? Camera.main.transform.position : bot.Position() + bot.transform.forward * -2f;
            Vector3 dir = (point - from);
            if (dir.sqrMagnitude < 0.001f)
                dir = bot.transform.forward;
            dir.Normalize();

            float appliedHp = healthDamage;
            if (lethal)
            {
                appliedHp = Mathf.Max(healthDamage, bot.health + 50f);
                // Bypass immortal for this Die call so death-squirm tests still work.
                _allowLethalFrames = 3;
            }
            else if (ImmortalBots)
            {
                // Keep reaction scoring (balance/impulse) but never drain HP to death.
                appliedHp = Mathf.Min(healthDamage, Mathf.Max(0f, bot.health - 5f));
            }

            DamageInfo info = DamageInfo.DefaultDamage(appliedHp);
            info.type = DamageInfo.DamageSourceType.Projectile;
            info.healthDamage = appliedHp;
            info.balanceDamage = balanceDamage;
            info.point = point;
            info.direction = dir;
            info.impactForce = dir * impulseMag;
            info.isCriticalHit = critical || zone == HitZone.Head;
            info.isSplashDamage = false;
            info.isPiercing = false;
            try { info.sourceActor = ActorManager.instance != null ? ActorManager.instance.player : null; } catch { /* ignore */ }

            bool ok;
            try
            {
                ok = bot.Damage(info);
            }
            catch (System.Exception ex)
            {
                // Fallback: drive our brain directly if Damage throws.
                var brain = WeroActorBrain.Ensure(bot);
                brain?.HandleDamage(info, bot.dead);
                LastStatus = bot.name + " / " + zone + " (direct) — " + ex.Message;
                return brain != null;
            }

            if (ImmortalBots && !lethal && bot != null && !bot.dead)
            {
                try
                {
                    bot.SetHealth(bot.maxHealth);
                }
                catch { /* ignore */ }
            }

            LastStatus = bot.name + "  zone=" + zone + "  dmg=" + info.healthDamage.ToString("0") +
                         "  bal=" + info.balanceDamage.ToString("0") + "  ok=" + ok +
                         (lethal ? "  LETHAL" : "") + (ImmortalBots && !lethal ? "  immortal" : "");
            return true;
        }

        public static void RespawnHealNearest()
        {
            Actor bot = FindTargetBot();
            if (bot == null)
            {
                LastStatus = "No bot to heal";
                return;
            }
            try
            {
                if (!bot.dead)
                {
                    bot.SetHealth(bot.maxHealth);
                    bot.balance = bot.maxBalance;
                    LastStatus = "Healed " + bot.name;
                }
                else
                {
                    LastStatus = bot.name + " is dead — spawn a new bot / restart round";
                }
            }
            catch (System.Exception ex)
            {
                LastStatus = "Heal failed: " + ex.Message;
            }
        }

        private static Vector3 ZonePoint(Actor actor, HitZone zone)
        {
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
                if (actor.ragdoll != null)
                {
                    Transform t = actor.ragdoll.HumanBoneTransformActive(bone);
                    if (t != null)
                        return t.position;
                }
            }
            catch { /* ignore */ }

            return actor.CenterPosition();
        }
    }
}
