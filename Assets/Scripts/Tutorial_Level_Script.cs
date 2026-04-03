using UnityEngine;

public class Tutorial_Level_Script : MonoBehaviour
{
    [Header("Settings")]
    public Vector2 fallbackPosition = new Vector2(-2, -1);

    private GameObject playerRef;
    private Rigidbody2D playerRb;
    private int respawnCount = 0;
    private bool isPlayerRegistered = false;

    // 1. Detect when the player touches THIS checkpoint
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerRef = other.gameObject;
            playerRb = other.GetComponent<Rigidbody2D>();
            isPlayerRegistered = true;
            Debug.Log("Player registered at special checkpoint.");
        }
    }

    // 2. Constantly check if the registered player has hit a spike
    void Update()
    {
        if (!isPlayerRegistered || playerRef == null) return;

        // We check the player's current collision status or distance to spikes
        // For simplicity, we can check if the player is currently touching a 'Spike' tag
        if (IsPlayerTouchingSpike())
        {
            RespawnPlayer();
        }
    }

    bool IsPlayerTouchingSpike()
    {
        // This checks if the player's collider is currently overlapping any object tagged "Spike"
        Collider2D playerCollider = playerRef.GetComponent<Collider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Default")); // Ensure this matches your Spike layer

        Collider2D[] results = new Collider2D[1];
        int count = playerCollider.OverlapCollider(filter, results);

        if (count > 0 && results[0].CompareTag("Spike"))
        {
            return true;
        }
        return false;
    }

    void RespawnPlayer()
    {
        respawnCount++;

        // Determine target: first time is the flag, every time after is the fallback
        Vector2 targetPos = (respawnCount == 1) ? (Vector2)transform.position : fallbackPosition;

        // Reset physics to stop momentum
        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.position = targetPos; // Precise physics teleport
        }

        playerRef.transform.position = new Vector3(targetPos.x, targetPos.y, 0);
        Physics2D.SyncTransforms(); // Force immediate position update

        Debug.Log($"Special Respawn #{respawnCount} to {targetPos}");
    }
}
