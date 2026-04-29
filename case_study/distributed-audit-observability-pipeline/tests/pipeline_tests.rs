use chrono::Utc;
use distributed_audit_observability_pipeline::{
    normalize_event, normalize_events, normalize_events_lenient, summarize, AuditEvent,
    EventSeverity, PipelineError,
};
use serde_json::json;
use std::collections::BTreeMap;

// ── helpers ─────────────────────────────────────────────────────────────────

fn valid_event(event_id: &str, severity: EventSeverity, result: &str) -> AuditEvent {
    AuditEvent {
        event_id: event_id.to_string(),
        timestamp: Utc::now(),
        service: "test-service".to_string(),
        actor_id: Some("user-1".to_string()),
        action: "test_action".to_string(),
        resource: "resource:1".to_string(),
        result: result.to_string(),
        severity,
        correlation_id: None,
        attributes: BTreeMap::new(),
    }
}

// ── redaction ────────────────────────────────────────────────────────────────

#[test]
fn redacts_sensitive_attributes_before_summary() {
    let mut attributes = BTreeMap::new();
    attributes.insert("api_key".to_string(), json!("secret-value"));
    attributes.insert("request_id".to_string(), json!("req-123"));

    let event = AuditEvent {
        event_id: "evt-1".to_string(),
        timestamp: Utc::now(),
        service: "identity-service".to_string(),
        actor_id: Some("user-1".to_string()),
        action: "token_exchange".to_string(),
        resource: "session".to_string(),
        result: "succeeded".to_string(),
        severity: EventSeverity::Info,
        correlation_id: None,
        attributes,
    };

    let normalized = normalize_event(event).expect("event should normalize");

    assert_eq!(normalized.attributes["api_key"], json!("[REDACTED]"));
    assert_eq!(normalized.attributes["request_id"], json!("req-123"));
}

#[test]
fn redacts_empty_attribute_map_without_panic() {
    let event = valid_event("evt-empty-attrs", EventSeverity::Info, "succeeded");
    let normalized = normalize_event(event).expect("event should normalize");
    assert!(normalized.attributes.is_empty());
}

// ── validation ───────────────────────────────────────────────────────────────

#[test]
fn rejects_events_without_required_identity() {
    let event = AuditEvent {
        event_id: " ".to_string(),
        timestamp: Utc::now(),
        service: "orders-api".to_string(),
        actor_id: None,
        action: "create_order".to_string(),
        resource: "order:1".to_string(),
        result: "succeeded".to_string(),
        severity: EventSeverity::Info,
        correlation_id: None,
        attributes: BTreeMap::new(),
    };

    let (_, error) = normalize_event(event).expect_err("missing event id should fail");
    assert_eq!(error, PipelineError::MissingEventId);
}

#[test]
fn rejects_blank_service() {
    let mut event = valid_event("evt-x", EventSeverity::Info, "succeeded");
    event.service = "   ".to_string();
    let (_, error) = normalize_event(event).expect_err("blank service should fail");
    assert_eq!(error, PipelineError::MissingService);
}

#[test]
fn rejects_blank_action() {
    let mut event = valid_event("evt-x", EventSeverity::Info, "succeeded");
    event.action = String::new();
    let (_, error) = normalize_event(event).expect_err("blank action should fail");
    assert_eq!(error, PipelineError::MissingAction);
}

#[test]
fn rejects_blank_resource() {
    let mut event = valid_event("evt-x", EventSeverity::Info, "succeeded");
    event.resource = String::new();
    let (_, error) = normalize_event(event).expect_err("blank resource should fail");
    assert_eq!(error, PipelineError::MissingResource);
}

// ── batch normalization ───────────────────────────────────────────────────────

#[test]
fn normalize_events_fails_fast_on_first_invalid() {
    let events = vec![
        valid_event("evt-ok", EventSeverity::Info, "succeeded"),
        AuditEvent {
            event_id: String::new(),
            timestamp: Utc::now(),
            service: "svc".to_string(),
            actor_id: None,
            action: "act".to_string(),
            resource: "res".to_string(),
            result: "succeeded".to_string(),
            severity: EventSeverity::Info,
            correlation_id: None,
            attributes: BTreeMap::new(),
        },
    ];

    let err = normalize_events(events).expect_err("batch should fail on invalid event");
    assert_eq!(err, PipelineError::MissingEventId);
}

#[test]
fn normalize_events_lenient_separates_valid_and_rejected() {
    let events = vec![
        valid_event("evt-good", EventSeverity::Info, "succeeded"),
        AuditEvent {
            event_id: String::new(),
            timestamp: Utc::now(),
            service: "svc".to_string(),
            actor_id: None,
            action: "act".to_string(),
            resource: "res".to_string(),
            result: "succeeded".to_string(),
            severity: EventSeverity::Info,
            correlation_id: None,
            attributes: BTreeMap::new(),
        },
        valid_event("evt-also-good", EventSeverity::Warning, "denied"),
    ];

    let result = normalize_events_lenient(events);
    assert_eq!(result.normalized.len(), 2);
    assert_eq!(result.rejected.len(), 1);
    assert_eq!(result.rejected[0].1, PipelineError::MissingEventId);
}

// ── risk scoring ──────────────────────────────────────────────────────────────

#[test]
fn summary_highlights_high_risk_events() {
    let event = AuditEvent {
        event_id: "evt-critical".to_string(),
        timestamp: Utc::now(),
        service: "billing-worker".to_string(),
        actor_id: None,
        action: "settlement_job".to_string(),
        resource: "batch:today".to_string(),
        result: "failed".to_string(),
        severity: EventSeverity::Critical,
        correlation_id: None,
        attributes: BTreeMap::new(),
    };

    let normalized = normalize_event(event).expect("event should normalize");
    let summary = summarize(&[normalized]);

    assert_eq!(summary.total_events, 1);
    assert_eq!(summary.high_risk_events, vec!["evt-critical"]);
    assert_eq!(summary.highest_risk_score, Some(100));
}

#[test]
fn risk_score_saturates_at_100_not_beyond() {
    // Critical (95) + failure (+10) + anonymous (+5) = 110, must cap at 100.
    let event = AuditEvent {
        event_id: "evt-sat".to_string(),
        timestamp: Utc::now(),
        service: "svc".to_string(),
        actor_id: None, // anonymous — adds anonymous_penalty
        action: "act".to_string(),
        resource: "res".to_string(),
        result: "failed".to_string(), // failure signal
        severity: EventSeverity::Critical,
        correlation_id: None,
        attributes: BTreeMap::new(),
    };

    let normalized = normalize_event(event).expect("event should normalize");
    assert_eq!(normalized.risk_score, 100, "score must not exceed 100");
}

#[test]
fn anonymous_actor_increases_risk_score() {
    let with_actor = {
        let mut e = valid_event("evt-actor", EventSeverity::Warning, "succeeded");
        e.actor_id = Some("user-known".to_string());
        normalize_event(e).unwrap()
    };
    let without_actor = {
        let mut e = valid_event("evt-anon", EventSeverity::Warning, "succeeded");
        e.actor_id = None;
        normalize_event(e).unwrap()
    };

    assert!(
        without_actor.risk_score > with_actor.risk_score,
        "anonymous actor should produce a higher risk score"
    );
}

#[test]
fn info_success_event_has_lowest_risk_score() {
    let event = valid_event("evt-low", EventSeverity::Info, "succeeded");
    let normalized = normalize_event(event).expect("event should normalize");
    assert_eq!(normalized.risk_score, 10);
}

// ── correlation id ────────────────────────────────────────────────────────────

#[test]
fn correlation_id_is_propagated_to_normalized_event() {
    let mut event = valid_event("evt-corr", EventSeverity::Info, "succeeded");
    event.correlation_id = Some("trace-abc-123".to_string());

    let normalized = normalize_event(event).expect("event should normalize");
    assert_eq!(normalized.correlation_id.as_deref(), Some("trace-abc-123"));
}

#[test]
fn absent_correlation_id_is_none_in_normalized_event() {
    let event = valid_event("evt-no-corr", EventSeverity::Info, "succeeded");
    let normalized = normalize_event(event).expect("event should normalize");
    assert!(normalized.correlation_id.is_none());
}

// ── summary ───────────────────────────────────────────────────────────────────

#[test]
fn summary_by_severity_keys_are_lowercase_snake_case() {
    let events = vec![
        normalize_event(valid_event("e1", EventSeverity::Info, "ok")).unwrap(),
        normalize_event(valid_event("e2", EventSeverity::Critical, "failed")).unwrap(),
    ];
    let summary = summarize(&events);

    assert!(summary.by_severity.contains_key("info"), "expected 'info' key");
    assert!(
        summary.by_severity.contains_key("critical"),
        "expected 'critical' key"
    );
    assert!(!summary.by_severity.contains_key("Info"), "no capitalized keys");
    assert!(!summary.by_severity.contains_key("Critical"), "no capitalized keys");
}

#[test]
fn summary_of_empty_slice_is_zero_counts() {
    let summary = summarize(&[]);
    assert_eq!(summary.total_events, 0);
    assert!(summary.high_risk_events.is_empty());
    assert!(summary.highest_risk_score.is_none());
}

