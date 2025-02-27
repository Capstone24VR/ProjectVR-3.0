using System;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using XRMultiplayer.MiniGames;

namespace XRMultiplayer
{
    [RequireComponent(typeof(Collider))]
    public class CardOwnerManager : MonoBehaviour
    {
        public ulong cardOwnerId = 9999;

        private MiniGameManager miniGameManager; // Reference to MiniGameManager
        [SerializeField] private NetworkedHand hand; // Reference to HandOwnerManager (parent object)
        private Card card; // Reference to the Card component on this object

        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable; // Manually assign this in the Inspector
        private bool previousInHandState = false; // Cache the previous inHand state


        private void Awake()
        {
            // Get the MiniGameManager instance
            miniGameManager = FindObjectOfType<MiniGameManager>();

            // Get the Card component on this object
            card = GetComponent<Card>();
            if (card == null)
            {
                //Debug.LogError("Card component not found on this object.");
            }

            // Add event listener for when the card is grabbed
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnCardGrabbed);
                grabInteractable.selectExited.AddListener(OnCardReleased);
            }
        }

        private void OnDestroy()
        {
            // Clean up the event listeners
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnCardGrabbed);
                grabInteractable.selectExited.RemoveListener(OnCardReleased);
            }
        }
        private void Update()
        {
            // Check if the card's "inHand" status has changed
            if (card != null && card.inHand != previousInHandState)
            {
                if (card.inHand)
                {
                    // Now attempt to get the HandOwnerManager from the new parent (player's hand)
                    hand = GetComponentInParent<NetworkedHand>();
                    if (hand != null)
                        cardOwnerId = hand.ownerID;
                }
                else
                    cardOwnerId = 9999;

                // Update the cached inHand state
                previousInHandState = card.inHand;
            }
        }


        // This method is triggered when the card is grabbed
        private void OnCardGrabbed(SelectEnterEventArgs args)
        {
            if (miniGameManager != null)
            {
                ulong interactingPlayerId = NetworkManager.Singleton.LocalClientId;
                Debug.Log($"Player with ID {interactingPlayerId} is interacting with the card.");

                // Check if the player is allowed to interact with the card
                if (IsOwner(interactingPlayerId) || cardOwnerId == 9999)
                {
                    Debug.Log($"Player with ID {interactingPlayerId} is the owner and can interact with the card.");
                }
                else
                {
                    Debug.Log($"Player with ID {interactingPlayerId} is NOT the owner and cannot interact with the card.");
                    ForceRelease();
                }
            }
        }

        // This method is triggered when the card is released
        private void OnCardReleased(SelectExitEventArgs args)
        {
            //Debug.Log("Card released.");
        }

        // Method to check if the player interacting is the owner of the card
        public bool IsOwner(ulong playerId)
        {
            return cardOwnerId == playerId;
        }

        private void ForceRelease()
        {
            grabInteractable.interactionManager.SelectExit(grabInteractable.firstInteractorSelecting, grabInteractable);
        }
    }
}