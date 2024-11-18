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
        public Action<bool> toggleTriggerState;

        private MiniGameManager miniGameManager;
        public bool playerInTrigger = false; // Tracks if player is in the trigger
        private long localPlayerID = -1; // Stores the current player's ID
        private ulong localClientID = 9999;

        public void Start()
        {
            toggleTriggerState += SetTriggerState;
        }

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
                localClientID = NetworkManager.Singleton.LocalClientId;
                Debug.Log($"Player with ID {localPlayerID} entered the trigger.");
            }

            toggleTriggerState?.Invoke(true);
        }

        private void OnTriggerExit(Collider other)
        {
            // When the player exits, reset the flag and player ID
            if (miniGameManager != null)
            {
                Debug.Log($"Player with ID {localPlayerID} exited the trigger.");
                localPlayerID = -1;
                localClientID = 9999;
            }

            toggleTriggerState?.Invoke(false);
        }

        public void SetTriggerState(bool entered)
        {
            SetTriggerStateServerRpc(entered);
        }

        [ServerRpc(RequireOwnership = false)]
        void SetTriggerStateServerRpc(bool isReady)
        {
            Debug.Log("Server has recieved message");
            SetTriggerStateClientRpc(isReady);
        }

        [ClientRpc]
        void SetTriggerStateClientRpc(bool isReady)
        {
            Debug.Log($"Synching Trigger state to {isReady} for {transform.parent.name}");
            playerInTrigger = isReady;
        }

        // Method to retrieve the current player ID
        public long GetLocalPlayerId()
        {
            return localPlayerID;
        }

        public ulong GetClientID()
        {
            return localClientID;
        }

        // Method to check if a player is currently in the trigger
        public bool IsPlayerInTrigger()
        {
            return playerInTrigger;
        }

        public void OnDestroy()
        {
            toggleTriggerState -= SetTriggerState;
        }
    }
}
