using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using StarterProject.UI;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[assembly: InternalsVisibleTo("StarterProject.EditModeTests")]

namespace StarterProject.Editor
{
    /// <summary>
    /// Starter Project 예제 설정을 에디터에서 생성·보정하고 Windows 미리보기 빌드를 만드는 도구입니다.
    /// 런타임 어셈블리와 분리되어 있으며 <see cref="AppConfig"/>, 세 씬의
    /// <see cref="StarterScreen"/>, Input System UI 연결을 편집 시점에 구성합니다.
    /// </summary>
    public static class StarterProjectSetup
    {
        /// <summary>프로젝트가 사용하는 기본 <see cref="AppConfig"/> 에셋 경로입니다.</summary>
        public const string ConfigPath = "Assets/04_Data/Config/SO_AppConfig.asset";
        private static readonly Color Background = new Color32(17, 23, 35, 255);
        private static readonly Color Muted = new Color32(154, 169, 190, 255);
        private static readonly Color Accent = new Color32(103, 226, 190, 255);
        private const float DesignScale = 1.5f;

        /// <summary>
        /// AppConfig를 생성하고 Boot, Title, Main을 빌드 씬 목록 앞에 배치한 뒤
        /// 각 씬의 예제 UI·입력·Bootstrap 연결을 구성하는 일회성 프로비저닝 진입점입니다.
        /// 기존 <see cref="StarterScreen"/>이 있는 씬의 UI 구조는 보존합니다.
        /// </summary>
        public static void CreateExampleAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before creating example assets.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save all modified scenes before creating example assets.");
            GetInputReferences(); // 변경 전에 필수 입력 에셋과 액션을 검사합니다.
            var config = AssetDatabase.LoadAssetAtPath<AppConfig>(ConfigPath);
            if (config == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/04_Data/Config"))
                    AssetDatabase.CreateFolder("Assets/04_Data", "Config");
                config = ScriptableObject.CreateInstance<AppConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            var paths = new[] { config.BootScene, config.TitleScene, config.MainScene };
            if (paths.Distinct().Count() != paths.Length
                || paths.Any(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p) == null))
                throw new InvalidOperationException("AppConfig must reference three different existing scene assets.");
            EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, true))
                .Concat(EditorBuildSettings.scenes.Where(s => !paths.Contains(s.path))).ToArray();

            CreateScreen(paths[0], StarterScreenKind.Boot);
            CreateScreen(paths[1], StarterScreenKind.Title);
            CreateScreen(paths[2], StarterScreenKind.Main);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(paths[0]);
            Debug.Log("[Starter Project] Example scenes and AppConfig are ready.");
        }

        /// <summary>
        /// 한 씬을 열어 역할에 맞는 예제 화면을 생성하고, 입력과 Boot 전용 설정을 연결합니다.
        /// 이미 StarterScreen이 있으면 화면을 재생성하지 않고 공통 연결만 보정합니다.
        /// </summary>
        /// <param name="path">편집할 씬의 프로젝트 상대 경로입니다.</param>
        /// <param name="kind">생성할 화면의 Boot, Title, Main 역할입니다.</param>
        private static void CreateScreen(string path, StarterScreenKind kind)
        {
            var scene = EditorSceneManager.OpenScene(path);
            // Opening a scene may unload unused assets. Reacquire the asset after it.
            var config = AssetDatabase.LoadAssetAtPath<AppConfig>(ConfigPath);
            if (config == null)
                throw new InvalidOperationException("AppConfig could not be loaded after opening the scene.");
            if (scene.GetRootGameObjects().Any(go => go.GetComponentInChildren<StarterScreen>(true) != null))
            {
                ConfigurePersistenceScreen(UnityEngine.Object.FindFirstObjectByType<StarterScreen>(), kind);
                ConfigureInput();
                if (kind == StarterScreenKind.Boot)
                    ConfigureBootstrap(config);
                EditorSceneManager.SaveScene(scene);
                return;
            }

            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                camera.backgroundColor = Background;
                camera.clearFlags = CameraClearFlags.SolidColor;
                if (camera.GetComponent<UniversalAdditionalCameraData>() == null)
                    camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            var canvasObject = new GameObject("Starter UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(StarterCanvasLayout.ReferenceWidth,
                StarterCanvasLayout.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var background = Panel(canvasObject.transform, "Background", Background);
            Stretch(background.rectTransform);
            background.raycastTarget = false;

            Label(canvasObject.transform, "Brand", "STARTER PROJECT   /   UNITY 6", 17, Muted, new Vector2(0, 240), new Vector2(900, 40));
            var heading = kind == StarterScreenKind.Boot ? "Getting ready" : kind == StarterScreenKind.Title ? "A clean beginning." : "Main Scene";
            Label(canvasObject.transform, "Heading", heading, 54, Color.white, new Vector2(0, 100), new Vector2(1050, 90));
            var line = Panel(canvasObject.transform, "Accent", Accent);
            Position(line.rectTransform, new Vector2(0, 35), new Vector2(64, 4));
            line.raycastTarget = false;
            var status = Label(canvasObject.transform, "Status", "Preparing...", 21, Muted, new Vector2(0, -30), new Vector2(1080, 100));

            Button button = null;
            if (kind != StarterScreenKind.Boot)
            {
                var panel = Panel(canvasObject.transform, kind == StarterScreenKind.Title ? "Start Game" : "Back to Title", Accent);
                Position(panel.rectTransform, new Vector2(0, -145), new Vector2(300, 64));
                button = panel.gameObject.AddComponent<Button>();
                button.targetGraphic = panel;
                var colors = button.colors;
                colors.highlightedColor = new Color(0.86f, 1, 0.96f);
                colors.pressedColor = new Color(0.65f, 0.85f, 0.78f);
                colors.disabledColor = new Color(0.4f, 0.45f, 0.45f);
                button.colors = colors;
                var text = Label(panel.transform, "Label", panel.name, 22, Background, Vector2.zero, new Vector2(300, 64));
                Stretch(text.rectTransform);
            }
            Label(canvasObject.transform, "Footer", "BOOTSTRAP   /   TITLE   /   MAIN", 14, Muted, new Vector2(0, -285), new Vector2(900, 30));
            var view = canvasObject.AddComponent<StarterScreen>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("screen").enumValueIndex = (int)kind;
            serialized.FindProperty("status").objectReferenceValue = status;
            serialized.FindProperty("actionButton").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ConfigurePersistenceScreen(view, kind);

            ConfigureInput();
            UnityEngine.Object.FindFirstObjectByType<EventSystem>().firstSelectedGameObject = button != null ? button.gameObject : null;
            if (kind == StarterScreenKind.Boot)
                ConfigureBootstrap(config);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>기존 예제 화면에 설정·저장 버튼을 중복 없이 연결합니다.</summary>
        private static void ConfigurePersistenceScreen(StarterScreen screenView, StarterScreenKind kind)
        {
            if (kind == StarterScreenKind.Boot) return;
            var serializedScreen = new SerializedObject(screenView);
            var actionButton = serializedScreen.FindProperty("actionButton").objectReferenceValue as Button;
            if (actionButton != null)
            {
                Position(actionButton.GetComponent<RectTransform>(), new Vector2(-175, -145), new Vector2(320, 54));
                actionButton.GetComponentInChildren<Text>().text = kind == StarterScreenKind.Title ? "New Game" : "Back to Title";
            }
            var statusText = serializedScreen.FindProperty("status").objectReferenceValue as Text;
            if (statusText != null)
            {
                Position(statusText.rectTransform, new Vector2(0, -20), new Vector2(1080, 130));
                statusText.fontSize = Mathf.RoundToInt(19 * DesignScale);
            }
            var footerTransform = screenView.transform.Find("Footer");
            if (footerTransform != null)
            {
                Position(footerTransform.GetComponent<RectTransform>(), new Vector2(0, -335), new Vector2(1100, 28));
                footerTransform.GetComponent<Text>().text = "ARROWS / WASD / GAMEPAD TO NAVIGATE  /  ENTER / A TO SELECT";
            }
            ConfigureButton("secondaryButton", "Secondary Action", kind == StarterScreenKind.Title ? "Continue" : "Save Game", 175, -145, 320);
            ConfigureButton("cancelButton", "Cancel Replace", "Cancel", 0, -90, 180);
            ConfigureButton("volumeButton", "Volume", "Volume: 100%", -350, -220, 320);
            ConfigureButton("fullscreenButton", "Fullscreen", "Fullscreen: On", 0, -220, 320);
            ConfigureButton("languageButton", "Language", "Language: en", 350, -220, 320);
            ConfigureButton("recoverGameButton", "Recover Game", "Recover Game Backup", -260, -290, 460);
            ConfigureButton("recoverSettingsButton", "Recover Settings", "Recover Settings Backup", 260, -290, 460);
            serializedScreen.ApplyModifiedPropertiesWithoutUndo();

            void ConfigureButton(string propertyName, string objectName, string buttonText, float x, float y, float width)
            {
                var button = serializedScreen.FindProperty(propertyName).objectReferenceValue as Button;
                if (button == null)
                {
                    var buttonImage = Panel(screenView.transform, objectName, Accent);
                    button = buttonImage.gameObject.AddComponent<Button>();
                    button.targetGraphic = buttonImage;
                    var buttonLabel = Label(buttonImage.transform, "Label", buttonText, 20, Background, Vector2.zero, new Vector2(width, 48));
                    Stretch(buttonLabel.rectTransform);
                    serializedScreen.FindProperty(propertyName).objectReferenceValue = button;
                }
                Position(button.GetComponent<RectTransform>(), new Vector2(x, y), new Vector2(width, 48));
            }
        }

        /// <summary>Boot 씬의 <see cref="AppBootstrap"/>을 찾거나 만들고 AppConfig 참조를 연결합니다.</summary>
        /// <param name="config">Bootstrap에 직렬화할 앱 설정 에셋입니다.</param>
        private static void ConfigureBootstrap(AppConfig config)
        {
            var bootstrap = UnityEngine.Object.FindFirstObjectByType<AppBootstrap>();
            if (bootstrap == null)
                bootstrap = new GameObject("Bootstrap").AddComponent<AppBootstrap>();
            var bootData = new SerializedObject(bootstrap);
            bootData.FindProperty("config").objectReferenceValue = config;
            bootData.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 현재 씬의 EventSystem에 Input System UI 모듈과 기존 UI 액션 참조를 연결합니다.
        /// EventSystem이 없으면 새로 생성합니다.
        /// </summary>
        internal static void ConfigureInput()
        {
            const string path = "Assets/13_Input/InputSystem_Actions.inputactions";
            var references = GetInputReferences();
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            var events = UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (events == null)
            {
                var go = new GameObject("EventSystem");
                go.SetActive(false);
                events = go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
            }
            var module = events.GetComponent<InputSystemUIInputModule>();
            if (module == null)
                module = events.gameObject.AddComponent<InputSystemUIInputModule>();
            foreach (var other in events.GetComponents<BaseInputModule>())
                if (other != module)
                    other.enabled = false;
            module.enabled = true;
            events.enabled = true;
            var data = new SerializedObject(module);
            data.FindProperty("m_ActionsAsset").objectReferenceValue = asset;
            SetAction("m_PointAction", "Point");
            SetAction("m_MoveAction", "Navigate");
            SetAction("m_SubmitAction", "Submit");
            SetAction("m_CancelAction", "Cancel");
            SetAction("m_LeftClickAction", "Click");
            SetAction("m_RightClickAction", "RightClick");
            SetAction("m_MiddleClickAction", "MiddleClick");
            SetAction("m_ScrollWheelAction", "ScrollWheel");
            SetAction("m_TrackedDevicePositionAction", "TrackedDevicePosition");
            SetAction("m_TrackedDeviceOrientationAction", "TrackedDeviceOrientation");
            data.ApplyModifiedPropertiesWithoutUndo();
            events.gameObject.SetActive(true);

            void SetAction(string field, string name)
            {
                data.FindProperty(field).objectReferenceValue = references.First(r => r.action != null
                    && r.action.actionMap.name == "UI" && r.action.name == name);
            }
        }

        /// <summary>필수 UI 액션 누락을 씬 수정 전에 명확한 오류로 보고합니다.</summary>
        private static InputActionReference[] GetInputReferences()
        {
            const string path = "Assets/13_Input/InputSystem_Actions.inputactions";
            var references = AssetDatabase.LoadAllAssetsAtPath(path).OfType<InputActionReference>().ToArray();
            var names = new[] { "Point", "Navigate", "Submit", "Cancel", "Click", "RightClick",
                "MiddleClick", "ScrollWheel", "TrackedDevicePosition", "TrackedDeviceOrientation" };
            foreach (var name in names)
                if (!references.Any(r => r.action != null && r.action.actionMap.name == "UI" && r.action.name == name))
                    throw new InvalidOperationException($"Required UI action is missing: {path} / UI / {name}");
            return references;
        }

        /// <summary>필수 설정, 씬 에셋, UI 입력과 Boot 화면 구성을 변경 없이 검사합니다.</summary>
        internal static void ValidateConfiguration()
        {
            var config = AssetDatabase.LoadAssetAtPath<AppConfig>(ConfigPath);
            if (config == null)
                throw new InvalidOperationException($"AppConfig asset is missing: {ConfigPath}");
            config.Validate();
            foreach (var path in new[] { config.BootScene, config.TitleScene, config.MainScene })
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new InvalidOperationException($"Scene asset is missing: {path}");
            GetInputReferences();

            var preview = EditorSceneManager.OpenPreviewScene(config.BootScene);
            try
            {
                ValidateSceneBootstrap(preview, config);

                var screens = preview.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<StarterScreen>(true)).ToArray();
                if (screens.Length != 1 || !screens[0].isActiveAndEnabled
                    || screens[0].GetComponent<Canvas>()?.isActiveAndEnabled != true)
                    throw new InvalidOperationException("Boot scene requires one active StarterScreen on a Canvas.");
                var screen = new SerializedObject(screens[0]);
                if (screen.FindProperty("screen").enumValueIndex != (int)StarterScreenKind.Boot
                    || screen.FindProperty("status").objectReferenceValue == null)
                    throw new InvalidOperationException("Boot StarterScreen must show the Boot status text.");

                ValidateResponsiveCanvas(screens[0], "Boot");

                ValidateSceneInput(preview, "Boot");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }

            foreach (var scenePath in new[] { config.TitleScene, config.MainScene })
            {
                preview = EditorSceneManager.OpenPreviewScene(scenePath);
                try
                {
                    var screens = preview.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<StarterScreen>(true)).ToArray();
                    if (screens.Length != 1 || !screens[0].isActiveAndEnabled)
                        throw new InvalidOperationException($"{scenePath} requires one active StarterScreen.");
                    ValidateResponsiveCanvas(screens[0], scenePath);
                    ValidateSceneInput(preview, scenePath);
                }
                finally { EditorSceneManager.ClosePreviewScene(preview); }
            }
        }

        /// <summary>Boot 초기화를 시작할 단일 활성 진입점과 설정 참조를 검사합니다.</summary>
        internal static void ValidateSceneBootstrap(Scene scene, AppConfig config)
        {
            var bootstraps = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<AppBootstrap>(true)).ToArray();
            if (bootstraps.Length != 1 || !bootstraps[0].isActiveAndEnabled)
                throw new InvalidOperationException("Boot scene requires one active AppBootstrap.");
            var assigned = new SerializedObject(bootstraps[0]).FindProperty("config").objectReferenceValue;
            if (assigned != config)
                throw new InvalidOperationException("Boot AppBootstrap must reference the configured AppConfig asset.");
        }

        /// <summary>각 예제 씬의 단일 입력 모듈과 필수 포인터·선택 액션 연결을 검사합니다.</summary>
        internal static void ValidateSceneInput(Scene scene, string sceneName)
        {
            var events = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)).ToArray();
            var module = events.Length == 1 ? events[0].GetComponent<InputSystemUIInputModule>() : null;
            if (events.Length != 1 || !events[0].isActiveAndEnabled || module == null
                || !module.isActiveAndEnabled || module.actionsAsset == null
                || events[0].GetComponents<BaseInputModule>().Count(input => input.isActiveAndEnabled) != 1)
                throw new InvalidOperationException($"{sceneName} requires one active EventSystem with one Input System UI module.");
            ValidateAction(module.point, "Point");
            ValidateAction(module.move, "Navigate");
            ValidateAction(module.submit, "Submit");
            ValidateAction(module.cancel, "Cancel");
            ValidateAction(module.leftClick, "Click");

            void ValidateAction(InputActionReference reference, string name)
            {
                var action = reference?.action;
                if (action == null || action.name != name || action.actionMap?.name != "UI"
                    || action.actionMap.asset != module.actionsAsset)
                    throw new InvalidOperationException($"{sceneName} UI {name} action is missing or assigned to a different action asset.");
            }
        }

        private static void ValidateResponsiveCanvas(StarterScreen screen, string sceneName)
        {
            var canvas = screen.GetComponent<Canvas>();
            var scaler = screen.GetComponent<CanvasScaler>();
            if (canvas == null || !canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.ScreenSpaceOverlay
                || scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                || scaler.referenceResolution != new Vector2(StarterCanvasLayout.ReferenceWidth,
                    StarterCanvasLayout.ReferenceHeight)
                || scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight
                || !Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f))
                throw new InvalidOperationException($"{sceneName} Canvas must use responsive Screen Space Overlay settings.");
        }

        [MenuItem("Tools/Starter Project/Validate Setup")]
        public static void ValidateSetup()
        {
            ValidateConfiguration();
            Debug.Log("[Starter Project] Setup validation passed: AppConfig, build scenes, responsive Boot canvas and UI input.");
        }

        /// <summary>UI 계층 아래에 단색 Image 패널을 생성합니다.</summary>
        /// <param name="parent">새 패널의 부모 Transform입니다.</param>
        /// <param name="name">GameObject 이름입니다.</param>
        /// <param name="color">Image에 적용할 색상입니다.</param>
        /// <returns>생성된 패널의 Image 컴포넌트입니다.</returns>
        private static Image Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>LegacyRuntime 글꼴을 사용하는 중앙 정렬 uGUI Text를 생성하고 배치합니다.</summary>
        /// <param name="parent">새 레이블의 부모 Transform입니다.</param>
        /// <param name="name">GameObject 이름입니다.</param>
        /// <param name="value">표시할 문자열입니다.</param>
        /// <param name="size">글꼴 크기입니다.</param>
        /// <param name="color">글자 색상입니다.</param>
        /// <param name="position">부모 중앙을 기준으로 한 위치입니다.</param>
        /// <param name="dimensions">RectTransform 크기입니다.</param>
        /// <returns>생성된 Text 컴포넌트입니다.</returns>
        private static Text Label(Transform parent, string name, string value, int size, Color color, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = value;
            label.fontSize = Mathf.RoundToInt(size * DesignScale);
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            Position(label.rectTransform, position, dimensions);
            return label;
        }

        /// <summary>RectTransform을 부모 중앙 기준의 고정 위치와 크기로 설정합니다.</summary>
        /// <param name="rect">배치할 RectTransform입니다.</param>
        /// <param name="position">중앙 기준 위치입니다.</param>
        /// <param name="dimensions">고정 크기입니다.</param>
        private static void Position(RectTransform rect, Vector2 position, Vector2 dimensions)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = dimensions * DesignScale;
            rect.anchoredPosition = position * DesignScale;
        }

        /// <summary>RectTransform이 부모 영역 전체를 여백 없이 채우도록 설정합니다.</summary>
        /// <param name="rect">늘릴 RectTransform입니다.</param>
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 현재 활성 빌드 씬으로 Windows x64 Development 빌드를 생성합니다.
        /// 빌드 전 AppConfig를 검증하며 결과는 Builds/Windows/{Product Name}.exe에 저장합니다.
        /// </summary>
        [MenuItem("Tools/Starter Project/Build Windows Preview")]
        public static void BuildWindowsPreview()
        {
            ValidateConfiguration();
            var outputDirectory = Path.Combine("Builds", "Windows");
            Directory.CreateDirectory(outputDirectory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = Path.Combine(outputDirectory, GetWindowsPreviewExecutableName(PlayerSettings.productName)),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Build failed: {report.summary.result}");
            Debug.Log($"[Starter Project] Windows build succeeded ({report.summary.totalSize} bytes).");
        }

        /// <summary>복제한 게임의 Product Name을 Windows 실행 파일명으로 안전하게 변환합니다.</summary>
        internal static string GetWindowsPreviewExecutableName(string productName)
        {
            const string invalidCharacters = "<>:\"/\\|?*";
            var name = new string((productName ?? string.Empty).Trim()
                .Select(character => character < ' ' || invalidCharacters.IndexOf(character) >= 0 ? '_' : character)
                .ToArray()).TrimEnd(' ', '.');
            if (string.IsNullOrEmpty(name)) name = "Game";
            var reservedName = name.Split('.')[0];
            if (new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }
                .Contains(reservedName, StringComparer.OrdinalIgnoreCase))
                name = "Game_" + name;
            return name + ".exe";
        }
    }
}
