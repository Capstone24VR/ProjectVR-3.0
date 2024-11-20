using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class NetworkedHand : NetworkBehaviour
{
    /// <summary>
    /// The max number of cards a player can hold.
    /// </summary>
    [SerializeField] public int maxCards = 9999;

    /// <summary>
    /// Whether someone is playing with this hand
    /// </summary>
    [SerializeField] public bool active = true;

    /// <summary>
    /// Manager for who can use the hand.
    /// </summary>
    public HandOwnerManager ownerManager;

    /// <summary>
    /// How close each card should be
    /// </summary>
    [SerializeField] float bunching = .12f;

    /// <summary>
    /// The Reference to cards the hand holds
    /// </summary>
    [SerializeField] public NetworkList<NetworkObjectReference> heldCards = new NetworkList<NetworkObjectReference>();

    /// <summary>
    /// FOR RESTING PURPOSES: the gameobject of the cards
    /// </summary>
    [SerializeField] public List<GameObject> heldCardsObj = new List<GameObject>();


    private void Awake()
    {
        heldCards = new NetworkList<NetworkObjectReference>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        heldCards.OnListChanged += OnHeldCardsChange;
    }

    public bool isFull() { return heldCards.Count == maxCards; }
    public bool isEmpty() { return heldCards.Count == 0; }
    public bool canDraw() { return heldCards.Count < maxCards; }
    public void ConfigureChildPositions()
    {
        List<ulong> cardObjectsIds = new List<ulong>();
        foreach (var cardReference in heldCards)  // Transform references into actual game Objects to use
        {
            if (cardReference.TryGet(out NetworkObject card))
            {
                cardObjectsIds.Add(card.NetworkObjectId);
            }
        }
        ConfigureChildrenPositionsClientRpc(cardObjectsIds.ToArray());
    }

    [ServerRpc]
    public void DrawCardServerRpc(NetworkObjectReference cardReference)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // Draw the card on the server and update all clients
            DrawCardOnServer(cardReference);
            DrawCardClientRpc(cardReference);  // Notify clients to update visuals
        }
    }

    public void DrawCardOnServer(NetworkObjectReference cardReference)
    {
        if (cardReference.TryGet(out NetworkObject card))
        {
            // The server adds the card to the heldCards list
            heldCards.Add(cardReference);
            ConfigureChildPositions();  // Re-arrange cards in hand
        }
    }

    [ClientRpc]
    public void DrawCardClientRpc(NetworkObjectReference cardReference)
    {
        // Only update the visuals on the client side
        if (cardReference.TryGet(out NetworkObject card))
        {
            card.transform.SetParent(transform, true);
            card.transform.localRotation = Quaternion.identity;
            card.transform.localPosition = Vector3.zero;
            card.gameObject.SetActive(true);
            ConfigureChildPositions();  // Update positions of cards
            
            Card cardComponent = card.GetComponent<Card>();
            if (cardComponent != null)
            {
                cardComponent.SetInHand(true);
            }
        }
    }

    [ServerRpc]
    public void RemoveCardServerRpc(ulong networkObjectId)
    {
        NetworkObject cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
        if (cardNetworkObject != null)
        {
            NetworkObjectReference cardReference = new NetworkObjectReference(cardNetworkObject);

            // Remove the card from the hand on the server
            heldCards.Remove(cardReference);

            // Call the ClientRpc to update the hand positions on all clients
            RemoveCardClientRpc(networkObjectId);
        }
    }

    // ClientRpc to handle card removal and reconfiguration on all clients
    [ClientRpc]
    public void RemoveCardClientRpc(ulong networkObjectId)
    {
        // Reconfigure child positions after card removal
        ConfigureChildPositions();
    }

    public void Clear()
    {
        heldCards.Clear();
        heldCardsObj.Clear();

        ClearCardsClientRpc();
    }

    [ClientRpc]
    private void ClearCardsClientRpc()
    {
        heldCardsObj.Clear();
    }


    public void ConfigureChildrenPositions(List<GameObject> cards)
    {
        float startingPos = -cards.Count / 2;

        if (cards.Count == 1) { startingPos = 0; }
        if (cards.Count % 2 == 0) { startingPos += 0.5f; }

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.localRotation = Quaternion.identity;
            Vector3 newPosition = new Vector3(startingPos * bunching, 0, 0);
            cards[i].transform.localPosition = newPosition;
            cards[i].GetComponent<Card>().SetPosition(newPosition);

            startingPos++;
        }
    }

    [ClientRpc]
    public void ConfigureChildrenPositionsClientRpc(ulong[] networkObjectIds)
    {
        float startingPos = -networkObjectIds.Length / 2;

        if (networkObjectIds.Length == 1) { startingPos = 0; }
        if (networkObjectIds.Length % 2 == 0) { startingPos += 0.5f; }

        for (int i = 0; i < networkObjectIds.Length; i++)
        {
            NetworkObject cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectIds[i]];
            if (cardNetworkObject != null)
            {
                GameObject card = cardNetworkObject.gameObject;
                card.transform.localRotation = Quaternion.identity;
                Vector3 newPosition = new Vector3(startingPos * bunching, 0, 0);
                card.transform.localPosition = newPosition;
                card.GetComponent<Card>().SetPosition(newPosition);

                startingPos++;
            }
        }
    }
    private void OnHeldCardsChange(NetworkListEvent<NetworkObjectReference> changeEvent)
    {
        // Handle changes to the heldCards list
        switch (changeEvent.Type)
        {
            case NetworkListEvent<NetworkObjectReference>.EventType.Add:
                //Debug.Log($"Card added: {changeEvent.Value}");
                if (changeEvent.Value.TryGet(out NetworkObject noA))
                {
                    heldCardsObj.Add(noA.gameObject);
                }
                break;
            case NetworkListEvent<NetworkObjectReference>.EventType.Remove:
                //Debug.Log($"Card removed: {changeEvent.Value}");
                if (changeEvent.Value.TryGet(out NetworkObject noR))
                {
                    heldCardsObj.Remove(noR.gameObject);
                }
                break;
        }
    }
}