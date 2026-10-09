using UnityEditor;
using UnityEditor.SceneManagement;

namespace BumperRoyale.Setup
{
    /// <summary>
    /// Makes the editor's Play button start from the main menu, like a build does, whichever scene
    /// is open. Toggle with "Bumper Royale/Start Play Mode From Main Menu" to play the open scene
    /// directly. The setting is per machine (EditorPrefs) and on by default.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartScene
    {
        private const string PrefKey = "BumperRoyale.StartPlayModeFromMainMenu";
        private const string MenuPath = "Bumper Royale/Start Play Mode From Main Menu";

        static PlayModeStartScene()
        {
            Apply();
            // Assets can fail to load during an import-triggered reload, so apply again once it settles.
            EditorApplication.delayCall += Apply;
        }

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            UnityEditor.Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void Apply()
        {
            EditorSceneManager.playModeStartScene = Enabled
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(BumperRoyaleSetup.MainMenuScenePath)
                : null;
        }
    }
}
