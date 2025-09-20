# 🐛 Critical Bug: Missing Database Constraints for Active Providers

## Issue Description

There's no unique constraint in the database to prevent multiple active providers per area, which violates business rules and can cause runtime errors.

## Affected Files
- `ProviderDbContext.cs`

## Current Code
```csharp
// ❌ Missing: Unique constraint for (Area, IsActive=true)
// This allows multiple active providers per area in database
entity.Property(p => p.Area).IsRequired();
entity.Property(p => p.IsActive);
// No unique constraint defined
```

## Problem
- **Business rule violation**: Multiple active providers can exist for the same area
- **Runtime errors**: Application assumes only one active provider per area
- **Data integrity issues**: Inconsistent state in the database
- **Selection ambiguity**: Unclear which provider to use when multiple are active

## Impact
- Data corruption due to multiple active providers
- Runtime exceptions when the application expects only one active provider
- Unpredictable behavior in provider selection
- Difficult to debug provider-related issues

## Proposed Solution
Add a unique constraint to ensure only one active provider per area.

## Example Fix Option 1: Unique Index
```csharp
// In ProviderDbContext.cs OnModelCreating
entity.HasIndex(p => new { p.Area, p.IsActive })
    .HasDatabaseName("IX_Provider_Area_IsActive_Unique")
    .IsUnique()
    .HasFilter("[IsActive] = 1");  // SQL Server syntax for filtered unique index
```

## Example Fix Option 2: Unique Constraint with Check
```csharp
// Alternative approach with explicit constraint
entity.HasIndex(p => p.Area)
    .HasDatabaseName("IX_Provider_ActivePerArea")
    .IsUnique()
    .HasFilter("[IsActive] = 1");

// Add check constraint to ensure data integrity
migrationBuilder.Sql(@"
    ALTER TABLE Providers 
    ADD CONSTRAINT CK_Provider_OneActivePerArea 
    CHECK (
        (SELECT COUNT(*) FROM Providers p2 
         WHERE p2.Area = Area AND p2.IsActive = 1) <= 1
    )
");
```

## Additional Recommendations
1. **Add application-level validation** to complement database constraints
2. **Create migration script** to handle existing duplicate data
3. **Add unit tests** to verify constraint enforcement
4. **Update documentation** to reflect the business rule

## Migration Considerations
```csharp
// Handle existing duplicate data before adding constraint
migrationBuilder.Sql(@"
    WITH DuplicateProviders AS (
        SELECT *, ROW_NUMBER() OVER (PARTITION BY Area ORDER BY Id) as rn
        FROM Providers 
        WHERE IsActive = 1
    )
    UPDATE Providers 
    SET IsActive = 0 
    WHERE Id IN (
        SELECT Id FROM DuplicateProviders WHERE rn > 1
    )
");
```

## Priority
**High** - This is a critical data integrity issue

## Labels
- bug
- high-priority
- database
- data-integrity
- constraints
