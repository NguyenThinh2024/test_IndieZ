using Nexzap.Base;
using UnityEngine;

namespace Nexzap.Template
{
    public class GamePlayMusic : MonoBehaviour
    {
        private void Start()
        {
            AudioController.Instance.PlayMusic(SoundName.GameMusic);
        }
    }
}

