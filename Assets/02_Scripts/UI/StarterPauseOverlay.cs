using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StarterProject.UI
{
    /// <summary>예제용 Pause/Resume UI입니다. 게임별 UI는 같은 AppRoot 상태 API를 사용합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class StarterPauseOverlay : MonoBehaviour
    {
        private AppRoot root;
        private Canvas canvas;
        private GameObject panel;
        private Button pauseButton;
        private Button resumeButton;
        private GameObject previousSelection;
        public bool IsVisible { get; private set; }

        public static StarterPauseOverlay EnsureCreated(AppRoot root)
        {
            if (root == null) return null;
            var existing = root.GetComponentInChildren<StarterPauseOverlay>(true);
            if (existing != null) return existing;
            var host = new GameObject("Pause Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            host.transform.SetParent(root.transform, false);
            var overlay = host.AddComponent<StarterPauseOverlay>();
            overlay.Initialize(root);
            return overlay;
        }

        private void Initialize(AppRoot app)
        {
            root = app;
            canvas = GetComponent<Canvas>();
            canvas.sortingOrder = 30000;
            pauseButton = CreateButton(transform, "Pause", new Vector2(-100, -40));
            var pauseRect = (RectTransform)pauseButton.transform;
            pauseRect.anchorMin = pauseRect.anchorMax = Vector2.one;
            pauseButton.onClick.AddListener(() => root.TryChangeGameState(GameState.Pause));
            panel = new GameObject("Pause Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 0.9f);
            resumeButton = CreateButton(panel.transform, "Resume", Vector2.zero);
            resumeButton.onClick.AddListener(() => root.TryChangeGameState(GameState.Gameplay));
            StarterCanvasLayout.EnsureConfigured(canvas);
            root.StateChanged += Refresh;
            Refresh();
        }

        private static Button CreateButton(Transform parent, string label, Vector2 position)
        {
            var host = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            host.transform.SetParent(parent, false);
            var rect = (RectTransform)host.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(170, 60);
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(host.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = label; text.fontSize = 24;
            text.color = Color.black; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return host.GetComponent<Button>();
        }

        private void Refresh()
        {
            var paused = root != null && root.CurrentGameState == GameState.Pause && root.State == AppState.Ready && !root.IsTransitioning;
            canvas.enabled = root != null && root.State == AppState.Ready && !root.IsTransitioning
                && (root.CurrentGameState == GameState.Gameplay || paused);
            GetComponent<GraphicRaycaster>().enabled = canvas.enabled;
            if (paused && !IsVisible)
            {
                previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
            }
            else if (!paused && IsVisible && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection);
            IsVisible = paused;
            panel.SetActive(paused);
            pauseButton.gameObject.SetActive(!paused);
        }

        private void OnDestroy() { if (root != null) root.StateChanged -= Refresh; }
    }
}