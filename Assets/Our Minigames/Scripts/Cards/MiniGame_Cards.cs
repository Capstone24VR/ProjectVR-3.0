using SerializableCallback;
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
            public GameObject playArea;
            public int currCards = 0;
            public int maxCards = 0;

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
            }

            public void ManDrawCard(GameObject card)
            {
                card.transform.SetParent(playArea.transform, false);
                heldCards.Add(card);
                currCards = heldCards.Count;
                SendCardData();
                ConfigureChildPositions();
            }
        }

        public enum game { Colors };

        public GameObject card;
        public bool allStartingHandsDrawn = false;

        public GameObject drawPileObj;
        public GameObject playPileObj;

        public int numCards = 12;
        public int startingHand = 1;

        [SerializeField] protected List<GameObject> deck = new List<GameObject>();

        [SerializeField] protected Stack<GameObject> _drawPile = new Stack<GameObject>();
        [SerializeField] protected Stack<GameObject> _playPile = new Stack<GameObject>();

        [SerializeField] protected Hand hand1;
        [SerializeField] protected Hand hand2;

        public override void SetupGame()
        {
            base.SetupGame();
            hand1.maxCards = 9999;
            hand2.maxCards = 9999;
            CreateDeck();
        }

        public override void StartGame()
        {
            base.StartGame();
            for(int i = 0; i < startingHand; i++)
            {
                if (hand1.canDraw()) { hand1.AutoDrawCard(_drawPile.Pop()); }
                if (hand2.canDraw()) { hand2.AutoDrawCard(_drawPile.Pop()); }
            }
            _drawPile.Peek().SetActive(true);
        }

        public override void UpdateGame(float deltaTime)
        {
            base.UpdateGame(deltaTime);
            hand1.SendCardData();
            hand2.SendCardData();
            CheckForPlayerWin();

        }

        public void CheckForPlayerWin()
        {
            Debug.Log("Am checking: " + hand1.currCards + "\t" + hand1.isEmpty());

            if (hand1.isEmpty()) {
                Debug.Log("Hand is empty calling courotine");
                StartCoroutine(PlayerWonRoutine());
            }
        }


        public override void FinishGame(bool submitScore = true)
        {
            base.FinishGame(submitScore);
            RemoveGeneratedCards();
        }


        protected void CreateDeck()
        {
            for(int i =- 0; i < numCards; i++)
            {
                GameObject newCard = Instantiate(card, drawPileObj.transform, false);
                newCard.name = "Card: " + i;
                newCard.SetActive(false);
                int color = UnityEngine.Random.Range(0, 3);

                switch (color)
                {
                    case 0:
                        newCard.GetComponent<Card>().val = Card.Value.Red;
                        newCard.GetComponent<Renderer>().material.color = Color.red;
                        break;
                    case 1:
                        newCard.GetComponent<Card>().val = Card.Value.Green;
                        newCard.GetComponent<Renderer>().material.color = Color.green;
                        break;
                    case 2:
                        newCard.GetComponent<Card>().val = Card.Value.Blue;
                        newCard.GetComponent<Renderer>().material.color = Color.blue;
                        break;
                }
                deck.Add(newCard);
                _drawPile.Push(newCard);
            }
        }


        public void ManualDrawCard(GameObject card)
        {
            if(_drawPile.Count > 0)
            {
                var topCard = _drawPile.Peek();
                if (card == topCard)
                {
                    hand1.ManDrawCard(topCard);
                    _drawPile.Pop();
                    if (_drawPile.Count > 0) { _drawPile.Peek().SetActive(true); }
                }
            }
        }

        public void PlayCard(GameObject card)
        {
            Debug.Log("Card: " + card.name + "\tCard Suit: " + card.GetComponent<Card>().suit + "\tCard Value: " + card.GetComponent<Card>().val);
            card.SetActive(false);
            hand1.heldCards.Remove(card);
            hand1.ConfigureChildPositions();
            card.transform.parent = playPileObj.transform;
            _playPile.Push(card);
        }


        IEnumerator PlayerWonRoutine()
        {
            if (m_MiniGameManager.LocalPlayerInGame)
            {
                PlayerHudNotification.Instance.ShowText($"Game Complete! Player 1 won.");
            }

            if (m_MiniGameManager.IsServer && m_MiniGameManager.currentNetworkedGameState == MiniGameManager.GameState.InGame)
                m_MiniGameManager.StopGameServerRpc();

            FinishGame();

            yield return null;
        }

        private void RemoveGeneratedCards()
        {
            hand1.heldCards.Clear();
            hand2.heldCards.Clear();
            _playPile.Clear();
            _drawPile.Clear();
            foreach (GameObject card in deck)
            {
                Destroy(card);
            }
            deck.Clear();
        }

        //private IEnumerator DelayedDraw(float time)
        //{

        //    yield return new WaitForSeconds(time);
        //    if (hand1.canDraw()) { hand1.AutoDrawCard(_drawPile.Pop()); }

        //    yield return new WaitForSeconds(time);
        //    if (hand2.canDraw()) { hand2.AutoDrawCard(_drawPile.Pop()); }

        //}

    }
}

//public enum Suit { Heart, Diamond, Club, Spade }
//public enum Value
//{
//    Ace = 1,
//    Two = 2,
//    Three = 3,
//    Four = 4,
//    Five = 5,
//    Six = 6,
//    Seven = 7,
//    Eight = 8,
//    Nine = 9,
//    Ten = 10,
//    Jack = 11,
//    Queen = 12,
//    King = 13
//}

//public class Card
//{
//    public Suit suit;
//    public Value value;
//}

//public static List<Card> CreateDeckBasic()
//{
//    List<Card> deck = new List<Card>();
//    foreach (Suit suit in Enum.GetValues(typeof(Suit)))
//    {
//        foreach (Value val in Enum.GetValues(typeof(Value)))
//        {
//            Card newCard = new Card();
//            newCard.suit = suit;
//            newCard.value = val;
//            deck.Add(newCard);
//        }
//    }
//    return deck;
//}

//public static void ShuffleDeck(List<Card> deck)
//{
//    Random r = new Random();
//    for (int n = deck.Count - 1; n > 0; --n)
//    {
//        int k = r.Next(n + 1);
//        Card temp = deck[n];
//        deck[n] = deck[k];
//        deck[k] = temp;
//    }
//}

//public static void Main(string[] args)
//{
//    List<Card> deck = CreateDeckBasic();
//    ShuffleDeck(deck);
//    foreach (Card card in deck)
//    {
//        Console.WriteLine(card.suit + " " + card.value);
//    }
//}