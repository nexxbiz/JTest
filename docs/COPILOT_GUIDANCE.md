# Copilot guidance for JTest project

## Communication principles

### Ask for clarification
- Always ask when requirements are unclear or ambiguous
- Confirm understanding before implementing changes
- Request explicit permission before working on items

### Response style
- Use sentence casing in all text and documentation
- Avoid using icons, emojis, or special symbols
- Keep responses concise and professional
- Focus on clarity over verbosity

## Code standards

### General principles
- Write dry and clean code
- Use clear, descriptive naming conventions
- Minimize comments - let code be self-documenting
- Prioritize readability and maintainability

### Method structure
- Maximum 1 level of indentation per method
- Maximum 7 lines per method
- Each method should have a single responsibility
- Extract complex logic into separate methods

### Naming conventions
- Use descriptive names that explain intent
- Prefer longer descriptive names over short cryptic ones
- Use consistent naming patterns across the codebase
- Follow C# naming conventions for .NET 8 projects

### Code organization
- Keep classes focused on single responsibilities
- Use interfaces to define contracts
- Minimize dependencies between components
- Favor composition over inheritance

## Project context

### Technology stack
- Target framework: .NET 8
- Primary language: C#
- Project type: CLI tool with extensible architecture

### Architecture principles
- Plugin-based extensibility
- Interface-driven design
- JSON-first configuration
- Context-driven execution model

### Key components
- Step registry for extensible step types
- Execution engine for test orchestration
- Context system for variable management
- Template system for reusable components

## Working approach

### Permission-based workflow
- Only work on items when explicitly permitted
- Confirm scope and approach before implementation
- Ask for guidance when uncertain about direction

### Documentation standards
- Keep documentation practical and actionable
- Focus on intent and architecture over implementation details
- Use concrete examples to illustrate concepts
- Maintain consistency with existing documentation style

### Quality expectations
- Validate code changes compile successfully
- Consider extensibility in all design decisions
- Maintain backward compatibility where possible
- Write testable code with clear boundaries

## Response guidelines

### When providing code examples
- Focus on architectural intent rather than complete implementations
- Show clear separation of concerns
- Demonstrate extensibility patterns
- Include validation and error handling considerations

### When explaining concepts
- Start with high-level overview
- Progress to specific details
- Use concrete examples from the JTest domain
- Connect to existing architecture patterns

### When uncertain
- Ask specific questions about requirements
- Propose alternative approaches when multiple options exist
- Seek clarification on scope and constraints
- Confirm understanding before proceeding