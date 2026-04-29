# OpenTix: Why Stellar and Soroban

**Document status:** Draft  
**Version:** 0.1  
**Last updated:** 2026-04-29  
**Related documents:** [03-architecture.md](03-architecture.md), [04-smart-contract-design.md](04-smart-contract-design.md), [07-security-and-trust.md](07-security-and-trust.md)

---

## 1. Summary

OpenTix requires a blockchain layer that can handle high-volume, low-latency ticket operations, enforce programmable resale and ownership rules, support fiat-adjacent payment settlement, and do all of this at a cost that scales to a ten-dollar concert ticket. This document explains why Stellar, with Soroban smart contracts, satisfies those requirements better than alternative Layer 1 and Layer 2 networks.

---

## 2. The Constraints That Drive the Platform Choice

Before comparing networks, it is worth being explicit about what the ticketing domain demands from a blockchain layer. These constraints eliminate many otherwise viable options.

| Constraint | Why it matters for OpenTix |
|---|---|
| Sub-5-second settlement finality | Gate scanners cannot hold fans for 12-second block times or wait for confirmation depth. |
| Predictable, sub-cent transaction fees | A $12 general-admission ticket cannot absorb $2–5 in gas fees. Fees must be predictable, not auction-based. |
| Native asset authorization controls | Ticket tokens must support policy flags (freeze, clawback, transfer restriction) without writing custom enforcement into every contract. |
| USDC as a first-class payment rail | Organizers need fiat-denominated settlement, not exposure to volatile native token price risk. |
| Rust-native smart contract tooling | The recommended backend stack is Rust. The contract layer should share the same language, toolchain, and testing idioms. |
| WebAssembly execution model | WASM provides deterministic execution, size-bounded contracts, and a well-understood security surface. |
| Production-grade REST indexing API | The scanner validation flow requires low-latency chain state reads. Building and operating a custom indexer is operational overhead that should be avoided if the network provides one. |
| Compliance-grade asset controls | In regulated markets, the ability to freeze or clawback a ticket (e.g., court order, fraud response) is a legal requirement, not a nice-to-have. |

---

## 3. Stellar Network: Core Properties

### 3.1 Stellar Consensus Protocol and Finality

Stellar uses the **Stellar Consensus Protocol (SCP)**, a variant of Federated Byzantine Agreement (FBA). Unlike Nakamoto-style proof-of-work or even most proof-of-stake designs, SCP provides:

- **Deterministic finality** — a transaction that is confirmed is final. There is no reorg risk, no waiting for six confirmations, no probabilistic safety window.
- **3–5 second end-to-end settlement** — from submission to finality under normal network conditions.
- **Safety over liveness** — SCP halts rather than produces conflicting ledgers in the presence of a quorum failure. For a ticketing system, a brief halt is acceptable; a double-spend is not.

For OpenTix's scanner flow, deterministic finality at 3–5 seconds means a gate validation can wait for on-chain confirmation of the redemption state rather than relying entirely on an off-chain optimistic cache. The scanner design becomes simpler and the trust model becomes stronger.

### 3.2 Transaction Fees

Stellar's fee model uses a **base fee** expressed in stroops (1 stroop = 0.0000001 XLM). At typical XLM valuations, a standard transaction costs a fraction of a cent. Critically, Stellar uses a **fee bump** mechanism for sponsored transactions, which means OpenTix can absorb transaction fees on behalf of fans — eliminating the onboarding friction of requiring fans to hold XLM before they can receive a ticket token.

This is a non-trivial advantage. On networks with gas-auction fee models, fee estimation under load is unreliable, and fee spikes during high-demand event on-sales would directly increase the cost of ticket purchases in a way OpenTix cannot control or absorb predictably.

### 3.3 Native Asset Infrastructure

Stellar has a built-in asset layer that operates independently of the smart contract layer. Before Soroban, Stellar's native assets already supported:

- **Authorization Required** — the issuer must approve every new trustline before an account can hold the asset.
- **Authorization Revocable** — the issuer can freeze an individual account's holdings.
- **Clawback Enabled** — the issuer can reclaim an asset from any holder.
- **Limit flags** — supply caps and trustline limits at the protocol level.

For OpenTix, these flags map directly to ticketing requirements:

| Stellar asset flag | OpenTix use case |
|---|---|
| `AUTH_REQUIRED` | Organizer must approve wallet before a ticket token can be received |
| `AUTH_REVOCABLE` | Freeze a stolen or disputed ticket pending investigation |
| `CLAWBACK_ENABLED` | Court-ordered or fraud-response ticket revocation |

These controls are enforced at the protocol level, not in application code. That means they cannot be bypassed by a contract bug or an unauthorized caller.

### 3.4 USDC on Stellar

Circle's **USDC is natively issued on Stellar**, making it available as a first-class payment asset without bridging, wrapping, or third-party custody. For OpenTix, this matters in several ways:

- Organizer payouts can be denominated in USD-equivalent value without native token price exposure.
- Resale royalties can be settled in USDC on-chain, providing a clean, auditable settlement trail.
- Fans who want to pay in USD-denominated stable value can do so without leaving the Stellar network.

The availability of a regulated, liquid stablecoin on the same network where ticket ownership is enforced simplifies the payment and settlement architecture substantially.

### 3.5 Horizon API

Stellar operates **Horizon**, a maintained REST API server that indexes chain state and exposes it over standard HTTP endpoints. For OpenTix, this means:

- Reading current ticket ownership state does not require building and operating a custom indexer.
- Scanner validation flows can query ticket state through a well-documented REST API.
- Event and transfer history is queryable without raw ledger parsing.
- Horizon instances can be self-hosted or consumed from managed providers, giving deployment flexibility.

This is operationally significant. Teams deploying on EVM chains typically need to run their own archival node or pay for a third-party indexing service (e.g., The Graph, Alchemy) to get the query surface that Horizon provides as a first-party component.

---

## 4. Soroban: Smart Contracts on Stellar

Soroban is Stellar's smart contract platform. It was purpose-built for the Stellar network and diverges significantly from EVM-style contract environments in ways that benefit OpenTix.

### 4.1 Rust-Native, WASM-Executed

Soroban contracts are written in **Rust** and compiled to **WebAssembly**. This has several concrete advantages for OpenTix:

- **Unified language across the stack** — the Ticket Service, Blockchain Adapter, and scanner validation service are all Rust. Contract code, SDK calls, and shared data types live in the same language. Engineers do not context-switch between Solidity or Move and a different backend language.
- **Compile-time safety** — Rust's ownership model and type system catch a large class of contract bugs at compile time rather than at runtime on a live network.
- **Deterministic WASM execution** — WebAssembly provides a well-defined, sandboxed, deterministic execution environment with bounded memory and no undefined behavior.
- **Existing Rust ecosystem** — serialization, cryptography, testing, and fuzzing libraries from the Rust ecosystem are available for use in contract development.

### 4.2 Resource Metering Model

Soroban uses a **resource metering model** that is more predictable than gas auctions. Each contract invocation specifies a resource budget (CPU instructions, memory, ledger I/O) upfront, and the fee is calculated from that budget. This provides:

- **Fee predictability** — the cost of minting a ticket, executing a transfer, or recording a redemption can be estimated accurately at design time, not just at invocation time.
- **No auction-based fee spikes** — the fee does not depend on network congestion at the moment of submission.
- **Explicit resource bounds** — contracts that exceed their declared resource budget fail cleanly rather than consuming unbounded resources.

### 4.3 State Archival

Soroban implements a **state archival** model in which contract state entries that have not been accessed within a defined TTL window are archived to a lower-cost storage tier. For OpenTix:

- High-frequency ticket state (recently minted, recently redeemed) remains in hot storage.
- Archived post-event ticket collectibles move to cold storage without being deleted, preserving the integrity of the historical ownership record.
- The archive model imposes a discipline on contract state design: contracts must explicitly think about which state is persistent and which is ephemeral.

### 4.4 Contract Invocation Model

Soroban uses an **authorization model** where each contract function call specifies which addresses are authorizing the invocation and what sub-invocations they are authorizing. This provides:

- **Composable authorization** — a resale transaction can involve the buyer, seller, and royalty recipient all authorizing their respective parts of the settlement in one atomic invocation.
- **No blind approval patterns** — unlike EVM allowance patterns, callers authorize specific operations with explicit scope, reducing the risk of approval-based exploits.
- **Auditable call trees** — the full authorization chain for a complex operation like a resale settlement is visible in the transaction envelope.

### 4.5 Soroban SDK and Tooling

The Soroban SDK (`soroban-sdk`) provides:

- Contract environment types (`Address`, `BytesN`, `Map`, `Vec`) that are serialization-safe and ledger-efficient.
- A test harness (`soroban-sdk::testutils`) for unit testing contract logic in a local environment without a running network.
- A CLI (`soroban`) for building, testing, deploying, and invoking contracts.
- Integration with standard Rust testing infrastructure (`cargo test`), enabling property-based testing, fuzzing, and coverage measurement with standard tools.

---

## 5. Competitive Analysis

The following table assesses Stellar and Soroban against the constraints defined in Section 2, compared to the most commonly considered alternatives.

| Requirement | Stellar + Soroban | Ethereum L1 | Ethereum L2 (e.g., Base, Arbitrum) | Solana |
|---|---|---|---|---|
| Sub-5s deterministic finality | ✅ 3–5s, deterministic | ❌ ~12s per slot, probabilistic finality | ⚠️ Fast slots, but L1 finality lags or relies on trusted sequencer | ⚠️ Fast, but history of outages affecting reliability |
| Sub-cent, predictable fees | ✅ Fractions of a cent, no auction | ❌ Auction-based, spikes during congestion | ✅ Low fees, but bridging costs add complexity | ✅ Very low, but fee spikes occur |
| Native asset authorization flags | ✅ Protocol-level freeze, clawback, auth | ❌ Requires custom contract implementation | ❌ Requires custom contract implementation | ❌ Requires custom program implementation |
| Native USDC payment rail | ✅ USDC natively issued on Stellar | ✅ USDC on Ethereum (canonical origin) | ✅ USDC via bridge | ✅ USDC natively on Solana |
| Rust smart contract language | ✅ Rust + WASM (Soroban) | ❌ Solidity (separate language, paradigm) | ❌ Solidity | ✅ Rust (Anchor framework) |
| Production REST indexing API | ✅ Horizon (first-party, maintained) | ❌ Requires custom indexer or paid provider | ❌ Requires custom indexer or paid provider | ⚠️ RPC nodes available, no first-party indexer |
| Compliance asset controls | ✅ Protocol-level, cannot be bypassed | ❌ Smart contract only, bypassable via bugs | ❌ Smart contract only | ❌ Program-level only |
| Sponsored transaction fees | ✅ Fee bump accounts | ❌ Meta-transactions require separate infrastructure | ⚠️ Some paymasters available | ⚠️ Requires custom fee payer logic |
| Fee predictability under load | ✅ Resource metering, no auction | ❌ Gas auction spikes | ✅ Generally stable | ⚠️ History of instability |
| Contract test harness in Rust | ✅ `soroban-sdk::testutils` | ❌ Hardhat / Foundry (separate toolchain) | ❌ Hardhat / Foundry | ⚠️ `solana-program-test` (more complex setup) |

### 5.1 Why Not Ethereum L1

Ethereum L1 is the most battle-tested smart contract platform and has the deepest security audit ecosystem. However, it fails the fee and finality requirements for a ticketing system at commercial scale. A $15 ticket cannot absorb $3–8 in gas fees on congested days. Probabilistic finality requires confirmation depth strategies that add latency incompatible with gate scanning. EVM contracts require a separate language (Solidity) and toolchain, fragmenting the team's expertise from the Rust backend stack.

### 5.2 Why Not Ethereum L2

Layer 2 networks like Base and Arbitrum solve the fee problem and approach deterministic finality for practical purposes. They are a reasonable alternative if the team already has EVM expertise. However, they introduce bridging complexity, depend on L1 for ultimate security, and require a separate indexing solution. The compliance asset controls that Stellar provides at the protocol level require custom contract implementations on EVM chains, increasing the attack surface. For a net-new team building on Rust, the L2 ecosystem does not provide a compelling advantage over Stellar.

### 5.3 Why Not Solana

Solana offers high throughput and low fees and has a Rust-based programming model. However, Solana's history of network-level outages is a significant risk for a venue-scanning use case where downtime during an event is a critical operational failure. Solana's account model and Anchor framework, while Rust-based, have a substantially steeper learning curve than Soroban and do not provide the protocol-level asset authorization flags that map to ticketing compliance requirements. Stellar's fee bump and sponsored transaction model is also more mature for custodial ticket issuance flows.

---

## 6. Specific Stellar Features That Map to OpenTix Requirements

### 6.1 Ticket Minting → Asset Issuance + Soroban Mint

Each ticket tier can be represented as a Stellar asset issued by the event organizer's account, with Soroban contracts providing the minting authorization layer and supply enforcement. The protocol-level asset model gives tickets immediate verifiability through any Stellar-compatible wallet or block explorer.

### 6.2 Resale Royalty Settlement → Atomic Path Payments

Stellar's **path payment** operation supports multi-asset atomic transfers in a single transaction. A resale settlement — buyer pays seller, royalty goes to organizer — can be expressed as a single atomic transaction with no intermediate settlement step and no smart contract complexity beyond rule verification.

### 6.3 Fraud Response → Clawback + Freeze

If a fraudulent ticket is detected or a dispute is filed, the organizer can invoke the Stellar **clawback** operation to revoke the ticket token and the **freeze** operation to prevent further transfers during investigation. Both operations are protocol-level and cannot be circumvented by a compromised contract.

### 6.4 Non-Custodial Fan Wallets → SEP-30 and Wallet Federation

Stellar Ecosystem Proposal **SEP-30** defines a standard for social recovery of non-custodial wallets. OpenTix can leverage SEP-30-compatible wallet providers to offer non-custodial wallets to fans without requiring them to manage seed phrases directly. This reduces the UX gap between blockchain-backed tickets and conventional email-based ticketing while preserving actual ownership properties.

### 6.5 Organizer Payout → USDC Settlement

Because USDC is natively available on Stellar, organizer payout after an event is a standard Stellar payment transaction. There is no bridging, no wrapping, and no third-party custody step between the smart contract settlement and the organizer's USDC balance.

---

## 7. Risks and Mitigations

Stellar and Soroban are strong choices, but no platform is without risk. The following risks should be acknowledged and tracked.

| Risk | Assessment | Mitigation |
|---|---|---|
| Soroban ecosystem maturity | Soroban reached General Availability in early 2024. The audit ecosystem and battle-tested production deployment history are less deep than Ethereum. | Keep contracts minimal. Require independent audit before real-value deployment. Follow Stellar Foundation upgrade announcements closely. |
| Stellar network validator distribution | Stellar's quorum configuration depends on a relatively small set of trusted validators compared to more decentralized networks. | Monitor quorum health. Design the off-chain system to degrade gracefully during any network pause rather than failing catastrophically. |
| Developer hiring | Rust + Soroban expertise is less common than Solidity expertise. | This is mitigated by the unified Rust stack across backend and contracts, which broadens the hiring pool relative to a Soroban-only requirement. |
| Regulatory classification of ticket tokens | Regulatory treatment of blockchain-backed tickets varies by jurisdiction and may evolve. | Consult legal counsel before launch in each target market. Design the token model to avoid characteristics associated with investment securities. |
| Horizon availability dependency | If a self-hosted Horizon instance goes down, read access to chain state is interrupted. | Operate at least two Horizon instances. Implement a fallback read path from the scanner's local cache for validation during brief Horizon outages. |

---

## 8. Conclusion

Stellar and Soroban represent the most coherent match for OpenTix's operational requirements across finality latency, fee predictability, native asset compliance controls, payment rail access, and developer tooling. The combination of protocol-level asset authorization flags, deterministic sub-5-second finality, fee-sponsored transactions, first-party Horizon indexing, and a Rust-native contract environment reduces the surface area of custom engineering required while providing a strong, auditable ownership foundation.

Alternative networks are viable for teams with existing EVM or Solana expertise and different operational tolerance for fees and finality behavior. For a net-new system with a Rust backend, a compliance-first ownership model, and a consumer-facing fan experience where UX friction is a primary risk, Stellar with Soroban is the correct starting point.
