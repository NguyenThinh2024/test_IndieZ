using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OnboardingConfig", menuName = "Game/Onboarding/Config")]
public class OnboardingConfigSO : ScriptableObject
{
    public enum FocusMode
    {
        None = 0,
        TargetTransform = 1,
        WorldArea = 2,
    }

    public enum AdvanceMode
    {
        TapResolvedTarget = 0,
        Manual = 1,
        None = 2,
    }

    public enum TargetSource
    {
        None = 0,
        GridPosition = 1,
        RuntimeTargetId = 2,
        WorldPosition = 3,
    }

    public enum FocusShape
    {
        Circle = 0,
        Rectangle = 1,
    }

    [Serializable]
    public class StepData
    {
        [SerializeField] private string stepId = "Step";
        [SerializeField] private string title = "Onboarding Step";
        [SerializeField, TextArea(2, 5)] private string description = string.Empty;
        [SerializeField] private FocusMode focusMode = FocusMode.None;
        [SerializeField] private AdvanceMode advanceMode = AdvanceMode.TapResolvedTarget;
        [SerializeField] private TargetSource targetSource = TargetSource.GridPosition;
        [SerializeField] private Vector2Int gridPosition = Vector2Int.zero;
        [SerializeField] private string runtimeTargetId = string.Empty;
        [SerializeField] private Vector3 worldPosition = Vector3.zero;
        [SerializeField] private bool trackTargetEveryFrame = true;
        [SerializeField] private Vector3 focusWorldOffset = Vector3.zero;
        [SerializeField] private Vector3 focusWorldSize = Vector3.one;
        [SerializeField, Min(0f)] private float delayBeforeShowing;
        [SerializeField] private string actionButtonText = "Next";

        public string StepId => stepId;
        public string Title => title;
        public string Description => description;
        public FocusMode Focus => focusMode;
        public AdvanceMode Advance => advanceMode;
        public TargetSource Target => targetSource;
        public Vector2Int GridPosition => gridPosition;
        public string RuntimeTargetId => runtimeTargetId;
        public Vector3 WorldPosition => worldPosition;
        public bool TrackTargetEveryFrame => trackTargetEveryFrame;
        public Vector3 FocusWorldOffset => focusWorldOffset;
        public Vector3 WorldFocusSize => focusWorldSize;
        public float DelayBeforeShowing => Mathf.Max(0f, delayBeforeShowing);
        public string ActionButtonText => string.IsNullOrWhiteSpace(actionButtonText) ? "Next" : actionButtonText.Trim();
    }

    [Serializable]
    public class LevelOnboarding
    {
        [SerializeField] private int levelIndex;
        [SerializeField] private List<StepData> steps = new List<StepData>();

        public int LevelIndex => Mathf.Max(0, levelIndex);
        public IReadOnlyList<StepData> Steps => steps;
        public int StepCount => steps != null ? steps.Count : 0;

        public bool TryGetStep(int stepIndex, out StepData step)
        {
            if (steps == null || stepIndex < 0 || stepIndex >= steps.Count)
            {
                step = null;
                return false;
            }

            step = steps[stepIndex];
            return step != null;
        }
    }

    [SerializeField] private List<LevelOnboarding> levels = new List<LevelOnboarding>();

    public IReadOnlyList<LevelOnboarding> Levels => levels;

    public bool TryGetLevel(int levelIndex, out LevelOnboarding level)
    {
        if (levels != null)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                LevelOnboarding candidate = levels[i];
                if (candidate == null || candidate.LevelIndex != Mathf.Max(0, levelIndex))
                {
                    continue;
                }

                level = candidate;
                return true;
            }
        }

        level = null;
        return false;
    }
}
