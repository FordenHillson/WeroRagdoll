# WeroRagdoll Design

## Goal
BepInEx plugin for Ravenfield that approximates RDR2 W.E.R.O (Western Euphoria Ragdoll Overhaul) via reaction-on-hit active ragdoll for bots only.

## Locked decisions
- New plugin; do not reuse ImprovedRagdoll
- Reaction-on-hit (not always-on physics locomotion)
- Bots only
- Graduated Soft/Medium/Hard + per-limb reactions
- Get-up with lasting wound debuffs
- Approach A (joint drive + strength lerp) + B (animation flinch for Soft)
- Full NaturalMotion-level Euphoria is a later phase on the same architecture

## Architecture
- Harmony patches on `Actor.Damage` / `Actor.Die` / get-up related paths
- Per-actor `WeroActorBrain`: hit classify → muscle strength → state machine
- `ActiveRaggy.SetDrive` + per-joint drive scaling for wounded limbs
- Soft = flinch without full ragdoll; Medium/Hard = KnockOver + living drive; Death = limp fade
- LOD by camera distance + concurrent ActiveStruggle cap

## Phases
1. MVP: states, muscle zones, flinch/struggle/get-up/debuff, LOD, config — done (0.1.x)
2. Richer wound grab / death squirm / impulse realism — done (0.2.x)
3. NaturalMotion-like behavior primitives on `IMuscleDriver` — started (0.3.0)
   - ProtectHead, BraceGround, CompensatoryStep, ZoneWrithe
   - Extensible `IMuscleDriver` / `PidMuscleDriver`
