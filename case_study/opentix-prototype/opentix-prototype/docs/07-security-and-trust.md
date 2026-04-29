# OpenTix: Security, Trust, and Compliance

**Document status:** Draft  
**Version:** 0.1  
**Last updated:** 2026-04-29  
**Related documents:** [04-smart-contract-design.md](04-smart-contract-design.md), [06-data-model.md](06-data-model.md)

---

## 1. Threat Model

OpenTix handles financial transactions, event access control, digital asset ownership, and personal identity data. Security is a first-class product requirement and MUST be treated as such from initial architecture decisions through production operations.

**Primary threat categories:**

| Category | Representative Threats |
|---|---|
| Ticket authenticity | Fake tickets, screenshot replay, duplicate minting |
| Double redemption | Concurrent scan attacks, scanner state desynchronization |
| Account compromise | Stolen wallet or session credentials, organizer account takeover |
| Insider abuse | Unauthorized ticket minting by privileged operators |
| Inventory manipulation | Overselling supply, unauthorized supply cap changes |
| Policy bypass | Resale rule circumvention, royalty skipping |
| Financial fraud | Payment fraud, chargeback abuse |
| Infrastructure attacks | Malicious or compromised scanner devices |
| Contract vulnerabilities | Smart contract logic errors, upgrade exploits |
| Content leakage | Unauthorized access to premium or gated content |

---

## 2. Security Boundaries

### 2.1 Organizer Operations

All organizer-initiated actions that affect event state, ticket inventory, or financial configuration MUST require strong authorization.

**Sensitive organizer operations:**

- Event creation, publication, and cancellation
- Supply cap modification
- Ticket price changes
- Payout account configuration
- Scanner staff provisioning
- Ticket freezing or forced cancellation

**Required controls:**

- Role-based access control (RBAC) with organization-scoped membership
- Multi-factor authentication (MFA) for all administrative roles
- Append-only audit logs for all sensitive configuration changes
- Approval workflow required before payout account changes take effect

### 2.2 Fan Operations

Fan-facing operations MUST be secure without imposing unnecessary friction on the purchase or entry experience.

**Required controls:**

- Wallet signing required for ownership-affecting operations (transfers, resale listing)
- Device and session risk scoring
- Secure account recovery flow for custodial wallet holders
- Explicit confirmation screens for transfer and resale actions
- Clear disclosure of transfer and resale rules prior to purchase

### 2.3 Scanner Operations

Venue scanner devices operate at the trust boundary between the physical and digital ticket systems. They MUST NOT be unconditionally trusted.

**Required controls:**

- Device registration and provisioning before deployment
- Event-scoped scanner authorization tokens
- Short-lived scanner session credentials with automatic expiry
- Signed offline event cache for degraded-mode operation
- Deferred reconciliation upon reconnection after offline periods
- Per-device audit trail for all scan events

---

## 3. Ticket Fraud Prevention

**Required strategies:**

- Dynamic QR codes or time-scoped signed presentation proofs (static QR codes MUST NOT be used for redemption)
- Server-side redemption locking with idempotent transaction semantics
- On-chain or contract-backed ownership verification at scan time
- Scanner authorization scoped to a specific event identifier
- Single-use redemption transactions that cannot be replayed
- Concurrent double-scan conflict detection with deterministic resolution
- Automated risk flagging for suspicious transfer chains or high-velocity resale activity

---

## 4. Smart Contract Safety

Smart contracts that govern real asset ownership require rigorous engineering discipline before mainnet deployment.

**Required practices:**

- Unit tests for every contract function, including all error paths
- Property-based tests verifying supply and transfer invariants under adversarial inputs
- Negative tests for all unauthorized caller scenarios
- Replay and idempotency tests for all state-mutating operations
- Upgrade and migration tests covering all active contract versions
- Independent security review by a qualified auditor before any real-value deployment
- Formally scoped emergency pause capability

---

## 5. Critical System Invariants

The following invariants MUST be enforced by the platform at all times and MUST be verified by both unit tests and operational monitoring:

1. A ticket MUST NOT be minted beyond the configured tier supply cap.
2. A ticket MUST NOT be redeemed more than once.
3. A non-transferable ticket MUST NOT be transferred under any code path.
4. A resale transaction MUST NOT exceed the configured markup limit.
5. The organizer royalty MUST NOT be omitted from any controlled resale settlement.
6. A canceled event MUST NOT accept new ticket purchases.
7. Private customer data MUST NOT be written to any on-chain contract.
8. Scanner devices MUST NOT be permitted to redeem tickets for events they are not authorized to serve.

---

## 6. Payment Security

OpenTix MUST delegate all payment instrument handling to a qualified payment service provider (PSP). Raw card data MUST NOT be stored, logged, or transmitted through OpenTix infrastructure.

**Required controls:**

- PSP tokenization for all payment instrument references
- Webhook signature verification on all PSP callbacks
- Reconciliation of payment events against internal purchase records
- Idempotent payment capture to prevent duplicate charges
- Chargeback and refund handling as first-class operational workflows

---

## 7. Data Protection

| Data Class | Handling Requirements |
|---|---|
| User email and name | Off-chain only; encrypted at rest |
| Payment instrument data | PSP-managed only; MUST NOT be stored by OpenTix |
| Wallet address | Pseudonymous; link to user identity with care |
| Ticket ownership | On-chain (authoritative) plus off-chain cache (reconciled) |
| Premium content files | Object storage with time-scoped signed URLs |
| Audit logs | Append-only, access-controlled, retained per policy |

---

## 8. Compliance Considerations

Depending on jurisdiction and business model, OpenTix deployments may be subject to the following regulatory areas. Legal guidance MUST be obtained for each applicable jurisdiction before production launch:

- Payment processing regulations and money transmission licensing
- Sales tax collection and remittance obligations
- Consumer refund and cancellation rights
- Event cancellation liability and refund requirements
- Data privacy laws (e.g., GDPR, CCPA, and applicable local equivalents)
- Consumer protection regulations governing secondary ticket markets
- Accessibility requirements for public-facing ticketing and venue systems

---

## 9. Operational Trust Features

To support organizer confidence and fan trust, the platform SHOULD expose the following transparency features:

- Ticket authenticity verification view accessible to ticket holders
- Public event supply summary (total issued vs. available)
- Organizer identity verification badges
- Clear, prominent resale policy disclosure on all listing pages
- Transaction receipt page with complete purchase provenance
- Escalation path for disputed or fraudulently transferred tickets
- Event cancellation and refund status page accessible without authentication
