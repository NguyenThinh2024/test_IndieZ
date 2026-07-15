# ZombieWar — Level Flow (Addressables)

Saved for later discussion. Aligns with project architecture rules.

## Flow

```
Boot Scene
  → ApplicationBootstrap (once per app)
      · Addressables init
      · Save / Analytics gateway (Infrastructure only)
      · Load catalog
  → Menu Scene

Menu
  → Press Level N button
  → LevelFlow / LevelSession
      · Load Addressable LevelConfig JSON (e.g. ZombieWar/Levels/01)
      · Optional: preload Addressable labels for that level
  → Load Gameplay Scene
  → GameplayCompositionRoot.Initialize(levelConfig)
      · Wire Player / EnemyTick / Spawn / Wave / Camera / Combat
      · Spawn from config (existing Soldier / Enemy Addressables)
  → Play

Level end / Back
  → CompositionRoot.Dispose + Release Addressable handles
  → Menu Scene
```

## Ownership

| Step | Owner | Must not |
|------|--------|----------|
| Boot | `ApplicationBootstrap` | Spawn gameplay / enemies |
| Menu UI | Menu presenter / UI | Scatter Addressables + SDK calls in Button OnClick |
| Level select | `LevelFlow` / `LevelSession` | `FindObjectOfType` chains |
| Level data | `LevelConfig` (Addressable JSON) | Store runtime state in config assets |
| Build match | `GameplayCompositionRoot` | `GameManager.Enemy…` singleton chains |
| Content | Addressable address / label | Gameplay calling Ads / Analytics SDKs directly |

## Suggested Addressable layout

```
Assets/_Game/Addressables/
  Configs/Levels/Level_01.json   → ZombieWar/Levels/01
  Configs/Player/SoldierCharacterConfig.json
  Configs/Enemy/ZombieEnemyConfig.json
```

Level JSON should reference: player id, waves, enemy addresses, map/scene key, balance overrides.

## Button → Play sequence

1. `Addressables.LoadAssetAsync<TextAsset>("ZombieWar/Levels/01")`
2. Parse → plain C# `LevelConfig`
3. Store in `LevelSession` (lives across scene load; not random static mutable globals)
4. `LoadSceneAsync(Gameplay)`
5. `GameplayCompositionRoot` reads `LevelSession.Current` → `Initialize`
6. On exit: Release handles, clear session

## Rules reminders

1. Config ≠ Save ≠ Runtime
2. Composition root builds the gameplay graph
3. Every Addressable Load has a Release owner
4. Menu only knows “start level id”, not Enemy/Weapon internals
5. Keep SDK template `GameController` for app services; ZombieWar gameplay has its own composition root in Gameplay scene

## Already in project (reuse)

- Player Addressable + JSON → link via `LevelConfig.playerId`
- `PlayerCharacterLoader` → start from CompositionRoot after gameplay ready
- Enemy Addressable pool / wave → feed from `LevelConfig`
- Boot/Menu currently template SDK — introduce `LevelFlow` next

## Implementation order (when continuing)

1. `LevelConfig` JSON + Addressable address
2. `LevelSession` + menu button loads config then opens Gameplay
3. `GameplayCompositionRoot.Initialize(config)`
4. Dispose / Release on return to menu
