using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CompostBin : MonoBehaviour
{
    [Header("Compost Bin Settings")]
    public List<string> acceptableTags; // Tags that the bin will accept
    public int requiredItems = 5; // Number of items needed to start composting
    public GameObject fertilizerPrefab; // Fertilizer prefab to instantiate
    public Slider timerSlider; // Timer slider UI
    public float compostingDuration = 30f; // Time in seconds to compost

    private int itemCount = 0; // Current count of items in the bin
    private float timer = 0; // Timer to track composting duration
    private bool isComposting = false; // Flag to check if composting is happening

    void Update()
    {
        if (isComposting)
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
                timerSlider.value = timer / compostingDuration;
            }
            else
            {
                Instantiate(fertilizerPrefab, transform.position, Quaternion.identity);
                ResetComposting();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the object's tag is in the list of acceptable tags
        Debug.Log("Triggered");
        if (acceptableTags.Contains(other.tag))
        {
            Destroy(other.gameObject); // Assume the item is destroyed when put in the bin
            itemCount++;
            Debug.Log("Added to composting");

            if (itemCount >= requiredItems && !isComposting)
            {
                StartComposting();
            }
        }
    }

    void StartComposting()
    {
        isComposting = true;
        timer = compostingDuration;
        timerSlider.maxValue = compostingDuration;
        timerSlider.value = compostingDuration;
        timerSlider.gameObject.SetActive(true);
        Debug.Log("Composting started.");
    }

    void ResetComposting()
    {
        isComposting = false;
        itemCount = 0;
        timerSlider.gameObject.SetActive(false);
        Debug.Log("Composting finished. Fertilizer created.");
    }
}

