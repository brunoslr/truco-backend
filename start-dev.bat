@echo off
echo Starting Truco Mineiro API in development mode...
cd /d %~dp0\TrucoMineiro.API
dotnet run --launch-profile "TrucoMineiro.API.Development"
