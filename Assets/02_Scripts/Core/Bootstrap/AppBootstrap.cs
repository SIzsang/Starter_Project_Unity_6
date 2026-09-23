using System;
using UnityEngine;

namespace StarterProject
{
    /// <summary>
    /// Boot 씬의 진입점입니다. 씬보다 오래 살아야 하는 작업은 직접 소유하지 않고
    /// <see cref="AppRoot"/>를 확보한 뒤 <see cref="AppRoot.Begin"/>에 시작을 위임합니다.
    /// </summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        [SerializeField] private AppConfig config;

#if UNITY_EDITOR
        /// <summary>Editor Play가 Boot 초기화 전에 개발용 설정을 주입하는 지점입니다.</summary>
        public static Action<AppRoot> EditorPrepareRoot;
#endif

        /// <summary>
        /// Boot 씬이 시작되면 영속 루트를 찾거나 생성하고 직렬화된 <see cref="AppConfig"/>로
        /// 초기화를 요청합니다. 이미 준비된 루트가 있으면 Title 복귀 요청으로 처리됩니다.
        /// </summary>
        private void Start()
        {
            var root = AppRoot.Instance;
            if (root == null)
                root = new GameObject("AppRoot").AddComponent<AppRoot>();
#if UNITY_EDITOR
            if (root.State == AppState.NotStarted)
                EditorPrepareRoot?.Invoke(root);
#endif
            root.Begin(config);
        }
    }
}
