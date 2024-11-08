using System.Collections.Generic;
using UnityEngine;

public class NewFishingLine : MonoBehaviour
{
    public Transform rodTip;
    public Rigidbody hook;
    public LineRenderer lineRenderer;

    public int lineSegmentCount = 20;  // Number of points in the line
    public float segmentLength = 0.1f; // Distance between each segment point
    public float gravity = -9.81f;
    public float hookMass = 0.2f;

    private List<Vector3> linePoints;
    private bool isCasting = false;
    private bool lineLocked = false;

    private void Start()
    {
        // Initialize line with segments
        linePoints = new List<Vector3>();
        for (int i = 0; i < lineSegmentCount; i++)
            linePoints.Add(rodTip.position);

        lineRenderer.positionCount = lineSegmentCount;
    }

    private void Update()
    {
        if (!isCasting)
        {
            // When not casting, set all points close to the rod tip
            for (int i = 0; i < lineSegmentCount; i++)
                linePoints[i] = rodTip.position;
        }
        else
        {
            if (!lineLocked)
            {
                SimulateVerlet();
                ApplyConstraints();
            }
        }

        DrawLine();
    }

    public void StartCasting()
    {
        isCasting = true;
        lineLocked = false;
    }

    public void StopCasting()
    {
        isCasting = false;
        lineLocked = true;
    }

    private void SimulateVerlet()
    {
        // Apply Verlet integration to simulate line segments
        for (int i = 1; i < lineSegmentCount; i++)
        {
            Vector3 currentPoint = linePoints[i];
            Vector3 prevPoint = linePoints[i];
            Vector3 acceleration = new Vector3(0, gravity * hookMass, 0);

            // Verlet position update
            linePoints[i] += (currentPoint - prevPoint) + acceleration * Time.deltaTime * Time.deltaTime;
        }
    }

    private void ApplyConstraints()
    {
        // Keep the first segment at the rod tip position
        linePoints[0] = rodTip.position;

        // Lock the last point to the hook's position
        linePoints[lineSegmentCount - 1] = hook.position;

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

    private void DrawLine()
    {
        // Render line based on the position of each segment
        for (int i = 0; i < lineSegmentCount; i++)
            lineRenderer.SetPosition(i, linePoints[i]);
    }
}