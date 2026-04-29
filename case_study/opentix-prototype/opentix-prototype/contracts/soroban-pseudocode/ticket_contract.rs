//! OpenTix Ticket Contract Sketch
//! This is pseudocode intended to communicate architecture, not production-ready Soroban code.

#![no_std]

use soroban_sdk::{contract, contractimpl, Address, BytesN, Env, Symbol};

#[derive(Clone, PartialEq)]
pub enum TicketStatus {
    Minted,
    Redeemed,
    Frozen,
    Canceled,
}

#[derive(Clone)]
pub struct TicketState {
    pub ticket_id: BytesN<32>,
    pub event_id: BytesN<32>,
    pub tier_id: BytesN<32>,
    pub owner: Address,
    pub status: TicketStatus,
    pub rule_id: BytesN<32>,
    pub minted_at: u64,
}

#[contract]
pub struct TicketContract;

#[contractimpl]
impl TicketContract {
    pub fn mint_ticket(
        env: Env,
        mint_authority: Address,
        ticket_id: BytesN<32>,
        event_id: BytesN<32>,
        tier_id: BytesN<32>,
        rule_id: BytesN<32>,
        buyer: Address,
    ) {
        mint_authority.require_auth();

        // Production implementation should validate:
        // - event is published
        // - tier belongs to event
        // - tier supply is not exhausted
        // - mint_authority is approved for the event

        let ticket = TicketState {
            ticket_id: ticket_id.clone(),
            event_id,
            tier_id,
            owner: buyer,
            status: TicketStatus::Minted,
            rule_id,
            minted_at: env.ledger().timestamp(),
        };

        env.storage().persistent().set(&ticket_id, &ticket);
        env.events().publish((Symbol::new(&env, "ticket_minted"),), ticket_id);
    }

    pub fn transfer_ticket(env: Env, ticket_id: BytesN<32>, to: Address, resale_price_cents: Option<u64>) {
        let mut ticket: TicketState = env.storage().persistent().get(&ticket_id).unwrap();
        ticket.owner.require_auth();

        if ticket.status != TicketStatus::Minted {
            panic!("ticket is not transferable");
        }

        // Production implementation should call RulesContract before transfer.
        // RulesContract validates markup limits, transfer windows, and royalty requirements.

        let from = ticket.owner.clone();
        ticket.owner = to.clone();
        env.storage().persistent().set(&ticket_id, &ticket);
        env.events().publish((Symbol::new(&env, "ticket_transferred"),), (ticket_id, from, to, resale_price_cents));
    }

    pub fn redeem_ticket(env: Env, scanner: Address, ticket_id: BytesN<32>) {
        scanner.require_auth();

        let mut ticket: TicketState = env.storage().persistent().get(&ticket_id).unwrap();
        if ticket.status != TicketStatus::Minted {
            panic!("ticket already used or invalid");
        }

        ticket.status = TicketStatus::Redeemed;
        env.storage().persistent().set(&ticket_id, &ticket);
        env.events().publish((Symbol::new(&env, "ticket_redeemed"),), ticket_id);
    }
}
