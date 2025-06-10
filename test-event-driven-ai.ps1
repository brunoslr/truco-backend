# Test script to verify event-driven AI functionality
$baseUrl = "http://localhost:5084"

Write-Host "Testing Event-Driven AI Functionality..." -ForegroundColor Green

# Start a new game
Write-Host "`n1. Starting a new game..." -ForegroundColor Yellow
$startGameRequest = @{
    PlayerName = "Human Player"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "$baseUrl/api/truco/start" -Method POST -Body $startGameRequest -ContentType "application/json"
$gameId = $response.gameId

Write-Host "Game ID: $gameId" -ForegroundColor Cyan
Write-Host "Current player seat: $($response.currentPlayerSeat)" -ForegroundColor Cyan

# Get initial game state
Write-Host "`n2. Getting initial game state..." -ForegroundColor Yellow
$gameState = Invoke-RestMethod -Uri "$baseUrl/api/truco/$gameId" -Method GET

Write-Host "Players:" -ForegroundColor Cyan
foreach ($player in $gameState.players) {
    Write-Host "  - Seat $($player.seat): $($player.name) (AI: $($player.isAI), Active: $($player.isActive))" -ForegroundColor White
}

# Play a card as human player if it's their turn
if ($gameState.currentPlayerIndex -eq 0) {
    Write-Host "`n3. Human player playing a card..." -ForegroundColor Yellow
    $humanCard = $gameState.players[0].hand[0]
    $playCardRequest = @{
        GameId = $gameId
        PlayerSeat = 0
        CardSuit = $humanCard.suit
        CardValue = $humanCard.value
    } | ConvertTo-Json

    $playResponse = Invoke-RestMethod -Uri "$baseUrl/api/truco/play-card" -Method POST -Body $playCardRequest -ContentType "application/json"
    Write-Host "Human played: $($humanCard.value) of $($humanCard.suit)" -ForegroundColor Cyan
    
    # Wait a moment for event processing
    Start-Sleep -Seconds 2
}

# Check game state after AI processing
Write-Host "`n4. Checking game state after event-driven AI processing..." -ForegroundColor Yellow
for ($i = 0; $i -lt 5; $i++) {
    Start-Sleep -Seconds 1
    $finalState = Invoke-RestMethod -Uri "$baseUrl/api/truco/$gameId" -Method GET
    
    Write-Host "`nRound $($finalState.currentRound), Current player: Seat $($finalState.currentPlayerIndex)" -ForegroundColor Cyan
    
    # Show played cards
    $playedCards = $finalState.playedCards | Where-Object { $_.card -and -not $_.card.isFold }
    if ($playedCards.Count -gt 0) {
        Write-Host "Played cards this round:" -ForegroundColor Cyan
        foreach ($pc in $playedCards) {
            $playerName = ($finalState.players | Where-Object { $_.seat -eq $pc.playerSeat }).name
            Write-Host "  - $playerName (Seat $($pc.playerSeat)): $($pc.card.value) of $($pc.card.suit)" -ForegroundColor White
        }
    }
    
    # Show recent action log entries
    $recentActions = $finalState.actionLog | Select-Object -Last 3
    if ($recentActions.Count -gt 0) {
        Write-Host "Recent actions:" -ForegroundColor Cyan
        foreach ($action in $recentActions) {
            Write-Host "  - Seat $($action.playerSeat): $($action.action)" -ForegroundColor White
        }
    }
    
    # Check if round is complete or if it's human's turn again
    if ($finalState.currentPlayerIndex -eq 0) {
        Write-Host "`nIt's human player's turn again." -ForegroundColor Green
        break
    }
    
    if ($playedCards.Count -eq 4) {
        Write-Host "`nRound complete!" -ForegroundColor Green
        break
    }
}

Write-Host "`n✅ Event-driven AI test completed!" -ForegroundColor Green
Write-Host "Check the action log above to verify AI players are making moves automatically." -ForegroundColor Yellow
