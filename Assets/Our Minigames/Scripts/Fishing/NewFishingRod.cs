using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

public class NewFishingRod : MonoBehaviour
{
    public LineRenderer fishingLine;
    public Rigidbody hook; // Rigidbody on the hook or lure object
    public float castThreshold = 2.0f; // Threshold speed for casting
    public Transform rodTip;


    private bool isCasting = false;
    private XRBaseInteractor currentInteractor;

    private void Awake()
    {
        fishingLine = GetComponent<LineRenderer>();
    }

    private void OnEnable()
    {
        // Register to grab and release events
        var grabInteractable = GetComponentInChildren<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        var grabInteractable = GetComponentInChildren<XRGrabInteractable>();
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Store the interactor (could be either hand)
        currentInteractor = args.interactorObject as XRBaseInteractor;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // Clear the interactor when the rod is released
        currentInteractor = null;
        isCasting = false;
    }

    private void Update()
    {
        if (!isCasting)
        {
            hook.transform.position = rodTip.position;
        }

        // Ensure the rod is being held and we have a valid interactor
        if (currentInteractor != null)
        {
            //// Access the InputDevice associated with the interactor
            //InputDevice device = currentInteractor.;
            //Vector3 velocity;

            //if (device.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity))
            //{
            //    // Check if casting threshold is met
            //    if (velocity.magnitude > castThreshold && !isCasting)
            //    {
            //        StartCoroutine(CastFishingLine(velocity));
            //        isCasting = true;
            //    }
            //}

            //// Check if the casting threshold is met
            //if (velocity.magnitude > castThreshold && !isCasting)
            //{
            //    StartCoroutine(CastFishingLine(velocity));
            //    isCasting = true;
            //}
        }
    }

    private IEnumerator CastFishingLine(Vector3 velocity)
    {
        Debug.Log("I am casting!");
        hook.isKinematic = false;
        hook.velocity = velocity * 1.5f; // Adjust multiplier for casting distance
        yield return new WaitForSeconds(0.2f);
        isCasting = false;
    }

    void LateUpdate()
    {
        // Update line renderer positions between rod tip and hook
        fishingLine.positionCount = 2;
        fishingLine.SetPosition(0, rodTip.position); // Rod tip position
        fishingLine.SetPosition(1, hook.position);      // Hook position
    }
}