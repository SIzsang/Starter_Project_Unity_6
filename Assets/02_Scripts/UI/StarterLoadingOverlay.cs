using UnityEngine;
using UnityEngine.UI;

namespace StarterProject.UI
{
    /// <summary>
    /// AppRoot 아래에서 씬 교체 동안 유지되는 로딩 화면입니다.
    /// 표시 계층이 공개 상태를 구독하므로 Core는 UI를 참조하지 않습니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class StarterLoadingOverlay : MonoBehaviour
    {
        private const float DesignScale = 1.5f;
        private AppRoot appRoot;
        private Canvas overlayCanvas;
        private CanvasGroup canvasGroup;
        private GraphicRaycaster raycaster;
        [SerializeField] private Image progressTrack;
        [Tooltip("Filled 이미지는 작성한 배치를 유지합니다. 그 외 이미지는 Progress Track의 자식으로 두며 왼쪽부터 채워지는 막대로 배치합니다.")]
        [SerializeField] private Image progressFill;
        [SerializeField] private Text messageText;
        [SerializeField] private Text progressText;
        private int displayedPercent = -1;

        /// <summary>현재 로딩 화면을 표시하고 포인터 입력을 차단하는지 나타냅니다.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>가장 최근에 표시한 0..1 범위의 로딩 진행률입니다.</summary>
        public float Progress { get; private set; }

        /// <summary>현재 로딩 화면에 표시하는 단계 문구입니다.</summary>
        public string Message { get; private set; } = string.Empty;

        /// <summary>루트의 수명을 공유하는 오버레이를 한 번만 생성하고 기존 인스턴스를 재사용합니다.</summary>
        public static StarterLoadingOverlay EnsureCreated(AppRoot root, StarterLoadingOverlay prefab = null)
        {
            if (root == null) return null;
            var existing = root.GetComponentInChildren<StarterLoadingOverlay>(true);
            if (existing != null) return existing;

            if (prefab != null)
            {
                var customOverlay = Instantiate(prefab, root.transform, false);
                customOverlay.gameObject.SetActive(true);
                customOverlay.Initialize(root, true);
                return customOverlay;
            }

            var overlayObject = new GameObject("Loading Overlay", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            overlayObject.transform.SetParent(root.transform, false);
            var overlay = overlayObject.AddComponent<StarterLoadingOverlay>();
            overlay.Initialize(root, false);
            return overlay;
        }

        private void Initialize(AppRoot root, bool usesPrefab)
        {
            appRoot = root;
            overlayCanvas = GetComponent<Canvas>();
            overlayCanvas.sortingOrder = short.MaxValue;
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            raycaster = GetComponent<GraphicRaycaster>();

            if (messageText == null || progressTrack == null || progressFill == null || progressText == null)
            {
                if (usesPrefab)
                    StarterLog.Warning(LogCategory.UI, "Loading prefab needs Message, Progress Track, Progress Fill and Percentage references. Using default visuals.", this);
                CreateDefaultVisuals();
            }
            else if (usesPrefab)
            {
                // Custom artwork can omit its own raycast target; transitions still block all pointer input.
                var blocker = CreateImage(transform, "Input Blocker", Color.clear);
                Stretch(blocker.rectTransform);
                blocker.raycastTarget = true;
                blocker.transform.SetAsFirstSibling();
            }
            StarterCanvasLayout.EnsureConfigured(overlayCanvas);
            appRoot.StateChanged -= Refresh;
            appRoot.StateChanged += Refresh;
            Refresh();
        }

        private void CreateDefaultVisuals()
        {
            var background = CreateImage(transform, "Input Blocker", new Color(0.035f, 0.065f, 0.10f, 0.98f));
            Stretch(background.rectTransform);
            background.raycastTarget = true;

            messageText = CreateLabel("Loading Message", 26, new Vector2(0, 55), new Vector2(1050, 100));
            progressTrack = CreateImage(transform, "Progress Track", new Color(0.17f, 0.23f, 0.29f));
            Position(progressTrack.rectTransform, new Vector2(0, -25), new Vector2(640, 14));
            progressFill = CreateImage(progressTrack.transform, "Progress Fill", new Color(0.39f, 0.89f, 0.76f));
            Stretch(progressFill.rectTransform);
            progressText = CreateLabel("Loading Percentage", 18, new Vector2(0, -70), new Vector2(300, 40));
        }

        private void OnEnable()
        {
            if (appRoot == null) return;
            appRoot.StateChanged += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            if (overlayCanvas == null) return;
            IsVisible = appRoot != null && appRoot.State != AppState.Failed
                && (appRoot.State == AppState.Initializing || appRoot.IsTransitioning);
            overlayCanvas.enabled = IsVisible;
            canvasGroup.alpha = IsVisible ? 1 : 0;
            canvasGroup.blocksRaycasts = IsVisible;
            raycaster.enabled = IsVisible;
            Progress = appRoot != null ? Mathf.Clamp01(appRoot.LoadingProgress) : 0;
            // 초기화 단계에는 수치 진행률이 없으므로 씬 로드 중에만 막대와 백분율을 표시합니다.
            var showProgress = IsVisible && appRoot.IsTransitioning;
            progressTrack.gameObject.SetActive(showProgress);
            progressText.gameObject.SetActive(showProgress);
            if (showProgress)
            {
                UpdateProgressFill(progressFill, Progress);
                var percent = Mathf.RoundToInt(Progress * 100);
                if (displayedPercent != percent)
                {
                    displayedPercent = percent;
                    progressText.text = percent + "%";
                }
            }
            var message = appRoot != null ? appRoot.CurrentStep : string.Empty;
            if (Message != message)
            {
                Message = message;
                messageText.text = message;
            }
        }

        private void OnDisable()
        {
            if (appRoot != null) appRoot.StateChanged -= Refresh;
        }

        /// <summary>
        /// Filled는 프리팹의 방향·형태·배치를 유지하고 채움 비율만 갱신합니다.
        /// 일반 이미지는 부모 Track 전체 높이를 쓰는 왼쪽 정렬 막대로 정규화합니다.
        /// </summary>
        internal static void UpdateProgressFill(Image fill, float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (fill.type == Image.Type.Filled)
            {
                fill.fillAmount = progress;
                return;
            }

            var rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(progress, 1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Image CreateImage(Transform parent, string objectName, Color color)
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text CreateLabel(string objectName, int fontSize, Vector2 position, Vector2 size)
        {
            var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(transform, false);
            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = Mathf.RoundToInt(fontSize * DesignScale);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            Position(label.rectTransform, position, size);
            return label;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Position(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position * DesignScale;
            rect.sizeDelta = size * DesignScale;
        }
    }
}
