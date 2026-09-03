using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Thinh.Base.Gameplay
{
    public class GameController : MonoSingleton<GameController>
    {
        [SerializeField] private List<GameplayServiceBehaviour> _services;

        public GameplayServices Services { get; private set; }

        private readonly List<IGameplayService> _enabled = new();
        private bool _started;

        protected override void Awake()
        {
            base.Awake();

            Services = new GameplayServices();
            _enabled.Clear();

            // 1) Register enabled services
            for (int i = 0; i < _services.Count; i++)
            {
                var svc = _services[i];
                if (svc == null || !svc.IsEnabled) continue;

                Services.Add(svc);
                _enabled.Add(svc);
            }

            // 2) Resolve deps
            for (int i = 0; i < _enabled.Count; i++)
                _enabled[i].OnRegister(Services);
        }

        private void Start()
        {
            _started = true;
            for (int i = 0; i < _enabled.Count; i++)
                _enabled[i].OnStart();
        }

        private void Update()
        {
            if (!_started) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < _enabled.Count; i++)
                _enabled[i].Tick(dt);
        }

        protected override void OnDestroy()
        {
            for (int i = _enabled.Count - 1; i >= 0; i--)
            {
                try { _enabled[i].OnStop(); }
                catch (System.Exception e) { Debug.LogException(e); }
            }
            _started = false;

            base.OnDestroy(); 
        }
    }
}