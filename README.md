# shipment-platform

API de transportadora em **.NET 10** com **Clean Architecture**, mensageria transacional, cache e observabilidade.

## Arquitetura

```text
src/
  ShipmentPlatform.Api/             → HTTP, JWT, OpenTelemetry/Prometheus
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

Dependências apontam para dentro: Api → Application/Infrastructure → Domain.

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
| Mensageria | MassTransit + RabbitMQ + **EF Outbox** |
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

## Rodar a API

```bash
dotnet run --project src/ShipmentPlatform.Api --launch-profile http
```

Base URL: http://localhost:5208  
Métricas: http://localhost:5208/metrics

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

1. `CreateAsync` adiciona o frete e publica `ShipmentCreatedEvent` via MassTransit.
2. O **EF Outbox** grava a mensagem na mesma transação do Postgres.
3. O bus entrega no RabbitMQ; `ShipmentCreatedConsumer` consome e registra nos logs.

## Migrations

Aplicadas automaticamente no startup (`Database.Migrate()`).

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

- **Unit**: regras de `Shipment` + `ShipmentService` (Moq + cache em memória)
- **Integration**: API real + Postgres (Testcontainers) + MassTransit in-memory + JWT

## Docker full stack

```bash
docker compose --profile full up --build
```

API em http://localhost:8080 (Prometheus/Grafana sobem com o profile `full`).
