FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TicketsToSky.TelegramSender/TicketsToSky.TelegramSender.csproj", "."]
RUN dotnet restore "TicketsToSky.TelegramSender.csproj"
COPY TicketsToSky.TelegramSender .
COPY appsettings.json .
WORKDIR "/src/."
RUN dotnet build "TicketsToSky.TelegramSender.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TicketsToSky.TelegramSender.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src/appsettings.json .
ENTRYPOINT ["dotnet", "TicketsToSky.TelegramSender.dll"]