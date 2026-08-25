# shipment-platform

API de transportadora em **.NET 10** com **Clean Architecture**, pensada como demo para vaga pleno backend.

## Arquitetura

```text
src/
  ShipmentPlatform.Api/             → HTTP (controllers)
  ShipmentPlatform.Application/     → use cases, DTOs, validators, ports
  ShipmentPlatform.Domain/          → entidades e regras de negócio
  ShipmentPlatform.Infrastructure/  → EF Core, Postgres, messaging
tests/
  ShipmentPlatform.UnitTests/
  ShipmentPlatform.IntegrationTests/
```

Dependências apontam para dentro: Api → Application/Infrastructure → Domain.

## Domínio

`Shipment` (frete) com status:

`Created → PickedUp → InTransit → Delivered` (ou `Cancelled`)

Regras ficam na entidade (não no controller).

## Pré-requisitos

- .NET 10 SDK
- Docker (Postgres + RabbitMQ)

## Subir infraestrutura

```bash
# Se a porta 5434 já estiver em uso por outro Postgres, pare o container antigo antes.
docker compose up -d postgres rabbitmq
```

- Postgres: `localhost:5434` (user/pass/db: `shipment` / `shipment` / `shipment_platform`)
- RabbitMQ management: http://localhost:15672 (`shipment` / `shipment`)

Mensageria: a Application publica `ShipmentCreatedEvent` via `IEventPublisher`.  
Hoje a implementação é `LoggingEventPublisher` (log). Trocar por RabbitMQ/MassTransit não exige mudar Domain/Application.

## Rodar a API

```bash
dotnet run --project src/ShipmentPlatform.Api --launch-profile http
```

Base URL: http://localhost:5208

### Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/shipments` | listar |
| GET | `/api/shipments/{id}` | buscar por id |
| GET | `/api/shipments/tracking/{code}` | buscar por tracking |
| POST | `/api/shipments` | criar frete |
| PATCH | `/api/shipments/{id}/status` | atualizar status |

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

- **Unit**: regras de `Shipment` + `ShipmentService` (Moq)
- **Integration**: API real + Postgres via **Testcontainers**

## Docker full stack (opcional)

```bash
docker compose --profile full up --build
```

API em http://localhost:8080

## Próximos passos (roadmap da demo)

1. Publisher RabbitMQ real (MassTransit)
2. Outbox pattern
3. Serilog + correlation id
4. Autenticação JWT
