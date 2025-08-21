FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM node:20-bookworm AS playwright-install
WORKDIR /playwright

RUN npm install -g playwright@1.54.0 && \
    npx playwright install-deps && \
    npx playwright install --with-deps chromium

RUN mkdir -p /playwright-cache && cp -r /root/.cache/ms-playwright /playwright-cache

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

# Устанавливаем Xvfb и зависимости для графической среды
RUN apt-get update && apt-get install -y \
    xvfb \
    libxfixes3 \
    libx11-6 \
    libx11-xcb1 \
    libxcomposite1 \
    libxcursor1 \
    libxdamage1 \
    libxi6 \
    libxtst6 \
    libnss3 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libdrm2 \
    libgbm1 \
    libasound2 \
    libpango-1.0-0 \
    libcairo2 \
    libcups2 \
    libxrandr2 \
    libegl1 \
    libgles2 \
    libgtk-3-0 \
    libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
COPY --from=build /src/appsettings.json .
COPY --from=playwright-install /playwright-cache /root/.cache/ms-playwright

# Запускаем приложение с Xvfb для эмуляции графической среды
ENTRYPOINT ["xvfb-run", "--auto-servernum", "--server-args=-screen 0 1920x1080x24", "dotnet", "TicketsToSky.Parser.dll"]