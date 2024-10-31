using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class FishManager : MonoBehaviour
{

    public int maxFish = 30;
    public int currFish = 10;
    public bool gameStart = false;

    public float spawnTimer = 0f;
    public float maxSpawnTime = 5f;

    public GameObject [] fish = new GameObject [3];

    public List<string> names = new List<string> ();
    private float[] baitChanceArr = { .001f, .05f, .15f, .25f, .35f, .40f, .60f};
    private float totalChance = 1.721f;


    private void Awake()
    {
        totalChance = 0f;
        foreach(var chance in baitChanceArr)
        {
            totalChance += chance;
        }

        names.Add("Selase");
        names.Add("Tony");
        names.Add("Jake");
        names.Add("Pam");
        names.Add("Rick");
        names.Add("John");
        names.Add("Farquad");
        names.Add("Goku");
        names.Add("Sturividant");
        names.Add("Chauh");
        names.Add("Thimble");
        names.Add("Enyo");
        names.Add("Rick");
        names.Add("Chindog");
        names.Add("Fabio");
        names.Add("Nii");
        names.Add("Sally");
        names.Add("Trish");
        names.Add("Dela");
    }


    // Update is called once per frame
    void Update()
    {
        currFish = this.transform.childCount;
        if(currFish <= maxFish)
        {
            spawnTimer += Time.deltaTime;
            if(spawnTimer >= maxSpawnTime ) 
            {
                float currentCheck = 0f;
                int type = 0;

                float roll = Random.Range(0f, totalChance);
                foreach (float fishChance in baitChanceArr) {
                    currentCheck += fishChance;

                    if(roll <= currentCheck)
                    {
                        break;
                    }

                    else if(currentCheck == totalChance)
                    {
                        break;
                    }
                    type++;
                }

                int name = Random.Range(0, names.Count);

                Vector3 spawnPoint = new Vector3(Random.Range(-22f, 22f), transform.position.y, Random.Range(-22f, 22f));
                fish[type].SetActive(true);
                var spawn = Instantiate(fish[type], spawnPoint, Quaternion.identity, this.transform);
                spawn.transform.localScale = Vector3.one * spawn.GetComponent<FishAI>().stats.weight;
                fish[type].SetActive(false);
                spawn.name = names[name] + " the " + fish[type].name;
                spawn.SetActive(true);
                spawnTimer = 0f;
            }
        }
    }
}
