using System;
using UnityEngine;
using Unity.Netcode;
using XRMultiplayer.MiniGames; // Import the MiniGameManager namespace

namespace XRMultiplayer
{
    [RequireComponent(typeof(Collider))]
    public class SeatHandler : MonoBehaviour
    {
        public Collider subTriggerCollider;

        private MiniGameManager miniGameManager;
        private bool playerInTrigger = false; // Tracks if player is in the trigger
        private long localPlayerID = -1; // Stores the current player's ID

        private void Awake()
        {
            if (subTriggerCollider == null)
                TryGetComponent(out subTriggerCollider);

            // Find the MiniGameManager instance (assuming it exists in the scene)
            miniGameManager = FindObjectOfType<MiniGameManager>();
            if (miniGameManager == null)
            {
                Debug.LogError("MiniGameManager not found!");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Get the player ID from the MiniGameManager and set the playerInTrigger flag to true
            if (miniGameManager != null)
            {
                localPlayerID = miniGameManager.GetLocalPlayerID();
                playerInTrigger = true;
                Debug.Log($"Player with ID {localPlayerID} entered the trigger.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // When the player exits, reset the flag and player ID
            if (miniGameManager != null)
            {
                Debug.Log($"Player with ID {localPlayerID} exited the trigger.");
                playerInTrigger = false;
                localPlayerID = -1;
            }
        }

        // Method to retrieve the current player ID
        public long GetLocalPlayerId()
        {
            Debug.Log("Getting local player Id: " + NetworkManager.Singleton.LocalClientId);
            return localPlayerID;
        }

        // Method to check if a player is currently in the trigger
        public bool IsPlayerInTrigger()
        {
            return playerInTrigger;
        }
    }
}
