using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Domino_data : MonoBehaviour
{
    // The two sides of the domino (e.g., 0-4)
    public int side1;
    public int side2;

    public bool inHand = false;

    // Array to store all the domino models/prefabs (with specific meshes)
    public GameObject[] dominoPrefabs;

    // Reference to the container where the domino prefab will be instantiated
    public Transform modelParent;

    // Local position and scale of the domino
    [SerializeField] protected Vector3 _position = Vector3.zero;
    [SerializeField] protected Vector3 _localScale = Vector3.one;

    private void Awake()
    {
        _localScale = transform.localScale;

        // Optional: You can also extract values from the name or set values externally
        // ExtractValuesFromName();

        // Automatically assign the correct domino prefab based on the side values
        AssignDominoPrefab();
    }

    // Method to extract side values from the name if needed (Optional)
    public void ExtractValuesFromName()
    {
        string name = gameObject.name;

        string[] parts = name.Split('_', '-');  // Split the name to get the values
        if (parts.Length == 3)
        {
            if (int.TryParse(parts[1], out int firstValue) && int.TryParse(parts[2], out int secondValue))
            {
                side1 = firstValue;
                side2 = secondValue;
                Debug.Log($"Domino sides set to [{side1}, {side2}] from name: {name}");
            }
            else
            {
                Debug.LogWarning($"Failed to parse side values from domino name: {name}");
            }
        }
        else
        {
            Debug.LogWarning($"Invalid domino name format: {name}");
        }
    }

    // Method to assign the correct domino prefab (visual representation) based on the side values
    public void AssignDominoPrefab()
    {
        // Calculate the index of the correct prefab based on the side values
        int index = CalculatePrefabIndex(side1, side2);

        if (index >= 0 && index < dominoPrefabs.Length)
        {
            // Destroy any existing child in modelParent to replace with the correct domino
            foreach (Transform child in modelParent)
            {
                Destroy(child.gameObject);
            }

            // Instantiate the correct domino model as a child of modelParent
            GameObject prefabInstance = Instantiate(dominoPrefabs[index], modelParent.position, modelParent.rotation, modelParent);

            // Optional: Adjust prefab's local position/scale if necessary
            prefabInstance.transform.localPosition = Vector3.zero;
            prefabInstance.transform.localScale = Vector3.one;

            Debug.Log($"Assigned prefab for domino [{side1}-{side2}]");
        }
        else
        {
            Debug.LogError("Invalid prefab index calculated.");
        }
    }

    // Custom logic to calculate the prefab index from the side values
    private int CalculatePrefabIndex(int side1, int side2)
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

    // Method to manually set the domino values and refresh its appearance
    public void SetDominoValues(int newSide1, int newSide2)
    {
        side1 = newSide1;
        side2 = newSide2;

        // Update the appearance based on the new values
        AssignDominoPrefab();
    }

    // Method to set the position of the domino
    public void SetPosition(Vector3 position)
    {
        _position = position;
    }

    // Method to reset the domino to its original position
    public void ResetPosition()
    {
        transform.localPosition = _position;
        transform.localRotation = Quaternion.identity;
    }

    // Method to highlight the domino when hovered over
    public void HoverSelect()
    {
        transform.localScale = _localScale * 1.25f;
    }

    // Method to return the domino to normal when hover is removed
    public void HoverDeSelect()
    {
        transform.localScale = _localScale;
    }

    // Method to set the domino as "in hand"
    public void SetInHand(bool isInHand)
    {
        inHand = isInHand;
        if (inHand)
        {
            Debug.Log($"Domino [{side1} | {side2}] is now in hand.");
        }
        else
        {
            Debug.Log($"Domino [{side1} | {side2}] is no longer in hand.");
        }
    }
}
