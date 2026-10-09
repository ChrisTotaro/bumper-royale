using BumperRoyale.Garage;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BumperRoyale.Menu
{
    /// <summary>
    /// Main menu: the title with a vertical list of options under it. PLAY loads the game scene;
    /// CAR toggles the car panel (color tiles), and the showcased car glides aside to make room.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [Tooltip("Scene loaded by the Play button. Must be in Build Settings.")]
        [SerializeField] private string gameSceneName = "Arena";

        [Header("Car panel")]
        [SerializeField] private Button carButton;
        [SerializeField] private GameObject carPanel;
        [SerializeField] private CarShowcase carShowcase;
        [Tooltip("Shown next to a menu item while its panel is open.")]
        [SerializeField] private GameObject carActiveMarker;
        [Tooltip("Car position on screen (viewport 0..1) with no panel open.")]
        [SerializeField] private Vector2 homeCarViewport = new Vector2(0.66f, 0.45f);
        [Tooltip("Car position on screen (viewport 0..1) while the car panel is open.")]
        [SerializeField] private Vector2 carPanelCarViewport = new Vector2(0.49f, 0.45f);

        [Header("Menu item colors")]
        [SerializeField] private Color itemColor = Color.white;
        [SerializeField] private Color itemActiveColor = new Color(1f, 0.55f, 0.1f);

        private bool carPanelOpen;

        private void Start()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            carButton.onClick.AddListener(OnCarClicked);
            SetCarPanelOpen(false);
        }

        private void OnDestroy()
        {
            playButton.onClick.RemoveListener(OnPlayClicked);
            carButton.onClick.RemoveListener(OnCarClicked);
        }

        private void OnPlayClicked()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        private void OnCarClicked() => SetCarPanelOpen(!carPanelOpen);

        private void SetCarPanelOpen(bool open)
        {
            carPanelOpen = open;
            carPanel.SetActive(open);
            carShowcase.ViewportPosition = open ? carPanelCarViewport : homeCarViewport;
            if (carActiveMarker != null)
                carActiveMarker.SetActive(open);
            carButton.targetGraphic.color = open ? itemActiveColor : itemColor;
        }
    }
}
