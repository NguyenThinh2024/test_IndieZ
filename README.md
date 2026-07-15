# TEST-INDIEZ — ZombieWar

Unity project thử nghiệm gameplay **ZombieWar**: top-down twin-stick, diệt zombie theo wave, map load bằng **Addressables**, camera **Cinemachine**, di chuyển joystick.

Repo: [abc2003vvv/TEST-INDIEZ](https://github.com/abc2003vvv/TEST-INDIEZ)

---

## Yêu cầu môi trường

| Mục | Giá trị |
|-----|---------|
| Unity Editor | **6000.0.72f1** (Unity 6) |
| Render pipeline | **URP 17.0.4** |
| Platform chính | Mobile (Android build settings có sẵn trong Addressables) |

Mở bằng Unity Hub → Open folder repo → đợi import xong. Không commit thư mục `Library/`, `Temp/`, `Logs/` (đã ignore).

---

## Cấu trúc thư mục chính

```
Assets/
├── _Game/                    # Gameplay ZombieWar (art, data, prefabs, scripts)
│   ├── Data/ZombieWar/Level/ # LevelMapCatalog + LevelWaveConfig (ScriptableObject)
│   ├── Prefabs/ZombieWar/    # Map level1/level2, zombie, VFX…
│   ├── Scripts/ZombieWar/    # Code theo domain (Level, Player, Enemy, Weapon…)
│   └── Docs/                 # Ghi chú kiến trúc bổ sung
├── _SDK/                     # Template Nexzap: boot, menu, profile, ads…
│   └── Template/Scenes/      # Loading, Menu, Gameplay
├── AddressableAssetsData/    # Groups + addresses
├── Plugins/                  # UniTask, DOTween, LeanGUI, Odin…
└── Settings/                 # URP / quality assets
ProjectSettings/              # Unity project settings (build scenes, tags, URP…)
Packages/                     # manifest.json + lock
```

Code gameplay **không** tách asmdef riêng — compile vào `Assembly-CSharp`.

---

## Luồng scene (boot)

```
Loading (index 0)
  → warmup Addressables (ZombieWarAddressableWarmup)
  → Menu
      → PLAY (ZombieWarMenuPlayButton) ghi level vào PlayerPrefs
      → Gameplay
```

| Scene | Đường dẫn |
|-------|-----------|
| Loading | `Assets/_SDK/Template/Scenes/Loading.unity` |
| Menu | `Assets/_SDK/Template/Scenes/Menu.unity` |
| Gameplay | `Assets/_SDK/Template/Scenes/Gameplay.unity` |

Hằng số tên scene: `SceneName` trong `Assets/_SDK/Scripts/UI/UISceneController.cs`.

---

## Level kỹ thuật — map Addressable (không tách scene từng level)

Gameplay luôn là **một scene**. Mỗi level = **một map prefab** instantiate qua Addressables.

### Dữ liệu (ScriptableObject)

| Asset | Vai trò |
|-------|---------|
| `Assets/_Game/Data/ZombieWar/Level/LevelMapCatalog.asset` | Bảng level → address map + wave config |
| `LevelWaveConfig_Level1/2.asset` | Thời lượng + danh sách wave / số spawn / zombie |

Entry catalog (`LevelMapEntry`): `levelNumber`, `mapAddress`, `waveConfig`.

### Address keys

| Level | Address | Prefab |
|-------|---------|--------|
| 1 | `ZombieWar/Levels/Level1` | `Assets/_Game/Prefabs/ZombieWar/Levels/level1.prefab` |
| 2 | `ZombieWar/Levels/Level2` | `Assets/_Game/Prefabs/ZombieWar/Levels/level2.prefab` |

### Runtime flow

```
LevelMapBootstrap.OnEnable
  → resolve level (PlayerPrefs ZW_SessionLevel → profile LEVEL)
  → catalog.TryGetEntry(level)
  → WaveManager.SetLevelConfig(waveConfig)
  → xoá map cũ dưới ZW_LevelMapRoot (kể cả instance bake sót)
  → LevelMapLoader.LoadAsync(mapAddress)   // Addressables.InstantiateAsync
  → bật / bake NavMesh (ưu tiên NavMesh trong prefab map)
  → WaveManager.PlaceActorsOnMap(map)      // player giữa map + spawn ring
  → MapReady → ZombieWarGameFlow bắt đầu wave
  → preload Addressable level kế tiếp (optional)
```

| Class | Path | Ownership |
|-------|------|-----------|
| `LevelMapBootstrap` | `.../Level/LevelMapBootstrap.cs` | Composition root load / release map |
| `LevelMapLoader` | `.../Level/LevelMapLoader.cs` | Instantiate / preload / ReleaseInstance |
| `LevelMapCatalog` | `.../Level/LevelMapCatalog.cs` | Catalog SO |
| `WaveManager` | `.../Level/WaveManager.cs` | Spawn wave, clear condition |
| `ZombieWarGameFlow` | `.../Level/ZombieWarGameFlow.cs` | Win / lose / Replay / Next |

### Progress level (PlayerPrefs)

| Key | Ý nghĩa |
|-----|---------|
| `ZW_SessionLevel` | Level đang chơi (ưu tiên khi resolve) |
| `currentLevel` | Đồng bộ profile SDK |

- **Replay**: giữ level hiện tại, reload Gameplay.  
- **Next Level**: `level + 1` → PersistSessionLevel → reload Gameplay.  
- **Chơi từ đầu**: Edit → Clear All PlayerPrefs (hoặc xoá 2 key trên).

---

## Gameplay systems

### Player

| Hệ thống | File | Ghi chú |
|----------|------|---------|
| Movement | `Player/PlayerMovement.cs` | Joystick, gravity, combat facing latch |
| Combat | `Player/PlayerCombat.cs` | Auto-aim + fire target trong zone (đi / đứng đều bắn) |
| Health | `Player/PlayerHealth.cs` | `Died` → lose |
| Character load | `Player/PlayerCharacterLoader.cs` | Addressable config / prefab / animator |
| Targeting | `Player/EnemyTargetScanner.cs` | Chọn target trong vùng |

### Weapon

- `WeaponController` — load gun config (JSON Addressable), switch weapon + cooldown.  
- `ProjectileWeapon` + `BulletProjectileSystem` — đạn pooled.  
- `PlayerWeaponAttach` — gắn visual súng + FirePoint.  
- `WeaponSwitchVfx` — VFX khi đổi súng.

### Enemy

- `Enemy` — composition root 1 zombie.  
- `EnemyTickSystem` — tick tập trung.  
- `ZombieEnemyPool` — pool Addressable zombie.  
- Wave data: `LevelWaveConfig` / `WaveData` (interval, count, config reference).

### Camera

- `GameCameraController` — Cinemachine follow `player__root` / `PlayerCameraTarget`.

### Win / Lose

- Win: `WaveManager.Cleared` (hết quota spawn + không còn zombie sống).  
- Lose: player chết.  
- UI panel + nút Replay / Next Level trong `ZombieWarGameFlow`.

---

## Dependencies quan trọng

| Package / lib | Dùng cho |
|---------------|----------|
| `com.unity.addressables` | Map, zombie, gun, player assets |
| `com.unity.ai.navigation` | NavMeshSurface trên map / scene |
| `com.unity.cinemachine` | Camera follow |
| UniTask (`Assets/Plugins/UniTask`) | Async load / warmup / spawn |
| Joystick Pack | Input di chuyển |
| SDK `Assets/_SDK` | Loading, Menu, profile, scene change |

Gameplay ZombieWar **không** gọi trực tiếp Ads / Analytics SDK — giữ ở lớp Infrastructure / SDK template.

---

## ProjectSettings trên Git

Folder `ProjectSettings/` và `Packages/` được version control để clone mở được project:

- `EditorBuildSettings.asset` — thứ tự scene Loading / Menu / Gameplay  
- `ProjectVersion.txt` — version Unity  
- `GraphicsSettings` / `URPProjectSettings` — pipeline  
- `TagManager`, `InputManager`, `QualitySettings`, …

Local-only (đã `.gitignore`): `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `.cursor/`.

---

## Cách chạy nhanh

1. Mở project bằng Unity **6000.0.72f1**.  
2. Enter Play từ scene **Loading**, hoặc Menu → **PLAY**.  
3. Hierarchy Gameplay lúc Play: dưới `ZW_LevelMapRoot` chỉ nên có **một** map (`level1` hoặc `level2(Clone)`).  
4. Confirm Addressables Groups còn địa chỉ `ZombieWar/Levels/Level1` và `Level2`.  
5. Play Mode Script Addressables nên để **Use Asset Database (fastest)** khi dev local.

---

## Ghi chú kiến trúc ngắn

1. **Config ≠ Save ≠ Runtime** — SO catalog/wave là dữ liệu tác giả; PlayerPrefs là progress; máu / wave elapsed là runtime.  
2. **Một scene Gameplay** — đổi level = đổi map prefab Addressable.  
3. **Ownership rõ**: Bootstrap sở hữu map load; WaveManager sở hữu spawn; GameFlow sở hữu win/lose.  
4. Addressable `Load` / `Instantiate` phải có `Release` tương ứng (`LevelMapLoader.Release`).  
5. Tài liệu dự định cũ (một phần đã lệch so với code hiện tại): `Assets/_Game/Docs/LevelFlow-Architecture.md`.

---

## License / nội dung third-party

Project chứa asset demo bên thứ ba (Pure Poly, PostApocalypse guns, Survivalist…). Khi publish commercial cần kiểm tra license từng pack. File Terrain lớn (~65MB) có thể vượt khuyến nghị GitHub — cân nhắc Git LFS nếu tiếp tục đẩy asset tương tự.
