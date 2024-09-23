using UnityEngine;
using UnityEngine.UI;

public class PlantGrowth : MonoBehaviour
{
    [Header("Plant Stages")]
    public GameObject[] growthStages;
    public Slider wateringSlider;
    public Slider timerSlider;  // Slider to show time until the next stage or death

    [Header("Stage Configurations")]
    public float wateringTimeRequired = 5f;
    public float[] stageDurations;

    private float wateringTime = 0f;
    private float showWateringSliderTime;
    private float stageTimer = 0f;
    private int currentStage = 0;
    private bool isWatered = false;

    public GameObject[] dropItems;
    public GameObject plantBed;


    void Awake()
    {
        SetActiveStage(currentStage);
        wateringSlider.gameObject.SetActive(currentStage < 3);
        timerSlider.gameObject.SetActive(false);  // Initially disable the timer slider
        UpdateWateringSlider();
        Debug.Log("Awake: Initial stage set.");
    }
    void Update()
    {
        if (currentStage == 3)
        {

        }
        if (currentStage > 0)
        {
            stageTimer += Time.deltaTime;
            Debug.Log($"Update: Stage {currentStage} timer updated to {stageTimer}.");
        }

        if (currentStage > 0 && currentStage < 3)
        {
            if (stageTimer >= stageDurations[currentStage - 1])
            {
                if (!isWatered)
                {
                    Debug.Log("Update: Timer exceeded without sufficient watering, plant will die.");
                    Die();
                }
                else
                {
                    Debug.Log("Update: Timer exceeded, plant has been watered, advancing stage.");
                    AdvanceStage();
                }
            }
        }

        if (currentStage < 3 && !isWatered)
        {
            wateringSlider.gameObject.SetActive(true);
            UpdateWateringSlider();
            if (wateringTime >= wateringTimeRequired)
            {
                isWatered = true;
                wateringSlider.gameObject.SetActive(false);
                Debug.Log($"Update: Plant has been sufficiently watered at stage {currentStage}.");
                if (currentStage == 0)
                {
                    AdvanceStage();
                }
            }
        }

        if (currentStage > 0 && currentStage < 3)
        {
            timerSlider.gameObject.SetActive(true);
            UpdateTimerSlider();
        }
        else if (currentStage == 0 || currentStage >= 3)
        {
            timerSlider.gameObject.SetActive(false);
        }
    }

    private void AdvanceStage()
    {
        if (currentStage < 3)
        {
            currentStage++;
            stageTimer = 0f;
            wateringTime = 0f;
            isWatered = false;
            SetActiveStage(currentStage);
            Debug.Log($"AdvanceStage: Moved to stage {currentStage}.");
        }
    }

    void Die()
    {
        currentStage = 4;
        SetActiveStage(currentStage);
        timerSlider.gameObject.SetActive(false);
        wateringSlider.gameObject.SetActive(false);
        Debug.Log("Die: Plant has died due to neglect.");
    }

    private void UpdateTimerSlider()
    {
        if (currentStage < 3)
        {
            float timeLeft = stageDurations[currentStage] - stageTimer;
            timerSlider.maxValue = stageDurations[currentStage];
            timerSlider.value = timeLeft;
            Debug.Log($"UpdateTimerSlider: Timer set to {timeLeft} seconds remaining.");
        }
        else
        {
            timerSlider.gameObject.SetActive(false);
        }
    }

    void SetActiveStage(int stage)
    {
        for (int i = 0; i < growthStages.Length; i++)
        {
            growthStages[i].SetActive(i == stage);
        }
        Debug.Log($"SetActiveStage: Active stage set to {stage}.");
    }

    void UpdateWateringSlider()
    {
        wateringSlider.value = wateringTime / wateringTimeRequired;
        Debug.Log($"UpdateWateringSlider: Watering slider updated to {wateringSlider.value}.");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Shovel" && currentStage == 3)
        {
            Debug.Log("OnCollisionEnter: Shovel detected, harvesting plant.");
            Harvest();
        }
        else
        {
            if (plantBed != null)
            {
                plantBed.tag = "Unplanted";
            }
            Destroy(gameObject);
            Debug.Log("Destroyed plant");
        }
    }

    void OnParticleCollision(GameObject other)
    {
        if (other.tag == "Water" && currentStage < 3)
        {
            wateringTime += Time.deltaTime;
            Debug.Log($"OnParticleCollision: Plant being watered, total watering time {wateringTime}.");
        }
    }

    void Harvest()
    {
        foreach (GameObject item in dropItems)
        {
            Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
            Instantiate(item, spawnPosition, Quaternion.identity);
            Debug.Log($"Harvest: Dropping item {item.name} at {spawnPosition}.");
        }

        if (plantBed != null)
        {
            plantBed.tag = "Unplanted";
        }
        Destroy(gameObject);
        Debug.Log("Harvest: Plant harvested and object destroyed.");

    }
}



