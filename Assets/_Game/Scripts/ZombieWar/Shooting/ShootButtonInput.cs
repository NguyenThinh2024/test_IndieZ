using UnityEngine;
using UnityEngine.EventSystems;

namespace ZombieWar.Shooting
{
    public sealed class ShootButtonInput : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private PlayerSimpleShooter shooter;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (shooter != null)
            {
                shooter.Shoot();
            }
        }
    }
}
