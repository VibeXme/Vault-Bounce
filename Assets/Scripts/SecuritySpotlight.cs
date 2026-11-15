using UnityEngine;

// Level 2+: Sweeping vision cone.
// Create a triangle sprite with a PolygonCollider2D (IsTrigger = true) as a child
// of a pivot object, and attach this script to the pivot.
public class SecuritySpotlight : MonoBehaviour
{
    public float sweepAngle = 45f;
    public float sweepSpeed = 1.6f;
    public float detectionFillRate = 1.35f; // Fills 100% in ~0.74 seconds

    private float startZ;

    void Start() => startZ = transform.eulerAngles.z;

    void Update()
    {
        float angle = Mathf.Sin(Time.time * sweepSpeed) * sweepAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, startZ + angle);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.GetComponent<BouncyPlayer>() != null)
        {
            GameManager.Instance.AddDetection(detectionFillRate);
        }
    }
}
