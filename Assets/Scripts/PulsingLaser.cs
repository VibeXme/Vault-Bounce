using UnityEngine;

// Level 3+: Timed telegraph laser beam.
// Attach to a thin red rectangle with BoxCollider2D (IsTrigger = true) and tag Hazard.
public class PulsingLaser : MonoBehaviour
{
    public float activeDuration = 1.6f;
    public float inactiveDuration = 1.6f;
    public float warningDuration = 0.45f;

    private BoxCollider2D laserCol;
    private SpriteRenderer spriteRend;
    private float timer;

    void Awake()
    {
        laserCol = GetComponent<BoxCollider2D>();
        spriteRend = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float cycle = activeDuration + inactiveDuration;
        timer = (timer + Time.deltaTime) % cycle;

        if (timer < inactiveDuration - warningDuration)
        {
            // Safe off state
            laserCol.enabled = false;
            spriteRend.color = new Color(1f, 0.2f, 0.3f, 0.1f);
        }
        else if (timer < inactiveDuration)
        {
            // Telegraph flicker before firing
            laserCol.enabled = false;
            float flicker = Mathf.PingPong(Time.time * 20f, 0.5f) + 0.2f;
            spriteRend.color = new Color(1f, 0.2f, 0.3f, flicker);
        }
        else
        {
            // Lethal active beam
            laserCol.enabled = true;
            spriteRend.color = new Color(1f, 0.15f, 0.25f, 1f);
        }
    }
}
