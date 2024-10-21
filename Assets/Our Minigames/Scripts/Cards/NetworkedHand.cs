using System.Collections;
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
    /// How thick the cards are
    /// </summary>
    [SerializeField] float _xOffset = 0.1f;

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

    private void OnHeldCardsChange(NetworkListEvent<NetworkObjectReference> changeEvent)
    {
        // Handle changes to the heldCards list
        switch (changeEvent.Type)
        {
            case NetworkListEvent<NetworkObjectReference>.EventType.Add:
                //Debug.Log($"Card added: {changeEvent.Value}");
                break;
            case NetworkListEvent<NetworkObjectReference>.EventType.Remove:
                //Debug.Log($"Card removed: {changeEvent.Value}");
                break;
        }
    }

    public bool isFull() { return heldCards.Count == maxCards; }
    public bool isEmpty() { return heldCards.Count == 0; }
    public bool canDraw() { return heldCards.Count < maxCards; }
    public void ConfigureChildPositions()
    {
        List<ulong> cardObjectsIds = new List<ulong>();
        // FOR TESTING ONLY
        heldCardsObj.Clear();
        // 
        foreach (var cardReference in heldCards)  // Transform references into actual game Objects to use
        {
            if (cardReference.TryGet(out NetworkObject card))
            {
                // FOR TESTING ONLY
                heldCardsObj.Add(card.gameObject);
                // 
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
            DrawCard(cardReference);
            DrawCardClientRpc(cardReference);  // Notify clients to update visuals
        }
    }

    [ClientRpc]
    private void DrawCardClientRpc(NetworkObjectReference cardReference)
    {
        // Only update the visuals on the client side
        if (cardReference.TryGet(out NetworkObject card))
        {
            card.transform.SetParent(transform, true);
            card.transform.localRotation = Quaternion.identity;
            card.transform.localPosition = Vector3.zero;
            card.gameObject.SetActive(true);
            ConfigureChildPositions();  // Update positions of cards
        }
    }

    public void DrawCard(NetworkObjectReference cardReference)
    {
        if (cardReference.TryGet(out NetworkObject card))
        {
            // The server adds the card to the heldCards list
            heldCards.Add(cardReference);
            ConfigureChildPositions();  // Re-arrange cards in hand

            card.transform.SetParent(transform, true);
            card.transform.localRotation = Quaternion.identity;
            card.transform.localPosition = Vector3.zero;
            card.gameObject.SetActive(true);

            // Update the card's status (optional)
            Card cardComponent = card.GetComponent<Card>();
            if (cardComponent != null)
            {
                cardComponent.SetInHand(true);
            }
        }
    }

    public void Clear()
    {
        heldCards.Clear();
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
}