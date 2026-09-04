using UnityEngine;

public class WinTrigger : MonoBehaviour
{
    public StickController stick;
    public GameObject winMessage;
    public string ballTag = "Ball";

    private bool won;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (won || !other.CompareTag(ballTag)) return;
        won = true;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (stick != null) stick.inputEnabled = false;
        if (winMessage != null) winMessage.SetActive(true);
    }
}
