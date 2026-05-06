using UnityEngine;

namespace ChaosTower.Managers
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<AudioManager>();
                }
                return _instance;
            }
        }

        [Header("Sources")]
        public AudioSource SFXSource;
        public AudioSource MusicSource;
        public AudioSource MenuMusicSource; // Dedicated for menu loops

        [Header("Clips")]
        public AudioClip DropClip;
        public AudioClip ImpactClip;
        public AudioClip BrickClip; // Explicitly for brick landing
        public AudioClip MenuMusicClip;
        public AudioClip CollapseClip;
        public AudioClip ClickClip;

        private float _originalMenuVolume = 1f;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (MenuMusicSource != null)
            {
                _originalMenuVolume = MenuMusicSource.volume;
            }

            // Force Global State
            AudioListener.pause = false;
            AudioListener.volume = 1.0f;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStart.AddListener(LowerMenuMusic);
                GameManager.Instance.OnGameOver.AddListener(PlayMenuMusic);
            }
            
            PlayMenuMusic();
        }

        public void PlayMenuMusic()
        {
            if (MenuMusicSource != null && MenuMusicClip != null)
            {
                MenuMusicSource.volume = _originalMenuVolume;
                MenuMusicSource.clip = MenuMusicClip;
                MenuMusicSource.loop = true;
                if (!MenuMusicSource.isPlaying) MenuMusicSource.Play();
            }
        }

        public void LowerMenuMusic()
        {
            if (MenuMusicSource != null)
            {
                MenuMusicSource.volume = _originalMenuVolume * 0.3f;
            }
        }

        public void StopMenuMusic()
        {
            if (MenuMusicSource != null) MenuMusicSource.Stop();
        }

        public void PlayDrop() { PlayOneShot(DropClip); }
        public void PlayImpact() { PlayOneShot(ImpactClip); }
        public void PlayBrick() { PlayOneShot(BrickClip); }
        public void PlayCollapse() { PlayOneShot(CollapseClip); }
        public void PlayClick() { PlayOneShot(ClickClip); }

        public void SetMasterVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (SFXSource != null && clip != null)
            {
                SFXSource.PlayOneShot(clip);
            }
        }
    }
}
