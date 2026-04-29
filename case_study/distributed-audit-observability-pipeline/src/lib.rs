pub mod event;
pub mod pipeline;
pub mod redaction;
pub mod summary;

pub use event::{AuditEvent, EventSeverity, NormalizedEvent};
pub use pipeline::{normalize_event, normalize_events, normalize_events_lenient, NormalizeResult, PipelineError};
pub use summary::{summarize, DiagnosticSummary};
