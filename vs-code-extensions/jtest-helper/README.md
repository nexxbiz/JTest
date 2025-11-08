# JTest Helper Extension

This extension provides enhanced support for working with **JTest** test suite files directly in Visual Studio Code. It improves your development experience by offering **IntelliSense**, **snippets**, and **JSON schema validation** for JTest configuration files, making it easier to write, manage, and execute API tests.

## Features

- **IntelliSense & Autocompletion**: Auto-complete for test suite files, steps, assertions, and save sections, with support for custom variables and JSONPath notation.
- **Validation Against JSON Schema**: Automatically validates your test suite files against a comprehensive JSON Schema that supports all JTest configuration options.
- **Snippets**: Predefined code snippets for common JTest structures (e.g., HTTP steps, assertions, variable saving, etc.).

## Requirements

- **Visual Studio Code** version 1.50 or later.
- **JTest Framework**: This extension is designed to be used with the [JTest framework](https://github.com/nexxbiz/JTest). Ensure you have it properly set up in your project.

## Extension Settings

This extension does not add any configuration settings to Visual Studio Code at this time, but it fully supports schema validation, IntelliSense, and snippets for `.json` test suite files.

## Known Issues

- **Cross-file IntelliSense**: While you can reference variables from global or template files, IntelliSense might not always show suggestions for cross-file variables, depending on the test suite structure.

## Release Notes

### 0.0.1
- Initial release of **JTest Helper** extension.
- Added full support for JTest test suite JSON schema validation.
- Integrated IntelliSense for test suite steps, assertions, and dynamic context variables.
- Provided basic snippets for common JTest configurations (e.g., HTTP requests, assertions, variable saving).

---

## For More Information

- [JTest Documentation](https://github.com/nexxbiz/JTest)
- [Visual Studio Code Extensions Guide](https://code.visualstudio.com/api/working-with-extensions)
- [JSON Schema for JTest](https://raw.githubusercontent.com/nexxbiz/JTest/main/schemas/testsuite.schema.json)

## Snippets

This extension comes with several predefined code snippets to help you quickly create common JTest configurations. The following snippets are available:

### How to Use the Snippets

1. **Start typing the snippet prefix** in a JSON file, such as `testSuite`, `httpStep`, `assertStep`, etc.
2. **Press `Tab`** to expand the snippet, or select it from the IntelliSense suggestions.
3. **Fill in the placeholder values** (e.g., `$1`, `$2`) as per your requirements. These placeholders will automatically be highlighted when the snippet is inserted, allowing you to quickly navigate between them.

### 1. **Test Suite File**  
**Scope**: `json`  
**Prefix**: `testSuite`  
**Description**: Creates a basic test suite file with default properties.

```json
{
  "version": "1.0",
  "info": {
    "name": "$1",
    "description": ""
  },
  "using": [],
  "globals": [],
  "tests": [
    {
      "name": "$2",
      "description": "",
      "steps": []
    }
  ]
}
```

### 2. **Test Case**  
**Scope**: `json`  
**Prefix**: `testCase`  
**Description**: Creates a basic test case object with default properties

```json
{
   "name": "$1",
   "description": "",
   "steps": [
       {
          "type": "$2"
       }
   ]
}
```

### 3. **DataSet**  
**Scope**: `json`  
**Prefix**: `dataset`  
**Description**: Creates a templates collection file with the default properties

```json
{
   "name": "$1",
   "case": {
      "$2": "$3"
    }
}
```

### 4. **Templates Collection File**  
**Scope**: `json`  
**Prefix**: `templatesCollection`  
**Description**: Creates a templates collection file with the default properties

```json
{
  "version": "1.0",
  "info": {
    "name": "$1",
    "description": ""
  },
  "components": {
      "templates": [
         {
           "name": "$2",
           "description": "",
           "params": {},
           "steps": []
         }
      ]
  }
}
```

### 5. **Template Object**  
**Scope**: `json`  
**Prefix**: `template`  
**Description**: Creates a template object with the default properties

```json
{
  "name": "$1",
  "description": "",
  "params": {
      "$2": {
          "type": "$3",
          "required": true,
          "description": ""
      }
   },
   "steps": []
}
```

### 6. **HTTP Step**  
**Scope**: `json`  
**Prefix**: `httpStep`  
**Description**: Creates an HTTP test step with required properties

```json
{
  "type": "http",
  "name": "$1",
  "url": "$2",
  "method": "$3",
  "assert": [
     {
         "op": "$4",
         "actualValue": "$5",
         "expectedValue": "$6"
     }
   ]
}
```

### 7. **Wait Step**  
**Scope**: `json`  
**Prefix**: `waitStep`  
**Description**: Creates a Wait test step with required properties

```json
{
  "type": "wait",
  "name": "$1",
  "ms": $2,
  "assert": [
     {
         "op": "$3",
         "actualValue": "$4",
         "expectedValue": "$5"
     }
   ]
}
```

### 8. **Use Step**  
**Scope**: `json`  
**Prefix**: `useStep`  
**Description**: Creates a Use test step with required properties

```json
{
  "type": "use",
  "name": "$1",
  "template": "$2",
  "with": {},
  "assert": []
}
```

### 9. **Assert Step**  
**Scope**: `json`  
**Prefix**: `assertStep`  
**Description**: Creates a Use test step with required properties

```json
{
  "type": "assert",
  "name": "$1",
  "assert": [
     {
         "op": "$2",
         "actualValue": "$3",
         "expectedValue": "$4"
     }
   ]
}
```

### 10. **Assertion**  
**Scope**: `json`  
**Prefix**: `assert`  
**Description**: Creates an assert object for in the "assert" section in a test case

```json
{
  "op": "$1",
  "actualValue": "$2",
  "expectedValue": "$3"
}
```