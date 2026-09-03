using System;
using Thinh.Base.Level;

namespace Thinh.Base.Gameplay
{
    public interface ILevelRuntime
    {
        event Action LevelLoaded;

        void LoadCurrentLevel();
        void ReloadCurrentLevel();
        void SetCurrentLevel(int levelIndex);
        int GetCurrentLevelIndexPublic();
        LevelDifficultyType GetCurrentLevelDifficultyType();
    }
}
