using Unity.Netcode;
using UnityEngine;

public class FishingHook : NetworkBehaviour
{

    public GameObject caughtObject = null;
    public NetworkVariable<bool> caughtSomething = new NetworkVariable<bool>(false);
    private void Update()
    {
        if (caughtSomething.Value)
        {
            gameObject.transform.position = caughtObject.transform.Find("HookSpot").transform.position;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.tag);
        if (collision.gameObject.tag == "Fish")
        {
            if (!caughtSomething.Value)
            {
                caughtObject = collision.transform.parent.gameObject;
                caughtObject.GetComponent<NetworkedFishAI>().SetFishStateServerRpc(NetworkedFishAI.FishState.Struggle);
                caughtSomething.Value = true;
                Debug.Log("I baited: " + collision.gameObject.transform.parent.name);
            }
        }
    }
}
