using UnityEngine;
using UnityEngine.UI;

public class PlantGrowth : MonoBehaviour
{
    [Header("Plant Stages")]
    public GameObject[] growthStages; // Array of plant stage prefabs to visualize growth stages
    public Slider wateringSlider; // UI slider to display watering progress
    public Slider timerSlider; // UI slider to display time left until next growth stage or death

    [Header("Stage Configurations")]
    public float wateringTimeRequired = 5f; // Time required to water the plant sufficiently
    public float[] stageDurations; // Duration of each growth stage

    private float wateringTime = 0f; // Tracks accumulated watering time
    private float stageTimer = 0f; // Timer for the current growth stage
    private int currentStage = 0; // Index of the current plant stage
    private bool isWatered = false; // Flag to track if the plant has been watered adequately

    public GameObject[] dropItems; // Items to drop when the plant is harvested
    public GameObject plantBed; // Reference to the plant bed containing this plant

    void Awake()
    {
        SetActiveStage(currentStage); // Initialize the plant's current stage at start
        wateringSlider.gameObject.SetActive(currentStage < 3); // Show watering slider if in early stages
        timerSlider.gameObject.SetActive(false); // Hide timer slider initially
        UpdateWateringSlider(); // Initialize the watering slider display
        Debug.Log("Awake: Initial stage set.");
    }

    void Update()
    {
        // Increment timers if the plant is in a growth stage
        if (currentStage > 0)
        {
            stageTimer += Time.deltaTime;
        }

        // Check if it's time to move to the next stage or if the plant dies
        if (currentStage > 0 && currentStage < 3)
        {
            if (stageTimer >= stageDurations[currentStage - 1])
            {
                if (!isWatered)
                {
                    Die(); // Kill the plant if it wasn't watered enough
                }
                else
                {
                    AdvanceStage(); // Move to the next growth stage
                }
            }
        }

        // Manage the watering slider visibility and updates
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

        // Manage the timer slider visibility and updates
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

    // Advances the growth stage of the plant
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

    // Handles the plant dying due to neglect
    void Die()
    {
        currentStage = 4; // Set to a 'dead' stage
        SetActiveStage(currentStage);
        timerSlider.gameObject.SetActive(false);
        wateringSlider.gameObject.SetActive(false);
        Debug.Log("Die: Plant has died due to neglect.");
    }

    // Updates the UI timer slider based on time remaining in the current stage
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

    // Sets the active visual stage for the plant
    void SetActiveStage(int stage)
    {
        for (int i = 0; i < growthStages.Length; i++)
        {
            growthStages[i].SetActive(i == stage);
        }
        Debug.Log($"SetActiveStage: Active stage set to {stage}.");
    }

    // Updates the UI watering slider based on the current watering progress
    void UpdateWateringSlider()
    {
        wateringSlider.value = wateringTime / wateringTimeRequired;
    }

    // Handles collisions with a shovel for harvesting or destroying the plant
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Shovel" && currentStage == 3)
        {
            Harvest(); // Harvest the plant if mature
        }
        else if (collision.gameObject.tag == "Shovel")
        {
            if (plantBed != null)
            {
                plantBed.tag = "Unplanted"; // Reset the bed tag if plant is destroyed before maturity
            }
            Destroy(gameObject); // Destroy the plant
            Debug.Log("Destroyed plant");
        }
    }

    // Handles water particle collisions for watering the plant
    void OnParticleCollision(GameObject other)
    {
        if (other.tag == "Water" && currentStage < 3)
        {
            wateringTime += Time.deltaTime; // Increment watering time when hit by water particles
        }
    }

    // Handles the harvesting process, creating drop items and cleaning up
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
            plantBed.tag = "Unplanted"; // Reset the plant bed's tag
        }
        Destroy(gameObject); // Destroy the plant after harvesting
        Debug.Log("Harvest: Plant harvested and object destroyed.");
    }
}