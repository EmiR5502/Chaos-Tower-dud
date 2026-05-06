using UnityEngine;

public class SandstormController : MonoBehaviour
{
    public ParticleSystem gustEffect;
    public float minDelay = 5f;
    public float maxDelay = 15f;

    void Start()
    {
        InvokeRepeating(nameof(PlayGust), Random.Range(minDelay, maxDelay), Random.Range(minDelay, maxDelay));
    }

    void PlayGust()
    {
        gustEffect.Play();
    }
}