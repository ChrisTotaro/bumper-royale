using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BumperRoyale.Menu
{
    /// <summary>
    /// Main menu: PLAY and CAR tabs. The Play tab's button loads the game scene; the Car tab shows
    /// the car showcase and color picker.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private enum Tab { Play, Car }

        [SerializeField] private Button playButton;
        [Tooltip("Scene loaded by the Play button. Must be in Build Settings.")]
        [SerializeField] private string gameSceneName = "Arena";

        [Header("Tabs")]
        [SerializeField] private Button playTabButton;
        [SerializeField] private Button carTabButton;
        [SerializeField] private GameObject playPanel;
        [SerializeField] private GameObject carPanel;
        [Tooltip("3D car display shown only on the Car tab.")]
        [SerializeField] private GameObject carShowcase;
        [SerializeField] private Color tabSelectedColor = new Color(1f, 0.55f, 0.1f);
        [SerializeField] private Color tabIdleColor = new Color(0.22f, 0.26f, 0.34f);

        private void Start()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            playTabButton.onClick.AddListener(OnPlayTabClicked);
            carTabButton.onClick.AddListener(OnCarTabClicked);
            SelectTab(Tab.Play);
        }

        private void OnDestroy()
        {
            playButton.onClick.RemoveListener(OnPlayClicked);
            playTabButton.onClick.RemoveListener(OnPlayTabClicked);
            carTabButton.onClick.RemoveListener(OnCarTabClicked);
        }

        private void OnPlayClicked()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        private void OnPlayTabClicked() => SelectTab(Tab.Play);

        private void OnCarTabClicked() => SelectTab(Tab.Car);

        private void SelectTab(Tab tab)
        {
            bool car = tab == Tab.Car;
            // The showcase must be active before the car panel so its car exists when the panel starts.
            carShowcase.SetActive(car);
            carPanel.SetActive(car);
            playPanel.SetActive(!car);
            playTabButton.targetGraphic.color = car ? tabIdleColor : tabSelectedColor;
            carTabButton.targetGraphic.color = car ? tabSelectedColor : tabIdleColor;
        }
    }
}
