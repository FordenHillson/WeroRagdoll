using System.Collections.Generic;
using UnityEngine;
using WeroRagdoll.Core.Euphoria.Behaviors;

namespace WeroRagdoll.Core.Euphoria
{
    /// <summary>
    /// Phase 3: selects and ticks Euphoria behavior primitives.
    /// </summary>
    public sealed class EuphoriaDirector : MonoBehaviour
    {
        private readonly List<IEuphoriaBehavior> _library = new List<IEuphoriaBehavior>();
        private readonly List<IEuphoriaBehavior> _active = new List<IEuphoriaBehavior>();
        private readonly EuphoriaContext _ctx = new EuphoriaContext();
        private WeroActorBrain _brain;

        public static EuphoriaDirector Ensure(Actor actor, WeroActorBrain brain)
        {
            if (actor == null)
                return null;
            var dir = actor.GetComponent<EuphoriaDirector>();
            if (dir == null)
                dir = actor.gameObject.AddComponent<EuphoriaDirector>();
            dir._brain = brain;
            dir._ctx.Actor = actor;
            if (brain != null)
                dir._ctx.Driver = brain.Driver;
            dir.EnsureLibrary();
            return dir;
        }

        public void NotifyHit(HitZone zone, Vector3 forceDir, Vector3 woundPoint, bool lethal)
        {
            _ctx.LastZone = zone;
            if (forceDir.sqrMagnitude > 0.001f)
                _ctx.ForceDir = forceDir.normalized;
            _ctx.WoundPoint = woundPoint;
            _ctx.Lethal = lethal;
            Reevaluate(forceEnter: true);
        }

        private void EnsureLibrary()
        {
            if (_library.Count > 0)
                return;
            _library.Add(new ProtectHeadBehavior());
            _library.Add(new BraceGroundBehavior());
            _library.Add(new CompensatoryStepBehavior());
            _library.Add(new ZoneWritheBehavior());
        }

        private void FixedUpdate()
        {
            if (!Plugin.Cfg.Enabled.Value || !Plugin.Cfg.EuphoriaEnabled.Value)
                return;
            if (_brain == null || _ctx.Actor == null)
                return;

            _ctx.State = _brain.State;
            _ctx.Driver = _brain.Driver;
            _ctx.TimeInState += Time.fixedDeltaTime;

            if (_ctx.State != WeroState.ActiveStruggle
                && _ctx.State != WeroState.Stagger
                && _ctx.State != WeroState.LimpDeath
                && _ctx.State != WeroState.GetUp)
            {
                ClearActive();
                return;
            }

            Reevaluate(forceEnter: false);
            float dt = Time.fixedDeltaTime;
            for (int i = 0; i < _active.Count; i++)
                _active[i].Tick(_ctx, dt);
        }

        private void Reevaluate(bool forceEnter)
        {
            EnsureLibrary();
            // Exit behaviors that no longer want.
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                IEuphoriaBehavior b = _active[i];
                if (!b.Wants(_ctx))
                {
                    b.Exit(_ctx);
                    _active.RemoveAt(i);
                }
            }

            // Enter new wanted behaviors.
            for (int i = 0; i < _library.Count; i++)
            {
                IEuphoriaBehavior b = _library[i];
                if (!b.Wants(_ctx))
                    continue;
                if (_active.Contains(b))
                    continue;
                _active.Add(b);
                b.Enter(_ctx);
            }

            if (forceEnter)
                _ctx.TimeInState = 0f;
        }

        private void ClearActive()
        {
            for (int i = 0; i < _active.Count; i++)
                _active[i].Exit(_ctx);
            _active.Clear();
            _ctx.TimeInState = 0f;
        }

        private void OnDestroy()
        {
            ClearActive();
        }
    }
}
