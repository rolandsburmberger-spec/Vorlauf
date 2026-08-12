# syntax=docker/dockerfile:1
#
# Container für den öffentlichen Demo-Betrieb (Render, Free-Tier).
# Die Ablage ist in-memory — es gibt bewusst kein Volume und keine
# Datenbank: Jeder Neustart stellt den Seed-Stand wieder her, was auf
# einer Instanz mit Spin-down genau dem gewünschten Demo-Reset entspricht.

# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Projektdateien zuerst kopieren: Solange sich keine Abhängigkeit ändert,
# bleibt der restore-Layer im Cache und der Build auf Render bleibt kurz.
COPY src/Vorlauf.Domain/Vorlauf.Domain.csproj                  src/Vorlauf.Domain/
COPY src/Vorlauf.Infrastructure/Vorlauf.Infrastructure.csproj  src/Vorlauf.Infrastructure/
COPY src/Vorlauf.Web/Vorlauf.Web.csproj                        src/Vorlauf.Web/
RUN dotnet restore src/Vorlauf.Web/Vorlauf.Web.csproj

COPY src/ src/
RUN dotnet publish src/Vorlauf.Web/Vorlauf.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---------- Laufzeit ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# QuestPDF rendert über SkiaSharp und benötigt fontconfig. Ohne dieses
# Paket scheitert die erste PDF-Erzeugung zur Laufzeit mit einem Fehler
# zu libSkiaSharp — im Image ist es nicht enthalten.
RUN apt-get update \
 && apt-get install --yes --no-install-recommends libfontconfig1 \
 && rm --recursive --force /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
# Render gibt den Port zur Laufzeit über PORT vor; Program.cs liest ihn aus.
# Der Vorgabewert dient nur dem lokalen "docker run".
ENV PORT=10000
EXPOSE 10000

# Nicht als root laufen (APP_UID ist im Microsoft-Image gesetzt).
USER $APP_UID

ENTRYPOINT ["dotnet", "Vorlauf.Web.dll"]
