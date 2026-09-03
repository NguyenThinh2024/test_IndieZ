using UnityEngine.Events;

namespace Thinh.Base
{
    public sealed class GameManager : MonoSingleton<GameManager>
    {
        public readonly UnityEvent OnInited = new();

        public bool IsInited { get; private set; }

        public override void Init()
        {
            base.Init();
            MarkInited();
        }

        public void MarkInited()
        {
            if (IsInited)
            {
                return;
            }

            IsInited = true;
            OnInited.Invoke();
        }
    }
}
