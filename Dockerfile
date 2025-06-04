# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["TrucoMineiro.API/TrucoMineiro.API.csproj", "TrucoMineiro.API/"]
RUN dotnet restore "TrucoMineiro.API/TrucoMineiro.API.csproj"

# Copy the project files and build
COPY . .
WORKDIR "/src/TrucoMineiro.API"
RUN dotnet build "TrucoMineiro.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "TrucoMineiro.API.csproj" -c Release -o /app/publish

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "TrucoMineiro.API.dll"]
