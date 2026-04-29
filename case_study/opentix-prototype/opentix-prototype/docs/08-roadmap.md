# OpenTix: Engineering Roadmap

**Document status:** Draft  
**Version:** 0.1  
**Last updated:** 2026-04-29  
**Related documents:** [02-product-requirements.md](02-product-requirements.md), [03-architecture.md](03-architecture.md)

---

## 1. Phase 0: Concept Prototype *(current)*

**Objective:** Validate the core product concept and demonstrate the end-to-end fan and organizer experience without backend dependencies.

**Scope:**

- Browser-only mock application demonstrating the core fan and organizer loop
- Ticket tier configuration and mock wallet simulation
- Resale and royalty policy visualization
- Technical architecture documentation

**Deliverables:** This repository.

---

## 2. Phase 1: Technical Spike

**Objective:** Prove the critical technical path from purchase to on-chain ticket ownership and QR validation on a test network.

**Scope:**

- Minimal Soroban ticket mint contract deployed to testnet
- Backend service that submits mint transactions
- Single-page checkout frontend with wallet connect integration
- End-to-end ownership proof via wallet presentation
- QR-based validation proof of concept

**Deliverables:**

- Testnet contract and contract test suite
- Minimal backend service
- Wallet connect proof of concept
- Scanner validation proof of concept

---

## 3. Phase 2: Minimum Viable Product

**Objective:** Deliver a production-capable system capable of running a real event end-to-end with a single trusted organizer partner.

**Scope:**

- One organizer creates and publishes an event with multiple ticket tiers
- Fans purchase tickets through a production checkout flow
- Tickets are minted and visible in wallet
- Venue gate staff can scan and redeem tickets
- Basic sales and redemption analytics are available to the organizer

**Deliverables:**

| Component | Description |
|---|---|
| Organizer console | Event creation, tier configuration, publishing |
| Fan checkout | Purchase flow with payment integration |
| Ticket wallet | Minted ticket display and QR presentation |
| Scanner app | Mobile gate validation application |
| Payment integration | Stripe or equivalent fiat payment provider |
| Ticket minting worker | Async mint orchestration with retry logic |
| Event database | PostgreSQL schema per data model |
| Operational admin tools | Internal tooling for support and incident response |

---

## 4. Phase 3: Controlled Beta

**Objective:** Operate real or near-real events with a small cohort of trusted organizer partners. Harden operational workflows and validate system behavior under live conditions.

**Scope:**

- Live event execution with trusted beta partners
- Scanner flow hardening under real venue conditions
- Improved support and incident tooling
- Conversion rate and scanner latency measurement
- Refund and event cancellation workflow validation

**Deliverables:**

| Component | Description |
|---|---|
| Support dashboard | Ticket lookup, override, and escalation tooling |
| Refund workflow | End-to-end refund processing with PSP reconciliation |
| Event cancellation workflow | Automated cancellation, notification, and refund trigger |
| Observability improvements | Structured logging, tracing, and alerting baselines |
| Beta partner onboarding documentation | Runbook and integration guide for launch partners |

---

## 5. Phase 4: V1 Launch

**Objective:** Expand the platform to include the full secondary-market feature set, premium content entitlements, and multi-event catalog support.

**Scope:**

- Resale marketplace with organizer royalty enforcement
- Premium content unlock entitlements per ticket tier
- Season and multi-event catalog listings
- Role-based organizer team management
- Production-grade security posture and operational monitoring

**Deliverables:**

| Component | Description |
|---|---|
| Resale service | Secondary market listing, purchase, and transfer orchestration |
| Rules contract | On-chain enforcement of resale and royalty policies |
| Content entitlement service | Ownership-gated content delivery with signed URL generation |
| Season catalog UX | Multi-event and bundle purchase flows |
| Public event pages | SEO-optimized event discovery pages |
| Partner venue tooling | Venue-specific scanner configuration and reporting |

---

## 6. Phase 5: Platform Expansion

**Objective:** Expand OpenTix into a platform that supports a broader ecosystem of organizers, venues, sponsors, and API consumers.

**Scope:**

- Event-level and organization-level digital collectibles
- Organization-level digital content stores
- Sponsor content campaign distribution
- Advanced interactive seat maps
- Public API platform for large venue operators and sports teams
- White-label ticketing surfaces for enterprise partners

---

## 7. Recommended Build Sequence

Within each phase, the following vertical ordering minimizes rework and maximizes early validation:

1. Event and tier data model
2. Fan purchase flow
3. Ticket minting contract
4. Ticket wallet display
5. Scanner validation
6. Organizer analytics
7. Transfer rule enforcement
8. Resale marketplace
9. Premium content entitlements
10. Season and multi-event catalogs

---

## 8. Team Requirements

**Early-stage roles required:**

| Role | Responsibility |
|---|---|
| Product/engineering lead | Architecture, prioritization, and technical direction |
| Full-stack engineer | API services, organizer console, and fan app |
| Smart contract engineer | Soroban contracts, adapter, and chain integration |
| UX/product designer | Fan experience, organizer console, and scanner app |
| Security advisor or auditor | Contract review and security posture |
| Event operations partner | Beta event execution and operational feedback |

---

## 9. Key Risks and Mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| Blockchain UX creates unacceptable friction for mainstream fans | Medium | Implement wallet abstraction and custodial wallet options from Phase 1 |
| Scanner latency exceeds acceptable thresholds at high-volume venues | Medium | Cache verification data locally and optimize the redemption API; establish latency SLOs in Phase 2 |
| Smart contract vulnerabilities are discovered post-launch | Low | Keep contracts minimal, test exhaustively, and complete an independent security audit before any real-value deployment |
| Organizers prioritize simplicity over collectible features | High | Lead with fraud prevention, resale control, and organizer analytics; defer collectibles to Phase 5 |
| Payment and minting reconciliation failures create orphaned records | Medium | Implement outbox pattern, idempotency keys, and automated reconciliation jobs from Phase 2 |
| Secondary-market regulatory exposure varies by jurisdiction | High | Design the resale policy engine to be configurable per market; obtain legal guidance before each regional launch |
