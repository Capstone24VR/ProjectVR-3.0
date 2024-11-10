using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using XRMultiplayer.MiniGames;

public class NewFishingRod : MonoBehaviour
{
    public NewFishingLine fishingLine;
    public Rigidbody hook; // Rigidbody on the hook or lure object
    public float castThreshold = 2.5f; // Threshold speed for casting


    public Transform rodTipTransform;
    public Transform rodBaseTransform;

    private List<Vector3> basePositions = new List<Vector3>();
    private List<Vector3> tipPositions = new List<Vector3>();
    private float sampleInterval = 0.05f;
    private float nextSampleTime;

    private int grabCount = 0;

    [Header("Casting")]
    private bool castTrigger = false;
    public bool isCasting = false;
    public float castingMultiplier = 10f;

    [Header("Hook Movement")]
    public float hookShiftForceMultiplier = 5f;  // Multiplier to control how much force is applied to the hook
    public float forceDamping = 0.1f;  // To reduce oscillation or overreaction of the hook

    [Header("Reeling")]
    public float prevReelChange = 0f;  // The previous value from reel (used to find the difference of reel change)
    
    public XRBaseInteractor currentInteractor;
    public HapticImpulsePlayer hapticFeedback;


    private void Awake()
    {
        fishingLine = GetComponent<NewFishingLine>();
    }

    private void OnEnable()
    {
        // Register to grab and release events
        var grabInteractable = GetComponentInChildren<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
        grabInteractable.activated.AddListener(OnActivate);
        grabInteractable.deactivated.AddListener(OnDeactivate);
    }

    private void OnDisable()
    {
        var grabInteractable = GetComponentInChildren<XRGrabInteractable>();
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
        grabInteractable.activated.RemoveListener(OnActivate);
        grabInteractable.deactivated.RemoveListener(OnDeactivate);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Store the interactor (could be either hand)
        if (grabCount == 0)
        {
            currentInteractor = args.interactorObject as XRBaseInteractor;
            hapticFeedback = currentInteractor.GetComponentInParent<HapticImpulsePlayer>();
        }
        grabCount++;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // Clear the interactor when the rod is released
        grabCount--;

        if (currentInteractor == args.interactorObject as XRBaseInteractor)
        {
            currentInteractor = null;
            hapticFeedback = null;
            isCasting = false;
            castTrigger = false;

            basePositions.Clear();
            tipPositions.Clear();
            ResetHook();
        }
    }

    private void OnActivate(ActivateEventArgs args)
    {
        if (grabCount > 0 && !isCasting)
        {
            castTrigger = true;
            isCasting = true;
            fishingLine.StartCasting();
        }
        else if (isCasting)
        {
            ResetHook();
        }
    }

    private void OnDeactivate(DeactivateEventArgs args)
    {
        //if (grabCount > 0)
        //    isCasting = true;
    }

    void Update()
    {
        if (!isCasting)
        {
            hook.transform.position = rodTipTransform.position;
            hook.transform.rotation = rodTipTransform.rotation;
        }

        if (currentInteractor == null || hapticFeedback == null) return;

        if (Time.time >= nextSampleTime)
        {
            SampleRodPositions();
            nextSampleTime = Time.time + sampleInterval;
        }

        if (castTrigger)
        {
            castTrigger = false;
            var castingQuality = CalculateCastingQuality();
            Debug.Log($"Casting Quality: {castingQuality}");
            if (castingQuality == 0) return;
            else if (castingQuality < 2.5f)
            {
                Debug.Log("Weak Cast");
                LaunchHook(castingQuality);
            }
            else if (castingQuality >= 2.5f && castingQuality < 5.0f)
            {
                Debug.Log("Medium Cast");
                hapticFeedback.SendHapticImpulse(0.3f, 0.2f, 0.5f);
                LaunchHook(castingQuality*2);
            }
            else if (castingQuality >= 5.0f)
            {
                Debug.Log("Strong Cast");
                hapticFeedback.SendHapticImpulse(0.6f, 0.4f, 1f);
                LaunchHook(castingQuality*5);
            }
        }
    }

    void LaunchHook(float castingQuality)
    {
        hook.mass = 15;
        hook.isKinematic = false;
        hook.useGravity = true;

        Vector3 castDirection = (tipPositions[tipPositions.Count - 1] - tipPositions[0]).normalized;

        float launchForce = castingQuality * castingMultiplier;
        hook.AddForce(castDirection * launchForce, ForceMode.Impulse);
    }

    void ResetHook()
    {
        hook.mass = 1;
        isCasting = false;
        fishingLine.StopCasting();

        hook.position = rodTipTransform.position;
        hook.useGravity = false;
        hook.isKinematic = true;
    }
    public void Reel(float change)
    {
        var reelChange = change - prevReelChange;
        prevReelChange = change;
        fishingLine.Reel(reelChange);
        Debug.Log(reelChange);
    }

    void SampleRodPositions()
    {
        if (basePositions.Count >= 5) basePositions.RemoveAt(0);
        if (tipPositions.Count >= 5) tipPositions.RemoveAt(0);

        // Assuming you have references to the base and tip of the rod
        Vector3 basePosition = rodBaseTransform.position;
        Vector3 tipPosition = rodTipTransform.position;

        basePositions.Add(basePosition);
        tipPositions.Add(tipPosition);
    }

    float CalculateCastingQuality()
    {
        if (tipPositions.Count < 5) return 0f;  // Not enough samples yet

        // Calculate velocity (distance between oldest and newest tip position)
        float velocity = Vector3.Distance(tipPositions[0], tipPositions[tipPositions.Count - 1]);

        // Calculate planarity (for simplicity, using deviation from the mean plane)
        Vector3 normal = Vector3.Cross(tipPositions[1] - tipPositions[0], tipPositions[2] - tipPositions[0]);
        float planarity = CalculatePlanarity(normal);

        // Calculate casting quality
        return velocity * planarity;
    }

    float CalculatePlanarity(Vector3 normal)
    {
        float deviationSum = 0f;
        foreach (var tipPosition in tipPositions)
        {
            // Project each tip position onto the normal to measure deviation from plane
            deviationSum += Vector3.Dot(tipPosition - tipPositions[0], normal);
        }
        return 1f / (1f + Mathf.Abs(deviationSum));  // Higher planarity if deviation is low
    }

}