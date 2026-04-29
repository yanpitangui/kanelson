# Kanelson

**[English 🇺🇸](README.md)**

[![SonarCloud](https://sonarcloud.io/images/project_badges/sonarcloud-white.svg)](https://sonarcloud.io/summary/overall?id=yanpitangui_kanelson)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=yanpitangui_kanelson&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=yanpitangui_kanelson)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=yanpitangui_kanelson&metric=coverage)](https://sonarcloud.io/summary/new_code?id=yanpitangui_kanelson)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=yanpitangui_kanelson&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=yanpitangui_kanelson)

Uma plataforma de quiz ao vivo, gratuita e open source, inspirada no Kahoot. O host conduz sessões pelo navegador; jogadores entram instantaneamente — sem precisar instalar nada.

## Funcionalidades

- **Múltiplos tipos de pergunta** — Verdadeiro/Falso, Quiz (resposta única) e Multi-correto (selecionar todas as corretas)
- **Jogo em tempo real** — respostas, pontuações e transições de rodada enviadas a todos os clientes simultaneamente via SignalR
- **Distribuição de votos** — gráfico de barras exibido entre as rodadas para que todos vejam como os outros responderam
- **Pontuação com latência** — respostas corretas mais rápidas valem mais pontos; a fórmula leva em conta a latência de rede
- **Extensão de tempo** — o host pode adicionar segundos durante a rodada; a pontuação se ajusta de forma justa para todos
- **Entrada por QR code** — a tela do host exibe um QR code escaneável para os jogadores entrarem diretamente na sala
- **Localização** — inglês e português brasileiro (pt-BR)

## Stack

| Camada | Tecnologia |
|---|---|
| Linguagem | C# / .NET 10 |
| Frontend | Blazor Server + SignalR |
| Componentes de UI | MudBlazor |
| Actor model | Akka.NET (Cluster Sharding, Persistence) |
| Persistência | PostgreSQL via Akka.Persistence.Sql |
| Autenticação | GitHub OAuth |
| Orquestração | .NET Aspire |

## Rodando Localmente

### 1. GitHub OAuth

A aplicação autentica usuários via GitHub. Crie um OAuth App em [github.com/settings/developers](https://github.com/settings/developers) com a callback URL `https://localhost:<porta>/signin-github`, e armazene as credenciais como user secrets:

```bash
dotnet user-secrets set "GithubAuth:ClientSecret" "<seu-secret>" --project "./Kanelson/"
dotnet user-secrets set "GithubAuth:ClientId" "<seu-client-id>" --project "./Kanelson/"
```

### 2. Subir com Docker Compose

A forma mais simples de rodar tudo localmente:

```bash
docker-compose up
```

Isso inicia o PostgreSQL e a aplicação juntos.

### 3. Rodar sem Docker

É necessário ter uma instância do PostgreSQL rodando, depois:

```bash
dotnet run --project Kanelson/Kanelson.csproj
```

Ou use o Aspire AppHost para uma orquestração local completa:

```bash
dotnet run --project Kanelson.AppHost/Kanelson.AppHost.csproj
```

## Desenvolvimento

```bash
# Build
dotnet build Kanelson.sln

# Rodar todos os testes
dotnet test Kanelson.sln

# Rodar um teste específico
dotnet test Kanelson.Tests/Kanelson.Tests.csproj --filter "ClassName.MethodName"
```

## Arquitetura

O Kanelson utiliza o **actor model** via [Akka.NET](https://getakka.net/) com event sourcing. Todo o estado do jogo vive em actors persistentes distribuídos pelo cluster via Akka Cluster Sharding.

```
RoomIndex (singleton)
└── Room (sharded por ID da sala)     ← máquina de estados principal do jogo
    └── SignalrActor (por sala)       ← ponte entre os actors e os clientes no browser

RoomTemplateIndex (singleton)
└── RoomTemplate (sharded)

User (sharded por ID do usuário)
UserQuestions (sharded por ID do usuário)
```

Mudanças de estado são persistidas como eventos no PostgreSQL. Snapshots são tirados a cada N eventos e no desligamento gracioso para acelerar a recuperação.

## Contribuindo

Pull requests são bem-vindos. Para mudanças significativas, abra uma issue primeiro para discutir.

## Licença

[MIT](LICENSE)
