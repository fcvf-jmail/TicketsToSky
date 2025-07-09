FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TicketsToSky.Parser/TicketsToSky.Parser.csproj", "."]
RUN dotnet restore "TicketsToSky.Parser.csproj"
COPY TicketsToSky.Parser .
COPY appsettings.json .
WORKDIR "/src/."
RUN dotnet build "TicketsToSky.Parser.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TicketsToSky.Parser.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src/appsettings.json .
ENTRYPOINT ["dotnet", "TicketsToSky.Parser.dll"]