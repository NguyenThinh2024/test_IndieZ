using System;
using UnityEngine;

namespace ZombieWar.Player
{
    [Serializable]
    public sealed class PlayerCharacterAnimationConfig
    {
        [SerializeField] private string controllerAddress;
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";
        [SerializeField] private string shootTrigger = "Shoot";
        [SerializeField] private string hitTrigger = "Hit";
        [SerializeField] private string dieTrigger = "Die";

        public string ControllerAddress => controllerAddress;
        public string MoveSpeedParameter => moveSpeedParameter;
        public string ShootTrigger => shootTrigger;
        public string HitTrigger => hitTrigger;
        public string DieTrigger => dieTrigger;
    }
}
