using UnityEngine;
using ChaosTower.Managers;

namespace ChaosTower.Gameplay
{
    public class Block : MonoBehaviour
    {
        private Rigidbody rb;
        private bool isDropped = false;
        private bool hasLanded = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        public void Drop()
        {
            isDropped = true;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.down; // 2023.3+ uses linearVelocity
            if (AudioManager.Instance != null) AudioManager.Instance.PlayDrop();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isDropped || hasLanded) return;

            if (collision.gameObject.CompareTag("Block") || collision.gameObject.CompareTag("Platform"))
            {
                hasLanded = true;
                EvaluatePlacement();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayBrick();
                TowerManager.Instance.OnBlockLanded(this);
            }
        }

        private void EvaluatePlacement()
        {
            Vector3 lastCenter = TowerManager.Instance.GetLastBlockCenter();
            float distance = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), 
                                              new Vector2(lastCenter.x, lastCenter.z));

            if (distance < 0.2f)
            {
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(10, true); 
                if (UIManager.Instance != null) UIManager.Instance.ShowFeedback("PERFECT!", Color.yellow);
                if (EffectsManager.Instance != null) EffectsManager.Instance.PlayImpactFlash();
            }
            else
            {
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(1, false);
                if (UIManager.Instance != null) UIManager.Instance.ShowFeedback("GOOD", Color.white);
            }
        }

        private void Update()
        {
            if (isDropped && transform.position.y < -10f)
            {
                GameManager.Instance.EndGame();
                Destroy(gameObject);
            }
        }
    }
}
