using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEnd : MonoBehaviour
{
    [Header("Settings")]
    public string nextSceneName = "SampleScene"; // Change this to your next level name
    
    private Vector2 lastCheckpointPos;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Default checkpoint is where the player starts the level
        lastCheckpointPos = transform.position;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Helpful debug to see what the player is hitting in the Console
        Debug.Log("Player touched: " + other.gameObject.name + " | Tag: " + other.tag);

        if (other.CompareTag("Finish"))
        {
            Debug.Log("Goal Reached!");
            SceneManager.LoadScene(nextSceneName);
        }
        else if (other.CompareTag("Spike"))
        {
            Debug.Log("Hit Spike! Resetting to checkpoint.");
            ResetToCheckpoint();
        }
        else if (other.CompareTag("Checkpoint"))
        {
            // Use other.gameObject.transform.position to ensure you get the scene instance
            Vector3 worldPos = other.gameObject.transform.position;
            
            // Force Z to 0 and save it
            lastCheckpointPos = new Vector2(worldPos.x, worldPos.y);

            Debug.Log("New Checkpoint Saved at: " + lastCheckpointPos);
        }

    }

       void ResetToCheckpoint()
    {
        // Move the player to the saved position
        transform.position = lastCheckpointPos;

        // Reset physics so the player doesn't keep falling/moving
        if (rb != null)
        {
            // Use .velocity instead of .linearVelocity for Unity 2022 and older
            rb.velocity = Vector2.zero; 
            rb.angularVelocity = 0f;
        }
    }

}
