
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static Card;
using static XRMultiplayer.MiniGames.MiniGameBase;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents the base for a card mini-game.
    /// </summary>)]
    public class MiniGame_Cards : MiniGameBase
    {

        [System.Serializable]
        public class Hand
        {
            public ulong playedId;
            public GameObject playArea;
            public int currCards = 0;
            public int maxCards = 0;
            public bool active = true;

            [SerializeField] public List<GameObject> heldCards = new List<GameObject>();

            public bool isFull() { return currCards == maxCards; }
            public bool isEmpty() { return currCards == 0; }
            public bool canDraw() { return currCards < maxCards; }
            public void SendCardData() 
            { 
                playArea.GetComponent<PlayArea>().cardData = heldCards;
                currCards = heldCards.Count;
            }
            public void ConfigureChildPositions() { playArea.GetComponent<PlayArea>().ConfigureChildrenPositions(); }

            public void AutoDrawCard(GameObject card)
            {
                card.transform.SetParent(playArea.transform, false);
                card.transform.localPosition = Vector3.zero;
                card.SetActive(true);
                heldCards.Add(card);
                currCards = heldCards.Count;
                SendCardData();
                ConfigureChildPositions();
                Card cardComponent = card.GetComponent<Card>();
                if (cardComponent != null)
                {
                    cardComponent.SetInHand(true);
                }
            }

            public void ManDrawCard(GameObject card)
            {
                card.transform.SetParent(playArea.transform, false);
                heldCards.Add(card);
                currCards = heldCards.Count;
                SendCardData();
                ConfigureChildPositions();
                Card cardComponent = card.GetComponent<Card>();
                if (cardComponent != null)
                {
                    cardComponent.SetInHand(true);
                }
            }

            public void Clear()
            {
                heldCards.Clear();
                playArea.GetComponent<PlayArea>().cardData.Clear();
                currCards = 0;
            }
        }

        public enum game { Colors, Crazy_Eights};

        public GameObject card;

        public GameObject drawPileObj;
        public GameObject playPileObj;

        public int numCards = 12;
        public int startingHand = 3;

        [SerializeField] protected List<GameObject> deck = new List<GameObject>();

        [SerializeField] protected Stack<GameObject> _drawPile = new Stack<GameObject>();
        [SerializeField] protected Stack<GameObject> _playPile = new Stack<GameObject>();

        [SerializeField] protected Hand hand1;
        [SerializeField] protected Hand hand2;

        [SerializeField] protected int currentHandIndex;

        [SerializeField] protected List<Hand> activeHands =new List<Hand>();

        public override void SetupGame()
        {
            
            base.SetupGame();

            if (hand1.active) { activeHands.Add(hand1); } else { hand1.playArea.SetActive(false); }
            if (hand2.active) { activeHands.Add(hand2); } else { hand2.playArea.SetActive(false); }

            hand1.maxCards = 9999;
            hand2.maxCards = 9999;
            CreateDeckBasic();
            ShuffleDeck(deck);
            InstatiateDrawPile();
        }

        public override void StartGame()
        {
            base.StartGame();
            Debug.Log(startingHand);
            for(int i = 0; i < startingHand; i++)
            {
                foreach(Hand hand in activeHands)
                {
                    if (hand.canDraw()) { hand.AutoDrawCard(_drawPile.Pop()); }
                }
            }
            currentHandIndex = 0;
            StartCrazyEights();
            _drawPile.Peek().SetActive(true);
            
        }

        public override void UpdateGame(float deltaTime)
        {
            base.UpdateGame(deltaTime);
            foreach(Hand hand in activeHands)
            {
                hand.SendCardData();
            }
            CheckForPlayerWin();
<<<<<<< HEAD
=======

        }

        public void CheckForPlayerWin()
        {
            //Debug.Log("Am checking: " + hand1.currCards + "\t" + hand1.isEmpty());

            if (hand1.isEmpty()) {
                Debug.Log("Hand is empty calling courotine");
                StartCoroutine(PlayerWonRoutine());
            }
>>>>>>> Tony
        }


        public override void FinishGame(bool submitScore = true)
        {
            base.FinishGame(submitScore);
            RemoveGeneratedCards();
        }

        protected void CreateDeckBasic()
        {
            Debug.Log("Creating Deck . . .");
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Value value in Enum.GetValues(typeof(Value)))
                {
                    UnityEngine.Object pPrefab  = ((int)value > 1 && (int)value < 11) ? Resources.Load("Free_Playing_Cards/PlayingCards_" + (int)value + suit) : Resources.Load("Free_Playing_Cards/PlayingCards_" + value + suit);

                    GameObject newCard = Instantiate(card, drawPileObj.transform, false);
                    newCard.transform.localPosition  = Vector3.zero;
                    GameObject model = (GameObject)Instantiate(pPrefab, newCard.transform, false);
                    model.transform.rotation = Quaternion.identity;
                    model.transform.localPosition = Vector3.zero;
                    newCard.GetComponent<Card>().suit = suit;
                    newCard.GetComponent<Card>().value = value;
                    newCard.name = "Card: " + suit + " " + value;
                    newCard.SetActive(false);
                    deck.Add(newCard);
                }
            }
            Debug.Log("Deck created.");
            numCards = deck.Count;
        }

        protected void ShuffleDeck(List<GameObject> deck)
        {
            Debug.Log("Shuffling Deck . . .");
            System.Random r = new System.Random();
            for (int n = deck.Count - 1; n > 0; --n)
            {
                int k = r.Next(n + 1);
                GameObject temp = deck[n];
                deck[n] = deck[k];
                deck[k] = temp;
            }
            Debug.Log("Deck Shuffled.");
        }

        protected void InstatiateDrawPile()
        {
            Debug.Log("Creating Draw Pile . . .");
            foreach (GameObject card in deck)
            {
                _drawPile.Push(card);
            }
            Debug.Log("Draw Pile created.");
        }

        public void ManualDrawCard(GameObject card)
        {
            if(_drawPile.Count > 0)
            {
                var topCard = _drawPile.Peek();
                if (card == topCard)
                {
                    activeHands[currentHandIndex].ManDrawCard(topCard);
                    _drawPile.Pop();
                    if (_drawPile.Count > 0) { _drawPile.Peek().SetActive(true); }
                }
            }

            UpdateCurrentIndex();
        }

        public void PlayCard(GameObject card)
        {
            Debug.Log(card.name);

            if (!activeHands[currentHandIndex].heldCards.Contains(card)) 
            {
                Debug.Log("Wrong player!");
                return; 
            }

            //if (!IsValidPlayCrazyEights(card))
            //{
            //    return;
            //}

            card.SetActive(false);
            card.GetComponent<Card>().played = true;
            card.GetComponent<XRGrabInteractable>().enabled = false;

            activeHands[currentHandIndex].heldCards.Remove(card);
            activeHands[currentHandIndex].ConfigureChildPositions();
            
            card.transform.parent = playPileObj.transform;
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;

            if (_playPile.Count > 0) { _playPile.Peek().SetActive(false); }
            _playPile.Push(card);
            _playPile.Peek().SetActive(true);

            UpdateCurrentIndex();
        }

        protected void StartCrazyEights()
        {
            GameObject firstCard = _drawPile.Pop();
            Debug.Log("Drawing First card(" + firstCard.name + ") for Crazy Eights . . . ");
            AddToPlayPile(firstCard);

            if (_playPile.TryPeek(out GameObject topCard))
            {
                topCard.SetActive(true);
            }

            Debug.Log("First card drawn.");
        }

        protected bool IsValidPlayCrazyEights(GameObject card)
        {
            if (_playPile.TryPeek(out GameObject topCard))
            {
                if (topCard.GetComponent<Card>().suit == card.GetComponent<Card>().suit)
                {
                    Debug.Log("Cards share the same suit: " + card.GetComponent<Card>().suit);
                    return true;
                }
                else if (topCard.GetComponent<Card>().value == card.GetComponent<Card>().value)
                {
                    Debug.Log("Cards share the same value: " + card.GetComponent<Card>().value);
                    return true;
                }
            }
            Debug.Log("Card setup failed or Card is not valid");
            return false;
        }

        public void UpdateCurrentIndex()
        {
            if (currentHandIndex == activeHands.Count - 1) { currentHandIndex = 0; }
            else { currentHandIndex++; }

            Debug.Log("Setting current hand to " + activeHands[currentHandIndex].playArea.name);
        }

        public void CheckForPlayerWin()
        {
           foreach(Hand hand in activeHands)
            {
                if (hand.isEmpty()){
                    Debug.Log(hand.playArea.name + "is empty, calling courotine");
                    StartCoroutine(PlayerWonRoutine(hand.playArea));
                }
            }
        }

        IEnumerator PlayerWonRoutine(GameObject winner)
        {
            if (m_MiniGameManager.LocalPlayerInGame)
            {
                PlayerHudNotification.Instance.ShowText($"Game Complete! " + winner.name + " has won.");
            }

            if (m_MiniGameManager.IsServer && m_MiniGameManager.currentNetworkedGameState == MiniGameManager.GameState.InGame)
                m_MiniGameManager.StopGameServerRpc();

            FinishGame();

            yield return null;
        }

        private void AddToPlayPile(GameObject card)
        {
            card.transform.parent = playPileObj.transform;
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;
            _playPile.Push(card);
        }

        private void AddToDrawPile(GameObject card)
        {
            card.transform.parent = drawPileObj.transform;
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;
            _drawPile.Push(card);
        }

        private void RemoveGeneratedCards()
        {
            foreach(Hand hand in activeHands)
            {
                hand.Clear();
            }

            _playPile.Clear();
            _drawPile.Clear();

            foreach (GameObject card in deck)
            {
                Destroy(card);
            }
            numCards = 0;
            deck.Clear();
        }
    }
}
