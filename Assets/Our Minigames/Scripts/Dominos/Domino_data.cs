using UnityEngine;
using System.Collections.Generic;
using Domino;

public class Domino_data : MonoBehaviour
{
    // The two sides of the domino (e.g., 0-4)
    public int Top_side;
    public int But_side;
    public bool played = false;
    public bool inHand = false;

    // Array to store all the domino models/prefabs (with specific meshes)
    public GameObject[] dominoPrefabs;

    private List<HitboxComponent> hitboxComponents = new List<HitboxComponent>();


    // Local position and scale of the domino
    [SerializeField] protected Vector3 _position = Vector3.zero;
    [SerializeField] protected Vector3 _localScale = Vector3.one;

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;  // Ensure gravity is disabled
            rb.isKinematic = true;  // Make the Rigidbody kinematic to ignore all physical forces
        }
        _localScale = transform.localScale;

        // Save references to the hitboxes so they’re not destroyed
        foreach (Transform child in transform)
        {
            var hitbox = child.GetComponent<HitboxComponent>();
            if (hitbox != null)
            {
                hitboxComponents.Add(hitbox);
            }
        }

    }

    // Method to initialize the domino with custom side values
    public void InitializeDomino(int newSide1, int newSide2)
    {
        Top_side = newSide1;
        But_side = newSide2;

        foreach (Transform child in transform)
        {
            var hitbox = child.GetComponent<HitboxComponent>();
            if (hitbox != null)
            {
                hitboxComponents.Add(hitbox);
            }
            else
            {
                Debug.LogWarning($"Child {child.name} does not have a HitboxComponent.");
            }
        }
    }

    // Method to assign the correct domino visual based on the side values
    public void AssignDominoVisual()
    {
        // Calculate the index of the correct prefab based on the side values
        int index = CalculatePrefabIndex(Top_side, But_side);

        if (index >= 0 && index < dominoPrefabs.Length)
        {
            // Destroy any existing LOD models in the transform, except for hitboxes
            foreach (Transform child in transform)
            {
                // Only destroy the child if it's part of the LOD model, not the hitboxes
                if (!hitboxComponents.Exists(hitbox => hitbox.transform == child))
                {
                    Destroy(child.gameObject);
                }
            }

            // Instantiate the correct prefab to access its LOD models
            GameObject prefabInstance = Instantiate(dominoPrefabs[index]);

            // Find the LODGroup in the prefabInstance
            LODGroup prefabLODGroup = prefabInstance.GetComponent<LODGroup>();

            if (prefabLODGroup != null)
            {
                // Get the LODs from the prefab's LODGroup
                LOD[] lods = prefabLODGroup.GetLODs();

                // Create a new LODGroup on the current Domino object if it doesn't have one
                LODGroup lodGroup = GetComponent<LODGroup>();
                if (lodGroup == null)
                {
                    lodGroup = gameObject.AddComponent<LODGroup>();
                }

                // Prepare a list of LODs to assign to the current LODGroup
                List<LOD> newLODs = new List<LOD>();

                // Iterate through each LOD level in the prefab's LODGroup
                for (int i = 0; i < lods.Length; i++)
                {
                    // Create an empty list for the renderers in this LOD level
                    List<Renderer> lodRenderers = new List<Renderer>();

                    foreach (Renderer renderer in lods[i].renderers)
                    {
                        // Instantiate the renderer's GameObject as a child of this Domino object
                        GameObject lodObject = Instantiate(renderer.gameObject, transform);
                        lodObject.transform.localPosition = Vector3.zero;
                        lodObject.transform.localRotation = Quaternion.identity;
                        lodObject.transform.localScale = Vector3.one;

                        // Add the renderer from the instantiated object to the list
                        Renderer lodRenderer = lodObject.GetComponent<Renderer>();
                        if (lodRenderer != null)
                        {
                            lodRenderers.Add(lodRenderer);
                        }
                    }

                    // Create a new LOD and add it to the new LOD list
                    newLODs.Add(new LOD(lods[i].screenRelativeTransitionHeight, lodRenderers.ToArray()));
                }

                // Apply the new LODs to the Domino's LODGroup
                lodGroup.SetLODs(newLODs.ToArray());
                lodGroup.RecalculateBounds(); // Recalculate bounds for accurate LOD switching

                // Destroy the temporary prefabInstance after copying its LOD data
                Destroy(prefabInstance);

                Debug.Log($"Assigned visual for domino [{But_side}-{Top_side}] with LODs.");
            }
            else
            {
                Debug.LogError("No LODGroup found on the selected prefab.");
                Destroy(prefabInstance);  // Clean up if no LODGroup is found
                return;
            }
        }
        else
        {
            Debug.LogError("Invalid prefab index calculated.");
        }
    }
    // Custom logic to calculate the prefab index from the side values
    public int CalculatePrefabIndex(int side1, int side2)
    {
        // Ensure that side1 is always the smaller or equal value
        if (side1 > side2)
        {
            int temp = side1;
            side1 = side2;
            side2 = temp;
        }

        // The number of dominoes before this "row" starts
        int index = 0;

        // Sum up all the dominoes in previous rows
        for (int i = 0; i < side1; i++)
        {
            index += (7 - i);  // Each row has (7 - i) elements (e.g., 7 for 0, 6 for 1, etc.)
        }

        // Add the offset within the current row (side1 to side2)
        index += (side2 - side1);

        return index;
    }

    // Optional: A method to be called externally to spawn and initialize the domino
    public static Domino_data CreateDomino(GameObject dominoSamplePrefab, int side1, int side2, Vector3 position, Quaternion rotation, Transform parent, GameObject[] dominoPrefabsArray)
    {
        // Instantiate the Domino Sample Prefab
        GameObject dominoObject = Instantiate(dominoSamplePrefab, position, rotation, parent);

        // Get the Domino_data component
        Domino_data dominoData = dominoObject.GetComponent<Domino_data>();

        if (dominoData != null)
        {
            // Assign the dominoPrefabs array
            dominoData.dominoPrefabs = dominoPrefabsArray;

            // Initialize the domino with the given sides
            dominoData.InitializeDomino(side1, side2);
        }
        else
        {
            Debug.LogError("Domino_data component not found on the instantiated prefab.");
        }

        return dominoData;
    }
}
