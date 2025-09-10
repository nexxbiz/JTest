# Debug Report for test_simple_complex.json

**Generated:** 2025-09-10 08:59:15
**Test File:** /tmp/test_simple_complex.json

## Test Execution

---

### Test: Complex nested structures test

**Status:** PASSED
**Duration:** 123ms

#### Steps

- **Step:** PASSED (52ms)
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
| Added | arrayOfObjects | <details><summary>show JSON</summary><pre>[
  {
    "id": 1,
    "data": {
      "value": "test1"
    }
  },
  {
    "id": 2,
    "data": {
      "value": "test2"
    }
  }
]</pre></details> |


- **Step:** PASSED (25ms)
**Saved Values:**

| Action | Variable | Value |
|--------|----------|-------|
| Added | secondStep | "completed" |
| Added | metadata | <details><summary>show JSON</summary><pre>{
  "timestamp": "2025-01-01",
  "version": "1.0"
}</pre></details> |



## Summary
- **Total tests:** 1
- **Passed:** 1
- **Failed:** 0

