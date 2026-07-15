using System;
using Nexzap.Base.Level;

namespace Nexzap.Base.Gameplay
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
