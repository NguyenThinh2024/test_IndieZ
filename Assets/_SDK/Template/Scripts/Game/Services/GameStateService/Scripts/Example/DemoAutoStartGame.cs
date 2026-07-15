using System.Collections;
using UnityEngine;


namespace Nexzap.Base.Gameplay.Example
{
    public class DemoAutoStartGame : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1f);

            AudioController.Instance.PlaySound(SoundName.UI_LevelStart);
            GameController.Instance.Services.Get<GameStateService>().Play();
        }
    }
}
