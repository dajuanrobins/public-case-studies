use serde_json::Value;
use std::collections::BTreeMap;

const SENSITIVE_KEY_FRAGMENTS: &[&str] = &[
    "password",
    "token",
    "secret",
    "api_key",
    "apikey",
    "ssn",
    "credit_card",
    "card_number",
];

/// Redacts sensitive fields by key name. In production, this layer would also
/// support schema-driven redaction and value-pattern detection.
pub fn redact_attributes(attributes: &BTreeMap<String, Value>) -> BTreeMap<String, Value> {
    attributes
        .iter()
        .map(|(key, value)| {
            if is_sensitive_key(key) {
                (key.clone(), Value::String("[REDACTED]".to_string()))
            } else {
                (key.clone(), value.clone())
            }
        })
        .collect()
}

fn is_sensitive_key(key: &str) -> bool {
    let normalized = key.to_lowercase();
    SENSITIVE_KEY_FRAGMENTS
        .iter()
        .any(|fragment| normalized.contains(fragment))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn identifies_sensitive_keys_case_insensitively() {
        assert!(is_sensitive_key("Payment_Token"));
        assert!(is_sensitive_key("API_KEY"));
        assert!(!is_sensitive_key("request_id"));
    }

    #[test]
    fn redacts_all_known_sensitive_fragments() {
        for fragment in &["password", "token", "secret", "api_key", "ssn", "credit_card"] {
            assert!(is_sensitive_key(fragment), "expected '{fragment}' to be sensitive");
            assert!(
                is_sensitive_key(&fragment.to_uppercase()),
                "expected uppercase '{fragment}' to be sensitive"
            );
        }
    }

    #[test]
    fn non_sensitive_keys_are_preserved() {
        for key in &["request_id", "user_agent", "region", "retry_count", "error_code"] {
            assert!(!is_sensitive_key(key), "'{key}' should not be redacted");
        }
    }

    #[test]
    fn redact_attributes_preserves_non_sensitive_values() {
        let mut attrs = BTreeMap::new();
        attrs.insert("request_id".to_string(), Value::String("req-999".to_string()));
        let result = redact_attributes(&attrs);
        assert_eq!(result["request_id"], Value::String("req-999".to_string()));
    }

    #[test]
    fn redact_attributes_replaces_sensitive_value_regardless_of_type() {
        let mut attrs = BTreeMap::new();
        attrs.insert("password".to_string(), Value::Number(serde_json::Number::from(42)));
        let result = redact_attributes(&attrs);
        assert_eq!(result["password"], Value::String("[REDACTED]".to_string()));
    }

    #[test]
    fn redact_attributes_on_empty_map_returns_empty() {
        let attrs = BTreeMap::new();
        let result = redact_attributes(&attrs);
        assert!(result.is_empty());
    }
}
