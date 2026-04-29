# Kanelson

**[Português 🇧🇷](README.pt-BR.md)**

[![SonarCloud](https://sonarcloud.io/images/project_badges/sonarcloud-white.svg)](https://sonarcloud.io/summary/overall?id=yanpitangui_kanelson)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=yanpitangui_kanelson&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=yanpitangui_kanelson)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=yanpitangui_kanelson&metric=coverage)](https://sonarcloud.io/summary/new_code?id=yanpitangui_kanelson)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=yanpitangui_kanelson&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=yanpitangui_kanelson)

A free, open-source real-time quiz platform inspired by Kahoot. Hosts run live sessions from a browser; players join instantly — no app required.

## Features

- **Multiple question types** — True/False, single-answer Quiz, and Multi-correct (select all that apply)
- **Real-time gameplay** — answers, scores, and round transitions pushed to all clients simultaneously over SignalR
- **Vote distribution** — bar chart shown between rounds so everyone sees how others answered
- **Latency-aware scoring** — faster correct answers earn more points; the scoring formula accounts for network latency
- **Time extension** — host can add extra seconds mid-round; scoring adjusts fairly for all players
- **QR code join** — host screen shows a scannable QR code so players can jump straight into the room
- **Localization** — English and Brazilian Portuguese (pt-BR)

## Tech Stack

| Layer | Technology |
|---|---|
| Language | C# / .NET 10 |
| Frontend | Blazor Server + SignalR |
| UI components | MudBlazor |
| Actor model | Akka.NET (Cluster Sharding, Persistence) |
| Persistence | PostgreSQL via Akka.Persistence.Sql |
| Auth | GitHub OAuth |
| Orchestration | .NET Aspire |

## Running Locally

### 1. GitHub OAuth

The app authenticates users via GitHub. Create an OAuth app at [github.com/settings/developers](https://github.com/settings/developers) with the callback URL `https://localhost:<port>/signin-github`, then store the credentials as user secrets:

```bash
dotnet user-secrets set "GithubAuth:ClientSecret" "<your-secret>" --project "./Kanelson/"
dotnet user-secrets set "GithubAuth:ClientId" "<your-client-id>" --project "./Kanelson/"
```

### 2. Start with Docker Compose

The easiest way to run everything locally:

```bash
docker-compose up
```

This starts PostgreSQL and the application together.

### 3. Run without Docker

You need a running PostgreSQL instance, then:

```bash
dotnet run --project Kanelson/Kanelson.csproj
```

Or use the Aspire AppHost for a full local orchestration experience:

```bash
dotnet run --project Kanelson.AppHost/Kanelson.AppHost.csproj
```

## Development

```bash
# Build
dotnet build Kanelson.sln

# Run all tests
dotnet test Kanelson.sln

# Run a specific test
dotnet test Kanelson.Tests/Kanelson.Tests.csproj --filter "ClassName.MethodName"
```

## Architecture

Kanelson uses the **actor model** via [Akka.NET](https://getakka.net/) with event sourcing. All game state lives in persistent actors distributed across the cluster via Akka Cluster Sharding.

```
RoomIndex (singleton)
└── Room (sharded per room ID)        ← core game state machine
    └── SignalrActor (per room)       ← bridges actor events to browser clients

RoomTemplateIndex (singleton)
└── RoomTemplate (sharded)

User (sharded per user ID)
UserQuestions (sharded per user ID)
```

State changes are persisted as events to PostgreSQL. Snapshots are taken every N events and on graceful shutdown to speed up recovery.

## Contributing

Pull requests are welcome. Please open an issue first to discuss significant changes.

## License

[MIT](LICENSE)
