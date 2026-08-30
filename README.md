# shipment-platform

API de transportadora em **.NET 10** com **Clean Architecture**, mensageria transacional, cache e observabilidade.

HTTP, poll da outbox e consumers rodam em **processos separados** (mesmo Postgres e RabbitMQ).

## Arquitetura

```text
src/
  ShipmentPlatform.Api/             → HTTP, JWT, Redis, migrate, OpenTelemetry/Prometheus
  ShipmentPlatform.OutboxWorker/    → poll da outbox (SKIP LOCKED) + publish MassTransit
  ShipmentPlatform.ConsumerWorker/  → consumers RabbitMQ + inbox + timeline
  ShipmentPlatform.Application/     → use cases, DTOs, validators, ports
  ShipmentPlatform.Domain/          → entidades e regras de negócio
  ShipmentPlatform.Infrastructure/  → EF Core, Postgres, MassTransit, Redis, JWT
tests/
  ShipmentPlatform.UnitTests/
  ShipmentPlatform.IntegrationTests/
observability/
  prometheus/
  grafana/
```

```text
Cliente HTTP
    │
    ▼
┌─────────┐  INSERT shipment + outbox_events (mesma transação)
│   API   │  Redis, JWT, Database.Migrate()
└─────────┘
    │
    │  Postgres
    ▼
┌───────────────┐  FOR UPDATE SKIP LOCKED → IPublishEndpoint
│ Outbox worker │  MassTransit só publica
└───────────────┘
    │
    │  RabbitMQ
    ▼
┌──────────────────┐
│ Consumer worker  │  competing consumers + InboxGuard + timeline
└──────────────────┘
```

Dependências apontam para dentro: hosts → Application/Infrastructure → Domain.

## Domínio

`Shipment` (frete) com status:

`Created → PickedUp → InTransit → Delivered` (ou `Cancelled`)

Regras ficam na entidade (não no controller).

## Stack

| Área | Tecnologia |
|------|------------|
| API | ASP.NET Core + FluentValidation |
| Auth | JWT Bearer (`POST /api/auth/login`) |
| Persistência | EF Core + PostgreSQL + migrations |
| Mensageria | MassTransit + RabbitMQ + outbox transacional próprio |
| Workers | Outbox poller e consumers em hosts separados |
| Cache | Redis (`IDistributedCache`) |
| Observabilidade | OpenTelemetry → Prometheus + Grafana |
| Testes | xUnit, Moq, Testcontainers |
| Infra | Docker Compose |

## Pré-requisitos

- .NET 10 SDK
- Docker

## Subir infraestrutura

```bash
docker compose up -d postgres rabbitmq redis
```

Opcional (métricas):

```bash
docker compose --profile observability up -d
```

| Serviço | URL / porta |
|---------|-------------|
| Postgres | `localhost:5434` (`shipment` / `shipment` / `shipment_platform`) |
| RabbitMQ | http://localhost:15672 (`shipment` / `shipment`) |
| Redis | `localhost:6380` |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 (`admin` / `admin`) |

## Rodar local (três processos)

```bash
dotnet run --project src/ShipmentPlatform.Api --launch-profile http
dotnet run --project src/ShipmentPlatform.OutboxWorker
dotnet run --project src/ShipmentPlatform.ConsumerWorker
```

Base URL: http://localhost:5208  
Métricas da API: http://localhost:5208/metrics  
Métricas do outbox worker: http://localhost:9464/metrics  
Métricas do consumer worker: http://localhost:9465/metrics  

Se só a API estiver no ar, o frete grava e o evento fica pendente em `outbox_events` até o outbox worker subir.

### Autenticação

```bash
curl -s -X POST http://localhost:5208/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"Admin123!"}'
```

Use o `accessToken` no header `Authorization: Bearer ...`.

Credenciais demo: `admin` / `Admin123!` (configuráveis em `appsettings.json`).

### Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| POST | `/api/auth/login` | público | obter JWT |
| GET | `/api/shipments` | JWT | listar |
| GET | `/api/shipments/{id}` | JWT | buscar por id |
| GET | `/api/shipments/tracking/{code}` | público | tracking |
| GET | `/api/shipments/{id}/timeline` | JWT | histórico projetado pelos consumers |
| GET | `/api/shipments/tracking/{code}/timeline` | público | mesmo histórico, via tracking |
| POST | `/api/shipments` | JWT | criar frete |
| PATCH | `/api/shipments/{id}/status` | JWT | atualizar status |

Exemplo de body (POST):

```json
{
  "senderName": "Indústria Alfa",
  "recipientName": "Loja Beta",
  "originCity": "Curitiba",
  "destinationCity": "Florianópolis",
  "weightKg": 42.5
}
```

### Fluxo de eventos (Outbox)

1. `CreateAsync` adiciona o frete e grava `ShipmentCreatedEvent` na tabela `outbox_events` **na mesma transação** do Postgres.
2. O **Outbox worker** reclama um lote com `FOR UPDATE SKIP LOCKED`, publica no bus (MassTransit) com `MessageId` = id da outbox e só então marca `ProcessedAtUtc`. Várias réplicas do worker competem sem duplicar linha.
3. Falha transiente incrementa `AttemptCount` e agenda `NextAttemptAtUtc` (backoff exponencial). Depois de 5 tentativas — ou tipo/JSON inválido — o evento fica com `PoisonedAtUtc` para inspeção e **não** é marcado como processado.
4. O **Consumer worker** grava `(MessageId, ConsumerName)` em `inbox_messages` **na mesma transação** em que projeta a linha em `shipment_timeline`. Replay at-least-once não duplica o histórico. Consulte `GET /api/shipments/{id}/timeline` (eventual consistency: a API responde 201 antes do consumer rodar).

## Migrations

Aplicadas automaticamente no startup da **API** (`Database.Migrate()`). Os workers não migram.

Para gerar uma nova:

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/ShipmentPlatform.Infrastructure \
  --startup-project src/ShipmentPlatform.Api \
  --output-dir Persistence/Migrations
```

## Testes

```bash
dotnet test
```

- **Unit**: regras de `Shipment` + `ShipmentService` (Moq + cache em memória) + retry/tipo da outbox
- **Integration**: API real + Postgres (Testcontainers) + MassTransit in-memory + JWT + processor da outbox (processed vs poison) + timeline projetada pelos consumers no mesmo processo de teste

## Docker full stack

```bash
docker compose --profile full up --build
```

API em http://localhost:8080 (Prometheus/Grafana sobem com o profile `full`).

Para duas réplicas de outbox e consumers (SKIP LOCKED + competing consumers):

```bash
docker compose --profile full up --build --scale outbox=2 --scale consumers=2
```
