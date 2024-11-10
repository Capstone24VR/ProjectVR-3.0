
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents the base for a card mini-game.
    /// </summary>)]
    public class MiniGame_Fishing : MiniGameBase
    {

        public NetworkedFishManager m_NetworkedGameplay;
        public GameObject Water;


        /// <summary>
        /// The current score of the mini-game.
        /// </summary>
        int m_CurrentScore = 0;

        public override void Start()
        {
            base.Start();

            TryGetComponent(out m_NetworkedGameplay);
        }

        public override void SetupGame()
        {
            base.SetupGame();
            //fishManager.enabled = false;
            //List<GameObject> currentFish = new List<GameObject>();
            //Water.GetChildGameObjects(currentFish);
            //foreach (GameObject fish in currentFish)
            //{
            //    Destroy(fish);
            //}

            m_NetworkedGameplay.ResetGame();
        }

        public override void StartGame()
        {
            base.StartGame();
            //fishManager.enabled = true;
            if ((m_NetworkedGameplay.IsServer))
            {
                m_NetworkedGameplay.SpawnProcessServer();
            }
        }

        public override void UpdateGame(float deltaTime)
        {
            base.UpdateGame(deltaTime);
            //if (m_NetworkedGameplay.IsServer)
            //{
            //    m_NetworkedGameplay.CheckForPlayerWin();
            //}
        }


        public override void FinishGame(bool submitScore = true)
        {
            base.FinishGame(submitScore);
            //fishManager.enabled = false;
            //List<GameObject> currentFish = new List<GameObject>();
            //Water.GetChildGameObjects(currentFish);
            //foreach (GameObject fish in currentFish)
            //{
            //    Destroy(fish);
            //}

            m_NetworkedGameplay.EndGame();
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