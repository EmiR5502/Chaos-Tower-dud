using UnityEngine;
using UnityEngine.Events;

namespace ChaosTower.Managers
{
    public class ScoreManager : MonoBehaviour
    {
        private static ScoreManager _instance;
        public static ScoreManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<ScoreManager>();
                }
                return _instance;
            }
        }

        public int CurrentScore { get; private set; }
        public int BestScore { get; private set; }
        public int CurrentCombo { get; private set; } = 0;

        public UnityAction<int> OnScoreChanged;
        public UnityAction<int> OnComboChanged;

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
        }

        public void AddScore(int amount, bool isPerfect)
        {
            if (isPerfect)
            {
                CurrentCombo++;
                amount *= (1 + CurrentCombo);
            }
            else
            {
                CurrentCombo = 0;
            }

            CurrentScore += amount;
            OnScoreChanged?.Invoke(CurrentScore);
            OnComboChanged?.Invoke(CurrentCombo);

            if (CurrentScore > BestScore)
            {
                BestScore = CurrentScore;
                if (SaveManager.Instance != null) SaveManager.Instance.Save();
            }
        }

        public void ResetScore()
        {
            CurrentScore = 0;
            CurrentCombo = 0;
            OnScoreChanged?.Invoke(CurrentScore);
            OnComboChanged?.Invoke(CurrentCombo);
        }

        public void SetBestScoreExternally(int score)
        {
            BestScore = score;
        }
    }
}
