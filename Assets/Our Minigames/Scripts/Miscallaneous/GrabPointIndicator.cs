using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabPointIndicator : MonoBehaviour
{
    public GameObject[] grabPointDots; // Assign your dot GameObjects here
    private int grabCount = 0;

    private XRBaseInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);

        ToggleDots(false); // Hide dots initially
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (grabCount == 0)
            ToggleDots(true);
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        ToggleDots(false);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        grabCount++;
        ToggleDots(false);
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if(grabCount == 0)
            ToggleDots(false);
    }

    void ToggleDots(bool show)
    {
        foreach (var dot in grabPointDots)
        {
            dot.SetActive(show);
        }
    }
}