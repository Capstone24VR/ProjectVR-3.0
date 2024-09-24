using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class WateringCan : MonoBehaviour
{
    public XRController leftHandController;
    public XRController rightHandController;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // if (leftHandController.inputDevice.TryGetFeatureValue(CommonUsages.trigger, out float leftTriggerValue) && leftTriggerValue > 0.1f)
        // {
        //     GardenScenario.currentTool = "WateringCan";
        //     Debug.Log("Left trigger pressed: " + leftTriggerValue);
        // } else if (rightHandController.inputDevice.TryGetFeatureValue(CommonUsages.trigger, out float rightTriggerValue) && rightTriggerValue > 0.1f)
        // {
        //     GardenScenario.currentTool = "WateringCan";
        //     Debug.Log("Right trigger pressed: " + rightTriggerValue);
        // }
    }

    void onMouseDown () 
    {
        Debug.Log("Clicked that");
    }
    
}
