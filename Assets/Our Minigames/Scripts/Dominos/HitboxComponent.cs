using UnityEngine;

namespace Domino
{
    public class HitboxComponent : MonoBehaviour
    {
        public bool isUsed = false; // Indicates if this hitbox is already used for snapping
        private Renderer hitboxRenderer;
        private SnapManager snapManager;

        // Enum to select which side this hitbox represents
        public enum Side
        {
            Top,
            Bottom
        }

        // Expose the Side selection in the Inspector
        public Side hitboxSide;

        // Variable to store the side's value based on the selection
        public int sideValue;

        private void Awake()
        {
            hitboxRenderer = GetComponent<Renderer>();
            snapManager = GetComponentInParent<SnapManager>();

            // Set the initial color of the hitbox to fully transparent
            if (hitboxRenderer != null)
            {
                Color transparentColor = hitboxRenderer.material.color;
                transparentColor.a = 0f; // Fully transparent
                hitboxRenderer.material.color = transparentColor;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // Check if the other collider belongs to a grabbed domino
            if (other.CompareTag("Domino"))
            {
                var otherGrabInteractable = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                var otherDominoData = other.GetComponentInParent<Domino_data>();

                if (otherGrabInteractable != null && otherGrabInteractable.isSelected && !isUsed)
                {
                    // Check if sideValue matches the Top_side or But_side of the colliding domino
                    if (sideValue == otherDominoData.Top_side || sideValue == otherDominoData.But_side)
                    {

                        Debug.Log("collide domino");
                        SetColor(Color.green); // Highlight green for matching value
                    }
                    else
                    {
                        SetColor(Color.red); // Highlight red for non-matching value
                    }

                    snapManager.HighlightHitbox(this, otherGrabInteractable);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Reset the color to transparent when the domino leaves the hitbox
            SetColor(new Color(1, 1, 1, 0)); // Fully transparent
        }

        public void SetColor(Color color)
        {
            if (hitboxRenderer != null)
            {
                hitboxRenderer.material.color = color;
            }
        }

        // Method to set the side's value based on the enum selection
        public void SetSideValue(int topSide, int bottomSide)
        {
            sideValue = (hitboxSide == Side.Top) ? topSide : bottomSide;
        }

        public void SnapDomino(Transform domino)
        {
            // Snap the domino to the hitbox position and orientation
            domino.position = transform.position;
            domino.rotation = transform.rotation;

            isUsed = true;
            SetColor(new Color(1, 1, 1, 0)); // Reset color to transparent after snapping
        }
    }
}
