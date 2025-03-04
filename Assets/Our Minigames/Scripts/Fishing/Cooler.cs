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
    public AudioSource fishSellSFX;

    public void OnFishCatch(ulong networkObjectId)
    {
        NetworkObject newFish = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];

        if (newFish != null)
        {
            fishSellSFX.Play();

            FishStats newFishStats = newFish.GetComponent<FishStats>();
            fishStatCanvas.transform.Find("Caught").GetComponent<TextMeshProUGUI>().text = "You Caught: " + newFish.gameObject.name;
            fishStatCanvas.transform.Find("Weight").GetComponent<TextMeshProUGUI>().text = "Weight " + System.Math.Round(newFishStats.weight, 2) + " lb";
            fishStatCanvas.transform.Find("Worth").GetComponent<TextMeshProUGUI>().text = "Worth: " + System.Math.Round(newFishStats.weight * newFishStats.multiplier, 2) + "$";
            fishStatCanvas.SetActive(true);

            newFish.GetComponent<NetworkedFishAI>().DestroyServerRpc();
        }
    }
}
