//using UnityEngine;

//public class Billboard : MonoBehaviour
//{
//    private Transform mainCameraTransform;

//    void Start()
//    {
//        // Cache the main camera's transform
//        mainCameraTransform = Camera.main.transform;
//    }

//    void Update()
//    {
//        // Get the current rotation of the GameObject in Euler angles
//        Vector3 currentRotation = transform.eulerAngles;

//        // Calculate the rotation angle towards the camera around the Y axis
//        Vector3 directionToCamera = mainCameraTransform.position - transform.position;
//        float targetAngle = Mathf.Atan2(directionToCamera.x, directionToCamera.z) * Mathf.Rad2Deg;

//        // Set the GameObject's rotation to match the camera's Y-axis rotation, preserving its current X and Z rotations
//        transform.eulerAngles = new Vector3(currentRotation.x, targetAngle, currentRotation.z);
//    }
//}

using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main; // Cache the main camera
    }

    void Update()
    {
        Vector3 directionToCamera = mainCamera.transform.position - transform.position;
        directionToCamera.y = 0; // Ensure the rotation is only around the Y-axis

        // Calculate the rotation to face the camera, adding 180 degrees to flip the direction
        Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
        targetRotation *= Quaternion.Euler(0, 180, 0); // Add 180 degrees to the Y-axis rotation

        // Set the rotation, only modifying the Y component
        transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
    }
}


