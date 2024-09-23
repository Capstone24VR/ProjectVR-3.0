using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlantSpawner : MonoBehaviour
{
    public GameObject plantPrefab; // The prefab of the plant

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable; // Reference to the XRGrabInteractable component

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grabInteractable.selectExited.AddListener(OnPlantReleased);
    }

    private void OnPlantReleased(SelectExitEventArgs args)
    {
        SpawnNewPlant();
        Destroy(gameObject); // Destroy the current plant after it is released
    }

    // Spawn a new plant at the current position
    void SpawnNewPlant()
    {
        Instantiate(plantPrefab, transform.position, Quaternion.identity);
    }

    void OnDestroy()
    {
        grabInteractable.selectExited.RemoveListener(OnPlantReleased);
    }
}

