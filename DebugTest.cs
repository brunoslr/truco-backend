using Microsoft.Extensions.Configuration;
using TrucoMineiro.API.Services;
using System.Text.Json;

// Quick debug test to understand the card visibility issue
var devModeSettings = new Dictionary<string, string?> {
    {"FeatureFlags:DevMode", "true"},
};
var devConfig = new ConfigurationBuilder()
    .AddInMemoryCollection(devModeSettings)
    .Build();

var gameService = new GameService(devConfig);
var game = gameService.CreateGame("TestPlayer");
var humanPlayer = game.Players.First(p => p.Seat == 0);

Console.WriteLine("=== BEFORE PLAY CARD ===");
Console.WriteLine($"Human player cards: {humanPlayer.Hand.Count}");
foreach (var card in humanPlayer.Hand)
{
    Console.WriteLine($"  {card.Value} of {card.Suit}");
}

foreach (var player in game.Players)
{
    Console.WriteLine($"Player {player.Seat} cards: {player.Hand.Count}");
}

// Act
var response = gameService.PlayCardEnhanced(game.GameId, humanPlayer.Id, 0, false, 0);

Console.WriteLine("\n=== AFTER PLAY CARD ===");
Console.WriteLine($"Success: {response.Success}");
Console.WriteLine($"Message: {response.Message}");
Console.WriteLine($"Hand count: {response.Hand.Count}");
Console.WriteLine($"PlayerHands count: {response.PlayerHands.Count}");

foreach (var playerHand in response.PlayerHands)
{
    Console.WriteLine($"\nPlayer Seat {playerHand.Seat}: {playerHand.Cards.Count} cards");
    foreach (var card in playerHand.Cards)
    {
        Console.WriteLine($"  Value: {card.Value}, Suit: {card.Suit}");
    }
}
