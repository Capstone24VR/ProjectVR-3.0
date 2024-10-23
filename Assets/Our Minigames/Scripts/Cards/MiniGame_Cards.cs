
using System.Collections;
using UnityEngine;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents the base for a card mini-game.
    /// </summary>)]
    public class MiniGame_Cards : MiniGameBase
    {

        NetworkedCards m_NetworkedGameplay;

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
                m_NetworkedGameplay.CheckForPlayerWin();
            }
        }


        public override void FinishGame(bool submitScore = true)
        {
            base.FinishGame(submitScore);
        }

        public IEnumerator PlayerWonRoutine(GameObject winner)
        {
            if (m_MiniGameManager.LocalPlayerInGame)
            {
                PlayerHudNotification.Instance.ShowText($"Game Complete! " + winner.name + " has won.");
            }

            m_MiniGameManager.SubmitScoreServerRpc(1, XRINetworkPlayer.LocalPlayer.OwnerClientId);

            if (m_MiniGameManager.IsServer && m_MiniGameManager.currentNetworkedGameState == MiniGameManager.GameState.InGame)
                m_MiniGameManager.StopGameServerRpc();

            FinishGame();

            yield return null;
        }

    }
}