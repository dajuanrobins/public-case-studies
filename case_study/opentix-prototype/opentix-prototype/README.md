# OpenTix Concept Prototype

OpenTix is a blockchain-backed digital ticketing concept for organizers, teams, venues, and fans. This prototype demonstrates the core product loop: create an event, configure digital ticket tiers, sell tickets through a wallet-style checkout, mint a verifiable ticket receipt, and enforce resale/content rules.

## What is included

```text
opentix-prototype/
├── prototype/                       # Dependency-free browser prototype
│   ├── index.html
│   ├── styles.css
│   └── app.js
├── docs/                            # Product, architecture, and technical documentation
│   ├── 01-concept-and-need.md
│   ├── 02-product-requirements.md
│   ├── 03-architecture.md
│   ├── 04-smart-contract-design.md
│   ├── 05-api-and-eventing.md
│   ├── 06-data-model.md
│   ├── 07-security-and-trust.md
│   ├── 08-roadmap.md
│   └── diagrams/
│       ├── system-context.mmd
│       └── transaction-flow.mmd
├── backend/api/openapi.yaml          # Example API surface
└── contracts/soroban-pseudocode/     # Soroban-style contract sketches
    ├── event_contract.rs
    ├── rules_contract.rs
    └── ticket_contract.rs
```

## How to run the prototype

No build step is required.

Open this file in a browser:

```text
prototype/index.html
```

The prototype uses plain HTML, CSS, and JavaScript so it can be viewed immediately, zipped, shared, or hosted as a static demo.

## What the demo shows

- Event discovery and selection
- Ticket tiers with supply, pricing, and benefits
- Mock wallet checkout
- Mock on-chain receipt generation
- Ticket ownership and QR-style verification panel
- Organizer ticket designer controls
- Resale royalty and transfer policy simulation
- Season/multi-event listing concept
- Ledger activity timeline

## Prototype disclaimer

This is a concept prototype, not production software. It does not connect to Stellar, Soroban, a real wallet, real payment rails, or a backend API. The included documentation describes how the concept would become a production-grade system.
