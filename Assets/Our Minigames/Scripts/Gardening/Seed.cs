using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;


public class Seed : MonoBehaviour
{
    public enum plants { cucumber, pumpkin, cabbage, carrot, onion, potato, tomato };
    public plants plantType;
    private int plantLayer;
    public GameObject plantPrefabs; // Array of plant prefabs instead of pre-existing objects
    private int plantIndex;
    private Vector3 originalPosition;
    private Quaternion originalRotation;


    private void Awake()
    {
        plantLayer = LayerMask.NameToLayer("Bed");
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == plantLayer && collision.gameObject.tag == "Unplanted")
        {

            GameObject newPlant = Instantiate(plantPrefabs,
                                               collision.gameObject.transform.Find("PlantLocation").transform.position,
                                               Quaternion.identity);

            newPlant.transform.SetParent(collision.gameObject.transform);

            // Set the plantBed reference on the newly instantiated plant
            PlantGrowth plantGrowth = newPlant.GetComponent<PlantGrowth>();
            if (plantGrowth != null)
            {
                plantGrowth.plantBed = collision.gameObject; // Set the plantbed reference
            }

            collision.gameObject.tag = "Planted";
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }
}
