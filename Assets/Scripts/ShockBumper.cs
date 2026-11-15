using UnityEngine;

// Level 4+: Launches the ball violently on touch
public class ShockBumper : MonoBehaviour
{
    public float launchForce = 18f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        BouncyPlayer player = collision.gameObject.GetComponent<BouncyPlayer>();
        if (player != null)
        {
            Vector2 bounceDir = (collision.transform.position - transform.position).normalized;
            player.LaunchFromBumper(bounceDir, launchForce);
        }
    }
}
