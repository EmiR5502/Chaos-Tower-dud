using UnityEngine;
using System.IO;
using System;

namespace ChaosTower.Managers
{
    [Serializable]
    public class SaveData
    {
        public int BestScore;
        public float MusicVolume = 1f;
        public float SFXVolume = 1f;
        public string LastPlayedDate;
    }

    public class SaveManager : MonoBehaviour
    {
        private static SaveManager _instance;
        public static SaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<SaveManager>();
                }
                return _instance;
            }
        }

        private string savePath;
        private SaveData currentData;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                savePath = Path.Combine(Application.persistentDataPath, "savedata.json");
                Load();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void Save()
        {
            if (currentData == null) currentData = new SaveData();

            // Update cross-manager data before saving
            if (ScoreManager.Instance != null)
            {
                currentData.BestScore = ScoreManager.Instance.BestScore;
            }
            
            currentData.LastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            try
            {
                string json = JsonUtility.ToJson(currentData, true);
                File.WriteAllText(savePath, json);
                Debug.Log($"Game Saved to: {savePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
            }
        }

        public void Load()
        {
            if (File.Exists(savePath))
            {
                try
                {
                    string json = File.ReadAllText(savePath);
                    currentData = JsonUtility.FromJson<SaveData>(json);
                    
                    // Push loaded data to managers
                    if (ScoreManager.Instance != null)
                    {
                        ScoreManager.Instance.SetBestScoreExternally(currentData.BestScore);
                    }
                    
                    Debug.Log("Game Data Loaded.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load game: {e.Message}");
                    currentData = new SaveData();
                }
            }
            else
            {
                Debug.Log("No save file found. Initializing new data.");
                currentData = new SaveData();
            }
        }

        public SaveData GetData() => currentData;
    }
}
