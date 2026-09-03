using Thinh.Base;
using UnityEngine;

namespace Thinh.Template
{
    public class GamePlayMusic : MonoBehaviour
    {
        private void Start()
        {
            AudioController.Instance.PlayMusic(SoundName.GameMusic);
        }
    }
}

