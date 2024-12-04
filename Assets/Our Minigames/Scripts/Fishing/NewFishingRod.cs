using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using Unity.Netcode;
using XRMultiplayer;

public class NewFishingRod : NetworkBehaviour
{
    public NewFishingLine fishingLine;
    public FishingHook hook;
    public Rigidbody floater; // Rigidbody on the floater or lure object
    public float castThreshold = 2.5f; // Threshold speed for casting


    public Transform rodTipTransform;
    public Transform rodBaseTransform;

    private List<Vector3> basePositions = new List<Vector3>();
    private List<Vector3> tipPositions = new List<Vector3>();
    private float sampleInterval = 0.05f;
    private float nextSampleTime;

    private int grabCount = 0;

    [Header("Casting")]
    public bool isCasting = false;
    public float castingMultiplier = 10f;

    [Header("Reeling")]
    public float prevReelChange = 0f;  // The previous value from reel (used to find the difference of reel change)

    public XRBaseInteractor currentInteractor;
    public HapticImpulsePlayer hapticFeedback;

    public ulong clientId = 9999;


    private void Awake()
    {
        hook = GetComponentInChildren<FishingHook>();
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

    void Update()
    {
        if (!IsOwner) return;

        if (!isCasting)
        {
            floater.transform.position = rodTipTransform.position;
            floater.transform.rotation = rodTipTransform.rotation;
        }

        if (grabCount == 0) return;

        if (Time.time >= nextSampleTime)
        {
            SampleRodPositions();
            nextSampleTime = Time.time + sampleInterval;
        }

        SyncFloaterTransformServerRpc(floater.transform.position, floater.transform.rotation);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {

        // Store the interactor (could be either hand)
        if (grabCount == 0)
        {
            currentInteractor = args.interactorObject as XRBaseInteractor;
            hapticFeedback = currentInteractor.GetComponentInParent<HapticImpulsePlayer>();
            clientId = NetworkManager.Singleton.LocalClientId;

            SetOwnerShipServerRpc(clientId);
            SyncGrabServerRpc();
        }
        grabCount++;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // Clear the interactor when the rod is released
        grabCount--;

        if (currentInteractor == args.interactorObject as XRBaseInteractor)
        {
            var grabInteractable = GetComponentInChildren<XRGrabInteractable>();
            grabInteractable.GetComponent<Rigidbody>().isKinematic = false;
            clientId = 9999;

            isCasting = false;

            basePositions.Clear();
            tipPositions.Clear();

            ResetCast();
            SyncReleaseServerRpc();
            ResetOwnerShipServerRpc();
        }
    }

    private void OnActivate(ActivateEventArgs args)
    {
        if (!IsOwner) return;

        if (grabCount > 0 && !isCasting)
        {
            isCasting = true;
            fishingLine.StartCastingServerRpc();

            var castingQuality = CalculateCastingQuality();
            Debug.Log($"Casting Quality: {castingQuality}");

            if (castingQuality == 0) return;
            else if (castingQuality < 2.5f)
            {
                Debug.Log("Weak Cast");
                LaunchCast(castingQuality);
            }
            else if (castingQuality >= 2.5f && castingQuality < 5.0f)
            {
                Debug.Log("Medium Cast");
                hapticFeedback?.SendHapticImpulse(0.3f, 0.2f, 0.5f);
                LaunchCast(castingQuality * 2);
            }
            else if (castingQuality >= 5.0f)
            {
                Debug.Log("Strong Cast");
                hapticFeedback?.SendHapticImpulse(0.6f, 0.4f, 1f);
                LaunchCast(castingQuality * 5);
            }
        }
        else if (isCasting)
        {
            ResetCast();
        }
    }

    private void OnDeactivate(DeactivateEventArgs args)
    {
        //if (grabCount > 0)
        //    isCasting = true;
    }

    void LaunchCast(float castingQuality)
    {
        floater.mass = 15;
        floater.isKinematic = false;
        floater.useGravity = true;
        SyncLaunchFloaterServerRpc();

        ToggleRodDroppedServerRpc(false);

        Vector3 castDirection = (tipPositions[tipPositions.Count - 1] - tipPositions[0]).normalized;

        float launchForce = castingQuality * castingMultiplier;
        floater.AddForce(castDirection * launchForce, ForceMode.Impulse);
    }

    void ResetCast()
    {
        floater.mass = 1;
        floater.useGravity = false;
        floater.isKinematic = true;

        SyncResetFloaterServerRpc();

        isCasting = false;
        fishingLine.StopCastingServerRpc();

        floater.position = rodTipTransform.position;


        ToggleCaughtSomethingServerRpc(false);
        ToggleRodDroppedServerRpc(true);
    }

    public void Reel(float change)
    {
        var reelChange = change - prevReelChange;
        prevReelChange = change;
        fishingLine.Reel(reelChange);
    }



    [ServerRpc(RequireOwnership = false)]
    private void SetOwnerShipServerRpc(ulong clientId)
    {
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject.OwnerClientId != clientId) networkObject.ChangeOwnership(clientId);

        NetworkObject rodNetworkObject = rodBaseTransform.GetComponentInParent<NetworkObject>();
        if(rodNetworkObject.OwnerClientId != clientId) rodNetworkObject.ChangeOwnership(clientId);
    }


    [ServerRpc(RequireOwnership = false)]
    private void ResetOwnerShipServerRpc()
    {
        GetComponent<NetworkObject>().RemoveOwnership();
        Debug.Log(rodBaseTransform.GetComponentInParent<NetworkObject>().name);
        rodBaseTransform.GetComponentInParent<NetworkObject>().RemoveOwnership();
    }

    [ServerRpc(RequireOwnership = true)]
    private void SyncGrabServerRpc()
    {
        SyncGrabClientRpc();
    }

    [ClientRpc]
    private void SyncGrabClientRpc()
    {
        if (!IsOwner)
        {
            rodBaseTransform.parent.SetParent(null, true);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void SyncReleaseServerRpc()
    {
        SyncReleaseClientRpc();
    }

    [ClientRpc]
    private void SyncReleaseClientRpc()
    {
        if (!IsOwner)
        {
            rodBaseTransform.parent.SetParent(transform, true);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void SyncFloaterTransformServerRpc(Vector3 position, Quaternion rotation)
    {
        SyncFloaterTransformClientRpc(position, rotation);
    }

    [ClientRpc]
    private void SyncFloaterTransformClientRpc(Vector3 position, Quaternion rotation)
    {
        if (!IsOwner)
        {
            floater.transform.position = position;
            floater.transform.rotation = rotation;
        }
    }

    [ServerRpc(RequireOwnership = true)]
    void SyncLaunchFloaterServerRpc()
    {
        SyncLaunchFloaterClientRpc();
    }

    [ClientRpc]
    void SyncLaunchFloaterClientRpc()
    {
        floater.mass = 15;
        floater.isKinematic = false;
        floater.useGravity = true;
    }

    [ServerRpc(RequireOwnership = true)]
    void SyncResetFloaterServerRpc()
    {
        SyncResetFloaterClientRpc();
    }

    [ClientRpc]
    void SyncResetFloaterClientRpc()
    {
        floater.mass = 1;
        floater.useGravity = false;
        floater.isKinematic = true;
    }

    [ServerRpc(RequireOwnership = false)]
    void ToggleRodDroppedServerRpc(bool toggle)
    {
        hook.rodDropped.Value = false;
    }

    [ServerRpc(RequireOwnership = false)]
    void ToggleCaughtSomethingServerRpc(bool toggle)
    {
        hook.caughtSomething.Value = false;
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