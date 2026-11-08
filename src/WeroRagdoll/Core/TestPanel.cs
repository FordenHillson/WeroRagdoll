using UnityEngine;

namespace WeroRagdoll.Core
{
    /// <summary>
    /// In-game button panel for W.E.R.O reaction tests. Toggle with hotkey (default F7).
    /// </summary>
    public static class TestPanel
    {
        private static bool _open;
        private static KeyCode _hotkey = KeyCode.F7;
        private static Rect _win = new Rect(20f, 50f, 360f, 520f);
        private static Vector2 _scroll;
        private static CursorLockMode _prevLock;
        private static bool _prevVisible;
        private static bool _cursorStolen;

        public static bool IsOpen => _open;

        public static void InitFromConfig()
        {
            ResolveHotkey();
            _open = Plugin.Cfg.TestUiStartOpen.Value;
            if (_open)
                StealCursor();
        }

        public static void Tick()
        {
            if (Plugin.Cfg == null || !Plugin.Cfg.Enabled.Value)
                return;

            ResolveHotkey();
            if (Input.GetKeyDown(_hotkey))
                SetOpen(!_open);
        }

        public static void SetOpen(bool open)
        {
            if (_open == open)
                return;
            _open = open;
            Plugin.Cfg.TestUiStartOpen.Value = open;
            if (_open)
                StealCursor();
            else
                RestoreCursor();
        }

        public static void OnGui()
        {
            if (!_open)
                return;

            _win = GUI.Window(872341, _win, DrawWindow, "Wero Test Panel  [" + _hotkey + "]");
        }

        private static void DrawWindow(int id)
        {
            GUILayout.BeginVertical();
            _scroll = GUILayout.BeginScrollView(_scroll);

            Actor target = TestHarness.FindTargetBot();
            GUILayout.Label(target != null ? ("Target: " + target.name) : "Target: (none — spawn bots)");
            GUILayout.Label(TestHarness.LastStatus);
            GUILayout.Space(6f);

            GUILayout.Label("Setup");
            if (GUILayout.Button(TestRange.Active ? "Test Range: ON" : "Test Range: OFF", GUILayout.Height(28f)))
                TestRange.SetActive(!TestRange.Active);
            if (GUILayout.Button(TestHarness.ImmortalBots ? "Immortal bots: ON" : "Immortal bots: OFF", GUILayout.Height(28f)))
            {
                TestHarness.ImmortalBots = !TestHarness.ImmortalBots;
                TestHarness.LastStatus = TestHarness.ImmortalBots
                    ? "Immortal ON — AI won't die (Lethal buttons still kill)"
                    : "Immortal OFF — bots can die normally";
            }
            if (GUILayout.Button("Heal nearest bot", GUILayout.Height(26f)))
                TestHarness.RespawnHealNearest();

            GUILayout.Space(8f);
            GUILayout.Label("Soft / Medium / Hard (torso)");
            if (GUILayout.Button("Soft flinch", GUILayout.Height(28f)))
                TestHarness.ApplyHit(HitZone.Torso, 6f, 4f, 40f, false, false);
            if (GUILayout.Button("Medium stagger", GUILayout.Height(28f)))
                TestHarness.ApplyHit(HitZone.Torso, 18f, 22f, 110f, false, false);
            if (GUILayout.Button("Hard struggle", GUILayout.Height(28f)))
                TestHarness.ApplyHit(HitZone.Torso, 28f, 40f, 180f, false, false);

            GUILayout.Space(8f);
            GUILayout.Label("By body zone (hard)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Head", GUILayout.Height(26f)))
                TestHarness.ApplyHit(HitZone.Head, 35f, 30f, 140f, true, false);
            if (GUILayout.Button("Torso", GUILayout.Height(26f)))
                TestHarness.ApplyHit(HitZone.Torso, 30f, 35f, 170f, false, false);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Arm L", GUILayout.Height(26f)))
                TestHarness.ApplyHit(HitZone.ArmL, 22f, 18f, 100f, false, false);
            if (GUILayout.Button("Arm R", GUILayout.Height(26f)))
                TestHarness.ApplyHit(HitZone.ArmR, 22f, 18f, 100f, false, false);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Leg L", GUILayout.Height(26f)))
                TestHarness.ApplyHit(HitZone.LegL, 24f, 28f, 120f, false, false);
            if (GUILayout.Button("Leg R", GUILayout.Height(26f)))
                TestHarness.ApplyHit(HitZone.LegR, 24f, 28f, 120f, false, false);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("Lethal");
            if (GUILayout.Button("Lethal torso (death squirm)", GUILayout.Height(28f)))
                TestHarness.ApplyHit(HitZone.Torso, 999f, 50f, 220f, false, true);
            if (GUILayout.Button("Lethal headshot", GUILayout.Height(28f)))
                TestHarness.ApplyHit(HitZone.Head, 999f, 40f, 160f, true, true);

            GUILayout.Space(8f);
            GUILayout.Label("Euphoria primitives (force hard ragdoll first)");
            if (GUILayout.Button("Test ProtectHead", GUILayout.Height(28f)))
                TestHarness.ApplyHit(HitZone.Head, 32f, 38f, 160f, true, false);
            if (GUILayout.Button("Test BraceGround (torso knock)", GUILayout.Height(28f)))
                TestHarness.ApplyHit(HitZone.Torso, 34f, 45f, 200f, false, false);
            if (GUILayout.Button("Test ZoneWrithe Leg L", GUILayout.Height(28f)))
                TestHarness.ApplyHit(HitZone.LegL, 30f, 40f, 170f, false, false);
            if (GUILayout.Button("Test ZoneWrithe Arm R", GUILayout.Height(28f)))
                TestHarness.ApplyHit(HitZone.ArmR, 28f, 30f, 150f, false, false);
            if (GUILayout.Button("Leg cripple combo x3", GUILayout.Height(28f)))
            {
                TestHarness.ApplyHit(HitZone.LegL, 20f, 25f, 100f, false, false);
                TestHarness.ApplyHit(HitZone.LegL, 20f, 25f, 100f, false, false);
                TestHarness.ApplyHit(HitZone.LegL, 25f, 35f, 140f, false, false);
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("Close panel", GUILayout.Height(26f)))
                SetOpen(false);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        private static void StealCursor()
        {
            if (_cursorStolen)
                return;
            _prevLock = Cursor.lockState;
            _prevVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _cursorStolen = true;
        }

        private static void RestoreCursor()
        {
            if (!_cursorStolen)
                return;
            Cursor.lockState = _prevLock;
            Cursor.visible = _prevVisible;
            _cursorStolen = false;
        }

        private static void ResolveHotkey()
        {
            if (!System.Enum.TryParse(Plugin.Cfg.TestUiHotkey.Value, true, out _hotkey))
                _hotkey = KeyCode.F7;
        }
    }
}
