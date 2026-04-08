using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEnd : MonoBehaviour
{
    [Header("Settings")]
    public string nextSceneName = "SampleScene";

    [Header("Teleport Settings")]
    public float teleportCooldown = 2.0f; // 2 second delay
    private float nextTeleportTime = 0f;

    private Vector2 lastCheckpointPos;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lastCheckpointPos = transform.position;
        Debug.Log("Player script initialized on: " + gameObject.name);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Player touched: " + other.gameObject.name + " | Tag: " + other.tag);

        // --- LEVEL FINISH LOGIC ---
        if (other.CompareTag("Finish"))
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int nextSceneIndex = (currentSceneIndex + 1) % SceneManager.sceneCountInBuildSettings;
            SceneManager.LoadScene(nextSceneIndex);
        }

        // --- SPIKE LOGIC ---
        else if (other.CompareTag("Spike"))
        {
            ResetToCheckpoint();
        }

        // --- CHECKPOINT LOGIC ---
        else if (other.CompareTag("Checkpoint"))
        {
            lastCheckpointPos = new Vector2(other.transform.position.x, other.transform.position.y);
            Debug.Log("New Checkpoint Saved at: " + lastCheckpointPos);
        }

        // --- PORTAL LOGIC ---
        else if (other.CompareTag("Portal"))
        {
            // 1. Check if the cooldown has finished
            if (Time.time >= nextTeleportTime)
            {
                // 2. Look for a script on the portal to get the destination
                // (Assuming your portal object has a simple script with a 'teleportTarget' variable)
                PortalDestination portalScript = other.GetComponent<PortalDestination>();

                if (portalScript != null && portalScript.teleportTarget != null)
                {
                    // 3. Start the cooldown
                    nextTeleportTime = Time.time + teleportCooldown;

                    // 4. Move the player
                    transform.position = portalScript.teleportTarget.position;

                    // 5. Reset physics so you don't fly out of the exit
                    if (rb != null) rb.velocity = Vector2.zero;

                    Debug.Log("Teleported! Cooldown active for " + teleportCooldown + "s");
                }
                else
                {
                    Debug.LogWarning("Portal touched, but no destination is assigned on the portal object!");
                }
            }
            else
            {
                Debug.Log("Portal on cooldown. Wait " + (nextTeleportTime - Time.time).ToString("F1") + "s");
            }
        }
    }

    void ResetToCheckpoint()
    {
        transform.position = lastCheckpointPos;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}
