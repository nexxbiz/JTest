# Debug Report for test_complex_markdown.json

**Generated:** 2025-09-10 08:58:43
**Test File:** /tmp/test_complex_markdown.json

## Test Execution

---

### Test: Complex nested test with templates

**Status:** FAILED
**Duration:** 100ms

#### Steps

- **Step:** PASSED (51ms)
**Saved Values:**

| Action | Variable | Value |
|--------|----------|-------|
| Added | mainVar | "main-value" |
| Added | complexStructure | <details><summary>show JSON</summary><pre>{
  "users": [
    {
      "name": "Alice",
      "roles": [
        "admin",
        "user"
      ]
    },
    {
      "name": "Bob",
      "roles": [
        "user"
      ]
    }
  ],
  "config": {
    "enabled": true,
    "settings": {
      "timeout": 30000,
      "retries": 3
    }
  }
}</pre></details> |


- **Step:** FAILED (2ms)
- **Error:** Template 'test-template' not found


## Summary
- **Total tests:** 1
- **Passed:** 0
- **Failed:** 1

## Errors
- ERROR in Complex nested test with templates: Template 'test-template' not found

