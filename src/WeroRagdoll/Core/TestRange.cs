using UnityEngine;

namespace WeroRagdoll.Core
{
    /// <summary>
    /// Freeze bot AI combat/movement so the player can shoot stationary targets.
    /// </summary>
    public static class TestRange
    {
        private static bool _active;
        private static KeyCode _hotkey = KeyCode.F8;
        private static float _flashUntil;
        private static string _flashMsg = "";

        public static bool Active => _active && Plugin.Cfg != null && Plugin.Cfg.Enabled.Value;

        public static void InitFromConfig()
        {
            _active = Plugin.Cfg.TestRangeEnabled.Value;
            ResolveHotkey();
            if (_active)
                Flash("TEST RANGE ON — bots idle (toggle " + _hotkey + ")");
        }

        public static void Tick()
        {
            if (Plugin.Cfg == null || !Plugin.Cfg.Enabled.Value)
                return;

            ResolveHotkey();
            if (Input.GetKeyDown(_hotkey))
                SetActive(!_active);
        }

        public static void SetActive(bool on)
        {
            _active = on;
            Plugin.Cfg.TestRangeEnabled.Value = on;
            Flash(on
                ? "TEST RANGE ON — bots stand still, no fire (" + _hotkey + ")"
                : "TEST RANGE OFF — normal AI (" + _hotkey + ")");
            Plugin.Log?.LogInfo(on ? "Test Range enabled" : "Test Range disabled");

            if (!on)
                return;

            try
            {
                Actor[] actors = Object.FindObjectsOfType<Actor>();
                for (int i = 0; i < actors.Length; i++)
                {
                    Actor a = actors[i];
                    if (a == null || !a.aiControlled || a.dead)
                        continue;
                    if (a.controller is AiActorController ai)
                    {
                        try { ai.DisableMovement(); } catch { /* ignore */ }
                    }
                }
            }
            catch { /* ignore */ }
        }

        public static void OnGui()
        {
            bool showFlash = Time.unscaledTime <= _flashUntil && !string.IsNullOrEmpty(_flashMsg);
            if (!Active && !showFlash)
                return;

            string line = Active
                ? "WERO TEST RANGE — bots idle / no shooting  [" + _hotkey + "]"
                : _flashMsg;

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.Box(new Rect(12f, 12f, 460f, 28f), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(20f, 16f, 440f, 22f), line);
        }

        private static void ResolveHotkey()
        {
            if (!System.Enum.TryParse(Plugin.Cfg.TestRangeHotkey.Value, true, out _hotkey))
                _hotkey = KeyCode.F8;
        }

        private static void Flash(string msg)
        {
            _flashMsg = msg;
            _flashUntil = Time.unscaledTime + 3.5f;
        }
    }
}
