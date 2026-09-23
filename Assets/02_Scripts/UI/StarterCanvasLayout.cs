using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using DeviceScreen = UnityEngine.Device.Screen;

[assembly: InternalsVisibleTo("StarterProject.PlayModeTests")]

namespace StarterProject.UI
{
    /// <summary>
    /// 예제 화면의 전체 배경은 화면을 채우고, 조작 가능한 콘텐츠는 안전 영역 안에 맞춥니다.
    /// 1920x1080 Canvas를 기준으로 가로형 PC·모바일 콘텐츠를 안전 영역에 맞춥니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public sealed class StarterCanvasLayout : MonoBehaviour
    {
        public const int ReferenceWidth = 1920;
        public const int ReferenceHeight = 1080;
        private static readonly Vector2 ReferenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        private RectTransform safeAreaRoot;
        private RectTransform contentRoot;
        private Canvas canvas;
        private int lastWidth = -1;
        private int lastHeight = -1;
        private Rect lastSafeArea;
        private float lastScaleFactor = -1;

        internal RectTransform SafeAreaRoot => safeAreaRoot;
        /// <summary>실행 중 추가하는 조작 UI를 안전 영역 안에 배치할 부모입니다.</summary>
        public RectTransform ContentRoot => contentRoot;

        /// <summary>기존 씬의 Canvas에도 설정을 적용하고 레이아웃을 한 번만 연결합니다.</summary>
        public static StarterCanvasLayout EnsureConfigured(Canvas target)
        {
            if (target == null) return null;
            var layout = target.GetComponent<StarterCanvasLayout>();
            if (layout == null) layout = target.gameObject.AddComponent<StarterCanvasLayout>();
            layout.ConfigureCanvas();
            layout.EnsureHierarchy();
            layout.RefreshForCurrentScreen();
            return layout;
        }

        private void Awake()
        {
            ConfigureCanvas();
            EnsureHierarchy();
        }

        private void OnEnable() => RefreshForCurrentScreen();

        private void LateUpdate() => RefreshForCurrentScreen();

        private void ConfigureCanvas()
        {
            canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void EnsureHierarchy()
        {
            if (safeAreaRoot != null && contentRoot != null) return;
            var safeArea = new GameObject("Safe Area", typeof(RectTransform));
            safeArea.transform.SetParent(transform, false);
            safeAreaRoot = safeArea.GetComponent<RectTransform>();
            safeAreaRoot.anchorMin = Vector2.zero;
            safeAreaRoot.anchorMax = Vector2.one;
            safeAreaRoot.offsetMin = safeAreaRoot.offsetMax = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(safeAreaRoot, false);
            contentRoot = content.GetComponent<RectTransform>();
            contentRoot.anchorMin = contentRoot.anchorMax = contentRoot.pivot = new Vector2(0.5f, 0.5f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = ReferenceResolution;

            // 화면 전체로 늘어난 배경·입력 차단막은 안전 영역 바깥까지 덮습니다.
            // 나머지는 기존 좌표와 직렬화된 버튼 참조를 보존하며 콘텐츠로 옮깁니다.
            for (var i = transform.childCount - 2; i >= 0; i--)
            {
                var child = transform.GetChild(i) as RectTransform;
                if (child == null || IsFullScreen(child)) continue;
                child.SetParent(contentRoot, false);
                child.SetAsFirstSibling();
            }
        }

        private static bool IsFullScreen(RectTransform rect) => rect.anchorMin == Vector2.zero
            && rect.anchorMax == Vector2.one && rect.offsetMin == Vector2.zero
            && rect.offsetMax == Vector2.zero;

        private void RefreshForCurrentScreen()
        {
            if (canvas == null || safeAreaRoot == null || contentRoot == null) return;
            ApplyLayout(DeviceScreen.width, DeviceScreen.height, DeviceScreen.safeArea, canvas.scaleFactor);
        }

        internal void ApplyLayout(int width, int height, Rect safeArea, float canvasScaleFactor)
        {
            if (width <= 0 || height <= 0 || safeArea.width <= 0 || safeArea.height <= 0
                || canvasScaleFactor <= 0) return;
            if (width == lastWidth && height == lastHeight && safeArea == lastSafeArea
                && Mathf.Approximately(canvasScaleFactor, lastScaleFactor)) return;
            lastWidth = width;
            lastHeight = height;
            lastSafeArea = safeArea;
            lastScaleFactor = canvasScaleFactor;

            safeAreaRoot.anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            safeAreaRoot.anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
            safeAreaRoot.offsetMin = safeAreaRoot.offsetMax = Vector2.zero;

            var fit = Mathf.Min(1f, safeArea.width / (ReferenceResolution.x * canvasScaleFactor),
                safeArea.height / (ReferenceResolution.y * canvasScaleFactor));
            contentRoot.localScale = Vector3.one * fit;
        }
    }
}
