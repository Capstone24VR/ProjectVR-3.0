using System;
using UnityEngine;
using Unity.Netcode;
using XRMultiplayer.MiniGames; // Import the MiniGameManager namespace

namespace XRMultiplayer
{
    [RequireComponent(typeof(Collider))]
    public class SubTrigger : MonoBehaviour
    {
        public Action<Collider, bool> OnTriggerAction;
        public Collider subTriggerCollider;

        private MiniGameManager miniGameManager;
        private bool playerInTrigger = false; // Tracks if player is in the trigger
        private long currentPlayerId = -1; // Stores the current player's ID

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
            OnTriggerAction?.Invoke(other, true);

            // Get the player ID from the MiniGameManager and set the playerInTrigger flag to true
            if (miniGameManager != null)
            {
                currentPlayerId = miniGameManager.GetCurrentPlayerId();
                playerInTrigger = true;
                Debug.Log($"Player with ID {currentPlayerId} entered the trigger.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            OnTriggerAction?.Invoke(other, false);

            // When the player exits, reset the flag and player ID
            if (miniGameManager != null)
            {
                Debug.Log($"Player with ID {currentPlayerId} exited the trigger.");
                playerInTrigger = false;
                currentPlayerId = -1;
            }
        }

        // Method to retrieve the current player ID
        public long GetCurrentPlayerId()
        {
            return currentPlayerId;
        }

        // Method to check if a player is currently in the trigger
        public bool IsPlayerInTrigger()
        {
            return playerInTrigger;
        }
    }
}
