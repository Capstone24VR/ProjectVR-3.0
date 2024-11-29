using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Seed : NetworkBehaviour
{
    // Enum to categorize different plant types available within the game.
    public enum plants { cucumber, pumpkin, cabbage, carrot, onion, potato, tomato };
    public plants plantType; // Variable to hold the specific type of plant this seed will grow into.

    // Layer on which the plant beds are set, used to detect collisions with the correct objects.
    private int plantLayer;
    public GameObject plantPrefabs; // Prefab to instantiate when the seed is planted.
    private NetworkVariable<Vector3> originalPosition = new NetworkVariable<Vector3>(); // Original position of the seed to reset after invalid movement or interaction.
    private NetworkVariable<Quaternion> originalRotation = new NetworkVariable<Quaternion>(); // Original rotation to maintain orientation upon reset.
    private float thresholdY = 0f; // Y position threshold to check if the seed has fallen below a certain height.

    private void Awake()
    {
        // Initialize the plantLayer by converting the layer name to an integer for use in collision detection.
        plantLayer = LayerMask.NameToLayer("Bed");

        if (IsServer) {
            // Capture the initial position and rotation of the seed to reset later if needed.
            originalPosition.Value = transform.position;
            originalRotation.Value = transform.rotation;
        }
    }

    void Update()
    {
        if(IsServer)
        {
            // Check if the seed has fallen below the allowed height (e.g., off the side of the platform).
            if (transform.position.y < thresholdY)
            {
                // Reset the position of the seed to its original state to prevent it from getting lost.
                transform.position = originalPosition.Value;
                transform.rotation = originalRotation.Value;
            }

            SetTransformClientRpc(transform.position, transform.rotation);
        }
    }

    [ClientRpc]
    private void SetTransformClientRpc(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
    }
        

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the collision is with a plant bed that is ready for planting ("Unplanted").
        if (collision.gameObject.layer == plantLayer && collision.gameObject.tag == "Unplanted")
        {
            SpawnPlantServerRpc(collision.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
            //// Instantiate a new plant at the specified location within the plant bed.
            //GameObject newPlant = Instantiate(plantPrefabs,
            //                                  collision.gameObject.transform.Find("PlantLocation").transform.position,
            //                                  Quaternion.identity);

            //// Set the parent of the new plant to be the plant bed, keeping the scene hierarchy organized.
            //newPlant.transform.SetParent(collision.gameObject.transform);

            //// Obtain a reference to the PlantGrowth script attached to the new plant.
            //PlantGrowth plantGrowth = newPlant.GetComponent<PlantGrowth>();
            //if (plantGrowth != null)
            //{
            //    // If the script is found, set the plant bed reference within it to manage growth-related updates.
            //    plantGrowth.plantBed = collision.gameObject;
            //}

            //// Update the plant bed's tag to "Planted" to indicate it now contains a growing plant.
            //collision.gameObject.tag = "Planted";
            //// Reset the seed's position and rotation to prevent reuse after planting.
            //transform.position = originalPosition;
            //transform.rotation = originalRotation;
            //// Also reset any movement dynamics to prevent the seed from drifting away after planting.
            //gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }

    [ServerRpc(RequireOwnership =false)]
    private void SpawnPlantServerRpc(ulong networkObjectId)
    {
        NetworkObject collision = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];

        GameObject newPlant = Instantiate(plantPrefabs,
                                             collision.gameObject.transform.Find("PlantLocation").transform.position,
                                             Quaternion.identity);

        newPlant.GetComponent<NetworkObject>().Spawn();

        // Set the parent of the new plant to be the plant bed, keeping the scene hierarchy organized.
        newPlant.transform.SetParent(collision.gameObject.transform);

        // Obtain a reference to the PlantGrowth script attached to the new plant.
        PlantGrowth plantGrowth = newPlant.GetComponent<PlantGrowth>();
        if (plantGrowth != null)
        {
            // If the script is found, set the plant bed reference within it to manage growth-related updates.
            plantGrowth.plantBed = collision.gameObject;
        }

        // Update the plant bed's tag to "Planted" to indicate it now contains a growing plant.
        collision.gameObject.tag = "Planted";
        // Reset the seed's position and rotation to prevent reuse after planting.
        transform.position = originalPosition.Value;
        transform.rotation = originalRotation.Value;

        SetTransformClientRpc(transform.position, transform.rotation);
        // Also reset any movement dynamics to prevent the seed from drifting away after planting.
        gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;

    }
}