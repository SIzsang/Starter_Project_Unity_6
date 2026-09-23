using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace StarterProject.UI
{
    /// <summary><see cref="StarterScreen"/>이 연결된 씬의 역할을 구분합니다.</summary>
    public enum StarterScreenKind
    {
        /// <summary>초기화 상태만 표시하는 Boot 화면입니다.</summary>
        Boot,

        /// <summary>Main 씬으로 이동하는 시작 버튼을 제공하는 Title 화면입니다.</summary>
        Title,

        /// <summary>Title 씬으로 돌아가는 버튼을 제공하는 Main 예제 화면입니다.</summary>
        Main
    }

    /// <summary>
    /// 씬별 상태 문구와 버튼을 <see cref="AppRoot"/>의 공개 상태에 맞춰 갱신하는 표시 계층입니다.
    /// 초기화나 전환 규칙을 직접 결정하지 않고 사용자 요청만 앱 루트에 전달합니다.
    /// </summary>
    public sealed class StarterScreen : MonoBehaviour
    {
        [SerializeField] private StarterScreenKind screen;
        [SerializeField] private Text status;
        [SerializeField] private Button actionButton;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button volumeButton;
        [SerializeField] private Button fullscreenButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button recoverGameButton;
        [SerializeField] private Button recoverSettingsButton;
        private bool isAwaitingOverwriteConfirmation;
        private AppRoot subscribedRoot;
        private InputAction cancelAction;
        private GameObject lastSelected;

        private bool hasDisplayedState;
        private AppRoot displayedRoot;
        private AppState displayedState;
        private bool displayedIsTransitioning;
        private string displayedStep;
        private string displayedFailure;
        private string displayedStorageMessage;

        /// <summary>버튼 리스너를 등록하고 현재 앱 상태를 즉시 반영합니다.</summary>
        private void OnEnable()
        {
            StarterCanvasLayout.EnsureConfigured(GetComponent<Canvas>());
            hasDisplayedState = false;
            if (actionButton != null)
                actionButton.onClick.AddListener(OnActionButtonClicked);
            if (secondaryButton != null) secondaryButton.onClick.AddListener(OnSecondaryButtonClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelButtonClicked);
            if (volumeButton != null) volumeButton.onClick.AddListener(OnVolumeButtonClicked);
            if (fullscreenButton != null) fullscreenButton.onClick.AddListener(OnFullscreenButtonClicked);
            if (languageButton != null) languageButton.onClick.AddListener(OnLanguageButtonClicked);
            if (recoverGameButton != null) recoverGameButton.onClick.AddListener(OnRecoverGameButtonClicked);
            if (recoverSettingsButton != null) recoverSettingsButton.onClick.AddListener(OnRecoverSettingsButtonClicked);
            Refresh();
            BindCancelAction();
        }

        /// <summary>늦게 생성되는 루트·입력 모듈을 연결하고 사라진 UI 선택을 복원합니다.</summary>
        private void Update()
        {
            Refresh();
            BindCancelAction();
            RestoreSelection();
        }

        private void BindRoot(AppRoot appRoot)
        {
            if (ReferenceEquals(subscribedRoot, appRoot)) return;
            if (subscribedRoot != null) subscribedRoot.StateChanged -= OnStateChanged;
            subscribedRoot = appRoot;
            if (subscribedRoot == null) return;
            StarterLoadingOverlay.EnsureCreated(subscribedRoot);
            subscribedRoot.StateChanged += OnStateChanged;
        }

        // 설정 저장 메시지가 이전과 같아도 새 설정 값을 즉시 표시합니다.
        private void OnStateChanged()
        {
            hasDisplayedState = false;
            Refresh();
        }

        /// <summary>
        /// 앱 루트의 준비·실패·전환 상태로 버튼 활성 여부와 사용자 메시지를 계산합니다.
        /// Boot를 거치지 않은 직접 실행에서는 버튼을 잠그고 시작 씬을 안내합니다.
        /// </summary>
        private void Refresh()
        {
            var appRoot = AppRoot.Instance;
            BindRoot(appRoot);
            var state = appRoot != null ? appRoot.State : AppState.NotStarted;
            var isTransitioning = appRoot != null && appRoot.IsTransitioning;
            var step = appRoot != null ? appRoot.CurrentStep : null;
            var failure = appRoot != null ? appRoot.Failure : null;
            var storageMessage = appRoot != null ? appRoot.StorageMessage : null;
            if (hasDisplayedState && ReferenceEquals(displayedRoot, appRoot) && displayedState == state
                && displayedIsTransitioning == isTransitioning && displayedStep == step && displayedFailure == failure
                && displayedStorageMessage == storageMessage)
                return;

            hasDisplayedState = true;
            displayedRoot = appRoot;
            displayedState = state;
            displayedIsTransitioning = isTransitioning;
            displayedStep = step;
            displayedFailure = failure;
            displayedStorageMessage = storageMessage;
            var isReady = appRoot != null && appRoot.State == AppState.Ready && !appRoot.IsTransitioning;
            if (actionButton != null)
                actionButton.interactable = isReady && screen != StarterScreenKind.Boot;
            if (secondaryButton != null)
            {
                secondaryButton.interactable = isReady && (screen == StarterScreenKind.Main
                    ? appRoot.Game.Current != null : appRoot.Game.CanContinue);
                SetLabel(secondaryButton, screen == StarterScreenKind.Title ? "Continue"
                    : isAwaitingOverwriteConfirmation ? "Confirm Replace Save" : "Save Game");
            }
            if (cancelButton != null) cancelButton.gameObject.SetActive(isAwaitingOverwriteConfirmation && isReady);
            if (recoverGameButton != null) recoverGameButton.gameObject.SetActive(isReady && appRoot.Game.Status == StorageStatus.Recovered);
            if (recoverSettingsButton != null) recoverSettingsButton.gameObject.SetActive(isReady && appRoot.Settings.Status == StorageStatus.Recovered);
            var canChangeSettings = isReady && appRoot.Settings.CanSave;
            if (volumeButton != null) volumeButton.interactable = canChangeSettings;
            if (fullscreenButton != null) fullscreenButton.interactable = canChangeSettings;
            if (languageButton != null) languageButton.interactable = canChangeSettings;
            if (isReady && screen != StarterScreenKind.Boot)
            {
                var settings = appRoot.Settings.Current;
                SetLabel(volumeButton, $"Volume: {settings.masterVolume:P0}");
                SetLabel(fullscreenButton, "Fullscreen: " + (settings.fullscreen ? "On" : "Off"));
                SetLabel(languageButton, "Language: " + settings.language);
            }

            string message;
            if (appRoot == null)
                message = screen == StarterScreenKind.Boot ? "Preparing..." : "Open 00_StartScene to begin.";
            else if (appRoot.State == AppState.Failed)
                message = Debug.isDebugBuild ? $"Could not start.\n{appRoot.Failure}" : "Could not start. Please restart the application.";
            else if (!isReady)
                message = appRoot.CurrentStep + "...";
            else
            {
                message = screen == StarterScreenKind.Main && appRoot.Game.Current != null
                    ? "Session: " + appRoot.Game.Current.SessionId.Substring(0, 8) : "Ready when you are.";
                if (!string.IsNullOrEmpty(appRoot.StorageMessage)) message += "\n" + appRoot.StorageMessage;
            }

            if (status != null && status.text != message)
                status.text = message;
            RestoreSelection();
        }

        private static bool CanInteract(AppRoot appRoot) => appRoot != null
            && appRoot.State == AppState.Ready && !appRoot.IsTransitioning;

        private bool OwnsSelection(GameObject selected)
        {
            return selected != null && ((actionButton != null && selected == actionButton.gameObject)
                || (secondaryButton != null && selected == secondaryButton.gameObject)
                || (cancelButton != null && selected == cancelButton.gameObject)
                || (volumeButton != null && selected == volumeButton.gameObject)
                || (fullscreenButton != null && selected == fullscreenButton.gameObject)
                || (languageButton != null && selected == languageButton.gameObject)
                || (recoverGameButton != null && selected == recoverGameButton.gameObject)
                || (recoverSettingsButton != null && selected == recoverSettingsButton.gameObject));
        }

        private static bool CanSelect(GameObject target)
        {
            if (target == null || !target.activeInHierarchy) return false;
            var selectable = target.GetComponent<Selectable>();
            return selectable != null && selectable.IsActive() && selectable.IsInteractable();
        }

        private void RestoreSelection()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || screen == StarterScreenKind.Boot) return;
            var selected = eventSystem.currentSelectedGameObject;
            if (!CanInteract(AppRoot.Instance))
            {
                if (OwnsSelection(selected))
                {
                    lastSelected = selected;
                    eventSystem.SetSelectedGameObject(null);
                }
                return;
            }
            if (CanSelect(selected))
            {
                if (OwnsSelection(selected)) lastSelected = selected;
                return;
            }
            var preferred = CanSelect(lastSelected) ? lastSelected
                : actionButton != null && CanSelect(actionButton.gameObject) ? actionButton.gameObject
                : secondaryButton != null && CanSelect(secondaryButton.gameObject) ? secondaryButton.gameObject : null;
            if (preferred != null) eventSystem.SetSelectedGameObject(preferred);
        }

        private void BindCancelAction()
        {
            var eventSystem = EventSystem.current;
            var module = eventSystem != null ? eventSystem.GetComponent<InputSystemUIInputModule>() : null;
            var currentAction = module != null && module.isActiveAndEnabled && module.cancel != null
                ? module.cancel.action : null;
            if (ReferenceEquals(cancelAction, currentAction)) return;
            if (cancelAction != null) cancelAction.performed -= OnCancelPerformed;
            cancelAction = currentAction;
            if (cancelAction != null) cancelAction.performed += OnCancelPerformed;
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            if (isAwaitingOverwriteConfirmation && CanInteract(AppRoot.Instance)) OnCancelButtonClicked();
        }

        private static void SetLabel(Button button, string labelText)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<Text>();
            if (label != null && label.text != labelText) label.text = labelText;
        }

        private void OnSecondaryButtonClicked()
        {
            var appRoot = AppRoot.Instance;
            if (!CanInteract(appRoot)) return;
            if (screen == StarterScreenKind.Title) appRoot.TryContinueGame();
            else
            {
                var isSaved = appRoot.TrySaveGame(isAwaitingOverwriteConfirmation);
                isAwaitingOverwriteConfirmation = !isSaved && !isAwaitingOverwriteConfirmation && appRoot.Game.RequiresReplacement;
            }
            hasDisplayedState = false;
            Refresh();
        }

        private void OnCancelButtonClicked()
        {
            if (!CanInteract(AppRoot.Instance)) return;
            isAwaitingOverwriteConfirmation = false;
            hasDisplayedState = false;
            Refresh();
        }
        private void OnVolumeButtonClicked() => ChangeSettings(0);
        private void OnFullscreenButtonClicked() => ChangeSettings(1);
        private void OnLanguageButtonClicked() => ChangeSettings(2);
        private void ChangeSettings(int settingIndex)
        {
            var appRoot = AppRoot.Instance;
            if (!CanInteract(appRoot) || appRoot.Settings == null) return;
            var settings = appRoot.Settings.Current;
            if (settingIndex == 0) settings.masterVolume = settings.masterVolume >= 1 ? 0 : Mathf.Min(1, settings.masterVolume + 0.25f);
            if (settingIndex == 1) settings.fullscreen = !settings.fullscreen;
            if (settingIndex == 2) settings.language = settings.language == "en" ? "ko" : "en";
            appRoot.TrySaveSettings(settings);
            hasDisplayedState = false;
            Refresh();
        }
        private void OnRecoverGameButtonClicked() => RecoverBackup(false);
        private void OnRecoverSettingsButtonClicked() => RecoverBackup(true);
        private void RecoverBackup(bool recoverSettings)
        {
            var appRoot = AppRoot.Instance;
            if (!CanInteract(appRoot)) return;
            appRoot.TryRecoverBackup(recoverSettings);
            hasDisplayedState = false;
            Refresh();
        }

        /// <summary>Title에서는 Main 진입을, Main에서는 Title 복귀를 앱 루트에 요청합니다.</summary>
        private void OnActionButtonClicked()
        {
            var appRoot = AppRoot.Instance;
            if (!CanInteract(appRoot))
                return;
            if (screen == StarterScreenKind.Title)
                appRoot.TryEnterMain();
            else if (screen == StarterScreenKind.Main)
                appRoot.TryReturnToTitle();
        }

        /// <summary>화면 비활성화 시 버튼 리스너를 해제해 재활성화에 따른 중복 구독을 막습니다.</summary>
        private void OnDisable()
        {
            if (subscribedRoot != null) subscribedRoot.StateChanged -= OnStateChanged;
            subscribedRoot = null;
            if (cancelAction != null) cancelAction.performed -= OnCancelPerformed;
            cancelAction = null;
            if (actionButton != null)
                actionButton.onClick.RemoveListener(OnActionButtonClicked);
            if (secondaryButton != null) secondaryButton.onClick.RemoveListener(OnSecondaryButtonClicked);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
            if (volumeButton != null) volumeButton.onClick.RemoveListener(OnVolumeButtonClicked);
            if (fullscreenButton != null) fullscreenButton.onClick.RemoveListener(OnFullscreenButtonClicked);
            if (languageButton != null) languageButton.onClick.RemoveListener(OnLanguageButtonClicked);
            if (recoverGameButton != null) recoverGameButton.onClick.RemoveListener(OnRecoverGameButtonClicked);
            if (recoverSettingsButton != null) recoverSettingsButton.onClick.RemoveListener(OnRecoverSettingsButtonClicked);
            isAwaitingOverwriteConfirmation = false;
            lastSelected = null;
        }
    }
}
