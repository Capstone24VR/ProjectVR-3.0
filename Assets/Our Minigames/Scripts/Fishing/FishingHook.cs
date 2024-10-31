using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class FishingHook : MonoBehaviour
{
    private int fishLayer;
    public GameObject caughtObject = null;
    public FishingMiniGame miniGame;
    public bool caughtSomething = false;

    private void Awake()
    {
        fishLayer = LayerMask.NameToLayer("Fish");
    }

    private void Update()
    {
        if (caughtSomething)
        {
            gameObject.transform.position = caughtObject.transform.Find("HookSpot").transform.position;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.tag);
        if (collision.gameObject.tag == "Fish")
        {
            if (!caughtSomething)
            {
                caughtObject = collision.gameObject.transform.parent.gameObject;
                caughtObject.GetComponent<FishAI>().state = FishAI.FishState.Struggle;
                miniGame.ResetBar();
                miniGame.struggleFish = caughtObject;
                miniGame.maxFailTimer = 3f * caughtObject.GetComponent<FishStats>().baitChance;
                miniGame.pause = false;
                miniGame.gameObject.SetActive(true);
                caughtSomething = true;
                Debug.Log("I baited: " + collision.gameObject.transform.parent.name);
            }
        }
    }
}
