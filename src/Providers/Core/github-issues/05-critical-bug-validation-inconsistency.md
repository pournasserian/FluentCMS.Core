# 🐛 Critical Bug: Inconsistent Validation Rules Between Entity and Database

## Issue Description

There's a mismatch between entity validation rules and database constraints for the `DisplayName` property, creating potential runtime failures.

## Affected Files
- `Provider.cs`
- `ProviderDbContext.cs`

## Current Code
```csharp
// Provider.cs
[StringLength(200, MinimumLength = 1, ErrorMessage = "Display name must be between 1 and 200 characters")]
public string DisplayName { get; set; } = string.Empty;

// ProviderDbContext.cs
entity.Property(p => p.DisplayName)
    .IsRequired()
    .HasMaxLength(400);  // ❌ Different constraint: 200 vs 400
```

## Problem
- **Validation mismatch**: Entity allows up to 200 characters, database allows 400
- **Runtime failures**: Database operations may fail unexpectedly
- **Inconsistent behavior**: Different validation in different layers
- **Data integrity issues**: Inconsistent constraints across the application

## Impact
- Database constraint violations at runtime
- Inconsistent user experience
- Difficult to debug validation failures
- Potential data truncation or corruption

## Proposed Solution
Align validation rules between entity and database configurations.

## Example Fix Option 1: Update Database Constraint
```csharp
// ProviderDbContext.cs
entity.Property(p => p.DisplayName)
    .IsRequired()
    .HasMaxLength(200);  // ✅ Match entity validation
```

## Example Fix Option 2: Update Entity Validation
```csharp
// Provider.cs
[StringLength(400, MinimumLength = 1, ErrorMessage = "Display name must be between 1 and 400 characters")]
public string DisplayName { get; set; } = string.Empty;
```

## Recommended Approach
1. **Create constants** for shared validation rules
2. **Use configuration-based validation** to ensure consistency
3. **Add validation tests** to catch future mismatches

## Example Constants Approach
```csharp
public static class ProviderValidation
{
    public const int DisplayNameMaxLength = 200;
    public const int DisplayNameMinLength = 1;
}

// In Provider.cs
[StringLength(ProviderValidation.DisplayNameMaxLength, 
    MinimumLength = ProviderValidation.DisplayNameMinLength)]
public string DisplayName { get; set; } = string.Empty;

// In ProviderDbContext.cs
entity.Property(p => p.DisplayName)
    .IsRequired()
    .HasMaxLength(ProviderValidation.DisplayNameMaxLength);
```

## Priority
**Medium** - Can cause runtime validation failures

## Labels
- bug
- medium-priority
- validation
- data-integrity
- database
