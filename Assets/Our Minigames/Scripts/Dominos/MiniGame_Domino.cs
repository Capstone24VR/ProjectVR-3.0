
using System.Collections;
using UnityEngine;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents the base for a card mini-game.
    /// </summary>)]
    public class MiniGame_Domino : MiniGameBase
    {

        NetworkedDomino m_NetworkedGameplay;

        public override void Start()
        {
            base.Start();

            TryGetComponent(out m_NetworkedGameplay);
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
                m_NetworkedGameplay.StartGame();
            }
        }

        public override void UpdateGame(float deltaTime)
        {
            base.UpdateGame(deltaTime);
            if (m_NetworkedGameplay.IsServer)
            {
                m_NetworkedGameplay.CheckForPlayerLeave();
                m_NetworkedGameplay.CheckForPlayerWin();
            }
        }


        public override void FinishGame(bool submitScore = true)
        {
            base.FinishGame(submitScore);
            m_NetworkedGameplay.EndGame();
        }

        public IEnumerator SendAllPlayersMessage(string message, int seconds)
        {
            while (seconds > 0)
            {
                if (m_MiniGameManager.LocalPlayerInGame)
                {
                    PlayerHudNotification.Instance.ShowText(message);
                }
                yield return new WaitForSeconds(1.0f);
                seconds--;
            }
        }

        public IEnumerator SendPlayerMessage(string message, ulong localId, int seconds)
        {
            while (seconds > 0)
            {
                if (m_MiniGameManager.LocalPlayerInGame && (ulong)m_MiniGameManager.GetLocalPlayerID() == localId)
                {
                    PlayerHudNotification.Instance.ShowText(message);
                }
                yield return new WaitForSeconds(1.0f);
                seconds--;
            }
        }

        public IEnumerator PlayerWonRoutine(NetworkedHandDomino winner)
        {
            if (m_MiniGameManager.LocalPlayerInGame)
            {
                PlayerHudNotification.Instance.ShowText($"Game Complete! " + winner.name + " has won.");
            }

            if (XRINetworkGameManager.Instance.GetPlayerByID(XRINetworkPlayer.LocalPlayer.OwnerClientId, out XRINetworkPlayer player))
            {
                if (XRINetworkPlayer.LocalPlayer.OwnerClientId == winner.ownerManager.seatHandler.GetClientID())
                    m_MiniGameManager.SubmitScoreServerRpc(m_MiniGameManager.currentPlayerDictionary[player].currentScore + 1, XRINetworkPlayer.LocalPlayer.OwnerClientId);
                else
                    m_MiniGameManager.SubmitScoreServerRpc(m_MiniGameManager.currentPlayerDictionary[player].currentScore, XRINetworkPlayer.LocalPlayer.OwnerClientId);
            }



            if (m_MiniGameManager.IsServer && m_MiniGameManager.currentNetworkedGameState == MiniGameManager.GameState.InGame)
                m_MiniGameManager.StopGameServerRpc();

            FinishGame();

            yield return null;
        }

        public IEnumerator PlayerLeftRoutine()
        {
            if (m_MiniGameManager.LocalPlayerInGame)
            {
                PlayerHudNotification.Instance.ShowText($"A player has left, ending game . . .");
            }


            if (m_MiniGameManager.IsServer && m_MiniGameManager.currentNetworkedGameState == MiniGameManager.GameState.InGame)
                m_MiniGameManager.StopGameServerRpc();

            FinishGame();

            yield return null;
        }

    }
}