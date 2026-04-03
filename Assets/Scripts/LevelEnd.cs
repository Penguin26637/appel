using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEnd : MonoBehaviour
{
    [Header("Settings")]
    public string nextSceneName = "SampleScene"; // Change this to your next level name
    
    private Vector2 lastCheckpointPos;
    private Vector2 portalTele;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Default checkpoint is where the player starts the level
        lastCheckpointPos = transform.position; 
        //print("start" + lastCheckpointPos);
        print(gameObject.name);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Helpful debug to see what the player is hitting in the Console
        Debug.Log("Player touched: " + other.gameObject.name + " | Tag: " + other.tag);

        if (other.CompareTag("Finish"))
        {
            Debug.Log("Goal Reached! Moving to next level.");

            // 1. Get the index of the scene you are currently in
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // 2. Calculate the next index
            // The % (modulo) ensures that if you are on the last level, it loops back to 0
            int nextSceneIndex = (currentSceneIndex + 1) % SceneManager.sceneCountInBuildSettings;

            // 3. Load by index instead of name
            SceneManager.LoadScene(nextSceneIndex);
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
            lastCheckpointPos = new Vector3(worldPos.x, worldPos.y, 0);
            print("LastCheckPos" + lastCheckpointPos);

            Debug.Log("New Checkpoint Saved at: " + portalTele);
        }

        // seperate script being used for portal teleporting, but keeping this here for reference if needed in the future
        else if (other.CompareTag("Portal"))
        {
            // Use other.gameObject.transform.position to ensure you get the scene instance
            Vector3 portalPos = GameObject.Find("01pairportal").transform.position;



            // Force Z to 0 and save it
            portalTele = portalPos;
            print("LastCheckPos" + portalTele);
            transform.position = portalTele;

            Debug.Log("New Checkpoint Saved at: " + lastCheckpointPos);
        }

    }

    void ResetToCheckpoint()
    {
        // Move the player to the saved position
        transform.position = lastCheckpointPos;
        print("Reset Pos" + transform.position);

        // Reset physics so the player doesn't keep falling/moving
        if (rb != null)
        {
            rb.velocity = Vector2.zero; 
            rb.angularVelocity = 0f;
        }
    }

}
