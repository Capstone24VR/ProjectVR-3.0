using UnityEngine;

public class PlantBedManager : MonoBehaviour
{
    public GameObject[] plantBeds; // Array of all plant beds in the scene
    private GameObject[] barriers; // Array to hold the barrier GameObjects for each plant bed
    public int[] unlockThresholds; // Number of completed jobs required to unlock each bed
    private int completedJobs = 0; // Counter for the number of completed jobs

    void Start()
    {
        barriers = new GameObject[plantBeds.Length];

        // Initialize by finding and disabling all barrier GameObjects except for the first one
        for (int i = 0; i < plantBeds.Length; i++)
        {
            barriers[i] = plantBeds[i].transform.Find("Barrier").gameObject; // Find the barrier GameObject

            if (i == 0)
            {
                barriers[i].SetActive(false); // Ensure the first bed's barrier is active (unlocked)
            }
            else
            {
                barriers[i].SetActive(true); // Deactivate other barriers (locked)
            }
        }
    }

    // Call this method when a job is completed
    public void OnJobCompleted()
    {
        completedJobs++; // Increment the count of completed jobs
        Debug.Log("Job completed. Total completed jobs: " + completedJobs);

        // Check if any plant bed can be unlocked based on completed jobs
        for (int i = 0; i < barriers.Length; i++)
        {
            if (barriers[i].activeSelf && completedJobs >= unlockThresholds[i])
            {
                barriers[i].SetActive(false); // Deactivate the barrier (unlock the bed)
                Debug.Log("Plant bed unlocked: " + plantBeds[i].name);
            }
        }
    }
}