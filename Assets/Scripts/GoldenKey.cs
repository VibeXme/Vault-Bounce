using UnityEngine;

// Attach to Key Prefab (CircleCollider2D with IsTrigger = true)
public class GoldenKey : MonoBehaviour
{
    public float bobAmplitude = 0.18f;
    public float bobFrequency = 3.5f;
    private Vector3 startPos;

    void Start() => startPos = transform.position;

    void Update()
    {
        float yOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = startPos + Vector3.up * yOffset;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<BouncyPlayer>() != null)
        {
            GameManager.Instance.CollectKey();
            Destroy(gameObject);
        }
    }
}
