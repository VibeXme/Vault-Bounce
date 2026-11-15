using UnityEngine;

// Attach to Exit Hatch (BoxCollider2D with IsTrigger = true)
public class ExitDoor : MonoBehaviour
{
    public SpriteRenderer doorRenderer;
    public Color lockedColor = new Color(0.8f, 0.2f, 0.2f);
    public Color unlockedColor = new Color(0.2f, 0.95f, 0.45f);
    private bool isUnlocked = false;

    void Start()
    {
        if (doorRenderer != null) doorRenderer.color = lockedColor;
    }

    public void UnlockDoor()
    {
        isUnlocked = true;
        if (doorRenderer != null) doorRenderer.color = unlockedColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isUnlocked && other.GetComponent<BouncyPlayer>() != null)
        {
            GameManager.Instance.CompleteLevel();
        }
    }
}
