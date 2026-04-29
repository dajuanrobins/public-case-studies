use crate::event::{EventSeverity, NormalizedEvent};
use serde::Serialize;
use std::collections::BTreeMap;

/// `by_severity` keys are fixed lowercase strings matching `EventSeverity`'s
/// serialized form so downstream consumers can rely on stable key names.
#[derive(Debug, Serialize, PartialEq, Eq)]
pub struct DiagnosticSummary {
    pub total_events: usize,
    pub by_service: BTreeMap<String, usize>,
    /// Keys are the canonical lowercase severity labels: `info`, `warning`,
    /// `error`, `critical`. Using `EventSeverity` serialization directly
    /// ensures no key drift between the enum and the summary output.
    pub by_severity: BTreeMap<String, usize>,
    pub high_risk_events: Vec<String>,
    pub highest_risk_score: Option<u8>,
}

pub fn summarize(events: &[NormalizedEvent]) -> DiagnosticSummary {
    let mut by_service: BTreeMap<String, usize> = BTreeMap::new();
    let mut by_severity: BTreeMap<String, usize> = BTreeMap::new();
    let mut high_risk_events = Vec::new();
    let mut highest_risk_score: Option<u8> = None;

    for event in events {
        *by_service.entry(event.service.clone()).or_insert(0) += 1;
        // Serialize through the enum to guarantee key consistency with the
        // canonical serde representation (`rename_all = "snake_case"`).
        let severity_key = serde_json::to_value(event.severity)
            .ok()
            .and_then(|v| v.as_str().map(String::from))
            .unwrap_or_else(|| severity_label(event.severity).to_string());
        *by_severity.entry(severity_key).or_insert(0) += 1;

        if event.risk_score >= 80 {
            high_risk_events.push(event.event_id.clone());
        }

        highest_risk_score = Some(highest_risk_score.unwrap_or(0).max(event.risk_score));
    }

    DiagnosticSummary {
        total_events: events.len(),
        by_service,
        by_severity,
        high_risk_events,
        highest_risk_score,
    }
}

fn severity_label(severity: EventSeverity) -> &'static str {
    match severity {
        EventSeverity::Info => "info",
        EventSeverity::Warning => "warning",
        EventSeverity::Error => "error",
        EventSeverity::Critical => "critical",
    }
}
