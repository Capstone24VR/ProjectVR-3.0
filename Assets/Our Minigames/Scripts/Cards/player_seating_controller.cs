using UnityEngine;
using Unity.Netcode;

public class SnapVolumeController : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the Snap Volume has a NetworkObject component
        if (other.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            // Ensure the player is the local player
            if (networkObject.IsLocalPlayer)
            {
                // Print the player's network ID
                ulong playerId = networkObject.OwnerClientId;
                Debug.Log("Player ID: " + playerId + " has snapped to the Snap Volume.");
            }
        }
    }
}
