# FluentCMS.Repositories.Tests

Comprehensive functional tests for the FluentCMS.Repositories library using SQLite in-memory database.

## Test Structure

### Test Entities
- `TestEntity` - A sample entity implementing `IEntity` and `IAuditableEntity`
- `TestDbContext` - Entity Framework Core DbContext for test entities

### Test Fixtures
- `ServiceProviderFixture` - Sets up DI container with SQLite in-memory database and repository registration

### Test Categories

#### 1. Basic Repository Operations
- **RepositoryTests.cs** - Tests for `IRepository<TEntity>` interface
  - Add, AddMany, Update, Remove operations
  - GetById, GetAll, Find, Count, Any queries
  - Basic CRUD functionality

#### 2. Transactional Operations
- **TransactionalRepositoryTests.cs** - Tests for `ITransactionalRepository<TEntity>` interface
  - BeginTransaction, Commit, Rollback functionality
  - Transaction state management
  - Error handling for transaction operations

#### 3. Interceptor Functionality
- **AuditInterceptorTests.cs** - Tests for audit trail functionality
  - Automatic setting of CreatedBy, CreatedAt, UpdatedBy, UpdatedAt
  - Version tracking
  - Audit properties in transactional operations

- **EventInterceptorTests.cs** - Tests for event publishing
  - Repository events for Add, Update, Remove operations
  - Event publishing in transactional context
  - Event rollback behavior

#### 4. Edge Cases and Error Handling
- **ErrorHandlingTests.cs** - Tests for error scenarios and edge cases
  - Null parameter validation
  - Non-existent entity handling
  - Transaction state error handling
  - Concurrent operations
  - Large dataset operations
  - Empty database scenarios

## Running Tests

```bash
dotnet test
```

## Database Configuration

Tests use SQLite in-memory database (`DataSource=:memory:`) for fast, isolated test execution. Each test runs in its own scope with a fresh database connection.

## Key Features Tested

- **Auto-save mode** - Default repository behavior
- **Transactional mode** - Manual transaction control
- **Audit trails** - Automatic audit property management
- **Event publishing** - Repository event system
- **Error handling** - Proper exception handling and validation
- **Concurrency** - Multiple simultaneous operations
- **Performance** - Large dataset handling

## Test Coverage

The tests cover:

1. **Basic CRUD operations** - All repository methods
2. **Transaction management** - Complete transaction lifecycle
3. **Interceptor functionality** - Audit trails and events
4. **Error scenarios** - Proper exception handling
5. **Edge cases** - Boundary conditions and special scenarios
6. **Performance** - Large dataset operations
7. **Integration** - Full stack integration testing

## Dependencies

- xUnit for testing framework
- Moq for mocking (used in event tests)
- Microsoft.EntityFrameworkCore.Sqlite for in-memory database
- FluentCMS.Repositories libraries
