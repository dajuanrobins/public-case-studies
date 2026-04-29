use anyhow::{Context, Result};
use distributed_audit_observability_pipeline::{normalize_events_lenient, summarize, AuditEvent};
use std::{env, fs};

fn main() -> Result<()> {
    let Some(input_path) = env::args().nth(1) else {
        anyhow::bail!("Usage: cargo run -- <events.json>");
    };

    let raw = fs::read_to_string(&input_path)
        .with_context(|| format!("failed to read '{input_path}'"))?;

    let events: Vec<AuditEvent> = serde_json::from_str(&raw)
        .context("invalid event JSON — expected an array of audit event objects")?;

    let result = normalize_events_lenient(events);

    if !result.rejected.is_empty() {
        for (event, err) in &result.rejected {
            eprintln!("warning: rejected event '{}': {err}", event.event_id);
        }
    }

    let summary = summarize(&result.normalized);
    println!("{}", serde_json::to_string_pretty(&summary)?);
    Ok(())
}
