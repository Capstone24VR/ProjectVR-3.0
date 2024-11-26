using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using XRMultiplayer.MiniGames;
using Unity.Netcode;
using UnityEngine.UIElements;
using Unity.Services.Lobbies.Models;
using XRMultiplayer;

public class NewFishingRod : NetworkBehaviour
{
    public NewFishingLine fishingLine;
    public FishingHook hook;
    public Rigidbody floater; // Rigidbody on the floater or lure object
    public float castThreshold = 2.5f; // Threshold speed for casting


    public Transform rodTipTransform;
    public Transform rodBaseTransform;

    private NetworkList<Vector3> basePositions = new NetworkList<Vector3>();
    private NetworkList<Vector3> tipPositions = new NetworkList<Vector3>();
    private float sampleInterval = 0.05f;
    private float nextSampleTime;

    private int grabCount = 0;

    [Header("Casting")]
    private NetworkVariable<bool> castTrigger = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isCasting = new NetworkVariable<bool>(false);
    public float castingMultiplier = 10f;

    [Header("Reeling")]
    public float prevReelChange = 0f;  // The previous value from reel (used to find the difference of reel change)

    public XRBaseInteractor currentInteractor;
    public HapticImpulsePlayer hapticFeedback;


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

    private void OnGrab(SelectEnterEventArgs args)
    {
        int controller = -1;

        // Store the interactor (could be either hand)
        if (grabCount == 0)
        {
            currentInteractor = args.interactorObject as XRBaseInteractor;
            hapticFeedback = currentInteractor.GetComponentInParent<HapticImpulsePlayer>();

            controller = hapticFeedback.name == "Right Controller" ? 0 : 1;
        }
        grabCount++;

        SyncGrabServerRpc(grabCount, NetworkManager.Singleton.LocalClientId, controller);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SyncGrabServerRpc(int newCount, ulong clientId, int controller)
    {
        grabCount = newCount;

        NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client);

        XRBaseInteractor newInteractable = null;
        HapticImpulsePlayer newHapticPlayer = null;

        switch (controller)
        {
            case 0:
                newInteractable = client.PlayerObject.transform.Find("Right Controller").GetComponentInChildren<NearFarInteractor>();
                newHapticPlayer = newInteractable.GetComponentInParent<HapticImpulsePlayer>();
                break;
            case 1:
                newInteractable = client.PlayerObject.transform.Find("Left Controller").GetComponentInChildren<NearFarInteractor>();
                newHapticPlayer = newInteractable.GetComponentInParent<HapticImpulsePlayer>();
                break;
            default:
                break;
        }

        if(newInteractable != null)
        {
            currentInteractor = newInteractable;
            hapticFeedback = newHapticPlayer;
        }

        SyncGrabClientRpc(grabCount, clientId, controller);
    }

    [ClientRpc]
    private void SyncGrabClientRpc(int newCount, ulong clientId, int controller)
    {
        grabCount = newCount;

        NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client);

        XRBaseInteractor newInteractable = null;
        HapticImpulsePlayer newHapticPlayer = null;

        switch (controller)
        {
            case 0:
                newInteractable = client.PlayerObject.transform.Find("Right Controller").GetComponentInChildren<NearFarInteractor>();
                newHapticPlayer = newInteractable.GetComponentInParent<HapticImpulsePlayer>();
                break;
            case 1:
                newInteractable = client.PlayerObject.transform.Find("Left Controller").GetComponentInChildren<NearFarInteractor>();
                newHapticPlayer = newInteractable.GetComponentInParent<HapticImpulsePlayer>();
                break;
            default:
                break;
        }

        if (newInteractable != null)
        {
            currentInteractor = newInteractable;
            hapticFeedback = newHapticPlayer;
        }
    }


    private void OnRelease(SelectExitEventArgs args)
    {
        // Clear the interactor when the rod is released
        grabCount--;

        if (currentInteractor == args.interactorObject as XRBaseInteractor)
        {
            //currentInteractor = null;
            //hapticFeedback = null;
            //isCasting = false;
            //castTrigger = false;

            //basePositions.Clear();
            //tipPositions.Clear();
            //ResetCast();
            HandleReleaseServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleReleaseServerRpc()
    {
        currentInteractor = null;
        hapticFeedback = null;

        SyncReleaseClientRpc();

        isCasting.Value = false;
        castTrigger.Value = false;

        basePositions.Clear();
        tipPositions.Clear();

        ResetCastServerRpc();
    }

    [ClientRpc]
    private void SyncReleaseClientRpc()
    {
        currentInteractor = null;
        hapticFeedback = null;
    }

    private void OnActivate(ActivateEventArgs args)
    {
        //if (grabCount > 0 && !isCasting)
        //{
        //    castTrigger = true;
        //    isCasting = true;
        //    fishingLine.StartCasting();
        //}
        //else if (isCasting)
        //{
        //    ResetCast();
        //}
        HandleActivateServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleActivateServerRpc()
    {
        if (grabCount > 0 && !isCasting.Value)
        {
            castTrigger.Value = true;
            isCasting.Value = true;
            fishingLine.StartCastingServerRpc();
        }
        else if (isCasting.Value)
        {
            ResetCastServerRpc();
        }
    }

    private void OnDeactivate(DeactivateEventArgs args)
    {
        //if (grabCount > 0)
        //    isCasting = true;
    }

    void Update()
    {
        if (IsServer)
        {
            if (!isCasting.Value)
            {
                floater.transform.position = rodTipTransform.position;
                floater.transform.rotation = rodTipTransform.rotation;
            }

            SyncFloaterTransformClientRpc(floater.transform.position, floater.transform.rotation);

            if (currentInteractor == null || hapticFeedback == null) return;

            if (Time.time >= nextSampleTime)
            {
                SampleRodPositions();
                nextSampleTime = Time.time + sampleInterval;
            }

            if (castTrigger.Value)
            {
                castTrigger.Value = false;
                var castingQuality = CalculateCastingQuality();
                Debug.Log($"Casting Quality: {castingQuality}");
                if (castingQuality == 0) return;
                else if (castingQuality < 2.5f)
                {
                    Debug.Log("Weak Cast");
                    LaunchCastServer(castingQuality);
                }
                else if (castingQuality >= 2.5f && castingQuality < 5.0f)
                {
                    Debug.Log("Medium Cast");
                    hapticFeedback.SendHapticImpulse(0.3f, 0.2f, 0.5f);
                    LaunchCastServer(castingQuality * 2);
                }
                else if (castingQuality >= 5.0f)
                {
                    Debug.Log("Strong Cast");
                    hapticFeedback.SendHapticImpulse(0.6f, 0.4f, 1f);
                    LaunchCastServer(castingQuality * 5);
                }
            }

            SyncFloaterTransformClientRpc(floater.transform.position, floater.transform.rotation);
        }
    }

    [ClientRpc]

    private void SyncFloaterTransformClientRpc(Vector3 position, Quaternion rotation)
    {
        floater.transform.position = position;
        floater.transform.rotation = rotation;
    }

    void LaunchCastServer(float castingQuality)
    {
        if (IsServer)
        {
            floater.mass = 15;
            floater.isKinematic = false;
            floater.useGravity = true;

            LaunchCastClientRpc();

            hook.rodDropped.Value = false;

            Vector3 castDirection = (tipPositions[tipPositions.Count - 1] - tipPositions[0]).normalized;

            float launchForce = castingQuality * castingMultiplier;
            floater.AddForce(castDirection * launchForce, ForceMode.Impulse);
        }
    }

    [ClientRpc]
    private void LaunchCastClientRpc()
    {
        floater.mass = 15;
        floater.isKinematic = false;
        floater.useGravity = true;

        Debug.Log($"Syncing floater: \tgravity: {floater.useGravity}\tkinematic: {floater.isKinematic}\tmass: {floater.mass}");
    }


    [ServerRpc(RequireOwnership = false)]
    void ResetCastServerRpc()
    {
        floater.mass = 1;
        isCasting.Value = false;
        fishingLine.StopCastingServerRpc();

        floater.position = rodTipTransform.position;
        floater.useGravity = false;
        floater.isKinematic = true;

        SyncResetClientRpc();

        hook.caughtSomething.Value = false;
        hook.rodDropped.Value = true;
    }

    [ClientRpc]
    private void SyncResetClientRpc()
    {
        floater.mass = 1;
        floater.position = rodTipTransform.position;
        floater.useGravity = false;
        floater.isKinematic = true;
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