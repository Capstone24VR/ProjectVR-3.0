using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace XRMultiplayer.MiniGames
{
    public class MiniGame_Gardening : MiniGameBase
    {
        int m_CurrentScore = 0;

        public override void Start()
        {
            base.Start();
        }

        public override void SetupGame()
        {
            base.SetupGame();
        }

        public override void StartGame()
        {
            base.StartGame();
            m_CurrentScore = 0;
            // Start game logic specific to gardening
        }

        public override void UpdateGame(float deltaTime)
        {
            base.UpdateGame(deltaTime);
        }

        public override void FinishGame(bool submitScore = true)
        {
            base.FinishGame(submitScore);
            m_MiniGameManager.FinishGame();
        }

        /// <summary>
        /// Called when the local player hits a target.
        /// </summary>
        /// <param name="targetValue"></param>
        public void LocalPlayerCompletedJob(int targetValue)
        {
            if (m_MiniGameManager.currentNetworkedGameState == MiniGameManager.GameState.InGame)
            {
                m_CurrentScore += targetValue;
                m_MiniGameManager.SubmitScoreServerRpc(m_CurrentScore, XRINetworkPlayer.LocalPlayer.OwnerClientId);
            }
        }
    }
}
