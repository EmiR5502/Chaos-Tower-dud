using UnityEngine;
using ChaosTower.Managers;
using System.Collections.Generic;

namespace ChaosTower.Gameplay
{
    public class BlockSpawner : MonoBehaviour
    {
        [Header("Settings")]
        public List<GameObject> BlockPrefabs;
        public float SwingSpeed = 1.2f;
        public float SwingAmplitude = 5f;
        public float SpawnHeightOffset = 2.55f;

        [Header("Difficulty Scaling")]
        public float SpeedIncreaseRate = 0.05f;

        [Header("Chaos Settings")]
        public float ChaosAmount = 0.5f;

        [Header("Visuals")]
        public LineRenderer CraneLine;
        public Transform CraneAnchor;

        private GameObject currentBlock;
        private bool isMoving = false;
        private float swingTimer = 0f;

        private void Start()
        {
            GameManager.Instance.OnGameStart.AddListener(StartSpawning);
            GameManager.Instance.OnGameOver.AddListener(StopSpawning);

            InputManager.OnTapEvent.AddListener(HandleTap);

            if (CraneLine == null) CraneLine = GetComponent<LineRenderer>();
            if (CraneAnchor == null) CraneAnchor = transform;
        }

        private void HandleTap()
        {
            Debug.Log($"BlockSpawner: HandleTap called. State: {GameManager.Instance.CurrentState}, isMoving: {isMoving}, currentBlock: {currentBlock != null}");
            if (GameManager.Instance.CurrentState == GameState.Playing && isMoving && currentBlock != null)
            {
                DropBlock();
            }
        }

        private void StartSpawning()
        {
            SpawnNewBlock();
            isMoving = true;
        }

        private void StopSpawning()
        {
            isMoving = false;

            CancelInvoke();

            if (currentBlock != null)
                Destroy(currentBlock);

            if (CraneLine != null)
                CraneLine.enabled = false;
        }

        private void SpawnNewBlock()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            // Robust singleton check
            if (TowerManager.Instance == null)
            {
                var mgr = Object.FindAnyObjectByType<TowerManager>();
                if (mgr == null)
                {
                    Debug.LogError("TowerManager instance not found in scene!");
                    return;
                }
            }

            float yPos = TowerManager.Instance.GetTowerHeight() + SpawnHeightOffset;
            Vector3 spawnPos = new Vector3(0, yPos, 0);

            GameObject randomPrefab = BlockPrefabs[Random.Range(0, BlockPrefabs.Count)];
            currentBlock = Instantiate(randomPrefab, spawnPos, Quaternion.identity);

            // Disable physics before drop
            Rigidbody rb = currentBlock.GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = true;

            currentBlock.transform.SetParent(transform);

            swingTimer = 0f;

            if (CraneLine != null)
                CraneLine.enabled = true;
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            // Follow tower height smoothly
            float targetY = TowerManager.Instance.GetTowerHeight() + SpawnHeightOffset;
            float currentY = transform.position.y;
            transform.position = new Vector3(0, Mathf.Lerp(currentY, targetY, Time.deltaTime * 2f), 0);

            if (isMoving && currentBlock != null)
            {
                UpdateSwing();

                // 📈 Difficulty scaling (gradually increase speed)
                SwingSpeed += Time.deltaTime * SpeedIncreaseRate;
            }
        }

        private void UpdateSwing()
        {
            swingTimer += Time.deltaTime * SwingSpeed;

            if (swingTimer > 1000f)
                swingTimer = 0f;

            // X-axis swing
            float baseX = Mathf.Sin(swingTimer) * SwingAmplitude;
            
            // Z-axis swing (using a different frequency for organic movement)
            float baseZ = Mathf.Cos(swingTimer * 0.6f) * (SwingAmplitude * 0.5f);

            // 🌪️ Smooth Chaos using Perlin Noise on both axes
            float noiseX = (Mathf.PerlinNoise(Time.time, 0f) * 2f - 1f) * ChaosAmount;
            float noiseZ = (Mathf.PerlinNoise(0f, Time.time) * 2f - 1f) * ChaosAmount;

            Vector3 pos = currentBlock.transform.position;
            pos.x = baseX + noiseX;
            pos.z = baseZ + noiseZ;
            currentBlock.transform.position = pos;

            if (CraneLine != null)
            {
                // Anchor is 10 units above the spawner for a vertical "cable" look
                Vector3 anchorPos = transform.position + Vector3.up * 10f;
                CraneLine.SetPosition(0, anchorPos);
                CraneLine.SetPosition(1, currentBlock.transform.position);
            }
        }

        private void DropBlock()
        {
            if (currentBlock == null) return;

            Block block = currentBlock.GetComponent<Block>();
            if (block != null)
            {
                block.Drop();
            }

            // 🔊 Sound hook
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDrop();
            }

            currentBlock.transform.SetParent(null);
            currentBlock = null;

            if (CraneLine != null)
                CraneLine.enabled = false;

            Invoke(nameof(SpawnNewBlock), 1.5f);
        }
    }
}