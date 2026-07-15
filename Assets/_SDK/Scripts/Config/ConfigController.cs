using Nexzap.Base;
using UnityEngine;

namespace Nexzap.Template
{
    /// <summary>
    /// Manager for all game configurations
    /// </summary>
    public class ConfigController : MonoSingleton<ConfigController>
    {
        [SerializeField] private GameConfig gameConfig;
        public GameConfig GameConfig => gameConfig;

        public override void Init()
        {
            base.Init();
            
            if (gameConfig == null)
                Debug.LogError("GameConfig not assigned in ConfigController!");
        }
    }
}