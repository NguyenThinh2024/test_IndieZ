using UnityEngine;

namespace Nexzap.Template
{
    /// <summary>
    /// Game configuration - Adjust these values to tune your game
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Template/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Levels")]
        [SerializeField] public int levelReward = 40;
        [SerializeField] public int levelRevice = 900;

        [Header("Life - Coin")]
        [SerializeField] public int refillLifeTime = 30 * 60; // 30 minutes in seconds
        [SerializeField] public int maxLife = 5;
        [SerializeField] public int refillLifePrice = 900;
        [SerializeField] public int startCoin = 900;
        [SerializeField] public int freeGetCoin = 100;
    }
}
