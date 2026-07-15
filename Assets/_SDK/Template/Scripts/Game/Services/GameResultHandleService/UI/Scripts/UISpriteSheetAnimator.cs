using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UISpriteSheetAnimator : MonoBehaviour
{
    [SerializeField] private Image target;
    [SerializeField] private List<Sprite> frames;
    [SerializeField] private float fps = 18f;
    [SerializeField] private bool playOnEnable = true;

    private int _idx;
    private float _t;
    private bool _playing;

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    private void Update()
    {
        if (!_playing || target == null || frames == null || frames.Count == 0) return;

        _t += Time.unscaledDeltaTime;
        float spf = 1f / Mathf.Max(1f, fps);

        while (_t >= spf)
        {
            _t -= spf;
            _idx = (_idx + 1) % frames.Count;
            target.sprite = frames[_idx];
        }
    }

    public void SetFrames(List<Sprite> newFrames)
    {
        frames = newFrames;
        _idx = 0;
        _t = 0;
        if (target != null && frames != null && frames.Count > 0)
            target.sprite = frames[0];
    }

    public void Play()
    {
        _playing = true;
        _idx = 0;
        _t = 0;
        if (target != null && frames != null && frames.Count > 0)
            target.sprite = frames[0];
    }

    public void Stop()
    {
        _playing = false;
    }
}