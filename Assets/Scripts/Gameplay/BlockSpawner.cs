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
        public float ChaosForce = 1.5f;
        public float ChaosSpeed = 2f;

        [Header("Rope Settings")]
        public int RopeSegments = 8;
        public float RopeRandomMovement = 0.35f;

        [Header("Visuals")]
        public LineRenderer CraneLine;
        public Transform CraneAnchor;

        private GameObject currentBlock;
        private bool isMoving = false;
        private float swingTimer = 0f;

        private float ropeNoiseSeedX;
        private float ropeNoiseSeedZ;

        private void Start()
        {
            GameManager.Instance.OnGameStart.AddListener(StartSpawning);
            GameManager.Instance.OnGameOver.AddListener(StopSpawning);

            InputManager.OnTapEvent.AddListener(HandleTap);

            if (CraneLine == null) CraneLine = GetComponent<LineRenderer>();
            if (CraneAnchor == null) CraneAnchor = transform;

            ropeNoiseSeedX = Random.Range(0f, 1000f);
            ropeNoiseSeedZ = Random.Range(0f, 1000f);

            if (CraneLine != null)
            {
                CraneLine.positionCount = RopeSegments;
            }
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
            {
                Destroy(currentBlock);
            }

            if (CraneLine != null)
            {
                CraneLine.enabled = false;
            }
        }

        private void SpawnNewBlock()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

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

            Rigidbody rb = currentBlock.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            currentBlock.transform.SetParent(transform);

            swingTimer = 0f;

            ropeNoiseSeedX = Random.Range(0f, 1000f);
            ropeNoiseSeedZ = Random.Range(0f, 1000f);

            if (CraneLine != null)
            {
                CraneLine.enabled = true;
                CraneLine.positionCount = Mathf.Max(2, RopeSegments);
            }
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            float targetY = TowerManager.Instance.GetTowerHeight() + SpawnHeightOffset;
            float currentY = transform.position.y;

            transform.position = new Vector3(
                0,
                Mathf.Lerp(currentY, targetY, Time.deltaTime * 2f),
                0
            );

            if (isMoving && currentBlock != null)
            {
                UpdateSwing();

                SwingSpeed += Time.deltaTime * SpeedIncreaseRate;
            }
        }

        private void UpdateSwing()
        {
            swingTimer += Time.deltaTime * SwingSpeed;

            if (swingTimer > 1000f)
            {
                swingTimer = 0f;
            }

            float baseX = Mathf.Sin(swingTimer) * SwingAmplitude;
            float baseZ = Mathf.Cos(swingTimer * 0.6f) * (SwingAmplitude * 0.5f);

            // Random chaos movement for the block
            float chaosTime = Time.time * ChaosSpeed;

            float chaosX = (Mathf.PerlinNoise(chaosTime, ropeNoiseSeedX) * 2f - 1f) * ChaosAmount * ChaosForce;
            float chaosZ = (Mathf.PerlinNoise(ropeNoiseSeedZ, chaosTime) * 2f - 1f) * ChaosAmount * ChaosForce;

            Vector3 pos = currentBlock.transform.position;
            pos.x = baseX + chaosX;
            pos.z = baseZ + chaosZ;
            currentBlock.transform.position = pos;

            UpdateRopeLine();
        }

        private void UpdateRopeLine()
        {
            if (CraneLine == null || currentBlock == null) return;

            Vector3 anchorPos;

            if (CraneAnchor != null)
            {
                anchorPos = CraneAnchor.position + Vector3.up * 10f;
            }
            else
            {
                anchorPos = transform.position + Vector3.up * 10f;
            }

            Vector3 blockPos = currentBlock.transform.position;

            int segmentCount = Mathf.Max(2, RopeSegments);
            CraneLine.positionCount = segmentCount;

            for (int i = 0; i < segmentCount; i++)
            {
                float t = i / (float)(segmentCount - 1);

                Vector3 point = Vector3.Lerp(anchorPos, blockPos, t);

                // Do not move the first and last point too much.
                // This keeps the rope attached to the crane and block.
                float middleInfluence = Mathf.Sin(t * Mathf.PI);

                float noiseTime = Time.time * ChaosSpeed;

                float randomX = Mathf.PerlinNoise(
                    ropeNoiseSeedX + i * 0.25f,
                    noiseTime
                ) * 2f - 1f;

                float randomZ = Mathf.PerlinNoise(
                    ropeNoiseSeedZ + i * 0.25f,
                    noiseTime
                ) * 2f - 1f;

                Vector3 randomOffset = new Vector3(randomX, 0f, randomZ);
                randomOffset *= RopeRandomMovement * ChaosAmount * middleInfluence;

                point += randomOffset;

                CraneLine.SetPosition(i, point);
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

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDrop();
            }

            currentBlock.transform.SetParent(null);
            currentBlock = null;

            if (CraneLine != null)
            {
                CraneLine.enabled = false;
            }

            Invoke(nameof(SpawnNewBlock), 1.5f);
        }
    }
}