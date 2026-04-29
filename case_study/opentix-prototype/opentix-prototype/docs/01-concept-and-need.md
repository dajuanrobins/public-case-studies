# OpenTix: Concept and Market Need

**Document status:** Draft  
**Version:** 0.1  
**Last updated:** 2026-04-29  
**Related documents:** [02-product-requirements.md](02-product-requirements.md), [03-architecture.md](03-architecture.md)

---

## 1. Executive Summary

OpenTix is a programmable digital ticketing platform serving event organizers, venues, artists, sports teams, conferences, and community organizers. It combines a familiar ticket-purchasing experience with cryptographically verifiable ownership, programmable resale enforcement, premium digital content delivery, and event-level collectibles.

The foundational premise is that a ticket should function as a durable, secure digital asset — not merely a barcode in an email. Each ticket should carry clear, auditable ownership, enforceable transfer rules, transparent issuance records, and the ability to retain post-event value for the holder.

## 2. Problem Statement

Existing ticketing platforms address access control at the venue level but leave several structurally significant problems unresolved.

### 2.1 Fans Do Not Hold a Durable Ticket Asset

The majority of tickets are ephemeral records in a private database controlled by the issuer. Once the event concludes, the ticket ceases to have functional utility. Fans seeking commemorative artifacts, verifiable proof of attendance, or collectible value are typically directed to separate, disconnected products.

### 2.2 Organizers Lose Enforcement Authority After Initial Sale

Uncontrolled secondary markets routinely produce extreme price markups, fraudulent listings, and reputational harm to the original organizer. Organizers frequently receive no share of secondary-market revenue despite being the source of the underlying event value.

### 2.3 Ticket Fraud and Duplicate Entry Remain Systemic Issues

Screenshot-based transfers, informal peer-to-peer marketplaces, and ambiguous ownership chains create material risks for both fans and venue operators. A verifiable, canonical source of ticket ownership is absent from most existing platforms.

### 2.4 Premium Access Entitlements Are Fragmented

VIP passes, downloadable content, artist media, sponsor offers, backstage access, season bundles, and event collectibles are routinely managed across multiple disconnected systems. This fragmentation increases operational overhead and degrades the fan experience.

### 2.5 Multi-Event and Season Catalog Support Is Inadequate

Sports teams, venue operators, festivals, universities, and conference organizers require the ability to publish and manage multiple related events under a unified catalog. Most platforms lack first-class support for season passes, event bundles, and consolidated organizer schedules.

## 3. Rationale for Blockchain Integration

OpenTix applies distributed ledger technology only where shared, durable, verifiable state produces concrete product value. It is not used to add complexity or as a marketing differentiator.

| Requirement | Value Delivered by Shared Ledger |
|---|---|
| Ticket ownership | A ticket token provides cryptographic proof of the current owner without relying on screenshots or database trust. |
| Transfer history | Resale and transfer events are recorded as immutable, auditable state transitions. |
| Resale rule enforcement | Smart contracts enforce royalty distributions, transfer windows, and markup ceilings deterministically. |
| Fan collectibles | Tickets persist as collectible digital artifacts after event entry, with continued verifiability. |
| Organizer transparency | Ticket issuance, supply limits, and policy changes are transparent and independently verifiable. |

## 4. Core Concept

OpenTix establishes a system in which each event defines one or more ticket tiers. Each tier carries configurable supply limits, pricing, transfer rules, visual templates, and optional content entitlements. Upon purchase, the platform mints a digital ticket record associated with the buyer's wallet. At venue entry, a verifier confirms that the ticket is valid, unredeemed, and held by the presenting wallet.

## 5. Target Users

### 5.1 Event Organizers

Representative examples: music venues, conference operators, theaters, festivals, universities, sports teams, and community organizations.

Core needs: streamlined event creation, ticket inventory management, tiered pricing, payout tracking, secondary-market enforcement, analytics, and audience engagement tooling.

### 5.2 Fans and Ticket Buyers

Core needs: a simple, mobile-optimized purchase experience, verifiable ticket ownership, secure peer-to-peer transfers, and confidence in ticket authenticity.

### 5.3 Venue Operators and Gate Staff

Core needs: low-latency ticket validation, offline-tolerant scanning, clear fraud signals, and unambiguous entry-state display.

### 5.4 Creators and Sponsors

Core needs: the ability to use ticket entitlements as a controlled distribution channel for premium media, digital artwork, promotional offers, collectibles, or exclusive access grants.

## 6. Product Objectives

OpenTix must deliver a ticket-purchasing experience that is no more complex than existing platforms while providing organizers and fans with stronger ownership guarantees, deterministic resale enforcement, and richer post-event value.

## 7. Prototype Scope and Constraints

The included prototype is a browser-only mock application intended to demonstrate the core product concept. It covers event discovery, tier selection, mock minting, wallet ownership simulation, resale policy simulation, ticket template configuration, premium content flags, and a mock ledger timeline.

The prototype intentionally excludes: real payment processing, cryptographic wallet signing, backend persistence, venue scanner hardware integration, and live Soroban contract execution. See [08-roadmap.md](08-roadmap.md) for the path from prototype to production.
