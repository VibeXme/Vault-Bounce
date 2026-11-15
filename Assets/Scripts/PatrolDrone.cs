using UnityEngine;

// Level 5+: Moves between waypoints; chases the player when they get too close.
// Needs a CircleCollider2D (IsTrigger = true) and a kinematic Rigidbody2D on this object.
public class PatrolDrone : MonoBehaviour
{
    public Transform[] waypoints;
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 6.2f;
    public float aggroRadius = 4.5f;

    private int currentPoint = 0;
    private Transform player;

    void Start()
    {
        BouncyPlayer p = FindFirstObjectByType<BouncyPlayer>();
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player != null && Vector2.Distance(transform.position, player.position) < aggroRadius)
        {
            // Dash toward player
            transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);
            return;
        }

        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentPoint];
        transform.position = Vector2.MoveTowards(transform.position, target.position, patrolSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) < 0.1f)
            currentPoint = (currentPoint + 1) % waypoints.Length;
    }

    // Added: the original guide never killed the player on contact with a drone.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<BouncyPlayer>() != null)
            GameManager.Instance.TriggerGameOver("CAUGHT BY A DRONE");
    }
}
