using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishingLine : MonoBehaviour
{
    public GameObject hook;
    private LineRenderer lineRenderer;
    // Start is called before the first frame update
    private void Awake()
    {
        // Set up the Line Renderer
        lineRenderer = GetComponentInParent<LineRenderer>();
        lineRenderer.positionCount = 2;
    }

    // Update is called once per frame
    void Update()
    {
        if (hook.activeSelf)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, hook.transform.position);
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }
}
