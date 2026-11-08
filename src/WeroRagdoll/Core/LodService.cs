using System.Collections.Generic;
using UnityEngine;

namespace WeroRagdoll.Core
{
    public static class LodService
    {
        private static readonly HashSet<int> ActiveStruggleIds = new HashSet<int>();
        private static Camera _cam;
        private static float _camRefresh;

        public static void Reset()
        {
            ActiveStruggleIds.Clear();
            _cam = null;
            _camRefresh = 0f;
        }

        public static void Tick(float dt)
        {
            _camRefresh -= dt;
            if (_camRefresh <= 0f)
            {
                _camRefresh = 0.5f;
                _cam = Camera.main;
            }
        }

        public static float DistanceToCamera(Vector3 world)
        {
            if (_cam == null)
                _cam = Camera.main;
            if (_cam == null)
                return 0f;
            return Vector3.Distance(_cam.transform.position, world);
        }

        public enum LodBand
        {
            Full,
            Medium,
            Cheap
        }

        public static LodBand BandFor(Actor actor)
        {
            if (actor == null)
                return LodBand.Cheap;
            float d = DistanceToCamera(actor.Position());
            if (d <= Plugin.Cfg.FullLodDistance.Value)
                return LodBand.Full;
            if (d <= Plugin.Cfg.CheapLodDistance.Value)
                return LodBand.Medium;
            return LodBand.Cheap;
        }

        public static bool TryAcquireStruggle(Actor actor)
        {
            if (actor == null)
                return false;
            int id = actor.GetInstanceID();
            if (ActiveStruggleIds.Contains(id))
                return true;
            if (ActiveStruggleIds.Count >= Plugin.Cfg.MaxConcurrentStruggle.Value)
                return false;
            ActiveStruggleIds.Add(id);
            return true;
        }

        public static void ReleaseStruggle(Actor actor)
        {
            if (actor == null)
                return;
            ActiveStruggleIds.Remove(actor.GetInstanceID());
        }
    }
}
