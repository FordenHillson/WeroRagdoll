using System;
using HarmonyLib;
using UnityEngine;

namespace WeroRagdoll.Core
{
    /// <summary>
    /// Ravenfield gotcha: Actor.Position() returns cachedPosition (from movementPosition /
    /// ragdoll hips), NOT transform.position. LateUpdate builds animatedSoldierHeightOffset
    /// from Position().y deltas — that is the float-then-settle look.
    ///
    /// Reliable recovery: skip Animate()/GetUp interpolate. Plant with transform+movementPosition+
    /// cachedPosition synced, then InstantGetUp once, then keep forcing flat visuals.
    /// </summary>
    internal static class ActorGetUp
    {
        private static readonly AccessTools.FieldRef<Actor, float> HeightOffsetRef =
            AccessTools.FieldRefAccess<Actor, float>("animatedSoldierHeightOffset");

        private static readonly AccessTools.FieldRef<Actor, float> LastSoldierHeightRef =
            AccessTools.FieldRefAccess<Actor, float>("lastSoldierHeight");

        private static readonly AccessTools.FieldRef<Actor, Vector3> ParentOffsetRef =
            AccessTools.FieldRefAccess<Actor, Vector3>("parentOffset");

        private static readonly AccessTools.FieldRef<Actor, Transform> AnimatedParentRef =
            AccessTools.FieldRefAccess<Actor, Transform>("animatedParentObject");

        private static readonly AccessTools.FieldRef<Actor, Vector3> MovementPositionRef =
            AccessTools.FieldRefAccess<Actor, Vector3>("movementPosition");

        private static readonly AccessTools.FieldRef<Actor, Vector3> CachedPositionRef =
            AccessTools.FieldRefAccess<Actor, Vector3>("cachedPosition");

        private const int GetUpRayMask = 0x221001;

        public static void StartAnimated(Actor actor)
        {
            // Kept name for call sites — implementation is InstantGetUp plant path.
            StartInstantPlanted(actor);
        }

        public static void StartInstantPlanted(Actor actor)
        {
            if (actor == null)
                return;

            actor.autoMoveActor = true;
            actor.wantsToGetUp = true;

            if (!TryFindGround(actor, out Vector3 ground))
                ground = actor.transform.position;

            TeleportFully(actor, ground);
            try { actor.InstantGetUp(); } catch { /* ignore */ }
            TeleportFully(actor, ground);
            ForceFlatVisual(actor);
            try { Physics.SyncTransforms(); } catch { /* ignore */ }
        }

        public static float Progress(Actor actor)
        {
            if (actor == null)
                return 1f;
            try { return actor.GetupProgress(); }
            catch { return actor.fallenOver ? 0f : 1f; }
        }

        public static bool IsGetUpActionDone(Actor actor)
        {
            if (actor == null)
                return true;
            // InstantGetUp finishes immediately via FinishGettingUp.
            return !actor.fallenOver;
        }

        public static bool IsFullyUpright(Actor actor)
        {
            if (actor == null)
                return true;
            return !actor.fallenOver;
        }

        public static void SoftFinish(Actor actor)
        {
            if (actor == null)
                return;
            if (TryFindGround(actor, out Vector3 ground))
                TeleportFully(actor, ground);
            ForceFlatVisual(actor);
        }

        public static void FailsafeInstant(Actor actor)
        {
            StartInstantPlanted(actor);
        }

        public static void ForceFlatVisual(Actor actor)
        {
            if (actor == null || actor.dead)
                return;

            Vector3 root = actor.transform.position;
            try { HeightOffsetRef(actor) = 0f; } catch { /* ignore */ }
            try { LastSoldierHeightRef(actor) = root.y; } catch { /* ignore */ }
            try { MovementPositionRef(actor) = root; } catch { /* ignore */ }
            try { CachedPositionRef(actor) = root; } catch { /* ignore */ }

            try
            {
                Transform ap = AnimatedParentRef(actor);
                if (ap != null)
                {
                    Vector3 parent = Vector3.zero;
                    try { parent = ParentOffsetRef(actor); } catch { /* ignore */ }
                    ap.localPosition = new Vector3(parent.x, 0f, parent.z);
                }
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Continuous correction after get-up. Uses transform.position (truth), not Position().
        /// </summary>
        public static void TickGrounding(Actor actor)
        {
            if (actor == null || actor.dead || actor.fallenOver)
                return;

            ForceFlatVisual(actor);

            Vector3 root = actor.transform.position;
            if (!TryFindGroundFrom(actor, root + Vector3.up * 2.5f, 8f, out Vector3 ground))
                return;

            float gap = root.y - ground.y;
            if (gap > 0.04f || gap < -0.2f)
                TeleportFully(actor, ground);
        }

        public static void PlantOnGround(Actor actor, bool force)
        {
            if (actor == null)
                return;
            if (!TryFindGround(actor, out Vector3 ground))
                return;
            float gap = actor.transform.position.y - ground.y;
            if (!force && Mathf.Abs(gap) < 0.08f)
                return;
            TeleportFully(actor, ground);
        }

        public static void CorrectFloat(Actor actor, float minGap)
        {
            TickGrounding(actor);
        }

        private static void TeleportFully(Actor actor, Vector3 ground)
        {
            Quaternion rot = FlattenYaw(actor.transform.rotation);

            // Prefer AI teleport helper when available.
            try
            {
                if (actor.controller is AiActorController ai)
                {
                    ai.ForceTeleportToPosition(ground);
                    // ForceTeleport uses identity rotation — restore yaw.
                    actor.transform.rotation = rot;
                }
                else
                {
                    actor.SetPositionAndRotation(ground, rot);
                }
            }
            catch
            {
                try { actor.SetPositionAndRotation(ground, rot); }
                catch
                {
                    actor.transform.position = ground;
                    actor.transform.rotation = rot;
                }
            }

            try { MovementPositionRef(actor) = ground; } catch { /* ignore */ }
            try { CachedPositionRef(actor) = ground; } catch { /* ignore */ }
            try { LastSoldierHeightRef(actor) = ground.y; } catch { /* ignore */ }
            try { HeightOffsetRef(actor) = 0f; } catch { /* ignore */ }
            ForceFlatVisual(actor);
        }

        private static bool TryFindGround(Actor actor, out Vector3 ground)
        {
            Vector3 sample = actor.transform.position;
            try
            {
                if (actor.ragdoll != null && actor.ragdoll.IsRagdoll())
                    sample = actor.ragdoll.Position();
            }
            catch { /* ignore */ }

            return TryFindGroundFrom(actor, sample + Vector3.up * 4f, 16f, out ground);
        }

        private static bool TryFindGroundFrom(Actor actor, Vector3 origin, float dist, out Vector3 ground)
        {
            ground = origin;
            if (RayGroundIgnoringSelf(actor, origin, dist, GetUpRayMask, out ground))
                return true;
            // Last resort: default layers but still ignore self.
            return RayGroundIgnoringSelf(actor, origin, dist, ~0, out ground);
        }

        private static bool RayGroundIgnoringSelf(Actor actor, Vector3 origin, float dist, int mask, out Vector3 ground)
        {
            ground = origin;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, dist, mask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return false;

            float bestY = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit h = hits[i];
                if (h.collider == null || IsOwnCollider(actor, h.collider))
                    continue;
                // Skip near-vertical hits (walls) — need floor-ish normal.
                if (h.normal.y < 0.4f)
                    continue;
                if (h.point.y < bestY)
                {
                    bestY = h.point.y;
                    ground = h.point;
                    found = true;
                }
            }
            return found;
        }

        private static bool IsOwnCollider(Actor actor, Collider col)
        {
            if (actor == null || col == null)
                return false;
            Transform t = col.transform;
            if (t == null)
                return false;
            if (t == actor.transform || t.IsChildOf(actor.transform))
                return true;
            try
            {
                if (actor.ragdoll != null)
                {
                    if (actor.ragdoll.ragdollObject != null &&
                        (t == actor.ragdoll.ragdollObject.transform || t.IsChildOf(actor.ragdoll.ragdollObject.transform)))
                        return true;
                    if (actor.ragdoll.animatedObject != null &&
                        (t == actor.ragdoll.animatedObject.transform || t.IsChildOf(actor.ragdoll.animatedObject.transform)))
                        return true;
                }
            }
            catch { /* ignore */ }
            return false;
        }

        private static Quaternion FlattenYaw(Quaternion rot)
        {
            Vector3 e = rot.eulerAngles;
            return Quaternion.Euler(0f, e.y, 0f);
        }
    }
}
