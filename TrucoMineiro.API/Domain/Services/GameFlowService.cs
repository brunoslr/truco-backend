using System;
using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Services
{    /// <summary>
    /// Service responsible for managing game flow and turn sequence
    /// </summary>
    public class GameFlowService : IGameFlowService
    {
        private readonly IAIPlayerService _aiPlayerService;
        private readonly IGameRepository _gameRepository;
        private readonly IHandResolutionService _handResolutionService;
        private readonly IEventPublisher _eventPublisher;

        public GameFlowService(
            IAIPlayerService aiPlayerService, 
            IGameRepository gameRepository,
            IHandResolutionService handResolutionService,
            IEventPublisher eventPublisher)
        {
            _aiPlayerService = aiPlayerService;
            _gameRepository = gameRepository;
            _handResolutionService = handResolutionService;
            _eventPublisher = eventPublisher;
        }

        /// <summary>
        /// Executes a player's card play and manages the subsequent game flow
        /// </summary>
        public bool PlayCard(GameState game, int playerSeat, int cardIndex)
        {
            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (player == null || !player.IsActive)
            {
                return false;
            }

            if (cardIndex < 0 || cardIndex >= player.Hand.Count)
            {
                return false;
            }

            // Play the card
            var card = player.PlayCard(cardIndex);

            // Find the player's played card slot
            var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == player.Seat);            if (playedCard != null)
            {
                playedCard.Card = card;
            }
            else            {
                game.PlayedCards.Add(new PlayedCard(player.Seat, card));
            }

            // ActionLog entry will be created by ActionLogEventHandler when CardPlayedEvent is published

            // Move to the next player's turn
            AdvanceToNextPlayer(game);

            // Check if all players have played a card in this round
            if (IsRoundComplete(game))
            {
                DetermineRoundWinner(game);
            }

            return true;
        }        
          /// <summary>
        /// Executes AI player turns in sequence with appropriate delays
        /// </summary>
        [Obsolete("This method is deprecated. AI turns are now handled by event-driven architecture via CardPlayedEvent -> PlayerTurnStartedEvent -> AIPlayerEventHandler. This will be removed in a future version.")]
        public async Task ProcessAITurnsAsync(GameState game, int aiPlayDelayMs)
        {
            // Continue playing for AI players until it's a human player's turn or round is complete
            var iterations = 0;

            while (iterations < TrucoConstants.AI.MaxIterations)
            {
                var activePlayer = game.Players.FirstOrDefault(p => p.IsActive);
                if (activePlayer == null) break;

                // Check if it's a human player or if round is complete
                if (activePlayer.Seat == TrucoConstants.Game.HumanPlayerSeat || IsRoundComplete(game))
                {
                    break;
                }

                // Add a delay before AI plays
                await Task.Delay(aiPlayDelayMs);

                // AI player selects and plays a card
                if (activePlayer.Hand.Count > 0)
                {
                    var cardIndex = _aiPlayerService.SelectCardToPlay(activePlayer, game);
                    if (cardIndex >= 0 && cardIndex < activePlayer.Hand.Count)
                    {
                        PlayCard(game, activePlayer.Seat, cardIndex);
                    }
                }

                iterations++;
            }
        }        /// <summary>
        /// Checks if a hand is complete and processes end-of-hand logic
        /// </summary>
        public async Task ProcessHandCompletionAsync(GameState game, int newHandDelayMs)
        {
            // Use the proper hand resolution service to check completion
            if (_handResolutionService.IsHandComplete(game))
            {
                // Determine the winning team and round information
                var winningTeam = game.TurnWinner ?? TrucoConstants.Teams.PlayerTeam;
                var roundWinners = new List<int>(); // This would need to be tracked throughout the hand
                var pointsAwarded = game.Stakes;

                // Publish hand completed event which will trigger cleanup
                await _eventPublisher.PublishAsync(new HandCompletedEvent(
                    Guid.Parse(game.Id), 
                    game.CurrentHand, 
                    winningTeam,
                    roundWinners,
                    pointsAwarded,
                    game));

                // Add delay before starting a new hand
                await Task.Delay(newHandDelayMs);

                // Determine new dealer and first player for next hand
                var currentDealerSeat = game.Players.FindIndex(p => p.IsDealer);
                var newDealerSeat = (currentDealerSeat + 1) % TrucoConstants.Game.MaxPlayers;
                var newFirstPlayerSeat = (newDealerSeat + 1) % TrucoConstants.Game.MaxPlayers;

                // Publish hand started event for the new hand
                await _eventPublisher.PublishAsync(new HandStartedEvent(
                    Guid.Parse(game.Id), 
                    game.CurrentHand + 1,
                    newDealerSeat,
                    newFirstPlayerSeat,
                    game));
            }
        }

        /// <summary>
        /// Advances the turn to the next player
        /// </summary>
        public void AdvanceToNextPlayer(GameState game)
        {
            // Find the current active player
            var currentActivePlayer = game.Players.FirstOrDefault(p => p.IsActive);
            if (currentActivePlayer == null)
            {
                // If no active player, set the first player (left of dealer) as active
                var firstPlayer = game.Players.FirstOrDefault(p => p.Seat == game.FirstPlayerSeat);
                if (firstPlayer != null)
                {
                    firstPlayer.IsActive = true;
                    game.CurrentPlayerIndex = firstPlayer.Seat;
                }
                return;
            }

            // Set the current player to inactive
            currentActivePlayer.IsActive = false;

            // Find the next player (to the left, which is next seat in clockwise order)
            var nextPlayerSeat = (currentActivePlayer.Seat + 1) % GameConfiguration.MaxPlayers;
            var nextPlayer = game.Players.FirstOrDefault(p => p.Seat == nextPlayerSeat);

            // Set the next player to active
            if (nextPlayer != null)
            {
                nextPlayer.IsActive = true;
                game.CurrentPlayerIndex = nextPlayer.Seat;
            }
        }        /// <summary>
        /// Checks if all players have played their cards for the current round
        /// </summary>
        public bool IsRoundComplete(GameState game)
        {
            // Ensure we have exactly 4 PlayedCard slots (one per player seat)
            if (game.PlayedCards.Count != TrucoConstants.Game.MaxPlayers)
            {
                return false;
            }            // Check if each player seat (0-3) has played a card in this round
            for (int seat = 0; seat < TrucoConstants.Game.MaxPlayers; seat++)
            {
                var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == seat);
                if (playedCard?.Card == null || playedCard.Card.IsFold)
                {
                    return false; // This player hasn't played yet (still fold card)
                }
            }

            return true; // All players have played
        }
        
        /// <summary>
        /// Determine the winner of the current round using proper card strength calculation
        /// </summary>
        private async void DetermineRoundWinner(GameState game)
        {
            // Use the proper hand resolution service for card strength determination
            Player? roundWinner = null;
            var strongestCardStrength = -1;            foreach (var playedCard in game.PlayedCards)
            {
                if (!playedCard.Card.IsFold)
                {
                    var player = game.Players.FirstOrDefault(p => p.Seat == playedCard.PlayerSeat);
                    if (player != null)
                    {
                        var cardStrength = _handResolutionService.GetCardStrength(playedCard.Card);
                        if (cardStrength > strongestCardStrength)
                        {
                            strongestCardStrength = cardStrength;
                            roundWinner = player;
                        }
                    }
                }
            }

            if (roundWinner != null)            {
                // Set the winner and add to log
                game.TurnWinner = roundWinner.Team;
                // ActionLog entry will be created by ActionLogEventHandler when RoundCompletedEvent is published

                // Add the points to the winner's team
                game.AddScore(roundWinner.Team, game.Stakes);// Publish round started event for the next round (which will trigger cleanup)
                await _eventPublisher.PublishAsync(new RoundStartedEvent(
                    Guid.Parse(game.Id),
                    game.CurrentRound + 1,
                    game.CurrentHand,
                    roundWinner.Seat,
                    game));

                // Check if hand is complete, otherwise prepare for next round
                if (_handResolutionService.IsHandComplete(game))
                {
                    // Hand completion will be handled by ProcessHandCompletionAsync
                    return;
                }
            }        }

        /// <summary>
        /// Starts a new hand by using event-driven cleanup
        /// </summary>
        public async void StartNewHand(GameState game)
        {
            // Publish hand started event which will trigger proper cleanup
            var currentDealerSeat = game.Players.FindIndex(p => p.IsDealer);
            var newDealerSeat = (currentDealerSeat + 1) % TrucoConstants.Game.MaxPlayers;
            var newFirstPlayerSeat = (newDealerSeat + 1) % TrucoConstants.Game.MaxPlayers;

            await _eventPublisher.PublishAsync(new HandStartedEvent(
                Guid.Parse(game.Id),
                game.CurrentHand + 1,
                newDealerSeat,
                newFirstPlayerSeat,
                game));
        }
    }
}
