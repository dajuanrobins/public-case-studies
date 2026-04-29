//! OpenTix Event Contract Sketch
//! This is pseudocode intended to communicate architecture, not production-ready Soroban code.

#![no_std]

use soroban_sdk::{contract, contractimpl, Address, BytesN, Env, Map, Symbol};

#[derive(Clone)]
pub enum EventStatus {
    Draft,
    Published,
    Closed,
    Canceled,
}

#[derive(Clone)]
pub struct EventState {
    pub event_id: BytesN<32>,
    pub organizer: Address,
    pub royalty_receiver: Address,
    pub status: EventStatus,
    pub created_at: u64,
    pub updated_at: u64,
}

#[derive(Clone)]
pub struct TierState {
    pub tier_id: BytesN<32>,
    pub event_id: BytesN<32>,
    pub supply_cap: u32,
    pub minted_count: u32,
    pub rule_id: BytesN<32>,
}

#[contract]
pub struct EventContract;

#[contractimpl]
impl EventContract {
    pub fn register_event(
        env: Env,
        event_id: BytesN<32>,
        organizer: Address,
        royalty_receiver: Address,
    ) {
        organizer.require_auth();

        let now = env.ledger().timestamp();
        let state = EventState {
            event_id: event_id.clone(),
            organizer,
            royalty_receiver,
            status: EventStatus::Draft,
            created_at: now,
            updated_at: now,
        };

        env.storage().persistent().set(&event_id, &state);
        env.events().publish((Symbol::new(&env, "event_registered"),), event_id);
    }

    pub fn create_tier(
        env: Env,
        event_id: BytesN<32>,
        tier_id: BytesN<32>,
        supply_cap: u32,
        rule_id: BytesN<32>,
    ) {
        let mut event: EventState = env.storage().persistent().get(&event_id).unwrap();
        event.organizer.require_auth();

        let tier = TierState {
            tier_id: tier_id.clone(),
            event_id: event_id.clone(),
            supply_cap,
            minted_count: 0,
            rule_id,
        };

        env.storage().persistent().set(&tier_id, &tier);
        env.events().publish((Symbol::new(&env, "tier_created"),), (event_id, tier_id));
    }

    pub fn publish_event(env: Env, event_id: BytesN<32>) {
        let mut event: EventState = env.storage().persistent().get(&event_id).unwrap();
        event.organizer.require_auth();

        event.status = EventStatus::Published;
        event.updated_at = env.ledger().timestamp();

        env.storage().persistent().set(&event_id, &event);
        env.events().publish((Symbol::new(&env, "event_published"),), event_id);
    }
}
