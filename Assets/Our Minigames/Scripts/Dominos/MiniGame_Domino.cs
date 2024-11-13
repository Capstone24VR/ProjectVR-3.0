using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents a mini-game called "Whack-A-Pig" where the player needs to hit pigs with a hammer.
    /// </summary>
    public class MiniGame_Domino : MiniGameBase
    {
   

        /// <summary>
        /// The current score of the mini-game.
        /// </summary>
        int m_CurrentScore = 0;

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();

           
        }


        /// <summary>
        /// Sets up the game by resetting the current score.
        /// </summary>
        public override void SetupGame()
        {
            base.SetupGame();
            m_CurrentScore = 0;
        }

        /// <summary>
        /// Starts the game by spawning pigs if the player is the server.
        /// </summary>
        public override void StartGame()
        {
            base.StartGame();

        }

        /// <summary>
        /// Finishes the game and ends the networked gameplay.
        /// </summary>
        /// <param name="submitScore">Whether to submit the score or not.</param>
        public override void FinishGame(bool submitScore = true)
        {
            base.FinishGame(submitScore);
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
