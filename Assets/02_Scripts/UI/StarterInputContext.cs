using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace StarterProject.UI
{
    /// <summary>공유 InputActionAsset을 복제해 앱 수명 동안 Player/UI/Debug 맵을 전환합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class StarterInputContext : MonoBehaviour
    {
        private AppRoot root;
        private InputContextService input;
        private InputSystemUIInputModule module;
        private InputActionAsset originalAsset;
        private bool originalEnabled;
        public InputActionAsset RuntimeActions { get; private set; }

        public static StarterInputContext EnsureCreated(AppRoot root)
        {
            if (root == null) return null;
            var adapter = root.GetComponent<StarterInputContext>();
            if (adapter == null) adapter = root.gameObject.AddComponent<StarterInputContext>();
            adapter.root = root;
            return adapter;
        }

        private void Update()
        {
            if (root == null || root.Services == null || root.Services.IsDisposed) return;
            if (input == null)
            {
                input = root.Input;
                input.Changed += Apply;
            }
            var current = EventSystem.current != null ? EventSystem.current.GetComponent<InputSystemUIInputModule>() : null;
            if (current == module) return;
            RestoreModule();
            module = current;
            if (module == null || module.actionsAsset == null) return;
            originalAsset = module.actionsAsset;
            originalEnabled = module.enabled;
            if (RuntimeActions == null) RuntimeActions = Instantiate(originalAsset);
            module.actionsAsset = RuntimeActions;
            Apply(input.Current);
        }

        private void Apply(InputContext context)
        {
            if (RuntimeActions == null) return;
            // UI module OnEnable이 액션을 켜므로 먼저 모듈 상태를 적용하고 맵을 정규화합니다.
            if (module != null) module.enabled = context == InputContext.UI || context == InputContext.Debug;
            RuntimeActions.Disable();
            if (context == InputContext.Gameplay) RuntimeActions.FindActionMap("Player", false)?.Enable();
            if (context == InputContext.UI || context == InputContext.Debug) RuntimeActions.FindActionMap("UI", false)?.Enable();
            if (context == InputContext.Debug) RuntimeActions.FindActionMap("Debug", false)?.Enable();
        }

        private void RestoreModule()
        {
            if (module != null && originalAsset != null)
            {
                module.actionsAsset = originalAsset;
                module.enabled = originalEnabled;
            }
            module = null; originalAsset = null;
        }

        private void OnDestroy()
        {
            if (input != null) input.Changed -= Apply;
            RestoreModule();
            if (RuntimeActions != null) { RuntimeActions.Disable(); Destroy(RuntimeActions); }
        }
    }
}