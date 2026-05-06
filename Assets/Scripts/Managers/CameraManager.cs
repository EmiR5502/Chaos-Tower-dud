using UnityEngine;
using Unity.Cinemachine;

namespace ChaosTower.Managers
{
    public class CameraManager : MonoBehaviour
    {
        private static CameraManager _instance;
        public static CameraManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<CameraManager>();
                }
                return _instance;
            }
        }

        public CinemachineCamera virtualCamera;

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

        public void SetTarget(Transform target)
        {
            if (virtualCamera != null)
            {
                virtualCamera.Follow = target;
            }
        }
    }
}