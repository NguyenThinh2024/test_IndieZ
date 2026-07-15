using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ZombieWar.Enemy
{
    /// <summary>
    /// Living zombies keep URP Lit materials. On death, swaps to dissolve shader instances then fades out.
    /// </summary>
    public sealed class ZombieDissolve : MonoBehaviour
    {
        private static readonly int DissolveAmountHash = Shader.PropertyToID("_DissolveAmount");
        private static readonly int BaseMapHash = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorHash = Shader.PropertyToID("_BaseColor");

        private static Shader dissolveShader;

        [SerializeField] private ZombieHealth health;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private float dissolveDuration = 0.7f;

        private MaterialPropertyBlock propertyBlock;
        private Material[][] originalMaterials;
        private Material[][] dissolveMaterials;
        private CancellationTokenSource dissolveCts;
        private bool usingDissolveMaterials;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            cacheOriginalMaterials();
        }

        private void OnEnable()
        {
            restoreOriginalMaterials();
            SetDissolve(0f);
            if (health != null)
            {
                health.Died += StartDissolve;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= StartDissolve;
            }

            cancelDissolve();
            restoreOriginalMaterials();
        }

        public void OnSpawn()
        {
            cancelDissolve();
            restoreOriginalMaterials();
            SetDissolve(0f);
        }

        public void OnDespawn()
        {
            cancelDissolve();
            restoreOriginalMaterials();
            SetDissolve(0f);
        }

        private void StartDissolve()
        {
            cancelDissolve();
            applyDissolveMaterials();
            dissolveCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            dissolveAsync(dissolveCts.Token).Forget();
        }

        private async UniTask dissolveAsync(CancellationToken cancellationToken)
        {
            float elapsed = 0f;
            while (elapsed < dissolveDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                elapsed += Time.deltaTime;
                SetDissolve(Mathf.Clamp01(elapsed / dissolveDuration));
            }

            SetDissolve(1f);
        }

        private void cancelDissolve()
        {
            if (dissolveCts == null)
            {
                return;
            }

            dissolveCts.Cancel();
            dissolveCts.Dispose();
            dissolveCts = null;
        }

        private void cacheOriginalMaterials()
        {
            if (renderers == null)
            {
                originalMaterials = null;
                return;
            }

            originalMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                originalMaterials[i] = targetRenderer != null ? targetRenderer.sharedMaterials : null;
            }
        }

        private void applyDissolveMaterials()
        {
            if (renderers == null)
            {
                return;
            }

            if (dissolveShader == null)
            {
                dissolveShader = Shader.Find("Custom/URP/HiddenCubeDissolve");
            }

            if (dissolveShader == null)
            {
                return;
            }

            if (originalMaterials == null)
            {
                cacheOriginalMaterials();
            }

            dissolveMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                Material[] source = originalMaterials != null && i < originalMaterials.Length
                    ? originalMaterials[i]
                    : targetRenderer != null ? targetRenderer.sharedMaterials : null;

                if (targetRenderer == null || source == null || source.Length == 0)
                {
                    continue;
                }

                Material[] created = new Material[source.Length];
                for (int m = 0; m < source.Length; m++)
                {
                    created[m] = createDissolveMaterial(source[m]);
                }

                dissolveMaterials[i] = created;
                targetRenderer.materials = created;
            }

            usingDissolveMaterials = true;
            SetDissolve(0f);
        }

        private Material createDissolveMaterial(Material source)
        {
            Material dissolveMaterial = new Material(dissolveShader);
            if (source == null)
            {
                return dissolveMaterial;
            }

            if (source.HasProperty(BaseMapHash))
            {
                dissolveMaterial.SetTexture(BaseMapHash, source.GetTexture(BaseMapHash));
            }

            if (source.HasProperty(BaseColorHash))
            {
                dissolveMaterial.SetColor(BaseColorHash, source.GetColor(BaseColorHash));
            }
            else
            {
                dissolveMaterial.SetColor(BaseColorHash, Color.white);
            }

            dissolveMaterial.SetFloat(DissolveAmountHash, 0f);
            dissolveMaterial.SetFloat("_ShadowStrength", 0.35f);
            dissolveMaterial.SetColor("_ShadowColor", new Color(0.45f, 0.45f, 0.55f, 1f));
            return dissolveMaterial;
        }

        private void restoreOriginalMaterials()
        {
            if (!usingDissolveMaterials || renderers == null || originalMaterials == null)
            {
                usingDissolveMaterials = false;
                return;
            }

            destroyDissolveMaterials();

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null || originalMaterials[i] == null)
                {
                    continue;
                }

                targetRenderer.sharedMaterials = originalMaterials[i];
                targetRenderer.SetPropertyBlock(null);
            }

            usingDissolveMaterials = false;
        }

        private void destroyDissolveMaterials()
        {
            if (dissolveMaterials == null)
            {
                return;
            }

            for (int i = 0; i < dissolveMaterials.Length; i++)
            {
                Material[] mats = dissolveMaterials[i];
                if (mats == null)
                {
                    continue;
                }

                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] != null)
                    {
                        Destroy(mats[m]);
                    }
                }
            }

            dissolveMaterials = null;
        }

        private void SetDissolve(float value)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(DissolveAmountHash, value);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void OnDestroy()
        {
            destroyDissolveMaterials();
        }
    }
}
