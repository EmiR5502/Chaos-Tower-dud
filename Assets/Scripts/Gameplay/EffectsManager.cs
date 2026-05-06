using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ChaosTower.Managers;

namespace ChaosTower.Gameplay
{
    public class EffectsManager : MonoBehaviour
    {
        private static EffectsManager _instance;
        public static EffectsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<EffectsManager>();
                }
                return _instance;
            }
        }

        [Header("Post Processing")]
        public Volume GlobalVolume;
        private Vignette vignette;
        private Bloom bloom;

        [Header("Shake Settings")]
        public float ShakeIntensity = 0.5f;
        public float ShakeDuration = 0.2f;

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
            
            if (GlobalVolume != null && GlobalVolume.profile.TryGet(out vignette))
            {
                vignette.intensity.overrideState = true;
            }
            if (GlobalVolume != null && GlobalVolume.profile.TryGet(out bloom))
            {
                bloom.intensity.overrideState = true;
            }
        }

        public void SetInstability(float intensity)
        {
            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(0.2f, 0.6f, intensity);
            }
        }

        public void SetBloomActive(bool active)
        {
            if (bloom != null)
            {
                bloom.active = active;
            }
        }

        public void PlayImpactFlash()
        {
            // Simple flash logic or particle trigger
        }
    }
}
