using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;
using XRMultiplayer.MiniGames;
using Unity.Netcode;
using UnityEngine.UIElements;

public class JobOrders : NetworkBehaviour
{
    [Header("Plant Stages")]
    public GameObject[] plantPrefabs; // Prefabs for each plant type
    public GameObject basket; // Reference to the pre-placed basket in the scene
    public TextMeshProUGUI jobOrderText; // TextMeshPro component to display the job order on the basket

    [Header("Job Configuration")]
    public int maxItems = 3; // Maximum number of different items that can be requested in a job

    private Dictionary<string, int> currentJobRequirements; // Current job requirements using tags
    private Dictionary<string, int> itemsInBasket = new Dictionary<string, int>(); // Items currently in the basket
    private List<GameObject> refBasketContents = new List<GameObject>(); // List to keep track of GameObjects in the basket for debugging
    private NetworkList<NetworkObjectReference> basketContents = new NetworkList<NetworkObjectReference>();// List to keep track of GameObjects in the basket
    string[] plantTypes = { "Carrot", "Cucumber", "Tomato", "Onion" };

    [Header("MiniGame")]
    public MiniGame_Gardening gardeningGame;
    public PlantBedManager pbm;

    void Start()
    {
        gardeningGame = FindObjectOfType<MiniGame_Gardening>(); // Find the MiniGame_Gardening script in the scene
        basketContents.OnListChanged += OnBasketChange;
        if (gardeningGame == null)
        {
            Debug.LogError("Failed to find the MiniGame_Gardening script.");
            return; // Optionally return to prevent further execution
        }
    }

    public void CreateNewJob()
    {
        if (IsServer)
        {
            StartCoroutine(CreateNewJobOnServer());
        }
    }

    IEnumerator CreateNewJobOnServer()
    {
        currentJobRequirements = GenerateRandomJob();
        
        ClearBasketContents(); // Clear basket contents from previous job
        Debug.Log("New job created by server");
        DisplayJobOrderClientRpc();
        yield return null;
    }

    Dictionary<string, int> GenerateRandomJob()
    {
        Dictionary<string, int> job = new Dictionary<string, int>();
        List<int> m_plantType = new List<int>();
        List<int> m_quantity = new List<int>();

        int index = 0;

        int totalItems = 0; // Track the total number of items
        while (totalItems < maxItems)
        {
            //string plantType = plantTypes[Random.Range(0, plantTypes.Length)];
            index = Random.Range(0, plantTypes.Length);
            string plantType = plantTypes[index];
            
           
            // Ensure that adding more items won't exceed the maxItems limit
            int maxPossibleQuantity = Mathf.Min(maxItems - totalItems, 3); // Ensure we don't exceed maxItems
            int quantity = Random.Range(1, maxPossibleQuantity + 1);

            if (!job.ContainsKey(plantType))
            {
                job.Add(plantType, quantity);
                m_plantType.Add(index);
                m_quantity.Add(quantity);
            }
            else
            {
                job[plantType] += quantity;
                m_quantity[index] += quantity;
            }
            totalItems += quantity; // Update the total items count

            if (totalItems >= maxItems) break; // Break if adding more items would exceed the limit
        }

        SyncJobClientRpc(m_plantType.ToArray(), m_quantity.ToArray());

        return job;
    }

    [ClientRpc]
    void DisplayJobOrderClientRpc()
    {
        jobOrderText.text = "Fill with:\n";
        foreach (var item in currentJobRequirements)
            jobOrderText.text += $"{item.Value} x {item.Key}\n";
    }

    [ClientRpc]
    void SyncJobClientRpc(int[] plantType, int[] quantity)
    {
        currentJobRequirements.Clear();
        for (int i = 0; i < plantType.Length; i++)
        {
            currentJobRequirements.Add(plantTypes[plantType[i]], quantity[i]);
        }
    }

    public void ClearBasketContents()
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
        if (gardeningGame != null)
        {
            gardeningGame.LocalPlayerCompletedJob(10); // Safely call the method if gardeningGame is not null
        }
        else
        {
            Debug.LogWarning("Gardening game script reference is not set.");
        }
        StartCoroutine(CompleteJobRoutine());
    }

    IEnumerator CompleteJobRoutine()
    {
        jobOrderText.text = "Job Complete!";
        yield return new WaitForSeconds(3); // Wait for 3 seconds
        CreateNewJob(); // Refresh the job order
        Debug.Log("Job complete. Attempting to unlock next plantbed");
        pbm.OnJobCompleted(); // Unlock the next plant bed
    }

    void OnBasketChange(NetworkListEvent<NetworkObjectReference> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<NetworkObjectReference>.EventType.Add:
                Debug.Log($"Item added to basket contents: {changeEvent.Value}");
                if (changeEvent.Value.TryGet(out NetworkObject noA))
                {
                    refBasketContents.Add(noA.gameObject);
                }
                break;

            case NetworkListEvent<NetworkObjectReference>.EventType.Remove:
                Debug.Log($"Item removed from basket contents: {changeEvent.Value}");
                if (changeEvent.Value.TryGet(out NetworkObject noR))
                {
                    refBasketContents.Remove(noR.gameObject);
                }
                break;
        }
    }
}