using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Cooler : NetworkBehaviour
{
    //public PlayerStats user;
    //public TimeAttack timeAttack;
    public GameObject fishStatCanvas;

    public void OnFishCatch(ulong networkObjectId)
    {
        Debug.Log("Here!");
        NetworkObject newFish = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];

        if(newFish != null)
        {
            FishStats newFishStats = newFish.GetComponent<FishStats>();
            fishStatCanvas.transform.Find("Caught").GetComponent<TextMeshProUGUI>().text = "You Caught: " + newFish.gameObject.name;
            fishStatCanvas.transform.Find("Weight").GetComponent<TextMeshProUGUI>().text = "Weight " + System.Math.Round(newFishStats.weight, 2) + " lb";
            fishStatCanvas.transform.Find("Worth").GetComponent<TextMeshProUGUI>().text = "Worth: " + System.Math.Round(newFishStats.weight * newFishStats.multiplier, 2) + "$";
            fishStatCanvas.SetActive(true);
        }

    }
}
