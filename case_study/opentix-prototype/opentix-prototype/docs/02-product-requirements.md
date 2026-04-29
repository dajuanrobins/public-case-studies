# OpenTix: Product Requirements

**Document status:** Draft  
**Version:** 0.1  
**Last updated:** 2026-04-29  
**Related documents:** [01-concept-and-need.md](01-concept-and-need.md), [03-architecture.md](03-architecture.md), [08-roadmap.md](08-roadmap.md)

---

## 1. Product Modules

### 1.1 Event Marketplace

The marketplace is the fan-facing surface through which buyers discover events, review ticket tiers, and complete purchases.

**Requirements:**

- Display upcoming events with filterable views by category, date, organizer, venue, and location
- Present ticket tier details including available inventory, pricing, and applicable transfer or resale rules prior to purchase commitment
- Support wallet connection or custodial wallet account creation
- Accept at least one supported payment method for ticket purchase
- Issue a digital ticket receipt upon successful purchase
- Display purchased tickets in the buyer's wallet

### 1.2 Organizer Console

The organizer console is the administrative surface through which event hosts configure events, ticket tiers, digital assets, and resale policies.

**Requirements:**

- Create and manage an organization profile
- Create individual events or multi-event season catalogs
- Configure venue, date, time, capacity, and event category
- Define ticket tiers with configurable inventory and pricing
- Upload ticket artwork and configure visual template parameters
- Attach premium content or downloadable media to ticket tiers
- Define resale and transfer rules, including organizer royalty percentage
- Publish events and manage event lifecycle (draft, published, closed, canceled)
- Review real-time sales data and payout status

### 1.3 Ticket Wallet

The wallet surface presents the fan's active and archived ticket holdings.

**Requirements:**

- Display current owner identity and ticket metadata
- Render a dynamic QR code or verifiable presentation proof
- Surface applicable transfer policy and premium content entitlements
- Support ticket transfer if permitted by organizer rules
- Support resale listing if permitted by organizer rules
- Archive tickets after the event concludes

### 1.4 Venue Scanner

The venue scanner is used by gate staff to validate and redeem tickets at entry.

**Requirements:**

- Scan QR codes or NFC-style presentation proofs
- Validate that the ticket exists, is assigned to the correct event and tier, and has not been previously redeemed
- Validate wallet ownership or a cryptographically signed presentation proof
- Mark the ticket as redeemed upon successful validation
- Operate in a degraded or offline mode with deferred reconciliation upon reconnection

### 1.5 Resale Marketplace

The resale marketplace enables organizer-governed secondary-market transfers.

**Requirements:**

- Enforce organizer-defined resale windows
- Apply configurable maximum markup limits expressed in basis points
- Apply and distribute organizer royalties on resale transactions
- Prevent resale after the configured cutoff date or for non-transferable tickets
- Record resale events with a complete audit trail
- Update ticket ownership atomically upon resale completion

### 1.6 Digital Content and Collectibles

OpenTix MUST support attaching premium content and collectible digital assets to ticket tiers.

**Requirements:**

- Attach downloadable content to individual ticket tiers
- Attach event-level or organization-level collectible assets to tickets
- Gate content access by verified ticket ownership
- Preserve archive access after the event concludes, subject to organizer configuration
- Support tier-specific content differentiation

---

## 2. MVP Requirements

The initial production MVP MUST deliver a narrow, end-to-end working path:

1. An organizer creates one event with three ticket tiers.
2. A fan purchases one ticket through the checkout flow.
3. The ticket is minted and visible in the buyer's wallet.
4. A venue scanner validates and redeems the ticket.
5. The organizer can view sales totals and basic redemption analytics.
6. Ticket transfer can be enabled or disabled via organizer-defined rules.

---

## 3. V1 Requirements

Following MVP stabilization, V1 SHOULD deliver:

- Resale marketplace with royalty enforcement
- Premium content entitlements
- Season and multi-event catalog listings
- Organization-level digital content
- Event-level collectibles
- Basic fraud and risk dashboard
- Mobile scanner application
- Fiat checkout via Stripe or equivalent payment provider
- Wallet abstraction for non-custodial users

---

## 4. MVP Non-Goals

The MVP MUST NOT include the following in order to maintain scope discipline:

- Social network functionality
- Complex creator economy tooling
- Open-ended NFT marketplace features
- Cross-chain asset support
- Advanced interactive seat maps (unless required by a launch partner)
- DAO governance mechanisms

---

## 5. Success Metrics

| Metric | Rationale |
|---|---|
| Purchase conversion rate | Indicates marketplace usability and checkout friction. |
| Scanner validation latency | Indicates venue readiness and operational performance. |
| Failed scan rate | Indicates fraud exposure and system reliability. |
| Transfer completion rate | Indicates resale and transfer feature usability. |
| Organizer event setup time | Indicates onboarding simplicity. |
| Support tickets per event | Indicates operational quality and product reliability. |
| Repeat organizer rate | Indicates commercial viability and platform stickiness. |

---

## 6. Representative User Stories

### Organizer

- As an organizer, I want to create ticket tiers with differentiated pricing and benefits so that I can address multiple audience segments and revenue streams.
- As an organizer, I want to configure resale markup limits so that secondary-market prices remain accessible to my fans.
- As an organizer, I want to receive a royalty on resale transactions so that my organization captures value when demand increases after initial sale.
- As an organizer, I want to attach exclusive digital content to premium tiers so that higher-priced tickets deliver additional tangible value.

### Fan

- As a fan, I want to verify that my ticket is authentic before attending the event so that I can purchase with confidence.
- As a fan, I want to transfer my ticket securely if I am unable to attend so that I do not lose the value of my purchase.
- As a fan, I want my ticket to function as a collectible artifact after the event so that it retains personal and potentially market value.

### Venue Staff

- As venue staff, I want to scan and validate tickets rapidly so that entry queues remain manageable.
- As venue staff, I want clear, actionable denial messages so that I can identify and escalate suspected fraud without ambiguity.
