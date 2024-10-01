using System;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using XRMultiplayer.MiniGames;
using Microsoft.Win32.SafeHandles;

namespace XRMultiplayer
{
    [RequireComponent(typeof(Collider))]
    public class CardOwnerManager : MonoBehaviour
    {
        [SerializeField]
        private long _cardOwnerId = -1; // Backing field for the player ID of the owner of this card

        public long CardOwnerId
        {
            get => _cardOwnerId;
            private set
            {
                _cardOwnerId = value;
                Debug.Log($"Card owner ID changed to {CardOwnerId}");
            }
        }

        private MiniGameManager miniGameManager; // Reference to MiniGameManager
        [SerializeField]private HandOwnerManager handOwnerManager; // Reference to HandOwnerManager (parent object)
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
                Debug.LogError("Card component not found on this object.");
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
                    handOwnerManager = GetComponentInParent<HandOwnerManager>();
                    if (handOwnerManager != null)
                    {
                        SetCardOwnerId(handOwnerManager.HandOwnerId);  // Set the CardOwnerId
                    }
                    else
                    {
                        Debug.LogError("HandOwnerManager not found on parent.");
                    }
                }
                else
                {
                    SetCardOwnerId(-1);
                }

                // Update the cached inHand state
                previousInHandState = card.inHand;
            }
        }


        // New method to set the card owner ID from the HandOwnerManager (parent)
        public void SetCardOwnerId() {

            SetCardOwnerId(handOwnerManager.HandOwnerId);

        }
        public void SetCardOwnerId(long newOwnerId)
        {
            CardOwnerId = newOwnerId;
            Debug.Log($"CardOwnerManager: Card owner ID set to {CardOwnerId}");
        }

        // This method is triggered when the card is grabbed
        private void OnCardGrabbed(SelectEnterEventArgs args)
        {
            if (miniGameManager != null)
            {
                long interactingPlayerId = (long)miniGameManager.GetCurrentPlayerId();
                Debug.Log($"Player with ID {interactingPlayerId} is interacting with the card.");

                // Check if the player is allowed to interact with the card
                if (IsOwner(interactingPlayerId) || CardOwnerId == -1)
                {
                    Debug.Log($"Player with ID {interactingPlayerId} is the owner and can interact with the card.");
                }
                else
                {
                    Debug.Log($"Player with ID {interactingPlayerId} is NOT the owner and cannot interact with the card.");
                    DisableInteraction();
                }
            }
        }

        // This method is triggered when the card is released
        private void OnCardReleased(SelectExitEventArgs args)
        {
            Debug.Log("Card released.");
        }

        // Method to check if the player interacting is the owner of the card
        public bool IsOwner(long playerId)
        {
            return CardOwnerId == playerId;
        }

        private void DisableInteraction()
        {
            Debug.Log("Interaction disabled for non-owner.");

            // You can forcefully release the card or disable the grab functionality.
            if (grabInteractable.isSelected)
            {
                grabInteractable.interactionManager.SelectExit(grabInteractable.firstInteractorSelecting, grabInteractable);
            }
        }
    }
}
