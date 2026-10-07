using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarterProject
{
    /// <summary>
    /// 앱 시작 흐름에 필요한 Boot, Title, Main 씬 경로를 보관하는 공유 설정 에셋입니다.
    /// <see cref="AppBootstrap"/>이 참조하고, <see cref="AppRoot"/>는 검증 후 필요한 값을
    /// 실행용 필드로 복사하므로 플레이 중 이 에셋을 상태 저장소처럼 수정하지 않습니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Starter Project/App Config", fileName = "SO_AppConfig")]
    public sealed class AppConfig : ScriptableObject
    {
        [SerializeField] private string bootScene = "Assets/01_Scenes/Boot/00_StartScene.unity";
        [SerializeField] private string titleScene = "Assets/01_Scenes/Main/01_Title.unity";
        [SerializeField] private string mainScene = "Assets/01_Scenes/Main/02_MainScene.unity";
        [SerializeField] private UserSettings defaultSettings = new UserSettings();
        [SerializeField] private DataCatalog dataCatalog;
        public DataCatalog DataCatalog => dataCatalog;

        /// <summary>공유 SO를 변경하지 않도록 기본 설정의 독립 복사본을 반환합니다.</summary>
        public UserSettings CreateDefaultSettings() => defaultSettings.Copy();

        /// <summary>빌드가 시작되는 Boot 씬의 프로젝트 상대 경로입니다.</summary>
        public string BootScene => bootScene;

        /// <summary>초기화 완료 후 표시할 Title 씬의 프로젝트 상대 경로입니다.</summary>
        public string TitleScene => titleScene;

        /// <summary>Title 화면의 시작 요청으로 진입할 Main 씬의 프로젝트 상대 경로입니다.</summary>
        public string MainScene => mainScene;

        /// <summary>
        /// 세 씬이 서로 다르고 활성 빌드 씬 목록에 포함되며, Boot가 첫 번째인지 검증합니다.
        /// 조건을 만족하지 않으면 <see cref="InvalidOperationException"/>을 발생시켜
        /// <see cref="AppRoot"/>가 실패 상태로 전환하도록 합니다.
        /// </summary>
        public void Validate()
        {
            if (defaultSettings == null) throw new InvalidOperationException("Default settings are missing.");
            defaultSettings.Validate();
            DataValidation.Validate(dataCatalog);
            ValidateScene(bootScene, "Boot");
            ValidateScene(titleScene, "Title");
            ValidateScene(mainScene, "Main");
            if (bootScene == titleScene || bootScene == mainScene || titleScene == mainScene)
                throw new InvalidOperationException("Boot, Title and Main must use different scenes.");
            if (SceneUtility.GetScenePathByBuildIndex(0) != bootScene)
                throw new InvalidOperationException("Boot must be the first enabled scene in the build scene list.");
        }

        /// <summary>
        /// 한 역할의 씬 경로가 비어 있지 않고 Unity 씬 파일이며 빌드 목록에서 사용 가능한지 검증합니다.
        /// </summary>
        /// <param name="path">검증할 프로젝트 상대 씬 경로입니다.</param>
        /// <param name="role">오류 메시지에 표시할 씬 역할입니다.</param>
        private static void ValidateScene(string path, string role)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".unity", StringComparison.Ordinal)
                || SceneUtility.GetBuildIndexByScenePath(path) < 0)
                throw new InvalidOperationException($"{role} scene is missing or disabled in the build scene list: {path}");
        }
    }
}
