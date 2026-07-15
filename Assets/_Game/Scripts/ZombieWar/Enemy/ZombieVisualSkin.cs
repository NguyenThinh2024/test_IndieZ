using UnityEngine;

namespace ZombieWar.Enemy
{
    /// <summary>
    /// Applies a shared skin material to zombie renderers (walk / run variants).
    /// </summary>
    public sealed class ZombieVisualSkin : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
            {
                cacheRenderers();
            }
        }

        public void ApplySkin(Material skinMaterial)
        {
            if (skinMaterial == null)
            {
                return;
            }

            if (renderers == null || renderers.Length == 0)
            {
                cacheRenderers();
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.sharedMaterial = skinMaterial;
            }
        }

        private void cacheRenderers()
        {
            Renderer[] all = GetComponentsInChildren<Renderer>(true);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] is SkinnedMeshRenderer || all[i] is MeshRenderer)
                {
                    count++;
                }
            }

            renderers = new Renderer[count];
            int write = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] is SkinnedMeshRenderer || all[i] is MeshRenderer)
                {
                    renderers[write++] = all[i];
                }
            }
        }
    }
}
