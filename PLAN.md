# Playground 3PL + DDIA

Documento de evolução deste repositório. **Não implementar agora** — voltar aqui fatia a fatia enquanto o livro é lido.

## Decisão: evoluir este projeto, não criar um repo novo

**Usar o `shipment-platform` existente.** Repo novo só faria sentido se a infra atual estivesse errada ou acoplada demais. Não está.

Já existe aqui o que o DDIA chama de fundamentos de sistemas confiáveis:

| Peça | Onde está | Capítulo DDIA |
|------|-----------|---------------|
| Transação + outbox | API grava `Shipment` e `outbox_events` juntos | 7, 9 |
| Worker de publicação | `OutboxWorker` + `SKIP LOCKED` | 1, 8 |
| Consumers + inbox | `ConsumerWorker` + `InboxGuard` | 8, 9 |
| Read model derivado | `shipment_timeline` | 11, 12 |
| Cache | Redis | 3 |
| Observabilidade | OpenTelemetry / Prometheus / Grafana | 1 |
| Testes de contrato | Testcontainers (Postgres) | 1 |

Criar outro repo seria reimplementar outbox, inbox, workers, Docker e testes só para ter o nome “3PL”. O custo é alto e o aprendizado de dados (o livro) atrasaria.

O que falta não é infra: é **domínio**. Hoje o agregado raiz é `Shipment` (TMS). Um operador tipo Multilog opera **carga** atravessando recinto, CD e transporte. Evoluir significa fazer o frete virar um contexto, não o sistema inteiro.

Quando um repo novo *faria* sentido (não é o caso):

- Quisesse um nome/solution limpa (`LogisticsOs`) e aceitasse copiar a infra.
- A solution atual tivesse um emaranhado impossível de fatiar — não tem.

Não renomear a solution agora (`ShipmentPlatform` → outro nome). Namespaces internos (`Kernel`, `Warehouse`, `Transport`) bastam.

## Por que este domínio (Multilog + DDIA)

A Multilog é 3PL: recintos alfandegados, CDs, transporte, e um hub de visibilidade (BIM / Genius / Torre de Controle) que cola WMS + TMS + YMS + ERP.

O playground **não** clona o BIM. É um mini operador onde a **mesma carga** atravessa contextos, com consistência eventual e dados derivados. Isso treina o livro e a entrevista (vocabulário da empresa, C#/.NET, mensageria, workers, containers).

## Espinha dorsal: a carga, não o frete

Hoje: `Shipment` com `Created → PickedUp → InTransit → Delivered` (ou `Cancelled`), tracking em `SP…`.

Alvo: `Cargo` (unidade de carga: volume, lote, container) é a identidade estável. `Shipment` passa a ser um **trecho** da jornada.

```text
Porto/cliente → Recinto → Armazém (WMS) → Transporte (TMS) → Entrega
                     ↘         ↘              ↘
                       Torre de controle (só projeção)
```

Bounded contexts (modular monolith, um processo HTTP + workers atuais):

- **Kernel** — `Customer`, `Site` (CD ou recinto), `Cargo` (tracking único).
- **Warehouse** — `Location`, `InventoryBalance`, `Receive` / `Dispatch`.
- **Transport** — já existe (`Shipment`). Depois: veículo, placa, motorista.
- **Customs** — entreposto, DTA, nacionalização fracionada (mock da Receita). Diferencial Multilog; não é a primeira fatia.
- **Control tower** — só dados derivados. Já começou em `shipment_timeline`.

Evitar: microserviços por contexto; um CRUD gigante de “pedido”.

## Arquitetura alvo (sem quebrar hosts)

Manter `Api` + `OutboxWorker` + `ConsumerWorker`. Mesmo Postgres, RabbitMQ, Redis.

```text
src/ShipmentPlatform.Domain/
  Kernel/
  Transport/          ← mover Shipment para cá quando couber
  Warehouse/
  Customs/
```

Postgres: schemas `transport`, `warehouse`, `customs`, `tower` (particionamento lógico barato — cap. 6 light). Outbox/inbox continuam compartilhados no início.

Eventos novos reutilizam o outbox atual (mesmo padrão de `ShipmentCreatedEvent` / `ShipmentStatusChangedEvent`):

- `CargoReceived`
- `StockMoved`
- `CargoDispatched`

Consumers da torre só **append** na timeline (ou num read model `cargo_timeline` se o tracking migrar de shipment → cargo).

## Mapa DDIA → fatias

Uma fatia por capítulo (ou grupo), sempre com caso de uso na API.

| Cap. | Exercício | Fatia neste repo |
|------|-----------|------------------|
| 1 Reliability | retry, poison, workers | estender outbox/inbox a eventos de estoque |
| 2 Data models | relacional + JSONB + hierarquia | `Location` (corredor/rua/nível); depois `CustomsDocument` JSONB |
| 3 Storage | índices, padrão de escrita | estoque por `(site, sku, lot)` |
| 4 Encoding | evolução de schema de evento | versionar JSON agora; Avro depois se o cap. pedir |
| 5 Replication | réplica de leitura | torre lendo replica; lag visível |
| 6 Partitioning | chave `site_id` | schemas + partition key nas tabelas quentes |
| 7 Transactions | isolamento, lost update | dois dispatches no mesmo saldo |
| 8 Unreliable | timeouts, relógio, duplicata | já há poison/retry; simular consumer lento |
| 9 Consistency | sem 2PC | reserva no WMS + despacho no TMS via outbox |
| 10 Batch | derivados offline | snapshot de inventário / fatura noturna |
| 11 Streams | log como fonte | GPS depois; Kafka só para contrastar com Rabbit |
| 12 Derived data | stream + batch | torre = projeção contínua + reconciliação batch |

## Primeira entrega (quando for implementar)

Ordem, e só isso:

1. **Kernel** — `Customer`, `Site`, `Cargo`. `Shipment` ganha `CargoId` (nullable no início para não quebrar a API atual).
2. **Warehouse mínimo** — `Location` + `InventoryBalance` + `Receive` (entra no CD) + `Dispatch` (sai para o TMS). Receive/Dispatch gravam saldo **e** outbox na mesma transação (`ShipmentService.CreateAsync` já é o molde).
3. **Torre** — consumers para `CargoReceived`, `StockMoved`, `CargoDispatched`; timeline unificada.
4. **API da jornada** — criar carga → receber no CD → criar frete vinculado → `GET` tracking/timeline.
5. **Teste DDIA cap. 7** — dois dispatches concorrentes no mesmo estoque (lost update / isolation).

Regras desta fatia:

- Dois `Receive` no mesmo `Cargo` são rejeitados no domínio.
- Tracking continua eventualmente consistente: HTTP 201 antes do consumer projetar (como hoje).
- Não abrir recinto, YMS, faturamento nem Kafka nesta fatia.

## Fora de escopo (até o capítulo correspondente)

- Microserviços por bounded context (workers separados já ensinam distribuição).
- Integração real Receita / SAP / Körber — só ports + fakes.
- Rename da solution.
- IA (Argos), data lake, portal Genius clone.
- Adapter SQL Server/Oracle — opcional só se for falar da stack da vaga; Postgres permanece o playground.

## Entrevista Multilog (quando o código existir)

Narrativa com este repo: carga do CD ao frete; torre é projeção, não OLTP; outbox no lugar de 2PC; partição lógica por unidade (`Site`); portal do cliente leria o modelo derivado. Isso mapeia BIM + Genius + Torre + plataforma de transporte, sem fingir que reimplementou o produto deles.

## Estado atual (ponto de partida)

- Domínio: só `Shipment` + `ShipmentStatus`.
- Persistência: `Shipments`, `outbox_events`, `inbox_messages`, `shipment_timeline`.
- Eventos: `ShipmentCreatedEvent`, `ShipmentStatusChangedEvent`.
- API: CRUD/status/tracking/timeline de frete + JWT.

Próximo commit de código, quando retomar: item 1 da primeira entrega (Kernel).
