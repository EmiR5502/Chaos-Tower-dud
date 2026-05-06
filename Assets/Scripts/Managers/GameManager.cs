using UnityEngine;
using UnityEngine.Events;

namespace ChaosTower.Managers
{
    public enum GameState
    {
        Menu,
        Loading,
        Playing,
        Paused,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GameManager>();
                }
                return _instance;
            }
        }

        [Header("Settings")]
        public GameState CurrentState = GameState.Menu;

        [Header("Events")]
        public UnityEvent OnGameStart;
        public UnityEvent OnGameOver;

        private void Update()
        {
            // Listen for Pause key
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame || 
                    UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    TogglePause();
                }
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void StartGame()
        {
            StartCoroutine(LoadSequence());
        }

        private System.Collections.IEnumerator LoadSequence()
        {
            CurrentState = GameState.Loading;
            UIManager.Instance.ShowLoading(true);
            
            // Artificial delay to simulate loading assets/scene
            yield return new WaitForSeconds(2.0f);
            
            UIManager.Instance.ShowLoading(false);
            CurrentState = GameState.Playing;
            OnGameStart?.Invoke();
        }

        public void EndGame()
        {
            CurrentState = GameState.GameOver;
            OnGameOver?.Invoke();
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            UIManager.Instance.ShowPause(false);
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
            {
                CurrentState = GameState.Paused;
                Time.timeScale = 0f;
                UIManager.Instance.ShowPause(true);
            }
            else if (CurrentState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        public void OpenMenu()
        {
            Time.timeScale = 1f;
            RestartGame(); // Simulating back to menu by reloading scene
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            CurrentState = GameState.Menu;
            OnGameStart.RemoveAllListeners();
            OnGameOver.RemoveAllListeners();
            InputManager.OnTapEvent.RemoveAllListeners();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        public void QuitGame()
        {
            Debug.Log("Quitting Chaos Tower...");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
