using UnityEngine;

/// <summary>
/// Lets the player win a round when they touch the collider this is attached to.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoalTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check whether the other trigger is the player.
        if (other.CompareTag("Player"))
        {
            // Indev: For now just print a message to the console.
            Debug.Log("Player entered a goal tile!");
        }
    }
}
