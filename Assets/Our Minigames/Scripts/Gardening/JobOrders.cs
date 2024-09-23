using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;

public class JobOrders : MonoBehaviour
{
    [Header("Plant Stages")]
    public GameObject[] plantPrefabs; // Prefabs for each plant type
    public GameObject basket; // Reference to the pre-placed basket in the scene
    public TextMeshProUGUI jobOrderText; // TextMeshPro component to display the job order on the basket

    [Header("Job Configuration")]
    public int maxItems = 3; // Maximum number of different items that can be requested in a job

    private Dictionary<string, int> currentJobRequirements; // Current job requirements using tags
    private Dictionary<string, int> itemsInBasket = new Dictionary<string, int>(); // Items currently in the basket
    private List<GameObject> basketContents = new List<GameObject>(); // List to keep track of GameObjects in the basket

    public PlantBedManager pbm;


    void Start()
    {
        CreateNewJob();
    }

    void CreateNewJob()
    {
        currentJobRequirements = GenerateRandomJob();
        DisplayJobOrder();
        ClearBasketContents(); // Clear basket contents from previous job
        Debug.Log("New job created");
    }

    Dictionary<string, int> GenerateRandomJob()
    {
        Dictionary<string, int> job = new Dictionary<string, int>();
        string[] plantTypes = { "Carrot", "Cucumber", "Tomato", "Onion" };

        int totalItems = 0; // Track the total number of items
        while (totalItems < maxItems)
        {
            string plantType = plantTypes[Random.Range(0, plantTypes.Length)];
            // Ensure that adding more items won't exceed the maxItems limit
            int maxPossibleQuantity = Mathf.Min(maxItems - totalItems, 3); // Ensure we don't exceed maxItems
            int quantity = Random.Range(1, maxPossibleQuantity + 1);

            if (!job.ContainsKey(plantType))
            {
                job.Add(plantType, quantity);
            }
            else
            {
                job[plantType] += quantity;
            }
            totalItems += quantity; // Update the total items count
            if (totalItems >= maxItems) break; // Break if adding more items would exceed the limit
        }

        return job;
    }


    void DisplayJobOrder()
    {
        jobOrderText.text = "Fill with:\n";
        foreach (var item in currentJobRequirements)
            jobOrderText.text += $"{item.Value} x {item.Key}\n";
    }

    void ClearBasketContents()
    {
        foreach (GameObject item in basketContents)
            Destroy(item); // Physically remove the items from the scene

        basketContents.Clear(); // Clear the list of item references
        itemsInBasket.Clear(); // Reset the count of items
        Debug.Log("Cleared Basket Contents");
    }

    void OnTriggerEnter(Collider other)
    {
        string tag = other.gameObject.tag;

        if (currentJobRequirements.ContainsKey(tag))
        {
            if (!itemsInBasket.ContainsKey(tag))
                itemsInBasket[tag] = 0;
            itemsInBasket[tag]++;
            basketContents.Add(other.gameObject); // Add the GameObject to the list
            CheckAndCompleteJob(); // Check if the job is completed after adding an item
        }
    }

    void OnTriggerExit(Collider other)
    {
        string tag = other.gameObject.tag;

        if (itemsInBasket.ContainsKey(tag))
        {
            itemsInBasket[tag]--;
            basketContents.Remove(other.gameObject); // Remove the GameObject from the list
            if (itemsInBasket[tag] <= 0)
            {
                itemsInBasket.Remove(tag);
            }
            CheckAndCompleteJob(); // Check if the job is still valid after removing an item
        }
    }

    void CheckAndCompleteJob()
    {
        foreach (var requirement in currentJobRequirements)
        {
            if (!itemsInBasket.ContainsKey(requirement.Key) || itemsInBasket[requirement.Key] < requirement.Value)
                return; // Job not completed if requirements not met
        }

        StartCoroutine(CompleteJobRoutine());
    }

    IEnumerator CompleteJobRoutine()
    {
        jobOrderText.text = "Job Complete!";
        yield return new WaitForSeconds(3); // Wait for 3 seconds
        CreateNewJob(); // Refresh the job order
        Debug.Log("Attempting to unlock next plantbed");
        pbm.OnJobCompleted(); // Unlock the next plant bed
    }
}







//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class JobOrders : MonoBehaviour
//{
//    [Header("Plant Stages")]
//    public GameObject[] plantPrefabs; // Prefabs for each plant type
//    public GameObject basketPrefab; // Prefab of the basket
//    public Transform basketSpawnLocation; // Location to spawn the baskets
//    public TextMeshProUGUI jobOrderText; // TextMeshPro component to display the job order on the basket

//    [Header("Job Configuration")]
//    public int maxItems = 3; // Maximum number of different items that can be requested in a job

//    private Dictionary<string, int> currentJobRequirements; // Current job requirements using tags
//    private Dictionary<string, int> itemsInBasket = new Dictionary<string, int>(); // Items currently in the basket

//    void Start()
//    {
//        CreateNewJob();
//    }

//    void CreateNewJob()
//    {
//        currentJobRequirements = GenerateRandomJob();
//        DisplayJobOrder();
//        InstantiateBasket();
//        Debug.Log("New job created with fresh requirements.");
//    }

//    Dictionary<string, int> GenerateRandomJob()
//    {
//        Dictionary<string, int> job = new Dictionary<string, int>();
//        string[] plantTypes = { "Cabbage", "Carrot", "Cucumber", "Onion", "Potato", "Pumpkin", "Tomato" };
//        int numItemsRequired = Random.Range(1, Mathf.Min(maxItems, plantTypes.Length) + 1);

//        for (int i = 0; i < numItemsRequired; i++)
//        {
//            string plantType = plantTypes[Random.Range(0, plantTypes.Length)];
//            int quantity = Random.Range(1, 4);

//            if (!job.ContainsKey(plantType))
//            {
//                job.Add(plantType, quantity);
//                Debug.Log($"Added {plantType} with quantity {quantity} to job requirements.");
//            }
//            else
//            {
//                job[plantType] += quantity; // In case the same type is randomly selected more than once
//                Debug.Log($"Increased quantity of {plantType} to {job[plantType]}.");
//            }
//        }

//        return job;
//    }

//    void DisplayJobOrder()
//    {
//        jobOrderText.text = "Fill with:\n";
//        foreach (var item in currentJobRequirements)
//        {
//            jobOrderText.text += $"{item.Value} x {item.Key}\n";
//        }
//    }

//    void InstantiateBasket()
//    {
//        GameObject existingBasket = GameObject.FindGameObjectWithTag("Basket");
//        if (existingBasket == null)
//        {
//            Instantiate(basketPrefab, basketSpawnLocation.position, Quaternion.identity).tag = "Basket";
//            Debug.Log("Basket instantiated at spawn location.");
//        }
//        else
//        {
//            Debug.Log("Basket already exists, not creating a new one.");
//        }
//    }

//    void OnTriggerEnter(Collider other)
//    {
//        string tag = other.gameObject.tag;

//        if (currentJobRequirements.ContainsKey(tag))
//        {
//            if (itemsInBasket.ContainsKey(tag))
//                itemsInBasket[tag]++;
//            else
//                itemsInBasket[tag] = 1;

//            Debug.Log($"Added {tag}. Total in basket: {itemsInBasket[tag]}.");
//            CheckAndCompleteJob();
//        }
//    }

//    void OnTriggerExit(Collider other)
//    {
//        string tag = other.gameObject.tag;

//        if (currentJobRequirements.ContainsKey(tag) && itemsInBasket.ContainsKey(tag))
//        {
//            itemsInBasket[tag]--;
//            if (itemsInBasket[tag] == 0)
//                itemsInBasket.Remove(tag);

//            Debug.Log($"Removed {tag}. Remaining in basket: {itemsInBasket[tag]}.");
//        }
//    }

//    void CheckAndCompleteJob()
//    {
//        foreach (var requirement in currentJobRequirements)
//        {
//            if (!itemsInBasket.ContainsKey(requirement.Key) || itemsInBasket[requirement.Key] < requirement.Value)
//            {
//                Debug.Log($"Job not completed, missing {requirement.Key} or insufficient quantity.");
//                return;
//            }
//        }

//        Debug.Log("Job completed successfully!");
//        Destroy(GameObject.FindGameObjectWithTag("Basket"));
//        CreateNewJob();
//    }
//}




//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro; // Make sure to include this for TextMeshPro components

//public class JobOrders : MonoBehaviour
//{
//    [Header("Plant Stages")]
//    public GameObject[] plantPrefabs; // Prefabs for each plant type
//    public GameObject basketPrefab; // Prefab of the basket
//    public Transform basketSpawnLocation; // Location to spawn the baskets
//    public TextMeshProUGUI jobOrderText; // TextMeshPro component to display the job order on the basket

//    private Dictionary<GameObject, int> currentJobRequirements; // Current job requirements using GameObjects
//    private Dictionary<GameObject, int> itemsInBasket = new Dictionary<GameObject, int>(); // Items currently in the basket

//    void Start()
//    {
//        CreateNewJob();
//    }

//    void CreateNewJob()
//    {
//        currentJobRequirements = GenerateRandomJob();
//        DisplayJobOrder();
//        InstantiateBasket();
//        Debug.Log("New job created with fresh requirements.");
//    }

//    Dictionary<GameObject, int> GenerateRandomJob()
//    {
//        Dictionary<GameObject, int> job = new Dictionary<GameObject, int>();
//        int numItemsRequired = Random.Range(1, 4); // Randomize the number of different items needed

//        for (int i = 0; i < numItemsRequired; i++)
//        {
//            GameObject plantType = plantPrefabs[Random.Range(0, plantPrefabs.Length)];
//            int quantity = Random.Range(1, 4); // Random quantity for each plant type

//            if (!job.ContainsKey(plantType))
//            {
//                job.Add(plantType, quantity);
//                Debug.Log($"Added {plantType.name} with quantity {quantity} to job requirements.");
//            }
//            else
//            {
//                job[plantType] += quantity; // In case the same type is randomly selected more than once
//                Debug.Log($"Increased quantity of {plantType.name} to {job[plantType]}.");
//            }
//        }

//        return job;
//    }

//    void DisplayJobOrder()
//    {
//        jobOrderText.text = "Fill with:\n";
//        foreach (var item in currentJobRequirements)
//        {
//            jobOrderText.text += $"{item.Value} x {item.Key.name}\n"; // Display the name of the plant type
//            Debug.Log($"Job requires {item.Value} x {item.Key.name}.");
//        }
//    }

//    void InstantiateBasket()
//    {
//        // Check if there's already a basket in the scene
//        GameObject existingBasket = GameObject.FindGameObjectWithTag("Basket");
//        if (existingBasket == null) // Only instantiate if no basket exists
//        {
//            GameObject newBasket = Instantiate(basketPrefab, basketSpawnLocation.position, Quaternion.identity);
//            newBasket.tag = "Basket"; // Ensure the instantiated basket has the correct tag
//            Debug.Log("Basket instantiated at spawn location.");
//        }
//        else
//        {
//            Debug.Log("Basket already exists, not creating a new one.");
//        }
//    }

//    void OnTriggerEnter(Collider other)
//    {
//        Debug.Log("Item collided with basket");
//        GameObject item = other.gameObject;

//        // Check if the collided object is one of the plant types required
//        if (currentJobRequirements.ContainsKey(item))
//        {
//            if (itemsInBasket.ContainsKey(item))
//                itemsInBasket[item]++;
//            else
//                itemsInBasket[item] = 1;

//            Debug.Log($"Added {item.name}. Total in basket: {itemsInBasket[item]}.");
//            CheckAndCompleteJob();
//        }
//    }

//    void OnTriggerExit(Collider other)
//    {
//        GameObject item = other.gameObject;

//        if (currentJobRequirements.ContainsKey(item) && itemsInBasket.ContainsKey(item))
//        {
//            itemsInBasket[item]--;
//            if (itemsInBasket[item] == 0)
//                itemsInBasket.Remove(item);

//            Debug.Log($"Removed {item.name}. Remaining in basket: {itemsInBasket[item]}.");
//        }
//    }

//    void CheckAndCompleteJob()
//    {
//        bool jobCompleted = true;
//        foreach (var requirement in currentJobRequirements)
//        {
//            if (!itemsInBasket.ContainsKey(requirement.Key) || itemsInBasket[requirement.Key] < requirement.Value)
//            {
//                Debug.Log($"Job not completed, missing {requirement.Key.name} or insufficient quantity.");
//                jobCompleted = false;
//                break;
//            }
//        }

//        if (jobCompleted)
//        {
//            Debug.Log("Job completed successfully!");
//            Destroy(GameObject.FindGameObjectWithTag("Basket")); // Assuming basket has a tag "Basket"
//            CreateNewJob();
//        }
//    }
//}


