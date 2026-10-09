using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BumperRoyale.Menu
{
    /// <summary>
    /// Main menu: the Play button loads the game scene.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [Tooltip("Scene loaded by the Play button. Must be in Build Settings.")]
        [SerializeField] private string gameSceneName = "Arena";

        private void Start()
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnDestroy()
        {
            playButton.onClick.RemoveListener(OnPlayClicked);
        }

        private void OnPlayClicked()
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
