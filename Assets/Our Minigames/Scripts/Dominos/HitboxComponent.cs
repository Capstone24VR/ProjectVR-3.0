using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XRMultiplayer.MiniGames;

namespace Domino
{
    public class HitboxComponent : MonoBehaviour
    {
        public enum HitboxType
        {
            Domino, // Represents a side of a domino
            Snap    // Represents a snap point
        }

        public HitboxType hitboxType; // The type of the hitbox

        public bool isUsed = false; // Indicates if this hitbox is already used for snapping
        private Renderer hitboxRenderer;
        private SnapManager snapManager;
        public NetworkedDomino m_MiniGame;

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
        public int hitboxindex;

        private Color matchColor = Color.green; // Color for a matching domino
        private Color mismatchColor = Color.red; // Color for a mismatching domino
        private Color defaultColor = new Color(0f, 0f, 0f, 0f); // Fully transparent color

        private void Awake()
        {
            // Find references
            m_MiniGame = FindAnyObjectByType<NetworkedDomino>();
            snapManager = GetComponentInParent<SnapManager>();
            hitboxRenderer = GetComponent<Renderer>();

            if (snapManager == null)
            {
                Debug.LogError($"SnapManager not found for {gameObject.name}. Hitbox may not function correctly.");
                return;
            }

            // Find the index of this hitbox in the SnapManager's list
            hitboxindex = snapManager.hitboxes.IndexOf(this);
            if (hitboxindex == -1)
            {
                Debug.LogError($"{gameObject.name} is not registered in SnapManager's hitboxes list.");
                return;
            }

            // Fetch the side value if this is a Domino hitbox
            if (hitboxType == HitboxType.Domino)
            {
                var dominoData = GetComponentInParent<Domino_data>();
                if (dominoData != null)
                {
                    sideValue = (hitboxSide == Side.Top) ? dominoData.Top_side : dominoData.But_side;
                }
                else
                {
                    Debug.LogError($"{gameObject.name}: Could not find Domino_data in parent.");
                }
            }

            // Initialize hitbox color
            if (hitboxRenderer != null)
            {
                SetColor(defaultColor);
            }
            else
            {
                Debug.LogError($"Renderer component is missing on {gameObject.name}. Cannot set initial color.");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Domino"))
            {

                if (GetComponentInParent<Domino_data>().inHand || !other.GetComponentInParent<Domino_data>().inHand) return;
                Debug.Log("I am: " + GetComponentInParent<Domino_data>().name);
                Debug.Log("Other hitbox is: " + other.GetComponentInParent<Domino_data>().name);


                int value = other.name == "TopDomino" ? other.GetComponentInParent<Domino_data>().Top_side : other.GetComponentInParent<Domino_data>().But_side;
                bool isTopSide = other.name == "TopDomino";
                Debug.Log($"{other.name} has a value of {value}");

                var dominoinPlay = GetComponentInParent<Domino_data>();
                if (dominoinPlay == null || !dominoinPlay.played)
                {
                    Debug.LogWarning($"{name}: Domino in play is either null or not marked as played.");
                    return;
                }

                // Ensure this hitbox is not already used
                if (isUsed)
                {
                    Debug.Log($"{name}: This hitbox is already used.");
                    return;
                }




                // Check if the side values match
                if (sideValue == value)
                {
                    // Determine if it's a valid play based on Top_side and But_side
                    bool isSelfMatch = other.GetComponentInParent<Domino_data>().Top_side == other.GetComponentInParent<Domino_data>().But_side;
                    bool isValidPlay = !isSelfMatch || hitboxindex <= 1;

                    Debug.Log(isValidPlay);


                    SetColor(matchColor);
                    other.GetComponentInParent<Domino_data>().canBePlayed = true;
                    other.GetComponentInParent<Domino_data>().playHitboxIndex = hitboxindex;
                    other.GetComponentInParent<Domino_data>().stillDominoId = GetComponentInParent<NetworkObject>().NetworkObjectId;
                    other.GetComponentInParent<Domino_data>().isTopSide = isTopSide;

                    Debug.Log($"Other domino({other.transform.parent.name}) attempting to play with me {transform.parent.name} with Id of: {GetComponentInParent<NetworkObject>().NetworkObjectId} and conneting to hitbox: {hitboxindex}, and are we playing the topSide: {isTopSide}");

                }
                else
                {
                    SetColor(mismatchColor);
                    other.GetComponentInParent<Domino_data>().canBePlayed = false;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Reset the color to fully transparent when the domino exits
            SetColor(defaultColor);
        }

        public void SetColor(Color color)
        {
            if (hitboxRenderer != null)
            {
                hitboxRenderer.material.color = color; // Set the color directly to the material
            }
            else
            {
                Debug.LogError($"Renderer component is missing. Cannot set color for {gameObject.name}.");
            }
        }

        public void SetSideValue(int topSide, int bottomSide)
        {
            sideValue = (hitboxSide == Side.Top) ? topSide : bottomSide;
        }
    }
}
