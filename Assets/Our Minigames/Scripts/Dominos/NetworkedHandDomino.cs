using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using XRMultiplayer;

[System.Serializable]
public class NetworkedHandDomino : NetworkBehaviour
{
    /// <summary>
    /// The max number of dominos a player can hold.
    /// </summary>
    [SerializeField] public int maxDominos = 9999;

    /// <summary>
    /// Whether someone is playing with this hand
    /// </summary>
    [SerializeField] public bool active = true;

    /// <summary>
    /// The Id for who can use the hand.
    /// </summary>
    public ulong ownerID;

    /// <summary>
    /// Handles seatTrigger Nonsense
    /// </summary>
    public SeatHandler seatHandler;

    /// <summary>
    /// How close each domino should be
    /// </summary>
    [SerializeField] float bunching = .12f;

    /// <summary>
    /// The Reference to dominos the hand holds
    /// </summary>
    [SerializeField] public NetworkList<NetworkObjectReference> heldDominos = new NetworkList<NetworkObjectReference>();

    /// <summary>
    /// FOR RESTING PURPOSES: the gameobject of the dominos
    /// </summary>
    [SerializeField] public List<GameObject> heldDominosObj = new List<GameObject>();


    private void Awake()
    {
        heldDominos = new NetworkList<NetworkObjectReference>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        heldDominos.OnListChanged += OnheldDominosChange;
    }

    public bool isFull() { return heldDominos.Count == maxDominos; }
    public bool isEmpty() { return heldDominos.Count == 0; }
    public bool canDraw() { return heldDominos.Count < maxDominos; }
    public void ConfigureChildPositions()
    {
        List<ulong> dominoObjectsIds = new List<ulong>();
        foreach (var dominoReference in heldDominos)  // Transform references into actual game Objects to use
        {
            if (dominoReference.TryGet(out NetworkObject domino))
            {
                dominoObjectsIds.Add(domino.NetworkObjectId);
            }
        }
        ConfigureChildrenPositionsClientRpc(dominoObjectsIds.ToArray());
    }

    [ServerRpc]
    public void DrawDominoServerRpc(NetworkObjectReference dominoReference)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // Draw the domino on the server and update all clients
            DrawDominoOnServer(dominoReference);
            DrawDominoClientRpc(dominoReference);  // Notify clients to update visuals
        }
    }

    public void DrawDominoOnServer(NetworkObjectReference dominoReference)
    {
        if (dominoReference.TryGet(out NetworkObject domino))
        {
            // The server adds the domino to the heldDominos list
            heldDominos.Add(dominoReference);
            ConfigureChildPositions();  // Re-arrange dominos in hand
        }
    }

    [ClientRpc]
    public void DrawDominoClientRpc(NetworkObjectReference dominoReference)
    {
        // Only update the visuals on the client side
        if (dominoReference.TryGet(out NetworkObject domino))
        {
            domino.transform.SetParent(transform, true);
            domino.transform.localRotation = Quaternion.identity;
            domino.transform.localPosition = Vector3.zero;
            domino.gameObject.SetActive(true);
            ConfigureChildPositions();  // Update positions of dominos
            
            Domino_data dominoComponent = domino.GetComponent<Domino_data>();
            if (dominoComponent != null)
            {
                dominoComponent.SetInHand(true);
            }
        }
    }

    [ServerRpc]
    public void RemoveDominoServerRpc(ulong networkObjectId)
    {
        NetworkObject dominoNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
        if (dominoNetworkObject != null)
        {
            NetworkObjectReference dominoReference = new NetworkObjectReference(dominoNetworkObject);

            // Remove the domino from the hand on the server
            heldDominos.Remove(dominoReference);

            // Call the ClientRpc to update the hand positions on all clients
            RemoveDominoClientRpc(networkObjectId);
        }
    }

    // ClientRpc to handle domino removal and reconfiguration on all clients
    [ClientRpc]
    public void RemoveDominoClientRpc(ulong networkObjectId)
    {
        // Reconfigure child positions after domino removal
        ConfigureChildPositions();
    }

    public void Clear()
    {
        heldDominos.Clear();
    }


    public void ConfigureChildrenPositions(List<GameObject> dominos)
    {
        float startingPos = -dominos.Count / 2;

        if (dominos.Count == 1) { startingPos = 0; }
        if (dominos.Count % 2 == 0) { startingPos += 0.5f; }

        for (int i = 0; i < dominos.Count; i++)
        {
            dominos[i].transform.localRotation = Quaternion.identity;
            Vector3 newPosition = new Vector3(startingPos * bunching, 0, 0);
            dominos[i].transform.localPosition = newPosition;
            dominos[i].GetComponent<Domino_data>().SetPosition(newPosition);

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
            NetworkObject dominoNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectIds[i]];
            if (dominoNetworkObject != null)
            {
                GameObject domino = dominoNetworkObject.gameObject;
                domino.transform.localRotation = Quaternion.identity;
                Vector3 newPosition = new Vector3(startingPos * bunching, 0, 0);
                domino.transform.localPosition = newPosition;
                domino.GetComponent<Domino_data>().SetPosition(newPosition);

                startingPos++;
            }
        }
    }

    public List<int> GetDominoValues()
    {
        HashSet<int> values = new HashSet<int>();
        foreach (var Domino in heldDominosObj) 
        {
            var ddata = Domino.GetComponent<Domino_data>();
            values.Add(ddata.But_side);
            values.Add(ddata.Top_side);
        }

        return values.ToList();
    }

    private void OnheldDominosChange(NetworkListEvent<NetworkObjectReference> changeEvent)
    {
        // Handle changes to the heldDominos list
        switch (changeEvent.Type)
        {
            case NetworkListEvent<NetworkObjectReference>.EventType.Add:
                //Debug.Log($"Domino added: {changeEvent.Value}");
                if (changeEvent.Value.TryGet(out NetworkObject noA))
                {
                    heldDominosObj.Add(noA.gameObject);
                }
                break;
            case NetworkListEvent<NetworkObjectReference>.EventType.Remove:
                //Debug.Log($"Domino removed: {changeEvent.Value}");
                if (changeEvent.Value.TryGet(out NetworkObject noR))
                {
                    heldDominosObj.Remove(noR.gameObject);
                }
                break;
        }
    }
}