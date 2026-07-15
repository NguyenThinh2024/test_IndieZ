using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public sealed class CenterImageFourWayStarParticle : MonoBehaviour
{
    private const int DirectionCount = 8;

    [Header("Sprites")]
    [SerializeField] private Sprite centerSprite;
    [SerializeField] private Sprite starSprite;

    [Header("Particle")]
    [SerializeField, Min(0.01f)] private float duration = 0.35f;
    [SerializeField, Min(0.01f)] private float centerSize = 0.34f;
    [SerializeField, Min(0.01f)] private float starSize = 0.09f;
    [SerializeField, Min(0f)] private float starSpeed = 1.1f;
    [SerializeField, Min(1)] private int starCountPerDirection = 3;
    [SerializeField] private Color starColorA = Color.white;
    [SerializeField] private Color starColorB = new Color(1f, 0.85f, 0.18f, 1f);

    [Header("Runtime")]
    [SerializeField] private ParticleSystem centerParticle;
    [SerializeField] private ParticleSystem[] starParticles = Array.Empty<ParticleSystem>();

    private static Sprite cachedStarSprite;

    public void SetCenterSprite(Sprite sprite)
    {
        centerSprite = sprite;
        Configure();
    }

    private void Awake()
    {
        Configure();
    }

    [ContextMenu("Configure")]
    public void Configure()
    {
        EnsureParticles();
        ConfigureCenterParticle();
        ConfigureStarParticles();
    }

    [ContextMenu("Play")]
    public void Play()
    {
        Configure();
        gameObject.SetActive(true);
        ResetParticle(centerParticle);
        centerParticle.Play(true);

        for (int i = 0; i < starParticles.Length; i++)
        {
            ResetParticle(starParticles[i]);
            starParticles[i].Play(true);
        }
    }

    [ContextMenu("Reset Particle")]
    public void ResetParticle()
    {
        ResetParticle(centerParticle);
        if (starParticles == null)
        {
            return;
        }

        for (int i = 0; i < starParticles.Length; i++)
        {
            ResetParticle(starParticles[i]);
        }
    }

    private void EnsureParticles()
    {
        if (centerParticle == null)
        {
            centerParticle = GetComponent<ParticleSystem>();
        }

        if (starParticles != null && starParticles.Length == DirectionCount)
        {
            return;
        }

        starParticles = new ParticleSystem[DirectionCount];
        for (int i = 0; i < DirectionCount; i++)
        {
            starParticles[i] = CreateChildParticle($"StarDirection_{i + 1}", transform);
        }
    }

    private void ConfigureCenterParticle()
    {
        ParticleSystem.MainModule mainModule = centerParticle.main;
        mainModule.duration = duration;
        mainModule.loop = false;
        mainModule.playOnAwake = false;
        mainModule.startDelay = 0f;
        mainModule.startLifetime = duration;
        mainModule.startSpeed = 0f;
        mainModule.startSize = centerSize;
        mainModule.startColor = Color.white;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
        mainModule.scalingMode = ParticleSystemScalingMode.Hierarchy;
        mainModule.maxParticles = 1;
        mainModule.stopAction = ParticleSystemStopAction.Disable;

        ParticleSystem.EmissionModule emissionModule = centerParticle.emission;
        emissionModule.rateOverTime = 0f;
        emissionModule.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)1, (short)1)
        });

        ParticleSystem.SizeOverLifetimeModule sizeModule = centerParticle.sizeOverLifetime;
        sizeModule.enabled = true;
        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 0.85f)));

        ParticleSystem.ColorOverLifetimeModule colorModule = centerParticle.colorOverLifetime;
        colorModule.enabled = true;
        colorModule.color = CreateFadeGradient(Color.white, Color.white);

        ConfigureSpriteParticleRenderer(centerParticle, centerSprite, 80);
    }

    private void ConfigureStarParticles()
    {
        for (int i = 0; i < DirectionCount; i++)
        {
            ParticleSystem starParticle = starParticles[i];
            float angle = 360f / DirectionCount * i;
            starParticle.transform.localPosition = Vector3.zero;
            starParticle.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            ConfigureStarParticle(starParticle, i);
        }
    }

    private void ConfigureStarParticle(ParticleSystem starParticle, int index)
    {
        ParticleSystem.MainModule mainModule = starParticle.main;
        mainModule.duration = duration;
        mainModule.loop = false;
        mainModule.playOnAwake = false;
        mainModule.startDelay = 0f;
        mainModule.startLifetime = new ParticleSystem.MinMaxCurve(duration * 0.5f, duration);
        mainModule.startSpeed = new ParticleSystem.MinMaxCurve(starSpeed * 0.65f, starSpeed);
        mainModule.startSize = new ParticleSystem.MinMaxCurve(starSize * 0.75f, starSize);
        mainModule.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        mainModule.startColor = index % 2 == 0 ? starColorA : starColorB;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
        mainModule.scalingMode = ParticleSystemScalingMode.Hierarchy;
        mainModule.maxParticles = Mathf.Max(1, starCountPerDirection);
        mainModule.stopAction = ParticleSystemStopAction.Disable;

        ParticleSystem.EmissionModule emissionModule = starParticle.emission;
        emissionModule.rateOverTime = 0f;
        short burstCount = (short)Mathf.Clamp(starCountPerDirection, 1, short.MaxValue);
        emissionModule.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, burstCount, burstCount)
        });

        ParticleSystem.ShapeModule shapeModule = starParticle.shape;
        shapeModule.enabled = true;
        shapeModule.shapeType = ParticleSystemShapeType.Cone;
        shapeModule.angle = 8f;
        shapeModule.radius = 0.02f;
        shapeModule.rotation = new Vector3(0f, 90f, 0f);

        ParticleSystem.ColorOverLifetimeModule colorModule = starParticle.colorOverLifetime;
        colorModule.enabled = true;
        colorModule.color = CreateFadeGradient(index % 2 == 0 ? starColorA : starColorB, Color.white);

        ParticleSystem.SizeOverLifetimeModule sizeModule = starParticle.sizeOverLifetime;
        sizeModule.enabled = true;
        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.65f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f)));

        ConfigureSpriteParticleRenderer(starParticle, ResolveStarSprite(), 79);
    }

    private Sprite ResolveStarSprite()
    {
        return starSprite != null ? starSprite : GetDefaultStarSprite();
    }

    private static Sprite GetDefaultStarSprite()
    {
        if (cachedStarSprite != null)
        {
            return cachedStarSprite;
        }

        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        texture.name = "RuntimeFourWayStar";
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, IsStarPixel(x, y) ? Color.white : clear);
            }
        }

        texture.Apply();
        cachedStarSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return cachedStarSprite;
    }

    private static bool IsStarPixel(int x, int y)
    {
        int centerX = 15;
        int centerY = 15;
        int dx = Mathf.Abs(x - centerX);
        int dy = Mathf.Abs(y - centerY);
        return dx + dy <= 8 || dx <= 2 && dy <= 13 || dy <= 2 && dx <= 13;
    }

    private static void ConfigureSpriteParticleRenderer(ParticleSystem particleSystem, Sprite sprite, int sortingOrder)
    {
        ParticleSystem.TextureSheetAnimationModule textureSheet = particleSystem.textureSheetAnimation;
        textureSheet.enabled = sprite != null;
        textureSheet.mode = ParticleSystemAnimationMode.Sprites;
        if (sprite != null)
        {
            while (textureSheet.spriteCount > 0)
            {
                textureSheet.RemoveSprite(0);
            }

            textureSheet.AddSprite(sprite);
        }

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = sortingOrder;
        renderer.minParticleSize = 0.01f;
        renderer.maxParticleSize = 0.5f;
    }

    private static Gradient CreateFadeGradient(Color startColor, Color endColor)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(endColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private static ParticleSystem CreateChildParticle(string particleName, Transform parent)
    {
        GameObject particleObject = new GameObject(particleName);
        particleObject.transform.SetParent(parent, false);
        return particleObject.AddComponent<ParticleSystem>();
    }

    private static void ResetParticle(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
        {
            return;
        }

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
