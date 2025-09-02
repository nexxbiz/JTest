# JTEST Debug Log

**Test File:** if-else-test.json
**Verbosity:** Detailed
**Timestamp:** 2025-08-31 20:39:47 UTC

---

## Test 1, Step 1: HttpStep
**Step ID:** execute-workflow
**Step Type:** HttpStep
**Enabled:** True

**Result:** ✅ Success
**Duration:** 332,74ms

📋 **Context Changes:**

**✅ Added:**
- `$.execute-workflow` = {object with 5 properties}
- `$.workflowInstanceId` = "b92e57abae5e5873"

**🔄 Modified:**
- `$.this`: {object with 0 properties} → {object with 3 properties}

💡 **For Assertions:** You can now reference these JSONPath expressions:
- `$.execute-workflow` or `{{ $.execute-workflow }}`
  - Example: `$.execute-workflow.status`
- `$.workflowInstanceId` or `{{ $.workflowInstanceId }}`
- `$.this` or `{{ $.this }}`

<details>
<summary>📋 Runtime Context (Click to expand)</summary>

```json
{
  "env": {
    "baseUrl": "https://api.totest.com",
    "tokenUrl": "https://api.totest.com",
    "username": "myUsername",
    "password": "myPassword"
  },
  "globals": {},
  "case": {},
  "ctx": {},
  "this": {
    "status": 200,
    "headers": [
      {
        "name": "Date",
        "value": "Sun, 31 Aug 2025 20:39:47 GMT"
      },
      {
        "name": "Transfer-Encoding",
        "value": "chunked"
      },
      {
        "name": "Connection",
        "value": "keep-alive"
      },
      {
        "name": "Strict-Transport-Security",
        "value": "max-age=31536000; includeSubDomains"
      },
      {
        "name": "Access-Control-Allow-Origin",
        "value": "https://api.totest.com"
      },
      {
        "name": "Content-Security-Policy",
        "value": "default-src 'self' *.nexxbiz.tech; script-src 'self' 'unsafe-inline' 'unsafe-eval' *.nexxbiz.tech js.monitor.azure.com; style-src 'self' 'unsafe-inline' *.nexxbiz.tech *.googleapis.com; img-src 'self' data: *.nexxbiz.tech; media-src 'self' data: *.nexxbiz.tech; font-src 'self' data: *.nexxbiz.tech fonts.gstatic.com; connect-src 'self' *.nexxbiz.tech https://dc.services.visualstudio.com https://js.monitor.azure.com;"
      },
      {
        "name": "Referrer-Policy",
        "value": "strict-origin-when-cross-origin"
      },
      {
        "name": "X-Content-Type-Options",
        "value": "nosniff"
      },
      {
        "name": "X-Frame-Options",
        "value": "SAMEORIGIN"
      },
      {
        "name": "X-XSS-Protection",
        "value": "1; mode=block"
      },
      {
        "name": "Content-Type",
        "value": "application/json; charset=utf-8"
      }
    ],
    "body": {
      "request": {
        "isTrue": false
      },
      "workflowInstanceId": "b92e57abae5e5873"
    }
  },
  "now": {
    "iso": "2025-08-31T20:39:47Z"
  },
  "random": {
    "uuid": "143601d8-5f67-4aab-9b49-53a557ce0837"
  },
  "execute-workflow": {
    "status": 200,
    "headers": [
      {
        "name": "Date",
        "value": "Sun, 31 Aug 2025 20:39:47 GMT"
      },
      {
        "name": "Transfer-Encoding",
        "value": "chunked"
      },
      {
        "name": "Connection",
        "value": "keep-alive"
      },
      {
        "name": "Strict-Transport-Security",
        "value": "max-age=31536000; includeSubDomains"
      },
      {
        "name": "Access-Control-Allow-Origin",
        "value": "https://api.totest.com"
      },
      {
        "name": "Content-Security-Policy",
        "value": "default-src 'self' *.nexxbiz.tech; script-src 'self' 'unsafe-inline' 'unsafe-eval' *.nexxbiz.tech js.monitor.azure.com; style-src 'self' 'unsafe-inline' *.nexxbiz.tech *.googleapis.com; img-src 'self' data: *.nexxbiz.tech; media-src 'self' data: *.nexxbiz.tech; font-src 'self' data: *.nexxbiz.tech fonts.gstatic.com; connect-src 'self' *.nexxbiz.tech https://dc.services.visualstudio.com https://js.monitor.azure.com;"
      },
      {
        "name": "Referrer-Policy",
        "value": "strict-origin-when-cross-origin"
      },
      {
        "name": "X-Content-Type-Options",
        "value": "nosniff"
      },
      {
        "name": "X-Frame-Options",
        "value": "SAMEORIGIN"
      },
      {
        "name": "X-XSS-Protection",
        "value": "1; mode=block"
      },
      {
        "name": "Content-Type",
        "value": "application/json; charset=utf-8"
      }
    ],
    "body": {
      "request": {
        "isTrue": false
      },
      "workflowInstanceId": "b92e57abae5e5873"
    },
    "last": {
      "status": 200,
      "headers": [
        {
          "name": "Date",
          "value": "Sun, 31 Aug 2025 20:39:47 GMT"
        },
        {
          "name": "Transfer-Encoding",
          "value": "chunked"
        },
        {
          "name": "Connection",
          "value": "keep-alive"
        },
        {
          "name": "Strict-Transport-Security",
          "value": "max-age=31536000; includeSubDomains"
        },
        {
          "name": "Access-Control-Allow-Origin",
          "value": "https://api.totest.com"
        },
        {
          "name": "Content-Security-Policy",
          "value": "default-src 'self' *.nexxbiz.tech; script-src 'self' 'unsafe-inline' 'unsafe-eval' *.nexxbiz.tech js.monitor.azure.com; style-src 'self' 'unsafe-inline' *.nexxbiz.tech *.googleapis.com; img-src 'self' data: *.nexxbiz.tech; media-src 'self' data: *.nexxbiz.tech; font-src 'self' data: *.nexxbiz.tech fonts.gstatic.com; connect-src 'self' *.nexxbiz.tech https://dc.services.visualstudio.com https://js.monitor.azure.com;"
        },
        {
          "name": "Referrer-Policy",
          "value": "strict-origin-when-cross-origin"
        },
        {
          "name": "X-Content-Type-Options",
          "value": "nosniff"
        },
        {
          "name": "X-Frame-Options",
          "value": "SAMEORIGIN"
        },
        {
          "name": "X-XSS-Protection",
          "value": "1; mode=block"
        },
        {
          "name": "Content-Type",
          "value": "application/json; charset=utf-8"
        }
      ],
      "body": {
        "request": {
          "isTrue": false
        },
        "workflowInstanceId": "b92e57abae5e5873"
      }
    },
    "history": [
      {
        "status": 200,
        "headers": [
          {
            "name": "Date",
            "value": "Sun, 31 Aug 2025 20:39:47 GMT"
          },
          {
            "name": "Transfer-Encoding",
            "value": "chunked"
          },
          {
            "name": "Connection",
            "value": "keep-alive"
          },
          {
            "name": "Strict-Transport-Security",
            "value": "max-age=31536000; includeSubDomains"
          },
          {
            "name": "Access-Control-Allow-Origin",
            "value": "https://api.totest.com"
          },
          {
            "name": "Content-Security-Policy",
            "value": "default-src 'self' *.nexxbiz.tech; script-src 'self' 'unsafe-inline' 'unsafe-eval' *.nexxbiz.tech js.monitor.azure.com; style-src 'self' 'unsafe-inline' *.nexxbiz.tech *.googleapis.com; img-src 'self' data: *.nexxbiz.tech; media-src 'self' data: *.nexxbiz.tech; font-src 'self' data: *.nexxbiz.tech fonts.gstatic.com; connect-src 'self' *.nexxbiz.tech https://dc.services.visualstudio.com https://js.monitor.azure.com;"
          },
          {
            "name": "Referrer-Policy",
            "value": "strict-origin-when-cross-origin"
          },
          {
            "name": "X-Content-Type-Options",
            "value": "nosniff"
          },
          {
            "name": "X-Frame-Options",
            "value": "SAMEORIGIN"
          },
          {
            "name": "X-XSS-Protection",
            "value": "1; mode=block"
          },
          {
            "name": "Content-Type",
            "value": "application/json; charset=utf-8"
          }
        ],
        "body": {
          "request": {
            "isTrue": false
          },
          "workflowInstanceId": "b92e57abae5e5873"
        }
      }
    ]
  },
  "workflowInstanceId": "b92e57abae5e5873"
}
```

</details>

---

## Test 1, Step 2: UseStep
**Step Type:** UseStep
**Enabled:** True

**Result:** ✅ Success
**Duration:** 359,92ms

📋 **Context Changes:** None

<details>
<summary>📋 Runtime Context (Click to expand)</summary>

```json
{
  "env": {
    "baseUrl": "https://api.totest.com",
    "tokenUrl": "https://api.totest.com/token",
    "username": "nexxbizadmin",
    "password": "yQ33Eyha9kBNrD"
  },
  "globals": {},
  "case": {},
  "ctx": {},
  "this": {
    "status": 200,
    "headers": [
      {
        "name": "Date",
        "value": "Sun, 31 Aug 2025 20:39:47 GMT"
      },
      {
        "name": "Transfer-Encoding",
        "value": "chunked"
      },
      {
        "name": "Connection",
        "value": "keep-alive"
      },
      {
        "name": "Strict-Transport-Security",
        "value": "max-age=31536000; includeSubDomains"
      },
      {
        "name": "Access-Control-Allow-Origin",
        "value": "https://api.totest.com"
      },
      {
        "name": "Content-Security-Policy",
        "value": "default-src 'self' *.nexxbiz.tech; script-src 'self' 'unsafe-inline' 'unsafe-eval' *.nexxbiz.tech js.monitor.azure.com; style-src 'self' 'unsafe-inline' *.nexxbiz.tech *.googleapis.com; img-src 'self' data: *.nexxbiz.tech; media-src 'self' data: *.nexxbiz.tech; font-src 'self' data: *.nexxbiz.tech fonts.gstatic.com; connect-src 'self' *.nexxbiz.tech https://dc.services.visualstudio.com https://js.monitor.azure.com;"
      },
      {
        "name": "Referrer-Policy",
        "value": "strict-origin-when-cross-origin"
      },
      {
        "name": "X-Content-Type-Options",
        "value": "nosniff"
      },
      {
        "name": "X-Frame-Options",
        "value": "SAMEORIGIN"
      },
      {
        "name": "X-XSS-Protection",
        "value": "1; mode=block"
      },
      {
        "name": "Content-Type",
        "value": "application/json; charset=utf-8"
      }
    ],
    "body": {
      "request": {
        "isTrue": false
      },
      "workflowInstanceId": "b92e57abae5e5873"
    }
  },
  "now": {
    "iso": "2025-08-31T20:39:47Z"
  },
  "random": {
    "uuid": "143601d8-5f67-4aab-9b49-53a557ce0837"
  },
  "execute-workflow": {
    "status": 200,
    "headers": [
      {
        "name": "Date",
        "value": "Sun, 31 Aug 2025 20:39:47 GMT"
      },
      {
        "name": "Transfer-Encoding",
        "value": "chunked"
      },
      {
        "name": "Connection",
        "value": "keep-alive"
      },
      {
        "name": "Strict-Transport-Security",
        "value": "max-age=31536000; includeSubDomains"
      },
      {
        "name": "Access-Control-Allow-Origin",
        "value": "https://api.totest.com"
      },
      {
        "name": "Content-Security-Policy",
        "value": "default-src 'self' *.nexxbiz.tech; script-src 'self' 'unsafe-inline' 'unsafe-eval' *.nexxbiz.tech js.monitor.azure.com; style-src 'self' 'unsafe-inline' *.nexxbiz.tech *.googleapis.com; img-src 'self' data: *.nexxbiz.tech; media-src 'self' data: *.nexxbiz.tech; font-src 'self' data: *.nexxbiz.tech fonts.gstatic.com; connect-src 'self' *.nexxbiz.tech https://dc.services.visualstudio.com https://js.monitor.azure.com;"
      },
      {
        "name": "Referrer-Policy",
        "value": "strict-origin-when-cross-origin"
      },
      {
        "name": "X-Content-Type-Options",
        "value": "nosniff"
      },
      {
        "name": "X-Frame-Options",
        "value": "SAMEORIGIN"
      },
      {
        "name": "X-XSS-Protection",
        "value": "1; mode=block"
      },
      {
        "name": "Content-Type",
        "value": "application/json; charset=utf-8"
      }
    ],
    "body": {
      "request": {
        "isTrue": false
      },
      "workflowInstanceId": "b92e57abae5e5873"
    },
    "last": {
      "status": 200,
      "headers": [
        {
          "name": "Date",
          "value": "Sun, 31 Aug 2025 20:39:47 GMT"
        },
        {
          "name": "Transfer-Encoding",
          "value": "chunked"
        },
        {
          "name": "Connection",
          "value": "keep-alive"
        },
        {
          "name": "Strict-Transport-Security",
          "value": "max-age=31536000; includeSubDomains"
        },
        {
          "name": "Access-Control-Allow-Origin",
          "value": "https://api.totest.com"
        },
        {
          "name": "Content-Security-Policy",
          "value": "default-src 'self' *.nexxbiz.tech; script-src 'self' 'unsafe-inline' 'unsafe-eval' *.nexxbiz.tech js.monitor.azure.com; style-src 'self' 'unsafe-inline' *.nexxbiz.tech *.googleapis.com; img-src 'self' data: *.nexxbiz.tech; media-src 'self' data: *.nexxbiz.tech; font-src 'self' data: *.nexxbiz.tech fonts.gstatic.com; connect-src 'self' *.nexxbiz.tech https://dc.services.visualstudio.com https://js.monitor.azure.com;"
        },
        {
          "name": "Referrer-Policy",
          "value": "strict-origin-when-cross-origin"
        },
        {
          "name": "X-Content-Type-Options",
          "value": "nosniff"
        },
        {
          "name": "X-Frame-Options",
          "value": "SAMEORIGIN"
        },
        {
          "name": "X-XSS-Protection",
          "value": "1; mode=block"
        },
        {
          "name": "Content-Type",
          "value": "application/json; charset=utf-8"
        }
      ],
      "body": {
        "request": {
          "isTrue": false
        },
        "workflowInstanceId": "b92e57abae5e5873"
      }
    },
    "history": [
      {
        "status": 200,
        "headers": [
          {
            "name": "Date",
            "value": "Sun, 31 Aug 2025 20:39:47 GMT"
          },
          {
            "name": "Transfer-Encoding",
            "value": "chunked"
          },
          {
            "name": "Connection",
            "value": "keep-alive"
          },
          {
            "name": "Strict-Transport-Security",
            "value": "max-age=31536000; includeSubDomains"
          },
          {
            "name": "Access-Control-Allow-Origin",
            "value": "https://api.totest.com"
          },
          {
            "name": "Content-Security-Policy",
            "value": "default-src 'self' *.nexxbiz.tech; script-src 'self' 'unsafe-inline' 'unsafe-eval' *.nexxbiz.tech js.monitor.azure.com; style-src 'self' 'unsafe-inline' *.nexxbiz.tech *.googleapis.com; img-src 'self' data: *.nexxbiz.tech; media-src 'self' data: *.nexxbiz.tech; font-src 'self' data: *.nexxbiz.tech fonts.gstatic.com; connect-src 'self' *.nexxbiz.tech https://dc.services.visualstudio.com https://js.monitor.azure.com;"
          },
          {
            "name": "Referrer-Policy",
            "value": "strict-origin-when-cross-origin"
          },
          {
            "name": "X-Content-Type-Options",
            "value": "nosniff"
          },
          {
            "name": "X-Frame-Options",
            "value": "SAMEORIGIN"
          },
          {
            "name": "X-XSS-Protection",
            "value": "1; mode=block"
          },
          {
            "name": "Content-Type",
            "value": "application/json; charset=utf-8"
          }
        ],
        "body": {
          "request": {
            "isTrue": false
          },
          "workflowInstanceId": "b92e57abae5e5873"
        }
      }
    ]
  },
  "workflowInstanceId": "b92e57abae5e5873"
}
```

</details>

---

## Summary

**Total Duration:** 0,89 seconds
**Tests Executed:** 1
**Completion Status:** ✅ Completed
**Errors:** 0
**Total Steps:** 2
**HTTP Requests:** 2

