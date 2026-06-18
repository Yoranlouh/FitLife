# FitLife

FitLife .NET solution — een fitness-/lesreserveringsplatform met een mobiele app, een webportaal en een gedeelde backend.

## Structuur

De solution ([`FitLife.slnx`](FitLife.slnx)) bestaat uit de volgende projecten:

### Clean Architecture-lagen (backend)
- **FitLife.Domain** – Domeinentiteiten en kernlogica
- **FitLife.Application** – Applicatielaag (use cases, services, interfaces)
- **FitLife.Infrastructure** – Data-access en externe integraties
- **FitLife.API** – ASP.NET Core Web API backend (.NET 10)

### Clients
- **FitLife.Maui** – .NET MAUI app (Android, iOS, MacCatalyst, Windows) — de primaire client voor leden en trainers
- **FitLife.BlazorWebApp** – Blazor Server webportaal (interactive server render mode)

### Gedeeld & tests
- **SharedLibrary** – Gedeelde modellen en DTO-contracten (zie [`SharedLibrary/README.md`](SharedLibrary/README.md))
- **FitLife.Tests** – Unit-/integratietests (xUnit)
- **FitLife.UITests** – Appium-gebaseerde UI-tests voor de MAUI-app

## Vereisten

- .NET 10 SDK
- Docker (voor de backend + database)
- Voor de MAUI-app: de bijbehorende workloads (`dotnet workload install maui`)

## Backend draaien met Docker

Het compose-bestand staat in de **root** van de repository ([`compose.yaml`](compose.yaml)) en start drie services:

- **api** – de Web API op poort `8080` (gebouwd uit [`FitLife.API/Dockerfile`](FitLife.API/Dockerfile))
- **db** – MySQL-database op poort `3306`
- **phpmyadmin** – databasebeheer op poort `8082`

Voer vanuit de **root** uit:

```bash
docker compose up --build
```

> **Let op:** de compose verwacht een extern image `fitlife-db:local` en een externe volume `api_mysql_data` (`external: true`). Zorg dat die bestaan vóór de eerste run, bijvoorbeeld:
> ```bash
> docker volume create api_mysql_data
> ```
> en bouw/pull het `fitlife-db:local` image. Zonder deze stappen faalt `docker compose up`.

## Tests draaien

```bash
dotnet test FitLife.Tests/FitLife.Tests.csproj
```

De UI-tests (`FitLife.UITests`) vereisen een draaiende Appium-server en de gebouwde MAUI-app.
