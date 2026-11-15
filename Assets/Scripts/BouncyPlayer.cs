using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BouncyPlayer : MonoBehaviour
{
    [Header("Bounce Physics")]
    public float baseBounceVelocity = 11.5f;
    public float superBounceMultiplier = 1.38f;
    public float wallKickHorizontal = 8.5f;
    public float wallKickUpward = 7.5f;

    [Header("Air Control & Slam")]
    public float moveSpeed = 8.5f;
    public float airAcceleration = 45f;
    public float slamDownVelocity = -22f;

    [Header("Visual Juice")]
    public Transform visualSprite; // Child sprite so collider doesn't distort
    public float squashDuration = 0.1f;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isSlamming = false;
    private float squashTimer = 0f;
    private Vector3 targetScale = Vector3.one;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Trigger Power Slam on Down Arrow or S
        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && !isSlamming)
        {
            isSlamming = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, slamDownVelocity);
        }

        AnimateSquashAndStretch();
    }

    void FixedUpdate()
    {
        float targetVx = moveInput * moveSpeed;
        float newVx = Mathf.MoveTowards(rb.linearVelocity.x, targetVx, airAcceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVx, rb.linearVelocity.y);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        ContactPoint2D contact = collision.GetContact(0);
        Vector2 normal = contact.normal;

        // Hazard check
        if (collision.gameObject.CompareTag("Hazard"))
        {
            GameManager.Instance.TriggerGameOver("HIT OBSTACLE");
            gameObject.SetActive(false);
            return;
        }

        // Floor Bounce (normal pointing mostly upward)
        if (normal.y > 0.65f)
        {
            float bounceForce = isSlamming ? baseBounceVelocity * superBounceMultiplier : baseBounceVelocity;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
            TriggerSquash(1.45f, 0.58f);
            isSlamming = false;
        }
        // Wall Ricochet (normal pointing left or right)
        else if (Mathf.Abs(normal.x) > 0.65f)
        {
            float kickVx = normal.x * wallKickHorizontal;
            float kickVy = Mathf.Max(rb.linearVelocity.y, wallKickUpward);
            rb.linearVelocity = new Vector2(kickVx, kickVy);
            TriggerSquash(0.62f, 1.38f);
            isSlamming = false;
        }
        // Ceiling Bonk
        else if (normal.y < -0.65f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -3f);
            TriggerSquash(1.3f, 0.7f);
            isSlamming = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Hazard"))
        {
            GameManager.Instance.TriggerGameOver("LASER TRIPPED");
            gameObject.SetActive(false);
        }
    }

    public void LaunchFromBumper(Vector2 direction, float force)
    {
        isSlamming = false;
        rb.linearVelocity = direction.normalized * force;
        TriggerSquash(0.55f, 1.5f);
    }

    private void TriggerSquash(float scaleX, float scaleY)
    {
        if (visualSprite == null) return;
        visualSprite.localScale = new Vector3(scaleX, scaleY, 1f);
        squashTimer = squashDuration;
    }

    private void AnimateSquashAndStretch()
    {
        if (visualSprite == null) return;

        if (squashTimer > 0f)
        {
            squashTimer -= Time.deltaTime;
            return;
        }

        // Velocity-based stretch in mid-air
        float speedY = Mathf.Abs(rb.linearVelocity.y);
        float stretch = Mathf.Clamp(speedY * 0.018f, 0f, 0.28f);
        targetScale = new Vector3(1f - stretch * 0.6f, 1f + stretch, 1f);
        visualSprite.localScale = Vector3.Lerp(visualSprite.localScale, targetScale, Time.deltaTime * 16f);
    }
}
