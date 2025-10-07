using UnityEngine;

public class WispPickup : MonoBehaviour
{
    public int amount = 1;
    public float life = 10f;        // auto-despawn
    public ParticleSystem pickupBurst;   // assign in prefab
    void Start()
    {
        if (life > 0) Destroy(gameObject, life);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Currency.I?.Add(amount);
        FindObjectOfType<GameManager>()?.AddWisps(1); // or the amount you want
        AudioManager.I?.PlayPickup(); // your pickup SFX

        // play burst
        if (pickupBurst)
        {
            pickupBurst.transform.parent = null;      // detach so it can finish
            pickupBurst.Play();
            Destroy(pickupBurst.gameObject, pickupBurst.main.duration);
        }
        Destroy(gameObject);
    }
}
