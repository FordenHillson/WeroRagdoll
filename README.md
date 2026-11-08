# WeroRagdoll

BepInEx plugin for Ravenfield that approximates **W.E.R.O**-style reaction-on-hit active ragdoll for **bots only**.

## Build

```powershell
dotnet build "C:\Users\fordg\Documents\WeroRagdoll\src\WeroRagdoll\WeroRagdoll.csproj" -c Release
```

## Install

1. Disable `ImprovedRagdoll` (rename its DLL) to avoid double Harmony patches.
2. Copy `WeroRagdoll.dll` to `Ravenfield\BepInEx\plugins\WeroRagdoll\`.
3. Launch game; config appears at `BepInEx\config\com.fordg.weroragdoll.cfg`.

## Scope

### Phase 1
- Soft → animated flinch
- Medium → stagger / conditional knock
- Hard → living active ragdoll (joint drive + per-limb strength)
- Get-up + wound debuffs (arm blocks fire, leg slows AI)

### Phase 2 (current)
- Wound clutch + ground brace (Animator IK upright, physics springs when ragdolled)
- Death squirm: tense → thrash → limp fade
- Directional / zone-biased impulses with launch clamps

### Phase 3 (0.3.0+)
- Extensible `IMuscleDriver`
- Euphoria primitives: protect head, brace ground, compensatory step, zone writhe
- Still reaction-on-hit / bots only — deeper NaturalMotion solvers can replace the driver later
