using System.Collections.Generic;

namespace TrucoMineiro.API.Models
{
    /// <summary>
    /// Represents a deck of cards for the Truco game
    /// </summary>
    public class Deck
    {
        private readonly List<Card> _cards = new();
        private readonly Random _random = new();

        public Deck()
        {
            InitializeDeck();
        }

        /// <summary>
        /// Initialize the deck with the 40 cards used in Truco Mineiro
        /// </summary>
        private void InitializeDeck()
        {
            // Truco Mineiro typically uses a 40-card deck without 8, 9, 10, and Jokers
            var values = new[] { "A", "2", "3", "4", "5", "6", "7", "Q", "J", "K" };
            var suits = new[] { "Clubs", "Hearts", "Spades", "Diamonds" };

            foreach (var suit in suits)
            {
                foreach (var value in values)
                {
                    _cards.Add(new Card(value, suit));
                }
            }
        }

        /// <summary>
        /// Shuffle the deck
        /// </summary>
        public void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        /// <summary>
        /// Draw a card from the top of the deck
        /// </summary>
        public Card DrawCard()
        {
            if (_cards.Count == 0)
            {
                throw new InvalidOperationException("The deck is empty!");
            }

            var card = _cards[0];
            _cards.RemoveAt(0);
            return card;
        }

        /// <summary>
        /// Draw multiple cards at once
        /// </summary>
        public List<Card> DrawCards(int count)
        {
            if (_cards.Count < count)
            {
                throw new InvalidOperationException($"Not enough cards in the deck! Requested: {count}, Available: {_cards.Count}");
            }

            var cards = _cards.Take(count).ToList();
            _cards.RemoveRange(0, count);
            return cards;
        }

        /// <summary>
        /// Get the number of cards left in the deck
        /// </summary>
        public int GetCardCount()
        {
            return _cards.Count;
        }

        /// <summary>
        /// Deal a card from the deck (alias for DrawCard)
        /// </summary>
        public Card DealCard()
        {
            return DrawCard();
        }
    }
}
