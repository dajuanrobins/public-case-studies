use crate::event::{AuditEvent, EventSeverity, NormalizedEvent};
use crate::redaction::redact_attributes;
use thiserror::Error;

#[derive(Debug, Error, PartialEq, Eq)]
pub enum PipelineError {
    #[error("event_id is required")]
    MissingEventId,
    #[error("service is required")]
    MissingService,
    #[error("action is required")]
    MissingAction,
    #[error("resource is required")]
    MissingResource,
}

/// Result of a lenient batch normalization. Valid events are collected into
/// `normalized`; invalid events are paired with their errors in `rejected`.
/// Callers decide how to handle each bucket — typically logging rejected events
/// and continuing with the normalized ones.
pub struct NormalizeResult {
    pub normalized: Vec<NormalizedEvent>,
    pub rejected: Vec<(AuditEvent, PipelineError)>,
}

/// Normalizes a batch of events. Fail-fast: the first invalid event aborts the
/// entire batch and returns its error. Use `normalize_events_lenient` when
/// partial-success semantics are preferred.
pub fn normalize_events(events: Vec<AuditEvent>) -> Result<Vec<NormalizedEvent>, PipelineError> {
    let mut result = Vec::with_capacity(events.len());
    for event in events {
        match normalize_event(event) {
            Ok(n) => result.push(n),
            Err((_, e)) => return Err(e),
        }
    }
    Ok(result)
}

/// Normalizes a batch of events with partial-success semantics. Each event is
/// processed independently; invalid events are collected into the `rejected`
/// bucket rather than aborting the entire batch. This is the preferred strategy
/// for audit pipelines where silently discarding valid events is worse than
/// forwarding a pipeline error to the caller.
pub fn normalize_events_lenient(events: Vec<AuditEvent>) -> NormalizeResult {
    let mut normalized = Vec::with_capacity(events.len());
    let mut rejected = Vec::new();

    for event in events {
        match normalize_event(event) {
            Ok(n) => normalized.push(n),
            Err(pair) => rejected.push(pair),
        }
    }

    NormalizeResult { normalized, rejected }
}

/// Validates and normalizes a single event. On validation failure the original
/// event is returned alongside the error so callers can log or re-queue it
/// without losing the source data.
pub fn normalize_event(event: AuditEvent) -> Result<NormalizedEvent, (AuditEvent, PipelineError)> {
    if let Err(e) = validate(&event) {
        return Err((event, e));
    }

    let risk_score = risk_score(&event);

    Ok(NormalizedEvent {
        event_id: event.event_id,
        timestamp: event.timestamp,
        service: event.service,
        actor_id: event.actor_id,
        action: event.action,
        resource: event.resource,
        result: event.result,
        severity: event.severity,
        risk_score,
        correlation_id: event.correlation_id,
        attributes: redact_attributes(&event.attributes),
    })
}

fn validate(event: &AuditEvent) -> Result<(), PipelineError> {
    require_non_empty(&event.event_id, PipelineError::MissingEventId)?;
    require_non_empty(&event.service, PipelineError::MissingService)?;
    require_non_empty(&event.action, PipelineError::MissingAction)?;
    require_non_empty(&event.resource, PipelineError::MissingResource)?;
    Ok(())
}

fn require_non_empty(value: &str, error: PipelineError) -> Result<(), PipelineError> {
    if value.trim().is_empty() {
        Err(error)
    } else {
        Ok(())
    }
}

fn risk_score(event: &AuditEvent) -> u8 {
    let base: u8 = match event.severity {
        EventSeverity::Info => 10,
        EventSeverity::Warning => 35,
        EventSeverity::Error => 70,
        EventSeverity::Critical => 95,
    };

    let result = event.result.to_lowercase();
    let failure_signal = matches!(result.as_str(), "denied" | "failed" | "timeout" | "rejected");

    // An anonymous actor (no actor_id) on a sensitive action raises the risk
    // slightly because attribution is unavailable for incident investigation.
    let anonymous_penalty: u8 = if event.actor_id.is_none() { 5 } else { 0 };

    let after_failure = if failure_signal {
        base.saturating_add(10)
    } else {
        base
    };

    after_failure.saturating_add(anonymous_penalty).min(100)
}
