FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5148

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TicketsToSky.Api/TicketsToSky.Api.csproj", "."]
RUN dotnet restore "./TicketsToSky.Api.csproj"
COPY TicketsToSky.Api .
COPY appsettings.json .
WORKDIR "/src/."
RUN dotnet build "TicketsToSky.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TicketsToSky.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

RUN apt-get update && apt-get install -y curl

COPY --from=publish /app/publish .
COPY --from=build /src/appsettings.json .
ENTRYPOINT ["dotnet", "TicketsToSky.Api.dll"]