using UnityEngine;
using System.Collections.Generic;

namespace ChaosTower.Gameplay
{
    public class TowerManager : MonoBehaviour
    {
        private static TowerManager _instance;
        public static TowerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<TowerManager>();
                }
                return _instance;
            }
        }

        [Header("References")]
        public Transform TowerRoot;
        public Transform FollowTarget;

        [Header("Settings")]
        public float MaxTiltAngle = 30f;

        private List<Block> stackedBlocks = new List<Block>();
        private float currentHeight = 0f;
        private Vector3 lastBlockCenter = Vector3.zero;

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

        public void OnBlockLanded(Block block)
        {
            stackedBlocks.Add(block);
            lastBlockCenter = block.transform.position;
            block.transform.SetParent(TowerRoot);
            
            UpdateHeight();
        }

        public Vector3 GetLastBlockCenter()
        {
            return lastBlockCenter;
        }

        private void UpdateHeight()
        {
            float maxHeight = 0f;
            foreach (var b in stackedBlocks)
            {
                if (b.transform.position.y > maxHeight)
                {
                    maxHeight = b.transform.position.y;
                }
            }
            currentHeight = maxHeight;
        }

        public float GetTowerHeight() => currentHeight;

        private void Update()
        {
            // Smoothly move follow target
            if (FollowTarget != null)
            {
                // Follow the tower, but keep the camera slightly above it
                Vector3 targetPos = new Vector3(0, currentHeight, 0);
                FollowTarget.position = Vector3.Lerp(FollowTarget.position, targetPos, Time.deltaTime * 2f);
            }
        }
    }
}
