using System.Collections.Generic;
using UnityEngine;

namespace ZombieWar.Core
{
    public sealed class DamageableHitboxResolver : MonoBehaviour
    {
        [SerializeField] private DamageableHitbox[] hitboxes;

        private readonly Dictionary<Collider, DamageableHitbox> colliderToHitbox = new Dictionary<Collider, DamageableHitbox>(128);

        private void Awake()
        {
            Rebuild();
        }

        private void OnEnable()
        {
            Rebuild();
        }

        public bool TryResolve(Collider collider, out DamageableHitbox hitbox)
        {
            hitbox = null;
            if (collider == null)
            {
                return false;
            }

            if (!colliderToHitbox.TryGetValue(collider, out hitbox))
            {
                return false;
            }

            return hitbox != null;
        }

        public void Rebuild()
        {
            colliderToHitbox.Clear();
            if (hitboxes == null)
            {
                return;
            }

            for (int i = 0; i < hitboxes.Length; i++)
            {
                Register(hitboxes[i]);
            }
        }

        public void Register(DamageableHitbox hitbox)
        {
            if (hitbox == null)
            {
                return;
            }

            Collider[] linkedColliders = hitbox.LinkedColliders;
            if (linkedColliders == null)
            {
                return;
            }

            for (int i = 0; i < linkedColliders.Length; i++)
            {
                Collider linkedCollider = linkedColliders[i];
                if (linkedCollider != null)
                {
                    colliderToHitbox[linkedCollider] = hitbox;
                }
            }
        }

        public void Unregister(DamageableHitbox hitbox)
        {
            if (hitbox == null)
            {
                return;
            }

            Collider[] linkedColliders = hitbox.LinkedColliders;
            if (linkedColliders == null)
            {
                return;
            }

            for (int i = 0; i < linkedColliders.Length; i++)
            {
                Collider linkedCollider = linkedColliders[i];
                if (linkedCollider != null && colliderToHitbox.TryGetValue(linkedCollider, out DamageableHitbox mappedHitbox) && mappedHitbox == hitbox)
                {
                    colliderToHitbox.Remove(linkedCollider);
                }
            }
        }
    }
}
