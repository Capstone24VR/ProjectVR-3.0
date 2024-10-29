using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Cooler : MonoBehaviour
{
    public PlayerStats user;
    public TimeAttack timeAttack;
    public GameObject fishStatCanvas;

    public FishAI newFish;

    public void OnFishCatch()
    {
        timeAttack.AddScore(Mathf.FloorToInt(newFish.stats.weight * 100));
        timeAttack.AddTime(newFish.stats.weight*20);


        user.money += newFish.stats.multiplier * newFish.stats.weight;
        user.totalCaughtFish += 1;
        if (newFish.stats.weight > user.biggestWeight)
        {
            user.biggestWeight = newFish.stats.GetComponent<FishStats>().weight;
            user.biggestFish = newFish.stats.name;
        }

        fishStatCanvas.transform.Find("Caught").GetComponent<TextMeshProUGUI>().text = "You Caught: " + newFish.gameObject.name;
        fishStatCanvas.transform.Find("Weight").GetComponent<TextMeshProUGUI>().text = "Weight " + System.Math.Round(newFish.stats.weight, 2) + " lb";
        fishStatCanvas.transform.Find("Worth").GetComponent<TextMeshProUGUI>().text = "Worth: " + System.Math.Round(newFish.stats.weight * newFish.stats.multiplier, 2) + "$";
        fishStatCanvas.SetActive(true);
    }
}
