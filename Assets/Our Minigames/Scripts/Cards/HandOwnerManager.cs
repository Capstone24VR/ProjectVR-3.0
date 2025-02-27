using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; // For networked player information
using XRMultiplayer;

public class HandOwnerManager : MonoBehaviour
{
    [SerializeField]
    private long _handOwnerId = -2; // Backing field for the player ID that is allowed to interact with the cards
    public ulong TestID = 9999;

    public SeatHandler seatHandler; // Reference to the SubTrigger object (assigned via Inspector)

    private void Start()
    {

    }
}