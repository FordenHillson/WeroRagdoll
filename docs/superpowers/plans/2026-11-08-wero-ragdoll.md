# WeroRagdoll Implementation Plan

> **For agentic workers:** Implement task-by-task. User approved skipping design-doc review gate.

**Goal:** Ship a new BepInEx plugin that gives Ravenfield bots W.E.R.O-like reaction-on-hit active ragdoll.

**Architecture:** Harmony hooks + per-bot brain (muscle strength, graduated states, wound debuff). Joint-drive Master/Slave via `ActiveRaggy`; Soft flinch stays animated.

**Tech Stack:** C# netstandard2.0, BepInEx 5.4.23, HarmonyX, Unity 2020.3 assemblies from Ravenfield.

## Global Constraints
- Do not copy or depend on ImprovedRagdoll
- Bots only (`aiControlled`)
- Disable ImprovedRagdoll when deploying to avoid double-patching

## Tasks
- [x] Scaffold project + docs
- [x] Core: zones, muscle, states, brain, LOD
- [x] Harmony patches
- [x] Build + deploy DLL; disable ImprovedRagdoll
