using System;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using XRMultiplayer.MiniGames;

namespace XRMultiplayer
{
    [RequireComponent(typeof(Collider))]
    public class DominoOwnerManager : MonoBehaviour
    {
        public ulong dominoOwnerId = 9999;

        private MiniGameManager miniGameManager; // Reference to MiniGameManager
        [SerializeField] private NetworkedHandDomino hand; // Reference to HandOwnerManager (parent object)
        private Domino_data domino; // Reference to the domino component on this object

        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable; // Manually assign this in the Inspector
        private bool previousInHandState = false; // Cache the previous inHand state


        private void Awake()
        {
            // Get the MiniGameManager instance
            miniGameManager = FindObjectOfType<MiniGameManager>();

            // Get the domino component on this object
            domino = GetComponent<Domino_data>();
            if (domino == null)
            {
                //Debug.LogError("domino component not found on this object.");
            }

            // Add event listener for when the domino is grabbed
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OndominoGrabbed);
                grabInteractable.selectExited.AddListener(OndominoReleased);
            }
        }

        private void OnDestroy()
        {
            // Clean up the event listeners
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OndominoGrabbed);
                grabInteractable.selectExited.RemoveListener(OndominoReleased);
            }
        }
        private void Update()
        {
            // Check if the domino's "inHand" status has changed
            if (domino != null && domino.inHand != previousInHandState)
            {
                if (domino.inHand)
                {
                    // Now attempt to get the HandOwnerManager from the new parent (player's hand)
                    hand = GetComponentInParent<NetworkedHandDomino>();
                    if (hand != null)
                        dominoOwnerId = hand.ownerID;
                }
                else
                    dominoOwnerId = 9999;

                // Update the cached inHand state
                previousInHandState = domino.inHand;
            }
        }


        // This method is triggered when the domino is grabbed
        private void OndominoGrabbed(SelectEnterEventArgs args)
        {
            if (miniGameManager != null)
            {
                ulong interactingPlayerId = NetworkManager.Singleton.LocalClientId;
                Debug.Log($"Player with ID {interactingPlayerId} is interacting with the domino.");

                // Check if the player is allowed to interact with the domino
                if (IsOwner(interactingPlayerId) || dominoOwnerId == 9999)
                {
                    Debug.Log($"Player with ID {interactingPlayerId} is the owner and can interact with the domino.");
                }
                else
                {
                    Debug.Log($"Player with ID {interactingPlayerId} is NOT the owner and cannot interact with the domino.");
                    ForceRelease();
                }
            }
        }

        // This method is triggered when the domino is released
        private void OndominoReleased(SelectExitEventArgs args)
        {
            //Debug.Log("domino released.");
        }

        // Method to check if the player interacting is the owner of the domino
        public bool IsOwner(ulong playerId)
        {
            return dominoOwnerId == playerId;
        }

        private void ForceRelease()
        {
            grabInteractable.interactionManager.SelectExit(grabInteractable.firstInteractorSelecting, grabInteractable);
        }
    }
}
