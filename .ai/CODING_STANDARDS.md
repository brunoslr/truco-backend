# Coding Standards for Truco Mineiro

## Constants and Enums

### **Use Constants Over Magic Strings**
❌ **Avoid:**
```csharp
public override string CommandType => "StartGame";
if (game.GameStatus == "active")
```

✅ **Prefer:**
```csharp
public override string CommandType => TrucoConstants.Commands.StartGame;
if (game.Status == GameStatus.Active)
```

### **Constant Organization**
- **Single Source**: Consolidate related constants into existing files (e.g., TrucoConstants.cs)
- **Logical Grouping**: Use nested static classes for organization
- **Clear Naming**: Use descriptive names that indicate purpose

```csharp
public static class TrucoConstants
{
    public static class Commands
    {
        public const string StartGame = "StartGame";
        public const string PlayCard = "PlayCard";
    }
    
    public static class Actions
    {
        public const string PlayCard = "play-card";
        public const string CallTruco = "call-truco-or-raise";
    }
}
```

### **Enum Usage**
- **Prefer Enums**: Use enums instead of string constants for state values
- **Backward Compatibility**: Provide string properties when needed for API compatibility
- **Consistent Naming**: Use PascalCase for enum values

```csharp
public enum GameStatus
{
    Waiting,
    Active, 
    Completed,
    Paused
}

// For API compatibility
public string GameStatus => Status.ToString().ToLower();
```

## Code Structure and Formatting

### **Method Spacing**
Always include blank lines between methods for better readability:

❌ **Avoid:**
```csharp
public void Method1()
{
    // code
}
public void Method2()
{
    // code
}
```

✅ **Prefer:**
```csharp
public void Method1()
{
    // code
}

public void Method2()
{
    // code
}
```

### **Class Organization**
1. Constants and static fields
2. Private fields
3. Public properties
4. Constructors
5. Public methods
6. Private methods

Each section separated by blank lines.

### **Using Statements**
- Order: System namespaces first, then project namespaces
- Remove unused using statements
- Group related usings together

```csharp
using System;
using System.Collections.Generic;

using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Models;
```

## Error Handling and Validation

### **Consistent Error Messages**
Use constants for common error messages:

```csharp
public static class ErrorMessages
{
    public const string GameNotFound = "Game not found";
    public const string PlayerNotFound = "Player not found";
    public const string InvalidGameState = "Game is not in a valid state for this action";
}
```

### **Validation Patterns**
- Early validation and return
- Use meaningful error messages
- Consistent failure response format

```csharp
public CommandResult ValidateCommand(IGameCommand command)
{
    if (command == null)
        return CommandResult.Failure(ErrorMessages.InvalidCommand);
        
    if (game.Status != GameStatus.Active)
        return CommandResult.Failure(ErrorMessages.InvalidGameState);
        
    return CommandResult.Success();
}
```

## Naming Conventions

### **Methods and Variables**
- **Descriptive Names**: Prefer clarity over brevity
- **Action Methods**: Use verbs (ProcessCommand, ValidatePlayer)
- **Boolean Properties**: Use "Is", "Has", "Can" prefixes
- **Event Names**: Use past tense (TrucoCalledEvent, PlayerMovedEvent)

### **Constants and Enums**
- **Constants**: UPPER_CASE or PascalCase depending on context
- **Enum Values**: PascalCase
- **Class Names**: PascalCase with descriptive suffixes (Command, Event, Service)

## Documentation Standards

### **XML Documentation**
All public members must have XML documentation:

```csharp
/// <summary>
/// Processes a game command and updates the game state accordingly
/// </summary>
/// <param name="command">The command to process</param>
/// <returns>Result indicating success or failure with details</returns>
public async Task<CommandResult> ProcessCommandAsync(IGameCommand command)
```

### **Inline Comments**
- Explain **why**, not **what**
- Use for complex business logic
- Remove TODO/HACK comments before committing

## Performance and Best Practices

### **Async/Await**
- Use async/await consistently
- Don't mix async and sync patterns
- Use ConfigureAwait(false) in library code

### **LINQ Usage**
- Prefer readable LINQ over complex loops
- Be mindful of performance in hot paths
- Use appropriate collection types

### **Memory Management**
- Dispose of resources properly
- Use using statements for IDisposable
- Be careful with string concatenation in loops

## Testing Standards

### **Test Naming**
Pattern: `MethodName_Scenario_ExpectedResult`

```csharp
[Fact]
public async Task ProcessCommandAsync_ValidTrucoCommand_ShouldPublishTrucoCalledEvent()
```

### **Test Structure**
Follow AAA pattern (Arrange, Act, Assert):

```csharp
[Fact]
public async Task Test_Method()
{
    // Arrange
    var expected = "result";
    
    // Act
    var actual = await method.Execute();
    
    // Assert
    Assert.Equal(expected, actual);
}
```

### **Mock Usage**
- Verify important interactions
- Use meaningful mock setups
- Clean up mocks between tests
