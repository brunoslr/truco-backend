# AI Development Guidelines for Truco Mineiro

> **Note**: This document focuses on implementation methodology. See also:
> - [`CODING_STANDARDS.md`](CODING_STANDARDS.md) - Code quality and formatting standards
> - [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md) - Project architecture and business rules

## Core Implementation Principles

### **Best Practices First, Ask When Needed**
- **Follow established patterns** and coding standards as the default approach
- **When best practices conflict** or are unclear, ask for guidance/clarification
- **Don't guess implementation details** - it's better to ask than assume
- **Clarify requirements** if business rules or expected behavior are ambiguous
- **Document any guidance received** for future reference and consistency

### **No Partial Implementations Rule**
- **Complete removal** of old code before adding new code
- **One concept, one implementation** - never leave multiple ways to do the same thing
- **Clean slate approach** - when changing something, change it completely
- **Remove all traces** of the old approach (properties, methods, comments)

### **Phase-by-Phase Development**
- Complete each phase fully before moving to next
- Validate with tests after each phase
- Commit changes only after successful phase validation
- No parallel development across phases

### **Change Management**
- Document any mid-stream change requests before implementing
- Apply the "30% Rule" - if change requires >30% rework, defer to future iteration
- Use "Good Enough for Now" mindset - perfect is enemy of done

### **Code Quality Standards**
- Remove unused properties, methods, and imports
- No commented-out "old" code left behind
- Single responsibility principle for all classes
- Clear, descriptive naming conventions

### **Technical Debt Prevention**
- Address technical improvements completely, not incrementally
- Prefer refactoring entire components over patching
- Maintain clean abstractions and interfaces
- Document architectural decisions for future reference

## Project-Specific Rules

### **Event-Driven Architecture**
- All game state changes must go through events
- Use existing event publisher/handler pattern
- Maintain loose coupling between components

### **Domain Model Integrity**
- GameState is single source of truth
- Validate all state transitions through TrucoRulesEngine
- Keep DTOs in sync with domain models

### **API Consistency**
- Leverage existing button press system for new actions
- Maintain backward compatibility where possible
- Use consistent error handling patterns

### **Constants and Type Safety**
- Use enums over string literals for state values
- Consolidate constants in TrucoConstants.cs
- Eliminate magic strings throughout codebase
- See [`CODING_STANDARDS.md`](CODING_STANDARDS.md) for detailed standards

## Implementation Workflow

1. **Plan** - Document what will be changed and why
2. **Remove** - Completely remove old implementations
3. **Implement** - Add new implementation cleanly
4. **Test** - Validate with unit tests and manual testing
5. **Commit** - Save changes with descriptive messages
6. **Validate** - Ensure no partial implementations remain

## Anti-Patterns to Avoid

❌ Leaving multiple implementations of same concept
❌ Commenting out old code "just in case"
❌ Adding new properties alongside deprecated ones
❌ Partial feature implementations across commits
❌ Mixing old and new patterns in same codebase

## Success Criteria

✅ Single, clear implementation path for each feature
✅ No duplicate or conflicting code patterns
✅ Clean, testable, maintainable code
✅ Consistent architectural patterns throughout
✅ Complete feature implementations per phase

### **Structured Legacy Code Removal**
- **Complete audit** of legacy code including tests, properties, methods, and patterns
- **Systematic removal** of deprecated implementations after new system validation
- **Test modernization** - update or remove tests that no longer match current architecture
- **API change documentation** - clearly document breaking changes for frontend integration
- **Breaking changes allowed** - prioritize clean architecture over backward compatibility
- **Frontend coordination** - provide clear migration guide for API changes
