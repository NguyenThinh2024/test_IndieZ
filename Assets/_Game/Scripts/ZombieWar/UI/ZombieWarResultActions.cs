using System;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Level;

namespace ZombieWar.UI
{
    /// <summary>
    /// Win/Lose action buttons. Owned UI only — calls into ZombieWarGameFlow.
    /// </summary>
    public sealed class ZombieWarResultActions : MonoBehaviour
    {
        [SerializeField] private ZombieWarGameFlow gameFlow;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private bool hideNextWhenNoLevel = true;

        private void OnEnable()
        {
            if (replayButton != null)
            {
                replayButton.onClick.AddListener(OnReplayClicked);
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);
                refreshNextVisibility();
            }
        }

        private void OnDisable()
        {
            if (replayButton != null)
            {
                replayButton.onClick.RemoveListener(OnReplayClicked);
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
            }
        }

        public void Bind(ZombieWarGameFlow flow, Button replay, Button nextLevel)
        {
            gameFlow = flow;
            replayButton = replay;
            nextLevelButton = nextLevel;
            refreshNextVisibility();
        }

        private void refreshNextVisibility()
        {
            if (nextLevelButton == null || !hideNextWhenNoLevel)
            {
                return;
            }

            bool hasNext = gameFlow != null && gameFlow.HasNextLevel;
            nextLevelButton.gameObject.SetActive(hasNext);
        }

        private void OnReplayClicked()
        {
            gameFlow?.Replay();
        }

        private void OnNextLevelClicked()
        {
            gameFlow?.GoToNextLevel();
        }
    }
}
