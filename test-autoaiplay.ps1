# PowerShell script to test AutoAiPlay functionality
Write-Host "Testing AutoAiPlay functionality..." -ForegroundColor Green

# Build the solution first
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build TrucoMineiro.sln

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Run all tests
Write-Host "Running all tests..." -ForegroundColor Yellow
dotnet test TrucoMineiro.Tests/TrucoMineiro.Tests.csproj --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit 1
}

Write-Host "All tests passed! AutoAiPlay feature is working correctly." -ForegroundColor Green
Write-Host ""
Write-Host "Summary of changes made:" -ForegroundColor Cyan
Write-Host "1. Added AutoAiPlay flag to appsettings.json and appsettings.Development.json" -ForegroundColor White
Write-Host "2. Updated GameService to read AutoAiPlay flag and use it to control AI behavior" -ForegroundColor White
Write-Host "3. Modified PlayCardEnhanced method to only call ProcessAITurnsAsync when AutoAiPlay is enabled" -ForegroundColor White
Write-Host "4. Updated controller documentation to reflect AutoAiPlay controls AI behavior" -ForegroundColor White
Write-Host "5. Updated all tests to use AutoAiPlay flag instead of DevMode for AI behavior expectations" -ForegroundColor White
Write-Host ""
Write-Host "Configuration flags:" -ForegroundColor Cyan
Write-Host "- DevMode: Controls card visibility (true = show all cards, false = hide AI cards)" -ForegroundColor White
Write-Host "- AutoAiPlay: Controls AI automatic play (true = AI plays automatically, false = AI waits for manual trigger)" -ForegroundColor White
