using BumperRoyale.Car;
using BumperRoyale.Match;
using UnityEngine;
using UnityEngine.UI;

namespace BumperRoyale.UI
{
    /// <summary>
    /// In-match HUD: players remaining, countdown, boost meter and the Victory/Defeat panel with Restart.
    /// Reads state from MatchManager each frame.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private Text remainingText;
        [SerializeField] private Text countdownText;
        [SerializeField] private Image boostFill;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultSubtitleText;
        [SerializeField] private Button restartButton;

        [SerializeField] private Color victoryColor = new Color(1f, 0.85f, 0.1f);
        [SerializeField] private Color defeatColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color boostReadyColor = new Color(0.3f, 0.9f, 1f);
        [SerializeField] private Color boostChargingColor = new Color(0.4f, 0.4f, 0.5f);

        private CarBoost playerBoost;

        private void Start()
        {
            resultPanel.SetActive(false);
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        private void OnDestroy()
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        private void Update()
        {
            MatchManager match = MatchManager.Instance;
            if (match == null)
                return;

            remainingText.text = $"Players Remaining: {match.CarsRemaining}";
            countdownText.text = match.CountdownLabel;

            UpdateBoostMeter(match);

            bool ended = match.State == MatchManager.MatchState.Ended;
            if (ended && !resultPanel.activeSelf)
                ShowResult(match.PlayerWon);
        }

        private void UpdateBoostMeter(MatchManager match)
        {
            if (boostFill == null)
                return;
            if (playerBoost == null && match.Player != null)
                playerBoost = match.Player.GetComponent<CarBoost>();

            float progress = playerBoost != null ? playerBoost.CooldownProgress : 0f;
            boostFill.fillAmount = progress;
            boostFill.color = progress >= 1f ? boostReadyColor : boostChargingColor;
        }

        private void ShowResult(bool playerWon)
        {
            resultPanel.SetActive(true);
            resultTitleText.text = playerWon ? "VICTORY!" : "ELIMINATED";
            resultTitleText.color = playerWon ? victoryColor : defeatColor;
            resultSubtitleText.text = playerWon ? "Last car standing!" : "Defeat - you were knocked off the arena";
        }

        private void OnRestartClicked()
        {
            MatchManager.Instance?.Restart();
        }
    }
}
