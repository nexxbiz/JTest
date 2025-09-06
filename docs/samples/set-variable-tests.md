# Debug Report for set-variable-tests.json

**Generated:** 2025-09-05 20:21:58
**Test File:** .\set-variable-tests.json

## Test Execution

---

### Test: Get tokens

**Status:** PASSED
**Duration:** 199ms

#### Steps

- **Step:** PASSED (198ms)
  - **Saved Values:**

  - **Template Steps:**
    - **HTTP POST httsp://wf.api.com/elsa/api/identity/login:** PASSED (197ms)
      (**Saved:** Added accessToken, refreshToken, **Assertions:** 3 passed)


<details><summary>Show saved variables</summary>

- **Added:** accessToken = "masked"
- **Added:** refreshToken = "masked"

</details>





---

### Test: Execute workflow instance  for set-variable activity

**Status:** FAILED
**Duration:** 212ms
**Dataset:** small-text

#### Steps

- **Step:** FAILED (212ms)
  - **Error:** Template step failed: One or more assertions failed - Expected '200' but got '404'-Value does not exist or is null/empty
  - **Assertions:**
    - FAILED: Activity condition should match expected value for small test
      (**Actual:** , **Expected:** a, **Error:** Expected 'a' but got '')
    - FAILED: Workflow instance ID should exist for small test
      (**Actual:** , **Error:** Value does not exist or is null/empty)



---

### Test: Execute workflow instance  for set-variable activity

**Status:** FAILED
**Duration:** 55ms
**Dataset:** large-text

#### Steps

- **Step:** FAILED (54ms)
  - **Error:** Template step failed: One or more assertions failed - Expected '200' but got '404'-Value does not exist or is null/empty
  - **Assertions:**
    - FAILED: Activity condition should match expected value for small test
      (**Actual:** , **Expected:** aasdfghjklkjhgfdsdfghjkljhgfdrtyutyifudsiofuviosdugfiubn  gfiouigfduigufdi guif ugfuigufodg fidgiofdui gufidusigfudiougfidug ifdu ig ufdiugfiugiufdigufiduiud  gfdi gufdiu  gofdusgi fuigufdug fdois, **Error:** Expected 'aasdfghjklkjhgfdsdfghjkljhgfdrtyutyifudsiofuviosdugfiubn  gfiouigfduigufdi guif ugfuigufodg fidgiofdui gufidusigfudiougfidug ifdu ig ufdiugfiugiufdigufiduiud  gfdi gufdiu  gofdusgi fuigufdug fdois' but got '')
    - FAILED: Workflow instance ID should exist for small test
      (**Actual:** , **Error:** Value does not exist or is null/empty)



---

### Test: Execute workflow instance  for set-variable activity

**Status:** FAILED
**Duration:** 61ms
**Dataset:** special texts

#### Steps

- **Step:** FAILED (60ms)
  - **Error:** Template step failed: One or more assertions failed - Expected '200' but got '404'-Value does not exist or is null/empty
  - **Assertions:**
    - FAILED: Activity condition should match expected value for special text
      (**Actual:** , **Expected:** ✨ 𝓼𝓲𝓵𝓪 (сила) 💪 = śvěт 🌍 + šаги 🚶 + ѕердце ❤️, **Error:** Expected '✨ 𝓼𝓲𝓵𝓪 (сила) 💪 = śvěт 🌍 + šаги 🚶 + ѕердце ❤️' but got '')
    - FAILED: Workflow instance ID should exist for special text
      (**Actual:** , **Error:** Value does not exist or is null/empty)



## Summary
- **Total tests:** 4
- **Passed:** 1
- **Failed:** 3

## Errors
- ERROR in Execute workflow instance  for set-variable activity: Template step failed: One or more assertions failed - Expected '200' but got '404'-Value does not exist or is null/empty
- ERROR in Execute workflow instance  for set-variable activity: Template step failed: One or more assertions failed - Expected '200' but got '404'-Value does not exist or is null/empty
- ERROR in Execute workflow instance  for set-variable activity: Template step failed: One or more assertions failed - Expected '200' but got '404'-Value does not exist or is null/empty

