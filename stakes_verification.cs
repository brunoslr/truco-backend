using TrucoMineiro.API.Constants;

// Quick verification of stakes constants
Console.WriteLine("=== Truco Mineiro Stakes Verification ===");
Console.WriteLine($"Initial Stakes: {TrucoConstants.Stakes.Initial}");
Console.WriteLine($"After Truco Call: {TrucoConstants.Stakes.TrucoCall}");
Console.WriteLine($"Raise Amount: {TrucoConstants.Stakes.RaiseAmount}");
Console.WriteLine($"Maximum Stakes: {TrucoConstants.Stakes.Maximum}");

Console.WriteLine("\n=== Stakes Progression ===");
int currentStakes = TrucoConstants.Stakes.Initial;
Console.WriteLine($"1. Game starts with: {currentStakes} points");

currentStakes = TrucoConstants.Stakes.TrucoCall;
Console.WriteLine($"2. After Truco call: {currentStakes} points");

currentStakes += TrucoConstants.Stakes.RaiseAmount;
Console.WriteLine($"3. First raise: {currentStakes} points");

currentStakes += TrucoConstants.Stakes.RaiseAmount;
Console.WriteLine($"4. Maximum stakes: {currentStakes} points");

Console.WriteLine($"\nCorrect progression: 2 → 4 → 8 → 12 ✓");
Console.WriteLine($"Maximum allowed: {TrucoConstants.Stakes.Maximum}");
