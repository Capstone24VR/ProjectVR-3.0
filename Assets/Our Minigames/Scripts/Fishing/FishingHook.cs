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

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Fish")
        {
            if (!caughtSomething.Value)
            {
                caughtObject = other.transform.parent.gameObject;
                caughtObject.GetComponent<NetworkedFishAI>().SetFishStateServerRpc(NetworkedFishAI.FishState.Struggle);
                caughtSomething.Value = true;
                Debug.Log("I baited: " + other.gameObject.transform.parent.name);
            }
        }
    }
}
