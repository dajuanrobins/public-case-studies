# OpenTix: API and Eventing Design

**Document status:** Draft  
**Version:** 0.1  
**Last updated:** 2026-04-29  
**Related documents:** [03-architecture.md](03-architecture.md), [06-data-model.md](06-data-model.md)

---

## 1. API Design Goals

The API surface MUST support the complete purchase flow while ensuring internal workflows are reliable, idempotent, and auditable.

- Provide a clear, versioned REST surface for all client applications
- Enforce idempotency on all purchase, minting, and redemption operations
- Apply strong, scope-based authorization for all organizer operations
- Publish durable domain events for all significant state transitions
- Maintain a clean separation between the public API surface and internal worker command interfaces

---

## 2. Public API Resources

### 2.1 Organizations

```
POST   /organizations
GET    /organizations/{organizationId}
PATCH  /organizations/{organizationId}
GET    /organizations/{organizationId}/members
```

### 2.2 Events

```
POST   /events
GET    /events
GET    /events/{eventId}
PATCH  /events/{eventId}
POST   /events/{eventId}/publish
POST   /events/{eventId}/cancel
```

### 2.3 Ticket Tiers

```
POST   /events/{eventId}/tiers
GET    /events/{eventId}/tiers
PATCH  /events/{eventId}/tiers/{tierId}
```

### 2.4 Purchases

```
POST   /purchases
GET    /purchases/{purchaseId}
```

### 2.5 Tickets

```
GET    /wallet/tickets
GET    /tickets/{ticketId}
POST   /tickets/{ticketId}/transfer
POST   /tickets/{ticketId}/redeem
```

### 2.6 Resale

```
POST   /resale/listings
GET    /resale/listings
POST   /resale/listings/{listingId}/buy
DELETE /resale/listings/{listingId}
```

### 2.7 Content Entitlements

```
GET    /tickets/{ticketId}/content
POST   /events/{eventId}/content
GET    /content/{contentId}/download-url
```

---

## 3. Idempotency

All state-mutating operations MUST support idempotency to prevent duplicate processing in the face of retries, network failures, or client timeouts.

**Operations requiring an idempotency key:**

- Purchase creation
- Payment capture
- Ticket allocation
- Mint transaction submission
- Ticket redemption
- Ticket transfer
- Resale purchase

**Required request header:**

```http
Idempotency-Key: 2c6ce529-c611-49b1-b888-e46f2315f902
```

The key MUST be a client-generated UUID (v4). Duplicate requests bearing the same key within the idempotency window MUST return the original response without re-executing the operation.

---

## 4. Example: Purchase Flow

**Request**

```json
{
  "event_id": "evt_01",
  "tier_id": "tier_premium",
  "quantity": 1,
  "buyer_wallet": "GBC4...9X2Q",
  "payment_method_id": "pm_123",
  "accept_rules_version": "rules_v3"
}
```

**Response**

```json
{
  "purchase_id": "pur_01",
  "status": "mint_pending",
  "payment_status": "authorized",
  "ticket_ids": ["tix_01"],
  "estimated_confirmation_seconds": 8
}
```

---

## 5. Internal Event Bus

All significant domain state transitions MUST be published to an internal event bus. Kafka is the recommended implementation given OpenTix's replay, analytics, and high-throughput fan-out requirements.

### 5.1 Topic Structure

| Topic | Purpose |
|---|---|
| `opentix.events.lifecycle` | Event created, published, updated, canceled |
| `opentix.ticketing.commands` | Mint, transfer, and redeem command messages |
| `opentix.ticketing.events` | Ticket minted, transferred, redeemed |
| `opentix.payments.events` | Payment authorized, captured, failed, refunded |
| `opentix.resale.events` | Listing created, purchased, canceled |
| `opentix.content.events` | Content attached, unlocked, revoked |
| `opentix.scanner.events` | Scan succeeded, failed, conflict detected |
| `opentix.notifications.commands` | Email, SMS, and push notification work items |

### 5.2 Event Envelope Schema

All published events MUST conform to a standard envelope:

```json
{
  "event_id": "evtmsg_01",
  "event_type": "ticket.minted.v1",
  "occurred_at": "2026-04-29T12:30:00Z",
  "producer": "ticket-service",
  "tenant_id": "org_01",
  "correlation_id": "corr_01",
  "causation_id": "purchase_01",
  "payload": {
    "ticket_id": "tix_01",
    "event_id": "evt_01",
    "owner_wallet": "GBC4...9X2Q",
    "contract_tx_hash": "abc123"
  }
}
```

---

## 6. Outbox Pattern

To guarantee at-least-once delivery without distributed transaction coordination, services MUST implement the transactional outbox pattern.

**Required flow:**

1. Service writes domain state changes to the operational database.
2. Service writes a corresponding outbox row within the same database transaction.
3. A dedicated outbox publisher process polls for unprocessed rows.
4. Publisher emits each row to the Kafka topic.
5. Publisher marks the row as published upon acknowledgment.
6. Downstream consumers process all events idempotently.

This ensures that no event is lost due to a service failure between a database write and a message publish.

---

## 7. Scanner Validation API

Scanner endpoints have strict latency requirements and MUST be optimized for speed, as gate throughput directly affects fan experience.

**Endpoint:**

```http
POST /scanner/validate
```

**Request:**

```json
{
  "event_id": "evt_01",
  "ticket_id": "tix_01",
  "presentation_signature": "sig_abc",
  "scanner_device_id": "scan_venue_gate_2"
}
```

**Response:**

```json
{
  "decision": "allow",
  "reason": "valid_owner_unredeemed",
  "ticket_status": "redeemed",
  "attendee_display": "Premium ticket",
  "receipt_id": "scanrec_01"
}
```

Possible `decision` values: `allow`, `deny`, `review`.

---

## 8. Error Model

All API errors MUST return stable, machine-readable error codes alongside a human-readable message. Error codes MUST NOT change between versions without a deprecation period.

| Error Code | Condition |
|---|---|
| `event_not_found` | The referenced event identifier does not exist |
| `event_not_published` | The event exists but is not in a published state |
| `tier_sold_out` | The requested tier has exhausted its supply cap |
| `payment_failed` | Payment authorization or capture was rejected |
| `mint_failed_retryable` | Minting failed due to a transient chain condition; caller may retry |
| `ticket_not_owned_by_wallet` | The presenting wallet does not hold the referenced ticket |
| `ticket_already_redeemed` | The ticket has previously been marked redeemed |
| `transfer_not_allowed` | The ticket's rules contract prohibits transfer |
| `resale_markup_exceeded` | The proposed resale price exceeds the configured markup limit |
| `royalty_settlement_failed` | The on-chain royalty distribution transaction failed |
