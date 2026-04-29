use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};
use std::collections::BTreeMap;

/// Severity is intentionally coarse. Operational systems need stable buckets
/// before they need perfect taxonomies.
///
/// The declaration order is meaningful: `PartialOrd`/`Ord` are derived, so
/// Info < Warning < Error < Critical holds by definition. This is intentional.
#[derive(Debug, Clone, Copy, Serialize, Deserialize, PartialEq, Eq, PartialOrd, Ord)]
#[serde(rename_all = "snake_case")]
pub enum EventSeverity {
    Info,
    Warning,
    Error,
    Critical,
}

/// Raw audit event accepted by the pipeline.
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct AuditEvent {
    pub event_id: String,
    pub timestamp: DateTime<Utc>,
    pub service: String,
    /// Identifies the human or system actor. `None` indicates an anonymous or
    /// system-initiated action, which elevates risk scoring.
    pub actor_id: Option<String>,
    pub action: String,
    pub resource: String,
    pub result: String,
    pub severity: EventSeverity,
    /// Propagated from upstream callers for cross-service event correlation.
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub correlation_id: Option<String>,
    #[serde(default)]
    pub attributes: BTreeMap<String, serde_json::Value>,
}

/// Stable event shape emitted after validation, normalization, and redaction.
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct NormalizedEvent {
    pub event_id: String,
    pub timestamp: DateTime<Utc>,
    pub service: String,
    pub actor_id: Option<String>,
    pub action: String,
    pub resource: String,
    pub result: String,
    pub severity: EventSeverity,
    pub risk_score: u8,
    /// Preserved from the raw event for cross-service correlation.
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub correlation_id: Option<String>,
    pub attributes: BTreeMap<String, serde_json::Value>,
}
