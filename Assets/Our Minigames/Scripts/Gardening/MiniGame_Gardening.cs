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

        public JobOrders[] m_JobOrders = new JobOrders[2];

        public override void Start()
        {
            base.Start();
            // Start game logic specific to gardening
            for (int i = 0; i < m_JobOrders.Length; i++)
            {
                m_JobOrders[i].CreateNewJob();
            }
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
            for (int i = 0; i < m_JobOrders.Length; i++)
            {
                m_JobOrders[i].CreateNewJob();
            }
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
