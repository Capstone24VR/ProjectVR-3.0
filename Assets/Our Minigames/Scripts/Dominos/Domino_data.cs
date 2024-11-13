using UnityEngine;

public class Domino_data : MonoBehaviour
{
    // The two sides of the domino (e.g., 0-4)
    public int Top_side;
    public int But_side;
    public bool played = false;
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

        // Optional: Assign modelParent if not set
        if (modelParent == null)
        {
            modelParent = transform.Find("ModelParent");
        }
    }

    // Method to initialize the domino with custom side values
    public void InitializeDomino(int newSide1, int newSide2)
    {
        Top_side = newSide1;
        But_side = newSide2;
    }

    // Method to assign the correct domino visual based on the side values
    public void AssignDominoVisual()
    {
        // Calculate the index of the correct prefab based on the side values
        int index = CalculatePrefabIndex(Top_side, But_side);

        if (index >= 0 && index < dominoPrefabs.Length)
        {
            // Destroy any existing child in modelParent to replace with the correct domino visual
            foreach (Transform child in modelParent)
            {
                Destroy(child.gameObject);
            }

            // Instantiate the correct domino model as a child of modelParent
            GameObject prefabInstance = Instantiate(dominoPrefabs[index], modelParent);

            // Optional: Adjust prefab's local position/scale if necessary
            prefabInstance.transform.localPosition = Vector3.zero;
            prefabInstance.transform.localRotation = Quaternion.identity;
            prefabInstance.transform.localScale = Vector3.one;

            Debug.Log($"Assigned visual for domino [{But_side}-{Top_side}]");
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
