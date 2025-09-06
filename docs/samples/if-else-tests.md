# Debug Report for if-else-testsv2.json

**Generated:** 2025-09-05 20:21:58
**Test File:** .\if-else-testsv2.json

## Test Execution

---

### Test: Get tokens

**Status:** PASSED
**Duration:** 45ms

#### Steps

- **Step:** PASSED (44ms)
  - **Saved Values:**

  - **Template Steps:**
    - **HTTP POST https://api.com/elsa/api/identity/login:** PASSED (40ms)
      (**Saved:** Added accessToken, refreshToken, **Assertions:** 3 passed)


<details><summary>Show saved variables</summary>

- **Added:** accessToken = "masked"
- **Added:** refreshToken = "masked"

</details>





---

### Test: Execute workflow instance for if-else activity

**Status:** PASSED
**Duration:** 126ms
**Dataset:** false-condition

#### Steps

- **Step:** PASSED (123ms)
  - **Saved Values:**
    - **Added:** ifConditionResult = 

<details><summary>show</summary>

```json
{
  "workflowInstanceId": "60fab6322564c863",
  "workflowResponse": {
    "request": {
      "isTrue": false
    },
    "workflowInstanceId": "60fab6322564c863"
  },
  "journal": {
    "items": [
      {
        "id": "af8fdde40914b9f3",
        "activityInstanceId": "bbcfd9553d54aac7",
        "activityId": "Workflow1",
        "activityType": "Elsa.Workflow",
        "activityTypeVersion": 1,
        "nodeId": "Workflow1",
        "timestamp": "2025-09-05T18:21:55.875962\u002B00:00",
        "sequence": 0,
        "eventName": "Started"
      },
      {
        "id": "f4de5e454ee90152",
        "activityInstanceId": "5912b925b4260cb9",
        "parentActivityInstanceId": "bbcfd9553d54aac7",
        "activityId": "93c4098d018f9cdd",
        "activityType": "Elsa.Flowchart",
        "activityTypeVersion": 1,
        "activityName": "Flowchart1",
        "nodeId": "Workflow1:93c4098d018f9cdd",
        "timestamp": "2025-09-05T18:21:55.876691\u002B00:00",
        "sequence": 1,
        "eventName": "Started"
      },
      {
        "id": "b8ac3c69eedc3246",
        "activityInstanceId": "3a0393b6f2c5e6c0",
        "parentActivityInstanceId": "5912b925b4260cb9",
        "activityId": "2829f86a3c709f12",
        "activityType": "Elsa.HttpEndpoint",
        "activityTypeVersion": 1,
        "activityName": "HttpEndpoint1",
        "nodeId": "Workflow1:93c4098d018f9cdd:2829f86a3c709f12",
        "timestamp": "2025-09-05T18:21:55.876841\u002B00:00",
        "sequence": 2,
        "eventName": "Started"
      },
      {
        "id": "a0316a086170de24",
        "activityInstanceId": "3a0393b6f2c5e6c0",
        "parentActivityInstanceId": "5912b925b4260cb9",
        "activityId": "2829f86a3c709f12",
        "activityType": "Elsa.HttpEndpoint",
        "activityTypeVersion": 1,
        "activityName": "HttpEndpoint1",
        "nodeId": "Workflow1:93c4098d018f9cdd:2829f86a3c709f12",
        "timestamp": "2025-09-05T18:21:55.877588\u002B00:00",
        "sequence": 3,
        "eventName": "Completed"
      },
      {
        "id": "2851478e0f536ab5",
        "activityInstanceId": "a836f0c50fcacdb5",
        "parentActivityInstanceId": "5912b925b4260cb9",
        "activityId": "b961ee5b6f66b01c",
        "activityType": "Elsa.FlowDecision",
        "activityTypeVersion": 1,
        "activityName": "FlowDecision1",
        "nodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
        "timestamp": "2025-09-05T18:21:55.878154\u002B00:00",
        "sequence": 4,
        "eventName": "Started"
      },
      {
        "id": "9846804c55857f6b",
        "activityInstanceId": "a836f0c50fcacdb5",
        "parentActivityInstanceId": "5912b925b4260cb9",
        "activityId": "b961ee5b6f66b01c",
        "activityType": "Elsa.FlowDecision",
        "activityTypeVersion": 1,
        "activityName": "FlowDecision1",
        "nodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
        "timestamp": "2025-09-05T18:21:55.879406\u002B00:00",
        "sequence": 5,
        "eventName": "Completed"
      },
      {
        "id": "33cdc89a57a06123",
        "activityInstanceId": "dc266a7f7bf38c15",
        "parentActivityInstanceId": "5912b925b4260cb9",
        "activityId": "8dc6c81ec40e368c",
        "activityType": "Elsa.WriteHttpResponse",
        "activityTypeVersion": 1,
        "activityName": "WriteHttpResponse1",
        "nodeId": "Workflow1:93c4098d018f9cdd:8dc6c81ec40e368c",
        "timestamp": "2025-09-05T18:21:55.88009\u002B00:00",
        "sequence": 6,
        "eventName": "Started"
      },
      {
        "id": "4215d2ab7ed3848f",
        "activityInstanceId": "dc266a7f7bf38c15",
        "parentActivityInstanceId": "5912b925b4260cb9",
        "activityId": "8dc6c81ec40e368c",
        "activityType": "Elsa.WriteHttpResponse",
        "activityTypeVersion": 1,
        "activityName": "WriteHttpResponse1",
        "nodeId": "Workflow1:93c4098d018f9cdd:8dc6c81ec40e368c",
        "timestamp": "2025-09-05T18:21:55.881397\u002B00:00",
        "sequence": 7,
        "eventName": "Completed"
      },
      {
        "id": "ad43329b6e028573",
        "activityInstanceId": "5912b925b4260cb9",
        "parentActivityInstanceId": "bbcfd9553d54aac7",
        "activityId": "93c4098d018f9cdd",
        "activityType": "Elsa.Flowchart",
        "activityTypeVersion": 1,
        "activityName": "Flowchart1",
        "nodeId": "Workflow1:93c4098d018f9cdd",
        "timestamp": "2025-09-05T18:21:55.881443\u002B00:00",
        "sequence": 8,
        "eventName": "Completed"
      },
      {
        "id": "d7b4590078132547",
        "activityInstanceId": "bbcfd9553d54aac7",
        "activityId": "Workflow1",
        "activityType": "Elsa.Workflow",
        "activityTypeVersion": 1,
        "nodeId": "Workflow1",
        "timestamp": "2025-09-05T18:21:55.881455\u002B00:00",
        "sequence": 9,
        "eventName": "Completed"
      }
    ],
    "totalCount": 10
  },
  "activityExecution": {
    "workflowInstanceId": "60fab6322564c863",
    "activityId": "b961ee5b6f66b01c",
    "activityNodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
    "activityType": "Elsa.FlowDecision",
    "activityTypeVersion": 1,
    "activityName": "FlowDecision1",
    "activityState": {
      "Condition": false
    },
    "payload": {
      "Outcomes": [
        "False"
      ],
      "_type": "ObjectMap"
    },
    "properties": {
      "_type": "ObjectMap"
    },
    "metadata": {
      "_type": "ObjectMap"
    },
    "startedAt": "2025-09-05T18:21:55.8780117\u002B00:00",
    "hasBookmarks": false,
    "status": "Completed",
    "aggregateFaultCount": 0,
    "completedAt": "2025-09-05T18:21:55.8794721\u002B00:00",
    "id": "a836f0c50fcacdb5"
  },
  "targetActivityInstanceId": "",
  "type": "template",
  "templateName": "execute-workflow-and-get-activity",
  "steps": 3
}
```
</details>



  - **Assertions:**
    - PASSED: Activity condition should match expected value for false condition path
    - PASSED: Workflow instance ID should exist for false condition path

  - **Template Steps:**
    - **HTTP POST https://wf.api.com/workflows/pm/qa/ifelse:** PASSED (31ms)
      (**Saved:** Added workflowInstanceId, workflowResponse, **Assertions:** 2 passed)


<details><summary>Show saved variables</summary>

- **Added:** workflowInstanceId = "60fab6322564c863"
- **Added:** workflowResponse = 

<details><summary>show</summary>

```json
{
  "request": {
    "isTrue": false
  },
  "workflowInstanceId": "60fab6322564c863"
}
```
</details>



</details>


    - **HTTP GET https://api.com/elsa/api/workflow-instances/60fab6322564c863/journal:** PASSED (26ms)
      (**Saved:** Added journal, targetActivityInstanceId, **Assertions:** 2 passed)


<details><summary>Show saved variables</summary>

- **Added:** journal = 

<details><summary>show</summary>

```json
{
  "items": [
    {
      "id": "af8fdde40914b9f3",
      "activityInstanceId": "bbcfd9553d54aac7",
      "activityId": "Workflow1",
      "activityType": "Elsa.Workflow",
      "activityTypeVersion": 1,
      "nodeId": "Workflow1",
      "timestamp": "2025-09-05T18:21:55.875962\u002B00:00",
      "sequence": 0,
      "eventName": "Started"
    },
    {
      "id": "f4de5e454ee90152",
      "activityInstanceId": "5912b925b4260cb9",
      "parentActivityInstanceId": "bbcfd9553d54aac7",
      "activityId": "93c4098d018f9cdd",
      "activityType": "Elsa.Flowchart",
      "activityTypeVersion": 1,
      "activityName": "Flowchart1",
      "nodeId": "Workflow1:93c4098d018f9cdd",
      "timestamp": "2025-09-05T18:21:55.876691\u002B00:00",
      "sequence": 1,
      "eventName": "Started"
    },
    {
      "id": "b8ac3c69eedc3246",
      "activityInstanceId": "3a0393b6f2c5e6c0",
      "parentActivityInstanceId": "5912b925b4260cb9",
      "activityId": "2829f86a3c709f12",
      "activityType": "Elsa.HttpEndpoint",
      "activityTypeVersion": 1,
      "activityName": "HttpEndpoint1",
      "nodeId": "Workflow1:93c4098d018f9cdd:2829f86a3c709f12",
      "timestamp": "2025-09-05T18:21:55.876841\u002B00:00",
      "sequence": 2,
      "eventName": "Started"
    },
    {
      "id": "a0316a086170de24",
      "activityInstanceId": "3a0393b6f2c5e6c0",
      "parentActivityInstanceId": "5912b925b4260cb9",
      "activityId": "2829f86a3c709f12",
      "activityType": "Elsa.HttpEndpoint",
      "activityTypeVersion": 1,
      "activityName": "HttpEndpoint1",
      "nodeId": "Workflow1:93c4098d018f9cdd:2829f86a3c709f12",
      "timestamp": "2025-09-05T18:21:55.877588\u002B00:00",
      "sequence": 3,
      "eventName": "Completed"
    },
    {
      "id": "2851478e0f536ab5",
      "activityInstanceId": "a836f0c50fcacdb5",
      "parentActivityInstanceId": "5912b925b4260cb9",
      "activityId": "b961ee5b6f66b01c",
      "activityType": "Elsa.FlowDecision",
      "activityTypeVersion": 1,
      "activityName": "FlowDecision1",
      "nodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
      "timestamp": "2025-09-05T18:21:55.878154\u002B00:00",
      "sequence": 4,
      "eventName": "Started"
    },
    {
      "id": "9846804c55857f6b",
      "activityInstanceId": "a836f0c50fcacdb5",
      "parentActivityInstanceId": "5912b925b4260cb9",
      "activityId": "b961ee5b6f66b01c",
      "activityType": "Elsa.FlowDecision",
      "activityTypeVersion": 1,
      "activityName": "FlowDecision1",
      "nodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
      "timestamp": "2025-09-05T18:21:55.879406\u002B00:00",
      "sequence": 5,
      "eventName": "Completed"
    },
    {
      "id": "33cdc89a57a06123",
      "activityInstanceId": "dc266a7f7bf38c15",
      "parentActivityInstanceId": "5912b925b4260cb9",
      "activityId": "8dc6c81ec40e368c",
      "activityType": "Elsa.WriteHttpResponse",
      "activityTypeVersion": 1,
      "activityName": "WriteHttpResponse1",
      "nodeId": "Workflow1:93c4098d018f9cdd:8dc6c81ec40e368c",
      "timestamp": "2025-09-05T18:21:55.88009\u002B00:00",
      "sequence": 6,
      "eventName": "Started"
    },
    {
      "id": "4215d2ab7ed3848f",
      "activityInstanceId": "dc266a7f7bf38c15",
      "parentActivityInstanceId": "5912b925b4260cb9",
      "activityId": "8dc6c81ec40e368c",
      "activityType": "Elsa.WriteHttpResponse",
      "activityTypeVersion": 1,
      "activityName": "WriteHttpResponse1",
      "nodeId": "Workflow1:93c4098d018f9cdd:8dc6c81ec40e368c",
      "timestamp": "2025-09-05T18:21:55.881397\u002B00:00",
      "sequence": 7,
      "eventName": "Completed"
    },
    {
      "id": "ad43329b6e028573",
      "activityInstanceId": "5912b925b4260cb9",
      "parentActivityInstanceId": "bbcfd9553d54aac7",
      "activityId": "93c4098d018f9cdd",
      "activityType": "Elsa.Flowchart",
      "activityTypeVersion": 1,
      "activityName": "Flowchart1",
      "nodeId": "Workflow1:93c4098d018f9cdd",
      "timestamp": "2025-09-05T18:21:55.881443\u002B00:00",
      "sequence": 8,
      "eventName": "Completed"
    },
    {
      "id": "d7b4590078132547",
      "activityInstanceId": "bbcfd9553d54aac7",
      "activityId": "Workflow1",
      "activityType": "Elsa.Workflow",
      "activityTypeVersion": 1,
      "nodeId": "Workflow1",
      "timestamp": "2025-09-05T18:21:55.881455\u002B00:00",
      "sequence": 9,
      "eventName": "Completed"
    }
  ],
  "totalCount": 10
}
```
</details>


- **Added:** targetActivityInstanceId = "{{$.this.body.items[?(@.activityId == 'b961ee5b6f66b01c' && @.eventName == 'Completed')].activityInstanceId}}"

</details>


    - **HTTP GET https://api.com/elsa/api/activity-executions/:** PASSED (54ms)
      (**Saved:** Added activityExecution, **Assertions:** 3 passed)


<details><summary>Show saved variables</summary>

- **Added:** activityExecution = 

<details><summary>show</summary>

```json
{
  "workflowInstanceId": "60fab6322564c863",
  "activityId": "b961ee5b6f66b01c",
  "activityNodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
  "activityType": "Elsa.FlowDecision",
  "activityTypeVersion": 1,
  "activityName": "FlowDecision1",
  "activityState": {
    "Condition": false
  },
  "payload": {
    "Outcomes": [
      "False"
    ],
    "_type": "ObjectMap"
  },
  "properties": {
    "_type": "ObjectMap"
  },
  "metadata": {
    "_type": "ObjectMap"
  },
  "startedAt": "2025-09-05T18:21:55.8780117\u002B00:00",
  "hasBookmarks": false,
  "status": "Completed",
  "aggregateFaultCount": 0,
  "completedAt": "2025-09-05T18:21:55.8794721\u002B00:00",
  "id": "a836f0c50fcacdb5"
}
```
</details>



</details>





---

### Test: Execute workflow instance for if-else activity

**Status:** PASSED
**Duration:** 96ms
**Dataset:** true-condition

#### Steps

- **Step:** PASSED (93ms)
  - **Saved Values:**
    - **Added:** ifConditionResult = 

<details><summary>show</summary>

```json
{
  "workflowInstanceId": "b85ee587df0139bc",
  "workflowResponse": {
    "request": {
      "isTrue": true
    },
    "workflowInstanceId": "b85ee587df0139bc"
  },
  "journal": {
    "items": [
      {
        "id": "1d7682703c7f380a",
        "activityInstanceId": "7862540789eda64c",
        "activityId": "Workflow1",
        "activityType": "Elsa.Workflow",
        "activityTypeVersion": 1,
        "nodeId": "Workflow1",
        "timestamp": "2025-09-05T18:21:56.013988\u002B00:00",
        "sequence": 0,
        "eventName": "Started"
      },
      {
        "id": "a6ac7d199d9ce64b",
        "activityInstanceId": "9b2d4cc013d15c7f",
        "parentActivityInstanceId": "7862540789eda64c",
        "activityId": "93c4098d018f9cdd",
        "activityType": "Elsa.Flowchart",
        "activityTypeVersion": 1,
        "activityName": "Flowchart1",
        "nodeId": "Workflow1:93c4098d018f9cdd",
        "timestamp": "2025-09-05T18:21:56.014726\u002B00:00",
        "sequence": 1,
        "eventName": "Started"
      },
      {
        "id": "39fda0bf0092526f",
        "activityInstanceId": "efb41459194b237c",
        "parentActivityInstanceId": "9b2d4cc013d15c7f",
        "activityId": "2829f86a3c709f12",
        "activityType": "Elsa.HttpEndpoint",
        "activityTypeVersion": 1,
        "activityName": "HttpEndpoint1",
        "nodeId": "Workflow1:93c4098d018f9cdd:2829f86a3c709f12",
        "timestamp": "2025-09-05T18:21:56.015014\u002B00:00",
        "sequence": 2,
        "eventName": "Started"
      },
      {
        "id": "4e8c52f28d4b0e8",
        "activityInstanceId": "efb41459194b237c",
        "parentActivityInstanceId": "9b2d4cc013d15c7f",
        "activityId": "2829f86a3c709f12",
        "activityType": "Elsa.HttpEndpoint",
        "activityTypeVersion": 1,
        "activityName": "HttpEndpoint1",
        "nodeId": "Workflow1:93c4098d018f9cdd:2829f86a3c709f12",
        "timestamp": "2025-09-05T18:21:56.015687\u002B00:00",
        "sequence": 3,
        "eventName": "Completed"
      },
      {
        "id": "462e4fda98c2873e",
        "activityInstanceId": "7081de2488fc26c7",
        "parentActivityInstanceId": "9b2d4cc013d15c7f",
        "activityId": "b961ee5b6f66b01c",
        "activityType": "Elsa.FlowDecision",
        "activityTypeVersion": 1,
        "activityName": "FlowDecision1",
        "nodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
        "timestamp": "2025-09-05T18:21:56.016331\u002B00:00",
        "sequence": 4,
        "eventName": "Started"
      },
      {
        "id": "edb45b527ada1241",
        "activityInstanceId": "7081de2488fc26c7",
        "parentActivityInstanceId": "9b2d4cc013d15c7f",
        "activityId": "b961ee5b6f66b01c",
        "activityType": "Elsa.FlowDecision",
        "activityTypeVersion": 1,
        "activityName": "FlowDecision1",
        "nodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
        "timestamp": "2025-09-05T18:21:56.017527\u002B00:00",
        "sequence": 5,
        "eventName": "Completed"
      },
      {
        "id": "861996caffbbb9ad",
        "activityInstanceId": "f6c6d27b3592b7b0",
        "parentActivityInstanceId": "9b2d4cc013d15c7f",
        "activityId": "8dc6c81ec40e368c",
        "activityType": "Elsa.WriteHttpResponse",
        "activityTypeVersion": 1,
        "activityName": "WriteHttpResponse1",
        "nodeId": "Workflow1:93c4098d018f9cdd:8dc6c81ec40e368c",
        "timestamp": "2025-09-05T18:21:56.018029\u002B00:00",
        "sequence": 6,
        "eventName": "Started"
      },
      {
        "id": "333c743c81a2c0a2",
        "activityInstanceId": "f6c6d27b3592b7b0",
        "parentActivityInstanceId": "9b2d4cc013d15c7f",
        "activityId": "8dc6c81ec40e368c",
        "activityType": "Elsa.WriteHttpResponse",
        "activityTypeVersion": 1,
        "activityName": "WriteHttpResponse1",
        "nodeId": "Workflow1:93c4098d018f9cdd:8dc6c81ec40e368c",
        "timestamp": "2025-09-05T18:21:56.01892\u002B00:00",
        "sequence": 7,
        "eventName": "Completed"
      },
      {
        "id": "608d300090639959",
        "activityInstanceId": "9b2d4cc013d15c7f",
        "parentActivityInstanceId": "7862540789eda64c",
        "activityId": "93c4098d018f9cdd",
        "activityType": "Elsa.Flowchart",
        "activityTypeVersion": 1,
        "activityName": "Flowchart1",
        "nodeId": "Workflow1:93c4098d018f9cdd",
        "timestamp": "2025-09-05T18:21:56.018955\u002B00:00",
        "sequence": 8,
        "eventName": "Completed"
      },
      {
        "id": "1b1306dc1d48a4db",
        "activityInstanceId": "7862540789eda64c",
        "activityId": "Workflow1",
        "activityType": "Elsa.Workflow",
        "activityTypeVersion": 1,
        "nodeId": "Workflow1",
        "timestamp": "2025-09-05T18:21:56.018961\u002B00:00",
        "sequence": 9,
        "eventName": "Completed"
      }
    ],
    "totalCount": 10
  },
  "activityExecution": {
    "workflowInstanceId": "b85ee587df0139bc",
    "activityId": "b961ee5b6f66b01c",
    "activityNodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
    "activityType": "Elsa.FlowDecision",
    "activityTypeVersion": 1,
    "activityName": "FlowDecision1",
    "activityState": {
      "Condition": true
    },
    "payload": {
      "Outcomes": [
        "True"
      ],
      "_type": "ObjectMap"
    },
    "properties": {
      "_type": "ObjectMap"
    },
    "metadata": {
      "_type": "ObjectMap"
    },
    "startedAt": "2025-09-05T18:21:56.0162836\u002B00:00",
    "hasBookmarks": false,
    "status": "Completed",
    "aggregateFaultCount": 0,
    "completedAt": "2025-09-05T18:21:56.0175733\u002B00:00",
    "id": "7081de2488fc26c7"
  },
  "targetActivityInstanceId": "",
  "type": "template",
  "templateName": "execute-workflow-and-get-activity",
  "steps": 3
}
```
</details>



  - **Assertions:**
    - PASSED: Activity condition should match expected value for true condition path
    - PASSED: Workflow instance ID should exist for true condition path

  - **Template Steps:**
    - **HTTP POST https://wf.api.com/workflows/pm/qa/ifelse:** PASSED (43ms)
      (**Saved:** Added workflowInstanceId, workflowResponse, **Assertions:** 2 passed)


<details><summary>Show saved variables</summary>

- **Added:** workflowInstanceId = "b85ee587df0139bc"
- **Added:** workflowResponse = 

<details><summary>show</summary>

```json
{
  "request": {
    "isTrue": true
  },
  "workflowInstanceId": "b85ee587df0139bc"
}
```
</details>



</details>


    - **HTTP GET https://api.com/elsa/api/workflow-instances/b85ee587df0139bc/journal:** PASSED (16ms)
      (**Saved:** Added journal, targetActivityInstanceId, **Assertions:** 2 passed)


<details><summary>Show saved variables</summary>

- **Added:** journal = 

<details><summary>show</summary>

```json
{
  "items": [
    {
      "id": "1d7682703c7f380a",
      "activityInstanceId": "7862540789eda64c",
      "activityId": "Workflow1",
      "activityType": "Elsa.Workflow",
      "activityTypeVersion": 1,
      "nodeId": "Workflow1",
      "timestamp": "2025-09-05T18:21:56.013988\u002B00:00",
      "sequence": 0,
      "eventName": "Started"
    },
    {
      "id": "a6ac7d199d9ce64b",
      "activityInstanceId": "9b2d4cc013d15c7f",
      "parentActivityInstanceId": "7862540789eda64c",
      "activityId": "93c4098d018f9cdd",
      "activityType": "Elsa.Flowchart",
      "activityTypeVersion": 1,
      "activityName": "Flowchart1",
      "nodeId": "Workflow1:93c4098d018f9cdd",
      "timestamp": "2025-09-05T18:21:56.014726\u002B00:00",
      "sequence": 1,
      "eventName": "Started"
    },
    {
      "id": "39fda0bf0092526f",
      "activityInstanceId": "efb41459194b237c",
      "parentActivityInstanceId": "9b2d4cc013d15c7f",
      "activityId": "2829f86a3c709f12",
      "activityType": "Elsa.HttpEndpoint",
      "activityTypeVersion": 1,
      "activityName": "HttpEndpoint1",
      "nodeId": "Workflow1:93c4098d018f9cdd:2829f86a3c709f12",
      "timestamp": "2025-09-05T18:21:56.015014\u002B00:00",
      "sequence": 2,
      "eventName": "Started"
    },
    {
      "id": "4e8c52f28d4b0e8",
      "activityInstanceId": "efb41459194b237c",
      "parentActivityInstanceId": "9b2d4cc013d15c7f",
      "activityId": "2829f86a3c709f12",
      "activityType": "Elsa.HttpEndpoint",
      "activityTypeVersion": 1,
      "activityName": "HttpEndpoint1",
      "nodeId": "Workflow1:93c4098d018f9cdd:2829f86a3c709f12",
      "timestamp": "2025-09-05T18:21:56.015687\u002B00:00",
      "sequence": 3,
      "eventName": "Completed"
    },
    {
      "id": "462e4fda98c2873e",
      "activityInstanceId": "7081de2488fc26c7",
      "parentActivityInstanceId": "9b2d4cc013d15c7f",
      "activityId": "b961ee5b6f66b01c",
      "activityType": "Elsa.FlowDecision",
      "activityTypeVersion": 1,
      "activityName": "FlowDecision1",
      "nodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
      "timestamp": "2025-09-05T18:21:56.016331\u002B00:00",
      "sequence": 4,
      "eventName": "Started"
    },
    {
      "id": "edb45b527ada1241",
      "activityInstanceId": "7081de2488fc26c7",
      "parentActivityInstanceId": "9b2d4cc013d15c7f",
      "activityId": "b961ee5b6f66b01c",
      "activityType": "Elsa.FlowDecision",
      "activityTypeVersion": 1,
      "activityName": "FlowDecision1",
      "nodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
      "timestamp": "2025-09-05T18:21:56.017527\u002B00:00",
      "sequence": 5,
      "eventName": "Completed"
    },
    {
      "id": "861996caffbbb9ad",
      "activityInstanceId": "f6c6d27b3592b7b0",
      "parentActivityInstanceId": "9b2d4cc013d15c7f",
      "activityId": "8dc6c81ec40e368c",
      "activityType": "Elsa.WriteHttpResponse",
      "activityTypeVersion": 1,
      "activityName": "WriteHttpResponse1",
      "nodeId": "Workflow1:93c4098d018f9cdd:8dc6c81ec40e368c",
      "timestamp": "2025-09-05T18:21:56.018029\u002B00:00",
      "sequence": 6,
      "eventName": "Started"
    },
    {
      "id": "333c743c81a2c0a2",
      "activityInstanceId": "f6c6d27b3592b7b0",
      "parentActivityInstanceId": "9b2d4cc013d15c7f",
      "activityId": "8dc6c81ec40e368c",
      "activityType": "Elsa.WriteHttpResponse",
      "activityTypeVersion": 1,
      "activityName": "WriteHttpResponse1",
      "nodeId": "Workflow1:93c4098d018f9cdd:8dc6c81ec40e368c",
      "timestamp": "2025-09-05T18:21:56.01892\u002B00:00",
      "sequence": 7,
      "eventName": "Completed"
    },
    {
      "id": "608d300090639959",
      "activityInstanceId": "9b2d4cc013d15c7f",
      "parentActivityInstanceId": "7862540789eda64c",
      "activityId": "93c4098d018f9cdd",
      "activityType": "Elsa.Flowchart",
      "activityTypeVersion": 1,
      "activityName": "Flowchart1",
      "nodeId": "Workflow1:93c4098d018f9cdd",
      "timestamp": "2025-09-05T18:21:56.018955\u002B00:00",
      "sequence": 8,
      "eventName": "Completed"
    },
    {
      "id": "1b1306dc1d48a4db",
      "activityInstanceId": "7862540789eda64c",
      "activityId": "Workflow1",
      "activityType": "Elsa.Workflow",
      "activityTypeVersion": 1,
      "nodeId": "Workflow1",
      "timestamp": "2025-09-05T18:21:56.018961\u002B00:00",
      "sequence": 9,
      "eventName": "Completed"
    }
  ],
  "totalCount": 10
}
```
</details>


- **Added:** targetActivityInstanceId = "{{$.this.body.items[?(@.activityId == 'b961ee5b6f66b01c' && @.eventName == 'Completed')].activityInstanceId}}"

</details>


    - **HTTP GET https://api.com/elsa/api/activity-executions/:** PASSED (29ms)
      (**Saved:** Added activityExecution, **Assertions:** 3 passed)


<details><summary>Show saved variables</summary>

- **Added:** activityExecution = 

<details><summary>show</summary>

```json
{
  "workflowInstanceId": "b85ee587df0139bc",
  "activityId": "b961ee5b6f66b01c",
  "activityNodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
  "activityType": "Elsa.FlowDecision",
  "activityTypeVersion": 1,
  "activityName": "FlowDecision1",
  "activityState": {
    "Condition": true
  },
  "payload": {
    "Outcomes": [
      "True"
    ],
    "_type": "ObjectMap"
  },
  "properties": {
    "_type": "ObjectMap"
  },
  "metadata": {
    "_type": "ObjectMap"
  },
  "startedAt": "2025-09-05T18:21:56.0162836\u002B00:00",
  "hasBookmarks": false,
  "status": "Completed",
  "aggregateFaultCount": 0,
  "completedAt": "2025-09-05T18:21:56.0175733\u002B00:00",
  "id": "7081de2488fc26c7"
}
```
</details>



</details>





---

### Test: Execute workflow instance for if-else activity

**Status:** PASSED
**Duration:** 127ms
**Dataset:** none-condition

#### Steps

- **Step:** PASSED (123ms)
  - **Saved Values:**
    - **Added:** ifConditionResult = 

<details><summary>show</summary>

```json
{
  "workflowInstanceId": "e356efe46e08634c",
  "workflowResponse": {
    "request": {
      "isTrue": ""
    },
    "workflowInstanceId": "e356efe46e08634c"
  },
  "journal": {
    "items": [
      {
        "id": "a3904e32c2f79f31",
        "activityInstanceId": "eb46b2ed0fb774dc",
        "activityId": "Workflow1",
        "activityType": "Elsa.Workflow",
        "activityTypeVersion": 1,
        "nodeId": "Workflow1",
        "timestamp": "2025-09-05T18:21:56.097332\u002B00:00",
        "sequence": 0,
        "eventName": "Started"
      },
      {
        "id": "4b6cb567faf7ebb2",
        "activityInstanceId": "9d475eaf96d78f54",
        "parentActivityInstanceId": "eb46b2ed0fb774dc",
        "activityId": "93c4098d018f9cdd",
        "activityType": "Elsa.Flowchart",
        "activityTypeVersion": 1,
        "activityName": "Flowchart1",
        "nodeId": "Workflow1:93c4098d018f9cdd",
        "timestamp": "2025-09-05T18:21:56.097749\u002B00:00",
        "sequence": 1,
        "eventName": "Started"
      },
      {
        "id": "286dcbc7a26370dc",
        "activityInstanceId": "583ad17527f6604d",
        "parentActivityInstanceId": "9d475eaf96d78f54",
        "activityId": "2829f86a3c709f12",
        "activityType": "Elsa.HttpEndpoint",
        "activityTypeVersion": 1,
        "activityName": "HttpEndpoint1",
        "nodeId": "Workflow1:93c4098d018f9cdd:2829f86a3c709f12",
        "timestamp": "2025-09-05T18:21:56.097977\u002B00:00",
        "sequence": 2,
        "eventName": "Started"
      },
      {
        "id": "d7f76622cc0b7cc",
        "activityInstanceId": "583ad17527f6604d",
        "parentActivityInstanceId": "9d475eaf96d78f54",
        "activityId": "2829f86a3c709f12",
        "activityType": "Elsa.HttpEndpoint",
        "activityTypeVersion": 1,
        "activityName": "HttpEndpoint1",
        "nodeId": "Workflow1:93c4098d018f9cdd:2829f86a3c709f12",
        "timestamp": "2025-09-05T18:21:56.098597\u002B00:00",
        "sequence": 3,
        "eventName": "Completed"
      },
      {
        "id": "ded36d19617ce9e9",
        "activityInstanceId": "77c6b1b70503d938",
        "parentActivityInstanceId": "9d475eaf96d78f54",
        "activityId": "b961ee5b6f66b01c",
        "activityType": "Elsa.FlowDecision",
        "activityTypeVersion": 1,
        "activityName": "FlowDecision1",
        "nodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
        "timestamp": "2025-09-05T18:21:56.099128\u002B00:00",
        "sequence": 4,
        "eventName": "Started"
      },
      {
        "id": "6e9e4391d75edd8e",
        "activityInstanceId": "77c6b1b70503d938",
        "parentActivityInstanceId": "9d475eaf96d78f54",
        "activityId": "b961ee5b6f66b01c",
        "activityType": "Elsa.FlowDecision",
        "activityTypeVersion": 1,
        "activityName": "FlowDecision1",
        "nodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
        "timestamp": "2025-09-05T18:21:56.100529\u002B00:00",
        "sequence": 5,
        "eventName": "Completed"
      },
      {
        "id": "5b2b43a8ce2e4c4e",
        "activityInstanceId": "b04dc92d4cd64872",
        "parentActivityInstanceId": "9d475eaf96d78f54",
        "activityId": "8dc6c81ec40e368c",
        "activityType": "Elsa.WriteHttpResponse",
        "activityTypeVersion": 1,
        "activityName": "WriteHttpResponse1",
        "nodeId": "Workflow1:93c4098d018f9cdd:8dc6c81ec40e368c",
        "timestamp": "2025-09-05T18:21:56.101068\u002B00:00",
        "sequence": 6,
        "eventName": "Started"
      },
      {
        "id": "2b08ced9424989eb",
        "activityInstanceId": "b04dc92d4cd64872",
        "parentActivityInstanceId": "9d475eaf96d78f54",
        "activityId": "8dc6c81ec40e368c",
        "activityType": "Elsa.WriteHttpResponse",
        "activityTypeVersion": 1,
        "activityName": "WriteHttpResponse1",
        "nodeId": "Workflow1:93c4098d018f9cdd:8dc6c81ec40e368c",
        "timestamp": "2025-09-05T18:21:56.10208\u002B00:00",
        "sequence": 7,
        "eventName": "Completed"
      },
      {
        "id": "8ba86f44d15cf495",
        "activityInstanceId": "9d475eaf96d78f54",
        "parentActivityInstanceId": "eb46b2ed0fb774dc",
        "activityId": "93c4098d018f9cdd",
        "activityType": "Elsa.Flowchart",
        "activityTypeVersion": 1,
        "activityName": "Flowchart1",
        "nodeId": "Workflow1:93c4098d018f9cdd",
        "timestamp": "2025-09-05T18:21:56.102112\u002B00:00",
        "sequence": 8,
        "eventName": "Completed"
      },
      {
        "id": "a3d50faf7708d605",
        "activityInstanceId": "eb46b2ed0fb774dc",
        "activityId": "Workflow1",
        "activityType": "Elsa.Workflow",
        "activityTypeVersion": 1,
        "nodeId": "Workflow1",
        "timestamp": "2025-09-05T18:21:56.102118\u002B00:00",
        "sequence": 9,
        "eventName": "Completed"
      }
    ],
    "totalCount": 10
  },
  "activityExecution": {
    "workflowInstanceId": "e356efe46e08634c",
    "activityId": "b961ee5b6f66b01c",
    "activityNodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
    "activityType": "Elsa.FlowDecision",
    "activityTypeVersion": 1,
    "activityName": "FlowDecision1",
    "activityState": {
      "Condition": null
    },
    "payload": {
      "Outcomes": [
        "False"
      ],
      "_type": "ObjectMap"
    },
    "properties": {
      "_type": "ObjectMap"
    },
    "metadata": {
      "_type": "ObjectMap"
    },
    "startedAt": "2025-09-05T18:21:56.0990802\u002B00:00",
    "hasBookmarks": false,
    "status": "Completed",
    "aggregateFaultCount": 0,
    "completedAt": "2025-09-05T18:21:56.1005793\u002B00:00",
    "id": "77c6b1b70503d938"
  },
  "targetActivityInstanceId": "",
  "type": "template",
  "templateName": "execute-workflow-and-get-activity",
  "steps": 3
}
```
</details>



  - **Assertions:**
    - PASSED: Activity condition should match expected value for null condition path
    - PASSED: Workflow instance ID should exist for null condition path

  - **Template Steps:**
    - **HTTP POST https://wf.api.com/workflows/pm/qa/ifelse:** PASSED (27ms)
      (**Saved:** Added workflowInstanceId, workflowResponse, **Assertions:** 2 passed)


<details><summary>Show saved variables</summary>

- **Added:** workflowInstanceId = "e356efe46e08634c"
- **Added:** workflowResponse = 

<details><summary>show</summary>

```json
{
  "request": {
    "isTrue": ""
  },
  "workflowInstanceId": "e356efe46e08634c"
}
```
</details>



</details>


    - **HTTP GET https://api.com/elsa/api/workflow-instances/e356efe46e08634c/journal:** PASSED (16ms)
      (**Saved:** Added journal, targetActivityInstanceId, **Assertions:** 2 passed)


<details><summary>Show saved variables</summary>

- **Added:** journal = 

<details><summary>show</summary>

```json
{
  "items": [
    {
      "id": "a3904e32c2f79f31",
      "activityInstanceId": "eb46b2ed0fb774dc",
      "activityId": "Workflow1",
      "activityType": "Elsa.Workflow",
      "activityTypeVersion": 1,
      "nodeId": "Workflow1",
      "timestamp": "2025-09-05T18:21:56.097332\u002B00:00",
      "sequence": 0,
      "eventName": "Started"
    },
    {
      "id": "4b6cb567faf7ebb2",
      "activityInstanceId": "9d475eaf96d78f54",
      "parentActivityInstanceId": "eb46b2ed0fb774dc",
      "activityId": "93c4098d018f9cdd",
      "activityType": "Elsa.Flowchart",
      "activityTypeVersion": 1,
      "activityName": "Flowchart1",
      "nodeId": "Workflow1:93c4098d018f9cdd",
      "timestamp": "2025-09-05T18:21:56.097749\u002B00:00",
      "sequence": 1,
      "eventName": "Started"
    },
    {
      "id": "286dcbc7a26370dc",
      "activityInstanceId": "583ad17527f6604d",
      "parentActivityInstanceId": "9d475eaf96d78f54",
      "activityId": "2829f86a3c709f12",
      "activityType": "Elsa.HttpEndpoint",
      "activityTypeVersion": 1,
      "activityName": "HttpEndpoint1",
      "nodeId": "Workflow1:93c4098d018f9cdd:2829f86a3c709f12",
      "timestamp": "2025-09-05T18:21:56.097977\u002B00:00",
      "sequence": 2,
      "eventName": "Started"
    },
    {
      "id": "d7f76622cc0b7cc",
      "activityInstanceId": "583ad17527f6604d",
      "parentActivityInstanceId": "9d475eaf96d78f54",
      "activityId": "2829f86a3c709f12",
      "activityType": "Elsa.HttpEndpoint",
      "activityTypeVersion": 1,
      "activityName": "HttpEndpoint1",
      "nodeId": "Workflow1:93c4098d018f9cdd:2829f86a3c709f12",
      "timestamp": "2025-09-05T18:21:56.098597\u002B00:00",
      "sequence": 3,
      "eventName": "Completed"
    },
    {
      "id": "ded36d19617ce9e9",
      "activityInstanceId": "77c6b1b70503d938",
      "parentActivityInstanceId": "9d475eaf96d78f54",
      "activityId": "b961ee5b6f66b01c",
      "activityType": "Elsa.FlowDecision",
      "activityTypeVersion": 1,
      "activityName": "FlowDecision1",
      "nodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
      "timestamp": "2025-09-05T18:21:56.099128\u002B00:00",
      "sequence": 4,
      "eventName": "Started"
    },
    {
      "id": "6e9e4391d75edd8e",
      "activityInstanceId": "77c6b1b70503d938",
      "parentActivityInstanceId": "9d475eaf96d78f54",
      "activityId": "b961ee5b6f66b01c",
      "activityType": "Elsa.FlowDecision",
      "activityTypeVersion": 1,
      "activityName": "FlowDecision1",
      "nodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
      "timestamp": "2025-09-05T18:21:56.100529\u002B00:00",
      "sequence": 5,
      "eventName": "Completed"
    },
    {
      "id": "5b2b43a8ce2e4c4e",
      "activityInstanceId": "b04dc92d4cd64872",
      "parentActivityInstanceId": "9d475eaf96d78f54",
      "activityId": "8dc6c81ec40e368c",
      "activityType": "Elsa.WriteHttpResponse",
      "activityTypeVersion": 1,
      "activityName": "WriteHttpResponse1",
      "nodeId": "Workflow1:93c4098d018f9cdd:8dc6c81ec40e368c",
      "timestamp": "2025-09-05T18:21:56.101068\u002B00:00",
      "sequence": 6,
      "eventName": "Started"
    },
    {
      "id": "2b08ced9424989eb",
      "activityInstanceId": "b04dc92d4cd64872",
      "parentActivityInstanceId": "9d475eaf96d78f54",
      "activityId": "8dc6c81ec40e368c",
      "activityType": "Elsa.WriteHttpResponse",
      "activityTypeVersion": 1,
      "activityName": "WriteHttpResponse1",
      "nodeId": "Workflow1:93c4098d018f9cdd:8dc6c81ec40e368c",
      "timestamp": "2025-09-05T18:21:56.10208\u002B00:00",
      "sequence": 7,
      "eventName": "Completed"
    },
    {
      "id": "8ba86f44d15cf495",
      "activityInstanceId": "9d475eaf96d78f54",
      "parentActivityInstanceId": "eb46b2ed0fb774dc",
      "activityId": "93c4098d018f9cdd",
      "activityType": "Elsa.Flowchart",
      "activityTypeVersion": 1,
      "activityName": "Flowchart1",
      "nodeId": "Workflow1:93c4098d018f9cdd",
      "timestamp": "2025-09-05T18:21:56.102112\u002B00:00",
      "sequence": 8,
      "eventName": "Completed"
    },
    {
      "id": "a3d50faf7708d605",
      "activityInstanceId": "eb46b2ed0fb774dc",
      "activityId": "Workflow1",
      "activityType": "Elsa.Workflow",
      "activityTypeVersion": 1,
      "nodeId": "Workflow1",
      "timestamp": "2025-09-05T18:21:56.102118\u002B00:00",
      "sequence": 9,
      "eventName": "Completed"
    }
  ],
  "totalCount": 10
}
```
</details>


- **Added:** targetActivityInstanceId = "{{$.this.body.items[?(@.activityId == 'b961ee5b6f66b01c' && @.eventName == 'Completed')].activityInstanceId}}"

</details>


    - **HTTP GET https://api.com/elsa/api/activity-executions/:** PASSED (69ms)
      (**Saved:** Added activityExecution, **Assertions:** 3 passed)


<details><summary>Show saved variables</summary>

- **Added:** activityExecution = 

<details><summary>show</summary>

```json
{
  "workflowInstanceId": "e356efe46e08634c",
  "activityId": "b961ee5b6f66b01c",
  "activityNodeId": "Workflow1:93c4098d018f9cdd:b961ee5b6f66b01c",
  "activityType": "Elsa.FlowDecision",
  "activityTypeVersion": 1,
  "activityName": "FlowDecision1",
  "activityState": {
    "Condition": null
  },
  "payload": {
    "Outcomes": [
      "False"
    ],
    "_type": "ObjectMap"
  },
  "properties": {
    "_type": "ObjectMap"
  },
  "metadata": {
    "_type": "ObjectMap"
  },
  "startedAt": "2025-09-05T18:21:56.0990802\u002B00:00",
  "hasBookmarks": false,
  "status": "Completed",
  "aggregateFaultCount": 0,
  "completedAt": "2025-09-05T18:21:56.1005793\u002B00:00",
  "id": "77c6b1b70503d938"
}
```
</details>



</details>





## Summary
- **Total tests:** 4
- **Passed:** 4
- **Failed:** 0

