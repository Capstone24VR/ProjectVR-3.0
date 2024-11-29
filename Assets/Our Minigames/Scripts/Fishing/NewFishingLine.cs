using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NewFishingLine : NetworkBehaviour
{
    public Transform rodTip;
    public Rigidbody floater;
    public LineRenderer lineRenderer;

    public int lineSegmentCount = 20;  // Number of points in the line
    public float segmentLength = 0.1f; // Distance between each segment point
    public float gravity = -9.81f;
    public float floaterMass = 0.2f;
    public float verletDamping = 0.98f;

    private NetworkList<Vector3> linePoints = new NetworkList<Vector3>();
    private NetworkList<Vector3> prevPoints = new NetworkList<Vector3>();
    private NetworkVariable<bool> isCasting = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> lineLocked = new NetworkVariable<bool>(false);

    public float maxRopeLength = 2f;
    public NetworkVariable<float> currentRopeLength = new NetworkVariable<float>();

    public NetworkVariable<bool> ropeLengthLocked = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if(IsServer) { 
            linePoints.Clear();
            prevPoints.Clear();

            // Initialize line with segments
            for (int i = 0; i < lineSegmentCount; i++)
            {
                linePoints.Add(rodTip.position);
                prevPoints.Add(rodTip.position);
            }
        }

        lineRenderer.positionCount = lineSegmentCount;
    }

    public void Start()
    {
        currentRopeLength.Value = 0;

        lineRenderer.positionCount = lineSegmentCount;
    }

    private void Update()
    {
        if (IsServer)
        {
            if (!isCasting.Value)
            {
                // When not casting, set all points close to the rod tip
                for (int i = 0; i < lineSegmentCount; i++)
                    linePoints[i] = rodTip.position;

            }
            else
            {
                if (!ropeLengthLocked.Value)
                {
                    var distanceTofloater = Vector3.Distance(rodTip.position, floater.position);
                    if (!floater.GetComponent<BuoyancyObject>().underwater)
                    {
                        currentRopeLength.Value = Mathf.Min(distanceTofloater, maxRopeLength);
                    }
                    else
                    {
                        ropeLengthLocked.Value = true;
                    }
                }
                else
                {
                    if (!floater.GetComponent<BuoyancyObject>().underwater)
                        floater.drag = 10f;
                }


                if (!lineLocked.Value)
                {
                    SimulateVerlet();
                    ApplyConstraints();
                }
            }

            DrawLineClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartCastingServerRpc()
    {
        floater.drag = 0;
        isCasting.Value = true;
        lineLocked.Value = false;
        ropeLengthLocked.Value = false;

        SyncCastingClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopCastingServerRpc()
    {
        floater.drag = 0;
        isCasting.Value = false;
        lineLocked.Value = true;
        ropeLengthLocked.Value = false;

        SyncCastingClientRpc();
    }

    [ClientRpc]
    private void SyncCastingClientRpc()
    {
        floater.drag = 0;
    }

    public void Reel(float reelChange)
    {
        currentRopeLength.Value = Mathf.Max(0, currentRopeLength.Value + reelChange);
    }


    private void SimulateVerlet()
    {
        // Apply Verlet integration to simulate line segments
        for (int i = 1; i < lineSegmentCount; i++)
        {
            Vector3 currentPoint = linePoints[i];
            Vector3 prevPoint = prevPoints[i];
            float effectiveGravity = floater.GetComponent<BuoyancyObject>().underwater ? gravity : gravity * 0.5f;
            Vector3 acceleration = new Vector3(0, effectiveGravity * floaterMass, 0);

            // Verlet position update
            linePoints[i] += (currentPoint - prevPoint) * verletDamping + acceleration * Time.deltaTime * Time.deltaTime;
            prevPoints[i] = currentPoint;
        }
    }

    private void ApplyConstraints()
    {
        // Keep the first segment at the rod tip position
        linePoints[0] = rodTip.position;

        int constraintIterations = 5;
        for(int z  = 0; z < constraintIterations; z++)
        {
            // Move floater closer to rod tip when length changes
            Vector3 floaterPosition = floater.position;
            float distanceToRod = Vector3.Distance(rodTip.position, floaterPosition);
            if (distanceToRod > currentRopeLength.Value)
            {
                Vector3 direcitonToRod = (rodTip.position - floaterPosition).normalized;
                floaterPosition = rodTip.position - direcitonToRod * currentRopeLength.Value;
                floater.MovePosition(floaterPosition);
            }

            // Lock the last point to the floater's position
            linePoints[lineSegmentCount - 1] = floater.position;

            // Apply distance constraints to maintain segment length
            for (int i = 0; i < lineSegmentCount - 1; i++)
            {
                Vector3 direction = (linePoints[i + 1] - linePoints[i]).normalized;
                float distance = Vector3.Distance(linePoints[i], linePoints[i + 1]);
                float error = distance - segmentLength;

                if (distance > 0)
                {
                    linePoints[i + 1] -= direction * error * 0.5f;
                    linePoints[i] += direction * error * 0.5f;
                }
            }
        }
    }

    [ClientRpc]
    private void DrawLineClientRpc()
    {
        // Render line based on the position of each segment
        for (int i = 0; i < lineSegmentCount; i++)
            lineRenderer.SetPosition(i, linePoints[i]);
    }
}