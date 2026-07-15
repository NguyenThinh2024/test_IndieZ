using UnityEngine;

namespace Nexzap.Base.Gameplay
{
    public interface IGameplayService
    {
        void OnRegister(GameplayServices services);
        void OnStart();
        void OnStop();
        void Tick(float dt);
    }

    /// Drag-drop reusable service. Disable => no register/start/tick.
    public abstract class GameplayServiceBehaviour : MonoBehaviour, IGameplayService
    {
        public bool IsEnabled => gameObject.activeSelf;

        public virtual void OnRegister(GameplayServices services) { }
        public virtual void OnStart() { }
        public virtual void OnStop() { }
        public virtual void Tick(float dt) { }
    }
}