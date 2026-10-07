using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace StarterProject.UI
{
    /// <summary>타이틀 내부의 메뉴와 슬롯 선택 화면입니다. 씬 이동은 슬롯 선택 이후에만 요청합니다.</summary>
    public enum StarterTitlePage { MainMenu, NewGame, Continue, SlotManagement, Options, Credits, DeleteConfirmation }

    /// <summary>씬에 작성된 타이틀 UI를 연결합니다. 저장·삭제 규칙은 AppRoot와 GameSessionService가 소유합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class StarterTitleMenu : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text statusText;
        [Header("Pages")]
        [SerializeField] private GameObject artworkPanel;
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text pageHeading;
        [SerializeField] private Text pageDescription;
        [SerializeField] private GameObject slotsPanel;
        [SerializeField] private Button[] slotButtons = Array.Empty<Button>();
        [SerializeField] private Button[] deleteSlotButtons = Array.Empty<Button>();
        [SerializeField] private Button[] recoverSlotButtons = Array.Empty<Button>();
        [SerializeField] private Button manageSlotsButton;
        [SerializeField] private Button backButton;
        [Header("Options and credits")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Button volumeButton;
        [SerializeField] private Button fullscreenButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button recoverSettingsButton;
        [SerializeField] private GameObject creditsPanel;
        [Header("Delete confirmation")]
        [SerializeField] private GameObject deleteConfirmationPanel;
        [SerializeField] private Text deleteConfirmationText;
        [SerializeField] private Button confirmDeleteButton;
        [SerializeField] private Button cancelDeleteButton;

        private readonly List<(Button Button, UnityAction Action)> listeners = new List<(Button, UnityAction)>();
        private AppRoot subscribedRoot;
        private InputAction cancelAction;
        private StarterTitlePage managementOrigin = StarterTitlePage.NewGame;
        private bool requestFocus;
        private string deleteFailure = string.Empty;
        private Button returnMenuButton;
        private int? preferredSlotId;

        public StarterTitlePage CurrentPage { get; private set; } = StarterTitlePage.MainMenu;
        public int? PendingDeleteSlotId { get; private set; }
        private bool IsReady => subscribedRoot != null && subscribedRoot.State == AppState.Ready
            && !subscribedRoot.IsTransitioning && subscribedRoot.Game != null;
        private bool CanNavigate => IsReady && CurrentPage != StarterTitlePage.DeleteConfirmation;

        private void OnEnable()
        {
            Bind(newGameButton, OpenNewGame);
            Bind(continueButton, OpenContinue);
            Bind(optionsButton, OpenOptions);
            Bind(creditsButton, OpenCredits);
            Bind(quitButton, Quit);
            Bind(manageSlotsButton, OpenSlotManagement);
            Bind(backButton, GoBack);
            Bind(cancelDeleteButton, GoBack);
            Bind(confirmDeleteButton, ConfirmDelete);
            Bind(volumeButton, () => ChangeSettings(0));
            Bind(fullscreenButton, () => ChangeSettings(1));
            Bind(languageButton, () => ChangeSettings(2));
            Bind(recoverSettingsButton, () => { if (IsReady) subscribedRoot.TryRecoverBackup(true); });
            for (var i = 0; i < slotButtons.Length; i++)
            {
                var slotId = i + 1;
                Bind(slotButtons[i], () => SelectSlot(slotId));
            }
            for (var i = 0; i < deleteSlotButtons.Length; i++)
            {
                var slotId = i + 1;
                Bind(deleteSlotButtons[i], () => RequestDeleteSlot(slotId));
            }
            for (var i = 0; i < recoverSlotButtons.Length; i++)
            {
                var slotId = i + 1;
                Bind(recoverSlotButtons[i], () => RecoverSlot(slotId));
            }
#if !(UNITY_STANDALONE || UNITY_EDITOR)
            if (quitButton != null) quitButton.gameObject.SetActive(false);
#endif
            requestFocus = true;
            FindRoot();
            RefreshFromRoot();
            BindCancelAction();
        }

        private void Update()
        {
            FindRoot();
            BindCancelAction();
            RestoreSelection();
        }

        private void FindRoot()
        {
            var root = AppRoot.Instance;
            if (ReferenceEquals(root, subscribedRoot)) return;
            if (subscribedRoot != null) subscribedRoot.StateChanged -= RefreshFromRoot;
            subscribedRoot = root;
            if (subscribedRoot != null)
            {
                StarterInputContext.EnsureCreated(subscribedRoot);
                subscribedRoot.StateChanged += RefreshFromRoot;
            }
            RefreshFromRoot();
        }

        private void Bind(Button button, UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(action);
            listeners.Add((button, action));
        }

        /// <summary>빈 슬롯을 선택할 화면을 엽니다. 가득 찬 경우 슬롯 관리로 이동할 수 있습니다.</summary>
        public void OpenNewGame() => OpenSlots(StarterTitlePage.NewGame);
        /// <summary>저장된 슬롯을 선택할 화면을 엽니다. 이 단계에서는 게임 씬을 로드하지 않습니다.</summary>
        public void OpenContinue() => OpenSlots(StarterTitlePage.Continue);
        private void OpenSlots(StarterTitlePage page)
        {
            if (!CanNavigate) return;
            returnMenuButton = page == StarterTitlePage.Continue ? continueButton : newGameButton;
            subscribedRoot.TryRefreshGameSlots();
            ShowPage(page);
        }
        public void OpenSlotManagement()
        {
            if (!CanNavigate) return;
            managementOrigin = CurrentPage == StarterTitlePage.Continue ? StarterTitlePage.Continue : StarterTitlePage.NewGame;
            subscribedRoot.TryRefreshGameSlots();
            ShowPage(StarterTitlePage.SlotManagement);
        }
        public void OpenOptions() { if (CanNavigate) { returnMenuButton = optionsButton; ShowPage(StarterTitlePage.Options); } }
        public void OpenCredits() { if (CanNavigate) { returnMenuButton = creditsButton; ShowPage(StarterTitlePage.Credits); } }

        public void GoBack()
        {
            if (!IsReady) return;
            if (CurrentPage == StarterTitlePage.DeleteConfirmation)
            {
                preferredSlotId = PendingDeleteSlotId;
                ShowPage(StarterTitlePage.SlotManagement);
            }
            else if (CurrentPage == StarterTitlePage.SlotManagement) ShowPage(managementOrigin);
            else if (CurrentPage != StarterTitlePage.MainMenu) ShowPage(StarterTitlePage.MainMenu);
        }

        private void ShowPage(StarterTitlePage page)
        {
            CurrentPage = page;
            if (page != StarterTitlePage.DeleteConfirmation)
            {
                PendingDeleteSlotId = null;
                deleteFailure = string.Empty;
            }
            requestFocus = true;
            RefreshFromRoot();
        }

        private SaveSlotInfo FindSlot(int slotId)
        {
            if (subscribedRoot?.Game == null) return null;
            foreach (var slot in subscribedRoot.Game.Slots)
                if (slot.SlotId == slotId) return slot;
            return null;
        }

        public void SelectSlot(int slotId)
        {
            if (!IsReady) return;
            var slot = FindSlot(slotId);
            if (slot == null) return;
            if (CurrentPage == StarterTitlePage.NewGame && slot.IsEmpty) subscribedRoot.TryStartNewGame(slotId);
            else if (CurrentPage == StarterTitlePage.Continue && slot.CanContinue) subscribedRoot.TryContinueGame(slotId);
        }

        public void RequestDeleteSlot(int slotId)
        {
            if (!IsReady || CurrentPage != StarterTitlePage.SlotManagement) return;
            var slot = FindSlot(slotId);
            if (slot == null || slot.IsEmpty || !subscribedRoot.Game.CanDelete) return;
            deleteFailure = string.Empty;
            PendingDeleteSlotId = slotId;
            ShowPage(StarterTitlePage.DeleteConfirmation);
        }

        public void ConfirmDelete()
        {
            if (!IsReady || CurrentPage != StarterTitlePage.DeleteConfirmation || !PendingDeleteSlotId.HasValue) return;
            var slotId = PendingDeleteSlotId.Value;
            if (subscribedRoot.TryDeleteGameSlot(slotId))
            {
                preferredSlotId = slotId;
                ShowPage(StarterTitlePage.SlotManagement);
            }
            else
            {
                deleteFailure = subscribedRoot.StorageMessage;
                RefreshFromRoot();
            }
        }

        public void RecoverSlot(int slotId)
        {
            if (!IsReady || CurrentPage != StarterTitlePage.SlotManagement) return;
            var slot = FindSlot(slotId);
            if (slot != null && slot.Status == StorageStatus.Recovered) subscribedRoot.TryRecoverGameBackup(slotId);
        }

        private void ChangeSettings(int settingIndex)
        {
            if (!IsReady || CurrentPage != StarterTitlePage.Options || !subscribedRoot.Settings.CanSave) return;
            var settings = subscribedRoot.Settings.Current;
            if (settingIndex == 0) settings.masterVolume = settings.masterVolume >= 1 ? 0 : Mathf.Min(1, settings.masterVolume + 0.25f);
            if (settingIndex == 1) settings.fullscreen = !settings.fullscreen;
            if (settingIndex == 2) settings.language = settings.language == "en" ? "ko" : "en";
            subscribedRoot.TrySaveSettings(settings);
        }

        private void Quit()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            if (IsReady && CurrentPage != StarterTitlePage.DeleteConfirmation) Application.Quit();
#endif
        }

        /// <summary>상태 이벤트 직후 모든 버튼 잠금, 슬롯 내용과 옵션 문구를 동기 갱신합니다.</summary>
        public void RefreshFromRoot()
        {
            var ready = IsReady;
            var confirming = CurrentPage == StarterTitlePage.DeleteConfirmation;
            var managing = CurrentPage == StarterTitlePage.SlotManagement || confirming;
            var choosing = CurrentPage == StarterTitlePage.NewGame || CurrentPage == StarterTitlePage.Continue;
            foreach (var listener in listeners) listener.Button.interactable = ready && !confirming;
            SetInteractable(continueButton, ready && !confirming && subscribedRoot.Game.AnyCanContinue);
            SetActive(artworkPanel, CurrentPage == StarterTitlePage.MainMenu);
            SetActive(detailPanel, CurrentPage != StarterTitlePage.MainMenu);
            SetActive(slotsPanel, choosing || managing);
            SetActive(optionsPanel, CurrentPage == StarterTitlePage.Options);
            SetActive(creditsPanel, CurrentPage == StarterTitlePage.Credits);
            SetActive(deleteConfirmationPanel, confirming);
            SetActive(manageSlotsButton?.gameObject, choosing);
            SetInteractable(cancelDeleteButton, ready && confirming);
            SetInteractable(confirmDeleteButton, ready && confirming && PendingDeleteSlotId.HasValue
                && subscribedRoot.Game.CanDelete && FindSlot(PendingDeleteSlotId.Value)?.IsEmpty == false);
            var pendingSlot = PendingDeleteSlotId.HasValue ? FindSlot(PendingDeleteSlotId.Value) : null;
            SetText(deleteConfirmationText, $"Delete Slot {PendingDeleteSlotId}?\nLast saved: {FormatSavedTime(pendingSlot?.SavedUtc)}\nThis removes this slot's save and backup.\nThis cannot be undone." + (string.IsNullOrEmpty(deleteFailure) ? "" : "\n\n" + deleteFailure));

            var hasEmpty = false;
            for (var i = 0; i < slotButtons.Length; i++)
            {
                var slot = FindSlot(i + 1);
                hasEmpty |= slot != null && slot.IsEmpty;
                SetLabel(slotButtons[i], FormatSlot(slot, i + 1));
                SetInteractable(slotButtons[i], ready && slot != null && (CurrentPage == StarterTitlePage.NewGame
                    ? slot.IsEmpty : CurrentPage == StarterTitlePage.Continue ? slot.CanContinue : CurrentPage == StarterTitlePage.SlotManagement));
                if (i < deleteSlotButtons.Length)
                {
                    SetActive(deleteSlotButtons[i]?.gameObject, managing);
                    SetInteractable(deleteSlotButtons[i], ready && !confirming && slot != null && !slot.IsEmpty && subscribedRoot.Game.CanDelete);
                }
                if (i < recoverSlotButtons.Length)
                {
                    SetActive(recoverSlotButtons[i]?.gameObject, managing && slot != null && slot.Status == StorageStatus.Recovered);
                    SetInteractable(recoverSlotButtons[i], ready && !confirming && slot != null && slot.Status == StorageStatus.Recovered);
                }
            }
            SetText(pageHeading, CurrentPage == StarterTitlePage.NewGame ? "New Game"
                : CurrentPage == StarterTitlePage.Continue ? "Continue"
                : managing ? "Slot Management" : CurrentPage.ToString());
            SetText(pageDescription, CurrentPage == StarterTitlePage.NewGame
                ? hasEmpty ? "Choose an empty slot to begin." : "All slots are occupied. Open Slot Management to free a slot."
                : CurrentPage == StarterTitlePage.Continue ? "Choose a saved game to continue."
                : managing ? "Manage your saves. Deleting a slot requires confirmation."
                : CurrentPage == StarterTitlePage.Options ? "Make yourself comfortable." : "The people and tools behind the journey.");
            if (ready)
            {
                var settings = subscribedRoot.Settings.Current;
                SetLabel(volumeButton, $"Volume: {settings.masterVolume:P0}");
                SetLabel(fullscreenButton, "Fullscreen: " + (settings.fullscreen ? "On" : "Off"));
                SetLabel(languageButton, "Language: " + settings.language);
                SetInteractable(volumeButton, !confirming && subscribedRoot.Settings.CanSave);
                SetInteractable(fullscreenButton, !confirming && subscribedRoot.Settings.CanSave);
                SetInteractable(languageButton, !confirming && subscribedRoot.Settings.CanSave);
            }
            SetActive(recoverSettingsButton?.gameObject, ready && subscribedRoot.Settings.Status == StorageStatus.Recovered);
            SetText(statusText, subscribedRoot == null ? "Open 00_StartScene to begin."
                : subscribedRoot.State == AppState.Failed ? "Could not start. " + (Debug.isDebugBuild ? subscribedRoot.Failure : "Please restart the application.")
                : !ready ? subscribedRoot.CurrentStep + "..." : CurrentPage == StarterTitlePage.MainMenu && !subscribedRoot.Game.AnyCanContinue
                    ? "No saved games available." + (string.IsNullOrEmpty(subscribedRoot.StorageMessage) ? "" : "\n" + subscribedRoot.StorageMessage)
                    : subscribedRoot.StorageMessage);
            RestoreSelection();
        }

        private static string FormatSlot(SaveSlotInfo slot, int slotId)
        {
            if (slot == null || slot.IsEmpty) return $"Slot {slotId}\nEmpty";
            var description = slot.CanContinue ? "Saved game" : slot.Status == StorageStatus.UnsupportedVersion
                ? "Incompatible save" : slot.Status == StorageStatus.IoError ? "Save unavailable" : "Damaged save";
            if (slot.Status == StorageStatus.Recovered) description = "Backup available";
            var date = string.IsNullOrEmpty(slot.SavedUtc) ? "" : FormatSavedTime(slot.SavedUtc);
            return $"Slot {slotId}\n{description}" + (string.IsNullOrEmpty(date) ? "" : "  /  " + date);
        }

        private static string FormatSavedTime(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Unavailable";
            return DateTimeOffset.TryParse(value, out var saved) ? saved.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : value;
        }

        private void RestoreSelection()
        {
            var events = EventSystem.current;
            if (events == null) return;
            var selected = events.currentSelectedGameObject;
            if (!IsReady)
            {
                if (selected != null && selected.transform.IsChildOf(transform)) events.SetSelectedGameObject(null);
                return;
            }
            if (!requestFocus && CanSelect(selected)
                && (CurrentPage != StarterTitlePage.DeleteConfirmation || selected == cancelDeleteButton?.gameObject
                    || selected == confirmDeleteButton?.gameObject)) return;
            var target = CurrentPage == StarterTitlePage.MainMenu ? returnMenuButton != null ? returnMenuButton : newGameButton
                : CurrentPage == StarterTitlePage.DeleteConfirmation ? cancelDeleteButton
                : CurrentPage == StarterTitlePage.Options ? volumeButton : backButton;
            if (CurrentPage == StarterTitlePage.NewGame || CurrentPage == StarterTitlePage.Continue || CurrentPage == StarterTitlePage.SlotManagement)
            {
                target = CurrentPage == StarterTitlePage.SlotManagement ? backButton : manageSlotsButton;
                foreach (var slotButton in slotButtons)
                    if (slotButton != null && CanSelect(slotButton.gameObject)) { target = slotButton; break; }
                if (preferredSlotId.HasValue && preferredSlotId.Value > 0 && preferredSlotId.Value <= slotButtons.Length)
                {
                    var preferred = slotButtons[preferredSlotId.Value - 1];
                    if (preferred != null && CanSelect(preferred.gameObject)) target = preferred;
                }
            }
            if (target == null || !CanSelect(target.gameObject)) target = CurrentPage == StarterTitlePage.MainMenu ? newGameButton : backButton;
            if (target != null && CanSelect(target.gameObject))
            {
                events.SetSelectedGameObject(target.gameObject);
                requestFocus = false;
                preferredSlotId = null;
            }
        }

        private static bool CanSelect(GameObject selected)
        {
            var selectable = selected != null && selected.activeInHierarchy ? selected.GetComponent<Selectable>() : null;
            return selectable != null && selectable.IsActive() && selectable.IsInteractable();
        }
        private void BindCancelAction()
        {
            var events = EventSystem.current;
            var module = events != null ? events.GetComponent<InputSystemUIInputModule>() : null;
            var action = module != null && module.isActiveAndEnabled ? module.cancel?.action : null;
            if (ReferenceEquals(action, cancelAction)) return;
            if (cancelAction != null) cancelAction.performed -= OnCancel;
            cancelAction = action;
            if (cancelAction != null) cancelAction.performed += OnCancel;
        }
        private void OnCancel(InputAction.CallbackContext context) => GoBack();
        private static void SetInteractable(Button button, bool value) { if (button != null) button.interactable = value; }
        private static void SetActive(GameObject target, bool value) { if (target != null && target.activeSelf != value) target.SetActive(value); }
        private static void SetText(Text text, string value) { if (text != null && text.text != value) text.text = value; }
        private static void SetLabel(Button button, string value) { if (button != null) SetText(button.GetComponentInChildren<Text>(true), value); }
        private void OnDisable()
        {
            if (subscribedRoot != null) subscribedRoot.StateChanged -= RefreshFromRoot;
            subscribedRoot = null;
            if (cancelAction != null) cancelAction.performed -= OnCancel;
            cancelAction = null;
            foreach (var listener in listeners) if (listener.Button != null) listener.Button.onClick.RemoveListener(listener.Action);
            listeners.Clear();
        }
    }
}