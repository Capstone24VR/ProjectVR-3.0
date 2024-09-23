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
    public class MiniGame_Gardening : MiniGameBase
    {
        public override void Start()
        {
            base.Start();
        }

        ///<inheritdoc/>
        public override void SetupGame()
        {
            base.SetupGame();
        }

        ///<inheritdoc/>
        public override void StartGame()
        {
            base.StartGame();
        }

        ///<inheritdoc/>
        public override void FinishGame(bool submitScore = true)
        {
            base.FinishGame(submitScore);
        }
    }
}
