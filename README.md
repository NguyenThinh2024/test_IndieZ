# TEST-INDIEZ — ZombieWar

Unity project thử nghiệm gameplay **ZombieWar**, tập trung vào gameplay top-down twin-stick shooter với cơ chế tiêu diệt zombie theo wave.

Project sử dụng **Addressables** để quản lý map và gameplay assets, **Cinemachine** cho camera, **NavMesh** cho AI navigation và joystick cho điều khiển trên mobile.

## Tech Stack

| Thành phần        | Công nghệ                     |
| ----------------- | ----------------------------- |
| Unity             | 6000.0.72f1                   |
| Render Pipeline   | URP 17.0.4                    |
| Platform          | Mobile / Android              |
| Camera            | Cinemachine                   |
| Navigation        | Unity AI Navigation / NavMesh |
| Async             | UniTask                       |
| Tween / Animation | DOTween                       |
| Asset Management  | Addressables                  |
| Input             | Joystick Pack                 |
| SDK               | Nexzap SDK                    |

## Project Structure

```text
Assets/
├── _Game/
│   ├── Data/
│   │   └── ZombieWar/
│   │       └── Level/
│   │           ├── LevelMapCatalog.asset
│   │           └── LevelWaveConfig_*.asset
│   │
│   ├── Prefabs/
│   │   └── ZombieWar/
│   │       ├── Levels/
│   │       ├── Zombie/
│   │       └── VFX/
│   │
│   ├── Scripts/
│   │   └── ZombieWar/
│   │       ├── Level/
│   │       ├── Player/
│   │       ├── Enemy/
│   │       ├── Weapon/
│   │       └── ...
│   │
│   └── Docs/
│
├── _SDK/
│   ├── Scripts/
│   └── Template/
│       └── Scenes/
│
├── AddressableAssetsData/
├── Plugins/
│   ├── UniTask/
│   ├── DOTween/
│   ├── LeanGUI/
│   └── Odin/
│
└── Settings/

ProjectSettings/
Packages/
```

Gameplay code hiện tại không sử dụng Assembly Definition riêng và được compile trực tiếp vào `Assembly-CSharp`.

---

# Scene Flow

Project sử dụng một scene Gameplay duy nhất. Level không được tách thành các scene riêng mà được load dưới dạng Addressable map prefab.

```text
Loading
   |
   v
Addressables Warmup
   |
   v
Menu
   |
   | PLAY
   v
Gameplay
   |
   v
Load Level Map
   |
   v
Spawn Waves
   |
   v
Win / Lose
```

## Scenes

| Scene    | Path                                         |
| -------- | -------------------------------------------- |
| Loading  | `Assets/_SDK/Template/Scenes/Loading.unity`  |
| Menu     | `Assets/_SDK/Template/Scenes/Menu.unity`     |
| Gameplay | `Assets/_SDK/Template/Scenes/Gameplay.unity` |

Scene `Loading` được sử dụng làm entry point của game.

Trong quá trình khởi động, hệ thống thực hiện Addressables warmup thông qua `ZombieWarAddressableWarmup`, sau đó chuyển sang Menu.

Khi người chơi nhấn PLAY, level hiện tại được lưu vào `PlayerPrefs` và scene Gameplay được load.

Tên scene được quản lý bởi `SceneName` trong:

```text
Assets/_SDK/Scripts/UI/UISceneController.cs
```

---

# Level Architecture

Gameplay chỉ sử dụng một scene:

```text
Gameplay.unity
```

Mỗi level được biểu diễn bởi một map prefab và được instantiate thông qua Addressables.

Ví dụ:

```text
Level 1
    Gameplay Scene
        └── Level1 Map Prefab

Level 2
    Gameplay Scene
        └── Level2 Map Prefab
```

Điều này giúp tránh việc phải tạo và quản lý nhiều Gameplay Scene cho từng level.

## Level Data

Level được quản lý thông qua các ScriptableObject.

| Asset                          | Chức năng                                        |
| ------------------------------ | ------------------------------------------------ |
| `LevelMapCatalog.asset`        | Mapping level với map Addressable và wave config |
| `LevelWaveConfig_Level1.asset` | Cấu hình wave của Level 1                        |
| `LevelWaveConfig_Level2.asset` | Cấu hình wave của Level 2                        |

`LevelMapEntry` chứa các thông tin chính:

```text
levelNumber
mapAddress
waveConfig
```

---

# Addressables

Map của từng level được đăng ký dưới các Address key:

| Level   | Address                   | Prefab                                                |
| ------- | ------------------------- | ----------------------------------------------------- |
| Level 1 | `ZombieWar/Levels/Level1` | `Assets/_Game/Prefabs/ZombieWar/Levels/level1.prefab` |
| Level 2 | `ZombieWar/Levels/Level2` | `Assets/_Game/Prefabs/ZombieWar/Levels/level2.prefab` |

Addressables được sử dụng cho:

* Level map
* Zombie prefab
* Player character
* Weapon configuration
* Các gameplay assets khác

Các asset được load bằng Addressables phải có ownership rõ ràng và được release tương ứng khi không còn sử dụng.

---

# Level Loading Flow

Hệ thống load level được điều phối bởi `LevelMapBootstrap`.

Flow runtime:

```text
LevelMapBootstrap.OnEnable
        |
        v
Resolve Level
        |
        +-- ZW_SessionLevel
        |
        +-- SDK Profile Level
        |
        v
LevelMapCatalog.TryGetEntry(level)
        |
        v
WaveManager.SetLevelConfig()
        |
        v
Remove Previous Map
        |
        v
LevelMapLoader.LoadAsync(mapAddress)
        |
        v
Addressables.InstantiateAsync()
        |
        v
Map Ready
        |
        +-- Enable / Bake NavMesh
        |
        +-- Place Player
        |
        +-- Setup Spawn Ring
        |
        v
WaveManager.PlaceActorsOnMap()
        |
        v
ZombieWarGameFlow
        |
        v
Start Wave
```

Các class chính:

| Class               | Path                       | Responsibility                      |
| ------------------- | -------------------------- | ----------------------------------- |
| `LevelMapBootstrap` | `Scripts/ZombieWar/Level/` | Composition root cho level loading  |
| `LevelMapLoader`    | `Scripts/ZombieWar/Level/` | Instantiate, preload và release map |
| `LevelMapCatalog`   | `Scripts/ZombieWar/Level/` | Quản lý level configuration         |
| `WaveManager`       | `Scripts/ZombieWar/Level/` | Spawn và quản lý wave               |
| `ZombieWarGameFlow` | `Scripts/ZombieWar/Level/` | Win, lose, replay và next level     |

---

# Level Progress

Level hiện tại được lưu bằng `PlayerPrefs`.

| Key               | Chức năng                          |
| ----------------- | ---------------------------------- |
| `ZW_SessionLevel` | Level hiện tại của session         |
| `currentLevel`    | Level được đồng bộ với SDK profile |

Khi resolve level, `ZW_SessionLevel` được ưu tiên trước profile level.

## Replay

Replay giữ nguyên level hiện tại:

```text
Current Level
      |
      v
Reload Gameplay
      |
      v
Load Current Map
```

## Next Level

Sau khi hoàn thành level:

```text
Current Level + 1
        |
        v
PersistSessionLevel()
        |
        v
Reload Gameplay
        |
        v
Load Next Map
```

## Reset Progress

Có thể reset progress bằng cách xóa các key:

```text
ZW_SessionLevel
currentLevel
```

Trong Unity Editor có thể sử dụng:

```text
Edit
└── Clear All PlayerPrefs
```

---

# Gameplay Systems

## Player

### PlayerMovement

Path:

```text
Assets/_Game/Scripts/ZombieWar/Player/PlayerMovement.cs
```

Chịu trách nhiệm:

* Joystick movement
* Gravity
* CharacterController movement
* Combat facing
* Combat facing latch

### PlayerCombat

Chịu trách nhiệm:

* Auto targeting
* Tự động tìm enemy trong vùng
* Tự động fire
* Hoạt động khi player đang di chuyển hoặc đứng yên

### PlayerHealth

Quản lý HP của player.

Khi player chết:

```text
PlayerHealth.Died
       |
       v
ZombieWarGameFlow
       |
       v
Lose
```

### PlayerCharacterLoader

Load character configuration, prefab và animator thông qua Addressables.

### EnemyTargetScanner

Tìm và lựa chọn enemy phù hợp trong vùng targeting của player.

---

# Weapon System

Weapon system bao gồm:

```text
WeaponController
      |
      +-- Weapon Configuration
      |
      +-- Weapon Switching
      |
      +-- Cooldown
      |
      v
ProjectileWeapon
      |
      v
BulletProjectileSystem
      |
      v
Pooled Projectile
```

Các class chính:

| Class                    | Chức năng                                  |
| ------------------------ | ------------------------------------------ |
| `WeaponController`       | Quản lý weapon, config, switch và cooldown |
| `ProjectileWeapon`       | Xử lý projectile weapon                    |
| `BulletProjectileSystem` | Spawn và quản lý bullet                    |
| `PlayerWeaponAttach`     | Attach visual weapon và FirePoint          |
| `WeaponSwitchVfx`        | VFX khi đổi weapon                         |

Projectile được quản lý bằng pooling để hạn chế allocation và garbage collection trong quá trình gameplay.

---

# Enemy System

Mỗi zombie được quản lý bởi `Enemy`.

Các thành phần chính:

```text
Enemy
   |
   +-- EnemyTickSystem
   |
   +-- ZombieEnemyPool
   |
   +-- WaveManager
```

### Enemy

Composition root của một zombie, quản lý các thành phần gameplay liên quan đến enemy.

### EnemyTickSystem

Sử dụng centralized tick thay vì để từng enemy tự xử lý update độc lập.

### ZombieEnemyPool

Quản lý zombie pool và Addressable zombie prefab.

### Wave System

Wave configuration được định nghĩa bằng:

```text
LevelWaveConfig
    |
    └── WaveData
```

`WaveData` chứa các thông tin như:

* Spawn interval
* Spawn count
* Zombie configuration

---

# Wave System

`WaveManager` chịu trách nhiệm:

* Load wave configuration
* Spawn zombie
* Theo dõi số lượng zombie
* Kiểm tra điều kiện hoàn thành wave
* Xác định khi level đã được clear

Điều kiện clear level:

```text
Đã spawn đủ quota
        AND
Không còn zombie sống
```

Khi điều kiện trên được thỏa mãn:

```text
WaveManager.Cleared
        |
        v
ZombieWarGameFlow
        |
        v
Win
```

---

# Camera

Camera được quản lý bởi:

```text
GameCameraController
```

Camera sử dụng Cinemachine và follow:

```text
player__root
```

và:

```text
PlayerCameraTarget
```

Cách tiếp cận này giúp tách camera target khỏi visual character, tránh việc camera bị phụ thuộc trực tiếp vào model.

---

# Navigation

Map sử dụng Unity AI Navigation / NavMesh.

Map prefab có thể chứa NavMesh đã được bake sẵn.

Khi map được load:

```text
Load Map
   |
   v
Check NavMesh
   |
   +-- Existing NavMesh
   |       |
   |       v
   |    Use it
   |
   +-- Need Bake
           |
           v
      Bake NavMesh
```

`LevelMapBootstrap` chịu trách nhiệm kích hoạt quá trình setup hoặc bake NavMesh sau khi map đã sẵn sàng.

---

# Win / Lose Flow

## Win

```text
All Wave Spawn Quota Reached
            |
            v
All Zombies Dead
            |
            v
WaveManager.Cleared
            |
            v
ZombieWarGameFlow
            |
            v
Win UI
```

Từ Win UI, người chơi có thể:

* Replay level hiện tại
* Chuyển sang level tiếp theo

## Lose

```text
Player Dies
    |
    v
PlayerHealth.Died
    |
    v
ZombieWarGameFlow
    |
    v
Lose UI
```

Người chơi có thể replay level hiện tại.

---

# Dependencies

| Package / Library         | Usage                                      |
| ------------------------- | ------------------------------------------ |
| `com.unity.addressables`  | Load và quản lý Addressable assets         |
| `com.unity.ai.navigation` | NavMesh và AI navigation                   |
| `com.unity.cinemachine`   | Camera follow                              |
| UniTask                   | Async loading và gameplay tasks            |
| DOTween                   | Tween và gameplay animation                |
| Joystick Pack             | Mobile movement input                      |
| LeanGUI                   | UI                                         |
| Odin Inspector            | Inspector và editor tooling                |
| Nexzap SDK                | Loading, Menu, Profile và Scene Management |

---

# Architecture Principles

Project được tổ chức dựa trên một số nguyên tắc chính.

## 1. Config, Save và Runtime là ba loại dữ liệu khác nhau

```text
Config
  └── ScriptableObject
      ├── LevelMapCatalog
      └── LevelWaveConfig

Save
  └── PlayerPrefs
      ├── ZW_SessionLevel
      └── currentLevel

Runtime
  ├── Current HP
  ├── Current Wave
  ├── Wave Timer
  ├── Active Zombies
  └── Runtime Map Instance
```

ScriptableObject dùng cho dữ liệu thiết kế.

PlayerPrefs dùng cho progress.

Runtime state chỉ tồn tại trong quá trình gameplay.

## 2. Một Gameplay Scene

Không tạo scene riêng cho từng level.

```text
Gameplay.unity
      |
      +-- Level 1 Map
      +-- Level 2 Map
      +-- Level 3 Map
      +-- ...
```

Level được thay đổi bằng cách load map Addressable tương ứng.

## 3. Ownership rõ ràng

```text
LevelMapBootstrap
    └── Map loading / releasing

WaveManager
    └── Zombie spawning / wave state

ZombieWarGameFlow
    └── Win / Lose / Replay / Next Level
```

Mỗi system chịu trách nhiệm cho một nhóm logic cụ thể, hạn chế việc một class quản lý quá nhiều lifecycle khác nhau.

## 4. Addressables phải có lifecycle rõ ràng

Asset được load hoặc instantiate thông qua Addressables phải có quá trình release tương ứng.

```text
Load / Instantiate
        |
        v
Use Asset
        |
        v
Release
```

`LevelMapLoader` chịu trách nhiệm quản lý lifecycle của Addressable map.

---

# Development Notes

Một số tài liệu kiến trúc cũ trong project có thể không còn phản ánh hoàn toàn code hiện tại.

Tài liệu:

```text
Assets/_Game/Docs/LevelFlow-Architecture.md
```

nên được xem như tài liệu tham khảo và cần đối chiếu với implementation hiện tại trước khi sử dụng.

---

# Build Requirements

Project yêu cầu đúng phiên bản:

```text
Unity 6000.0.72f1
```

Render Pipeline:

```text
URP 17.0.4
```

Platform chính:

```text
Android / Mobile
```

Android build settings và Addressables configuration đã được chuẩn bị sẵn trong project.

---

# Repository

GitHub repository:

`abc2003vvv/TEST-INDIEZ`

Project này hiện được sử dụng như một môi trường thử nghiệm và phát triển gameplay cho ZombieWar.
