//! OpenTix Rules Contract Sketch
//! This is pseudocode intended to communicate architecture, not production-ready Soroban code.

#![no_std]

use soroban_sdk::{contract, contractimpl, Address, BytesN, Env, Symbol};

#[derive(Clone)]
pub struct TransferRule {
    pub rule_id: BytesN<32>,
    pub organizer: Address,
    pub transferable: bool,
    pub resale_allowed: bool,
    pub original_price_cents: u64,
    pub max_markup_bps: u32,
    pub royalty_bps: u32,
    pub royalty_receiver: Address,
    pub resale_starts_at: Option<u64>,
    pub resale_ends_at: Option<u64>,
}

#[contract]
pub struct RulesContract;

#[contractimpl]
impl RulesContract {
    pub fn create_rule(env: Env, rule: TransferRule) {
        rule.organizer.require_auth();
        env.storage().persistent().set(&rule.rule_id, &rule);
        env.events().publish((Symbol::new(&env, "rule_created"),), rule.rule_id);
    }

    pub fn assert_transfer_allowed(env: Env, rule_id: BytesN<32>, resale_price_cents: Option<u64>) -> bool {
        let rule: TransferRule = env.storage().persistent().get(&rule_id).unwrap();
        let now = env.ledger().timestamp();

        if !rule.transferable {
            return false;
        }

        if let Some(price) = resale_price_cents {
            if !rule.resale_allowed {
                return false;
            }

            if let Some(starts_at) = rule.resale_starts_at {
                if now < starts_at {
                    return false;
                }
            }

            if let Some(ends_at) = rule.resale_ends_at {
                if now > ends_at {
                    return false;
                }
            }

            let max_price = rule.original_price_cents + ((rule.original_price_cents * rule.max_markup_bps as u64) / 10_000);
            if price > max_price {
                return false;
            }
        }

        true
    }

    pub fn calculate_royalty(env: Env, rule_id: BytesN<32>, resale_price_cents: u64) -> u64 {
        let rule: TransferRule = env.storage().persistent().get(&rule_id).unwrap();
        (resale_price_cents * rule.royalty_bps as u64) / 10_000
    }
}
