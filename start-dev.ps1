# Run the Truco Mineiro backend API in development mode
Write-Host "Starting Truco Mineiro API in development mode..." -ForegroundColor Green

# Navigate to the API project directory
Set-Location -Path "$PSScriptRoot\TrucoMineiro.API"

# Run the API with the development profile
dotnet run --launch-profile "TrucoMineiro.API.Development"
