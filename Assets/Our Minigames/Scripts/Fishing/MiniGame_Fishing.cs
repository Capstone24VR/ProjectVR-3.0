
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents the base for a card mini-game.
    /// </summary>)]
    public class MiniGame_Fishing : MiniGameBase
    {
        /// <summary>
        /// The time to wait before resetting the fish rod's position.
        /// </summary>
        [SerializeField] float m_RodResetTime = .25f;

        /// <summary>
        /// Hooks for Fish to use
        /// </summary>
        [SerializeField] public Transform[] m_Hooks = new Transform[4];

        /// <summary>
        /// The networked gameplay to use for handling the networked gameplay logic.
        /// </summary>
        public NetworkedFishManager m_NetworkedGameplay;

        /// <summary>
        /// The interactable objects to use for the mini-game.
        /// </summary>
        readonly Dictionary<XRBaseInteractable, Pose> m_InteractablePoses = new();


        /// <summary>
        /// The current score of the mini-game.
        /// </summary>
        int m_CurrentScore = 0;

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();

            TryGetComponent(out m_NetworkedGameplay);

            foreach (var interactable in m_GameInteractables)
            {
                if (!m_InteractablePoses.ContainsKey(interactable))
                {
                    m_InteractablePoses.Add(interactable, new Pose(interactable.transform.position, interactable.transform.rotation));
                    interactable.selectExited.AddListener(RodDropped);
                }
            }
        }

        void OnDestroy()
        {
            foreach (var kvp in m_InteractablePoses)
            {
                kvp.Key.selectExited.RemoveListener(RodDropped);
            }
        }

        public override void SetupGame()
        {
            base.SetupGame();
            m_NetworkedGameplay.ResetGame();
        }

        public override void StartGame()
        {
            base.StartGame();
            if ((m_NetworkedGameplay.IsServer))
            {
                m_NetworkedGameplay.StartSpawningFish();
            }
        }

        public override void FinishGame(bool submitScore = true)
        {
            base.FinishGame(submitScore);
            m_NetworkedGameplay.EndGame();
        }

        /// <summary>
        /// Called when the hammer is dropped on an interactable object.
        /// </summary>
        /// <param name="args">The interaction event arguments.</param>
        void RodDropped(BaseInteractionEventArgs args)
        {
            XRBaseInteractable interactable = (XRBaseInteractable)args.interactableObject;
            if (m_InteractablePoses.ContainsKey(interactable))
            {
                StartCoroutine(DropRodAfterTimeRoutine(interactable));
            }
        }

        /// <summary>
        /// Coroutine that drops the hammer after a specified time and resets the interactable's position.
        /// </summary>
        /// <param name="interactable">The interactable object.</param>
        IEnumerator DropRodAfterTimeRoutine(XRBaseInteractable interactable)
        {
            yield return new WaitForSeconds(m_RodResetTime);
            if (!interactable.isSelected)
            {
                Rigidbody body = interactable.transform.GetComponentInChildren<GrabPointIndicator>().gameObject.GetComponent<Rigidbody>();
                bool wasKinematic = body.isKinematic;
                body.isKinematic = true;
                interactable.transform.SetPositionAndRotation(m_InteractablePoses[interactable].position, m_InteractablePoses[interactable].rotation);
                yield return new WaitForFixedUpdate();
                body.isKinematic = wasKinematic;
                foreach (var collider in interactable.colliders)
                {
                    collider.enabled = true;
                }
            }
        }

        /// <summary>
        /// Updates the local player's score and submits it to the server.
        /// </summary>
        /// <param name="pointValue">The point value to add to the score.</param>
        public void LocalPlayerScored(int pointValue)
        {
            m_CurrentScore += pointValue;
            if (m_CurrentScore < 0) m_CurrentScore = 0;
            m_MiniGameManager.SubmitScoreServerRpc(m_CurrentScore, XRINetworkPlayer.LocalPlayer.OwnerClientId);
        }

    }
}