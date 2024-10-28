using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; // For networked player information
using XRMultiplayer;

public class HandOwnerManager : MonoBehaviour
{
    [SerializeField]
    private long _handOwnerId = -2; // Backing field for the player ID that is allowed to interact with the cards
    public ulong _localClientId = 9999;



    // Public property for HandOwnerId with getter and setter
    public long HandOwnerId
    {
        get => _handOwnerId; // Getter returns the current owner ID
        set
        {
            _handOwnerId = value; // Setter updates the owner ID
            //Debug.Log($"Hand owner ID changed to {HandOwnerId}");
        }
    }

    public SeatHandler seatHandler; // Reference to the SubTrigger object (assigned via Inspector)

    private void Start()
    {
        // Ensure the SubTrigger reference is assigned
        if (seatHandler == null)
        {
            //Debug.LogError("SubTrigger not assigned to HandOwnerManager!");
        }
    }

    private void Update()
    {
        // Check if there is a player in the SubTrigger
        if (seatHandler != null && seatHandler.IsPlayerInTrigger())
        {
            long playerId = seatHandler.GetLocalPlayerId();

            // Update HandOwnerId only if the current player is different
            if (HandOwnerId != playerId)
            {
                HandOwnerId = playerId;
                //Debug.Log($"HandOwnerManager: Hand owner ID set to {HandOwnerId}.");
            }
        }
        else
        {
            // Optionally, reset HandOwnerId if no player is in the trigger
            if (HandOwnerId != -2)
            {
                HandOwnerId = -2; // Reset to default or unassigned state
                //Debug.Log("HandOwnerManager: No player in SubTrigger, Hand owner ID reset.");
            }
        }
    }
}
