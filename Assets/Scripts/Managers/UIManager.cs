using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace ChaosTower.Managers
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<UIManager>();
                }
                return _instance;
            }
        }

        [Header("Game UI")]
        public TextMeshProUGUI ScoreText;
        public TextMeshProUGUI FeedbackText;
        public Button PauseButton;

        [Header("Main Menu")]
        public GameObject MenuPanel;
        public GameObject LoadingPanel;
        public Button StartButton;
        public Button SettingsButton;
        public Button QuitButton;

        [Header("Settings Menu")]
        public GameObject SettingsPanel;
        public GameObject CloseSettingsButton;
        public Slider VolumeSlider;
        public Toggle BloomToggle;

        [Header("Pause Menu")]
        public GameObject PausePanel;
        public GameObject CloseButton;
        public GameObject ResumeButton;
        public GameObject MenuButton;

        [Header("Game Over Menu")]
        public GameObject GameOverPanel;
        public Button RestartButton;
        public TextMeshProUGUI FinalScoreText;
        public TextMeshProUGUI BestScoreText;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }

            if (FeedbackText != null) FeedbackText.gameObject.SetActive(false);
            if (LoadingPanel != null) LoadingPanel.gameObject.SetActive(false);
            if (SettingsPanel != null) SettingsPanel.gameObject.SetActive(false);
            if (PausePanel != null) PausePanel.gameObject.SetActive(false);
            if (GameOverPanel != null) GameOverPanel.gameObject.SetActive(false);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStart.AddListener(ShowPlayingUI);
                GameManager.Instance.OnGameOver.AddListener(ShowGameOverUI);
            }
            
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += UpdateScoreText;
            }

            // Wire HUD & Menu Buttons
            if (StartButton != null) StartButton.onClick.AddListener(StartGame);
            if (SettingsButton != null) SettingsButton.onClick.AddListener(() => ShowSettings(true));
            if (QuitButton != null) QuitButton.onClick.AddListener(QuitGame);
            if (PauseButton != null) PauseButton.onClick.AddListener(TogglePause);
            if (RestartButton != null) RestartButton.onClick.AddListener(RestartGame);

            // Wire Settings Controls
            if (VolumeSlider != null)
            {
                VolumeSlider.value = AudioListener.volume;
                VolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            if (BloomToggle != null)
            {
                BloomToggle.onValueChanged.AddListener(OnBloomToggled);
            }

            // Wire Sub-Menu Buttons (Using explicit slots or finding by name as fallback)
            WireButton(ResumeButton, ResumeGame, "ResumeButton");
            WireButton(CloseButton, ResumeGame, "CloseButton"); // Assuming Close on Pause = Resume
            WireButton(MenuButton, OpenMenu, "MenuButton");
            WireButton(CloseSettingsButton, () => ShowSettings(false), "CloseSettingsButton");

            ShowMenuUI();
        }

        private void WireButton(GameObject obj, UnityEngine.Events.UnityAction action, string fallbackName)
        {
            Button btn = null;
            if (obj != null) btn = obj.GetComponent<Button>();
            
            if (btn == null)
            {
                // Fallback search by name
                var allButtons = GetComponentsInChildren<Button>(true);
                foreach (var b in allButtons)
                {
                    if (b.name == fallbackName) { btn = b; break; }
                }
            }

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners(); // Avoid double wiring
                btn.onClick.AddListener(action);
            }
        }

        public void ShowMenuUI()
        {
            if (MenuPanel != null) MenuPanel.SetActive(true);
            if (LoadingPanel != null) LoadingPanel.SetActive(false);
            if (SettingsPanel != null) SettingsPanel.SetActive(false);
            if (PausePanel != null) PausePanel.SetActive(false);
            if (GameOverPanel != null) GameOverPanel.SetActive(false);
            if (ScoreText != null) ScoreText.gameObject.SetActive(false);
            if (FeedbackText != null) FeedbackText.gameObject.SetActive(false);
            if (PauseButton != null) PauseButton.gameObject.SetActive(false);
        }

        public void ShowSettings(bool show)
        {
            if (SettingsPanel != null) SettingsPanel.SetActive(show);
        }

        public void ShowPause(bool show)
        {
            if (PausePanel != null) PausePanel.SetActive(show);
        }

        public void ShowLoading(bool show)
        {
            if (LoadingPanel != null) LoadingPanel.SetActive(show);
            if (show)
            {
                if (MenuPanel != null) MenuPanel.SetActive(false);
                if (GameOverPanel != null) GameOverPanel.SetActive(false);
            }
        }

        public void ShowPlayingUI()
        {
            if (MenuPanel != null) MenuPanel.SetActive(false);
            if (GameOverPanel != null) GameOverPanel.SetActive(false);
            if (ScoreText != null) ScoreText.gameObject.SetActive(true);
            if (PauseButton != null) PauseButton.gameObject.SetActive(true);
            UpdateScoreText(0);
        }

        public void ShowGameOverUI()
        {
            if (GameOverPanel != null) GameOverPanel.SetActive(true);
            if (FinalScoreText != null) FinalScoreText.text = "Score: " + ScoreManager.Instance.CurrentScore;
            if (BestScoreText != null) BestScoreText.text = "Best: " + ScoreManager.Instance.BestScore;
        }

        public void ShowFeedback(string message, Color color)
        {
            if (FeedbackText == null) return;

            FeedbackText.text = message;
            FeedbackText.color = color;
            FeedbackText.gameObject.SetActive(true);
            
            CancelInvoke(nameof(HideFeedback));
            Invoke(nameof(HideFeedback), 1.5f);
        }

        private void HideFeedback()
        {
            if (FeedbackText != null) FeedbackText.gameObject.SetActive(false);
        }

        private void UpdateScoreText(int score)
        {
            if (ScoreText != null) ScoreText.text = score.ToString();
        }

        // --- PUBLIC EVENT WRAPPERS ---

        public void OnVolumeChanged(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(value);
        }

        public void OnBloomToggled(bool value)
        {
            if (ChaosTower.Gameplay.EffectsManager.Instance != null) 
                ChaosTower.Gameplay.EffectsManager.Instance.SetBloomActive(value);
        }

        public void StartGame() => GameManager.Instance.StartGame();
        public void RestartGame() => GameManager.Instance.RestartGame();
        public void ResumeGame() => GameManager.Instance.ResumeGame();
        public void TogglePause() => GameManager.Instance.TogglePause();
        public void OpenMenu() => GameManager.Instance.OpenMenu();
        public void QuitGame() => GameManager.Instance.QuitGame();
    }
}
