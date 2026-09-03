using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Thinh.Base
{
    public enum SoundName
    {
        GameMusic = -1,       // Nhạc nền khi vào gameplay

        UI_ButtonClick = 0,   // Bấm button thường: play, next, retry, continue, close, back
        UI_PopupOpen = 1,     // Mở popup: setting, shop, win, lose, reward, pause
        UI_PopupClose = 2,    // Đóng popup hoặc dismiss popup
        UI_LevelStart = 3,    // Bắt đầu level, countdown go, bắt đầu gameplay
        UI_LevelGoalShow = 4, // Hiện mục tiêu level / objective / target ở đầu màn
        UI_Progress = 5,      // Tăng progress bar, tăng %, hoàn thành 1 phần mục tiêu
        UI_CollectCoin = 6,   // Nhận coin, coin bay về HUD, cộng coin sau win
        UI_CollectGem = 7,    // Nhận gem, star, hoặc reward nhỏ có giá trị hơn coin
        UI_BoosterSelect = 8, // Chọn booster: hammer, undo, shuffle, hint
        UI_BoosterUse = 9,    // Xác nhận và dùng booster thành công
        UI_Invalid = 10,      // Thao tác sai: không đủ tiền, không dùng được, action bị chặn
        UI_Warning = 11,      // Cảnh báo: sắp thua, timer thấp, gần đầy slot
        UI_ClaimReward = 12,  // Bấm claim thưởng, nhận thưởng sau win, daily reward
        UI_RewardAds = 13,    // Nhận thưởng từ ads: revive, x2 reward, bonus reward
        UI_ChestOpen = 14,    // Mở chest, mở hộp quà, reveal phần thưởng
        UI_Unlock = 15,       // Unlock feature, unlock item, unlock mechanic mới
        UI_LevelComplete = 16,// Level vừa complete logic, chuẩn bị chuyển sang win flow
        UI_Win = 17,          // Popup thắng, win moment, màn hình victory
        UI_Lose = 18,         // Popup thua, lose moment, fail screen
        UI_Revive = 19,       // Revive thành công: thêm time, thêm move, tiếp tục chơi
        UI_Clock = 20,        // Tick cảnh báo khi timer xuống thấp

        // Gameplay sounds
        GP_CubeTap = 21,          // Cube tapped/selected from formation
        GP_CubeImpactSlot = 22,   // Cube lands on topic slot
        GP_CubeImpactQueue = 23,  // Cube lands on queue
        GP_TopicComplete = 24,    // Topic slot completed (all cubes collected)
        GP_Shuffle = 25,          // Formation shuffle/transform starts
        GP_BombExplode = 26,      // Bomb cube explodes (lose trigger)
        GP_HiddenReveal = 27,     // Hidden cube revealed (counter reached 0)
        GP_IceCrack = 28,         // Ice cube cracked (counter decremented)
        GP_BombTick = 29,         // Bomb countdown tick (1s interval)
        GP_CubeTapFail = 30,      // Cube tap rejected / release failed
        GP_CubeCutPixel = 31,     // Saw cuts a matching pixel cube
        GP_CutterCutSaw = 32      // Cutter slices and spawns saw blades
    }

    [System.Serializable]
    public class SoundAsset
    {
        public SoundName soundName;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [CreateAssetMenu(menuName = "Thinh/SoundAssetConfigs", fileName = "SoundAssetConfigs")]
    public class SoundAssetConfigs: ScriptableObject
    {
        public List<SoundAsset> musics;
        public List<SoundAsset> sounds;

        public SoundAsset GetSound(SoundName soundName)
        {
            return sounds.Find(x=>x.soundName == soundName);
        }

        public SoundAsset GetMusic(SoundName soundName) 
        {
            return musics.Find(x => x.soundName == soundName);
        }
    }
}

