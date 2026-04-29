# Operational Model

## Event lifecycle

1. A service emits a raw operational event with an optional `correlation_id` for cross-service tracing.
2. The pipeline validates required identity fields (`event_id`, `service`, `action`, `resource`).
3. The event is normalized into a stable internal model.
4. Sensitive attributes are redacted by key name.
5. Severity, result outcome, and actor anonymity are combined into a risk score (0–100).
6. Diagnostic summaries are generated for humans and downstream tools.

## Batch processing strategies

The pipeline exposes two batch modes. The choice is a caller contract, not a pipeline implementation detail:

- **Fail-fast** (`normalize_events`): the first invalid event aborts the batch and returns a structured error. Use when the sink requires transactional all-or-nothing semantics.
- **Partial-success** (`normalize_events_lenient`): valid events are forwarded; rejected events are returned in a separate bucket with their originating event and error. This prevents valid events from being silently discarded when a single bad producer is present in the batch.

The tradeoff is explicit: fail-fast provides stronger producer feedback but can block healthy events behind a broken one. Partial-success maximizes throughput at the cost of requiring the caller to handle rejection routing.

## Safety guarantees in this showcase

| Guarantee | How it is handled |
|---|---|
| Required event identity | Fail-fast validation |
| Secret leakage reduction | Key-based redaction |
| Reviewability | Deterministic JSON output |
| Prioritization | Severity/result/anonymity risk scoring |
| Attribution tracing | `correlation_id` propagated through the normalized event |
| Rejection visibility | Rejected events returned with source data and error, never silently dropped |
| Maintainability | Small modules with direct tests |

## Non-goals

- This is not a full SIEM.
- This is not a distributed tracing platform.
- This is not tied to any proprietary product architecture.
