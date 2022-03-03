FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["CityGuessBot.fsproj", "CityGuessBot/"]
RUN dotnet restore "CityGuessBot/CityGuessBot.fsproj"
WORKDIR "/src/CityGuessBot"
COPY . .
RUN dotnet build "CityGuessBot.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CityGuessBot.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CityGuessBot.dll"]