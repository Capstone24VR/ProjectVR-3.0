using Unity.Netcode;
using UnityEngine;

public class FishingHook : NetworkBehaviour
{

    public GameObject caughtObject = null;
    public NetworkVariable<bool> rodDropped = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> caughtSomething = new NetworkVariable<bool>(false);
    private void Update()
    {
        //if (caughtSomething.Value)
        //{
        //    gameObject.transform.position = caughtObject.transform.Find("HookSpot").transform.position;
        //}
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Fish")
        {
            if (!caughtSomething.Value)
            {
                //caughtObject = other.transform.parent.gameObject;
                //caughtObject.GetComponent<NetworkedFishAI>().SetFishStateServerRpc(NetworkedFishAI.FishState.Struggle); 
                //caughtSomething.Value = true;

                ulong fishNetworkId = other.transform.parent.GetComponent<NetworkObject>().NetworkObjectId;
                CatchFishServerRpc(fishNetworkId);

                Debug.Log("I baited: " + other.gameObject.transform.parent.name);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CatchFishServerRpc(ulong fishNetworkId)
    {
        if(!caughtSomething.Value)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(fishNetworkId, out NetworkObject fishNetworkObject))
            {
                caughtSomething.Value = true;
                caughtObject = fishNetworkObject.gameObject;

                // Notify clients about the catch
                NotifyCatchClientRpc(fishNetworkId);


                // Update fish state
                fishNetworkObject.GetComponent<NetworkedFishAI>().SetFishStateServerRpc(NetworkedFishAI.FishState.Struggle);
            }
            else
            {
                Debug.LogError($"Fish with NetworkObjectId {fishNetworkId} not found!");
            }
        }
    }


    [ClientRpc]
    private void NotifyCatchClientRpc(ulong fishNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(fishNetworkId, out NetworkObject fishNetworkObject))
        {
            caughtObject = fishNetworkObject.gameObject;
            Debug.Log($"Caught fish: {caughtObject.name} on client.");
        }
        else
        {
            Debug.LogError($"Fish with NetworkObjectId {fishNetworkId} not found on client!");
        }
    }
}

