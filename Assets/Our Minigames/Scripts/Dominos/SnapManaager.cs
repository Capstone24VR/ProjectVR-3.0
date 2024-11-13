using UnityEngine;

using System.Collections.Generic;
using Domino;

public class SnapManager : MonoBehaviour
{
    public List<HitboxComponent> hitboxes; // List of hitbox components for snap detection
    public Color highlightColor = new Color(0, 0, 1, 0.5f); // Color to highlight when a grabbed domino is detected
    public Color defaultColor = Color.clear; // Default color for the hitbox

    private HitboxComponent activeHitbox = null; // The hitbox currently being highlighted
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable currentDomino; // The specific domino currently in the hitbox

    private void Start()
    {
        // Initialize all hitbox colors to default at start
        foreach (var hitbox in hitboxes)
        {
            hitbox.SetColor(defaultColor);
        }
    }

    public void HighlightHitbox(HitboxComponent hitbox, UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable domino)
    {
        // Highlight this hitbox if a domino is detected
        if (activeHitbox != hitbox)
        {
            if (activeHitbox != null)
            {
                activeHitbox.SetColor(defaultColor); // Reset previous hitbox color
            }
            activeHitbox = hitbox;
            currentDomino = domino;
            activeHitbox.SetColor(highlightColor);
        }
    }

    public void ResetHighlight(HitboxComponent hitbox)
    {
        // Reset the highlight if the active hitbox is exited
        if (activeHitbox == hitbox)
        {
            activeHitbox.SetColor(defaultColor);
            activeHitbox = null;
            currentDomino = null;
        }
    }

    private void FixedUpdate()
    {
        // Check if the domino was released within an active hitbox area
        if (activeHitbox != null && currentDomino != null && !currentDomino.isSelected)
        {
            SnapToHitbox(activeHitbox, currentDomino);
        }
    }

    public void SnapToHitbox(HitboxComponent hitbox, UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable releasedDomino)
    {
        // Snap the released domino to match the position and rotation of the hitbox
        releasedDomino.transform.position = hitbox.transform.position;
        releasedDomino.transform.rotation = hitbox.transform.rotation;

        // Mark this hitbox as used
        hitbox.isUsed = true;

        // Disable the grab interactable on the released domino to prevent further grabbing
        releasedDomino.enabled = false;

        // Reset the hitbox color and clear the active hitbox
        hitbox.SetColor(defaultColor);
        activeHitbox = null;
        currentDomino = null;

        Debug.Log("Domino has been snapped and marked as played.");
    }
}
