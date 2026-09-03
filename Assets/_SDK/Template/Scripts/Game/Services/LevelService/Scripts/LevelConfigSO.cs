using Thinh.Base.Level;
using System.Collections.Generic;
using Thinh.Base.Data;
using UnityEngine;

namespace Thinh.Base.Level
{

    [System.Serializable]
    public class LevelConfig
    {
        public int second; //thời gian của level
        public TextAsset json; //data level là json
        public LevelDifficultyType difficultyType = LevelDifficultyType.Normal;
    }


    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Thinh/Level Config", order = 1)]
    public class LevelConfigSO : ScriptableObject
    {
        public List<LevelConfig> levels = new List<LevelConfig>();
        public int levelStartLoop = 20; //loop lại từ level 20

        public LevelConfig GetLevelByIndex(int index)
        {
            if (levels == null || levels.Count == 0) return null;

            // Nếu trong range thì trả trực tiếp (clone với levelIndex đúng)
            if (index >= 0 && index < levels.Count)
            {
                var baseConfig = levels[index];
                var config = CloneLevelConfig(baseConfig);
                return config;
            }

            // Nếu vượt quá → tìm startLoopingIndex
            int startLoopingIndex = levelStartLoop - 1;

            // tổng số level có thể loop
            int loopCount = levels.Count - startLoopingIndex;
            if (loopCount <= 0)
            {
                var baseConfig = levels[levels.Count - 1];
                var config = CloneLevelConfig(baseConfig);
                return config;
            }

            // tính offset trong loop
            int relativeIndex = (index - startLoopingIndex) % loopCount;
            int mappedIndex = startLoopingIndex + relativeIndex;

            var loopConfig = levels[mappedIndex];
            var newConfig = CloneLevelConfig(loopConfig);
            return newConfig;
        }

        /// <summary>
        /// Hàm clone để tránh sửa nhầm data gốc trong ScriptableObject
        /// </summary>
        private LevelConfig CloneLevelConfig(LevelConfig source)
        {
            var clone = new LevelConfig
            {
                second = source.second,
                json = source.json,
                difficultyType = source.difficultyType,
            };
            return clone;
        }

    }
}

