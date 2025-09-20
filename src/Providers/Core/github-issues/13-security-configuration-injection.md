# 🔒 Security Issue: Configuration Injection Vulnerabilities

## Issue Description

The provider system deserializes JSON configuration without proper validation, creating potential for configuration injection attacks.

## Risk Level
**Medium**

## Affected Components
- `ProviderManager.cs`
- JSON deserialization logic
- Configuration processing

## Vulnerabilities Identified

### 1. Unvalidated JSON Deserialization
```csharp
// Current vulnerable code
options = JsonSerializer.Deserialize(provider.Options, module.OptionsType);
// No validation, no schema checking, no size limits
```

### 2. No Schema Validation
- JSON input is not validated against expected schemas
- Malformed or malicious JSON structures are accepted
- No type safety guarantees

### 3. Unlimited Input Size
- No size restrictions on JSON input
- Potential for memory exhaustion attacks
- DoS vulnerability through large payloads

## Security Risks

### Configuration Injection Attacks
```json
{
  "ConnectionString": "Server=malicious.com;Database=stolen;",
  "AllowUnsafeOperations": true,
  "ScriptInjection": "<script>alert('xss')</script>",
  "PathTraversal": "../../../etc/passwd"
}
```

### Memory Exhaustion
```json
{
  "LargeArray": [/* 10 million entries */],
  "DeeplyNested": { /* 1000 levels deep */ }
}
```

### Type Confusion
```json
{
  "ExpectedString": { "ActualObject": "causes_type_error" },
  "ExpectedNumber": "not_a_number"
}
```

## Recommended Security Measures

### 1. Schema Validation
```csharp
public class ConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;
    private readonly JsonSchemaRegistry _schemaRegistry;
    
    public T ValidateAndDeserialize<T>(string json, string schemaName) where T : new()
    {
        // Validate size first
        if (json.Length > MaxConfigurationSize)
            throw new SecurityException($"Configuration exceeds maximum size of {MaxConfigurationSize} characters");
        
        // Get schema for validation
        var schema = _schemaRegistry.GetSchema(schemaName);
        if (schema == null)
            throw new InvalidOperationException($"No schema found for {schemaName}");
        
        // Validate JSON against schema
        var document = JsonDocument.Parse(json);
        var validationResults = schema.Validate(document.RootElement);
        
        if (!validationResults.IsValid)
        {
            var errors = string.Join(", ", validationResults.Errors.Select(e => e.Message));
            throw new ValidationException($"Configuration validation failed: {errors}");
        }
        
        // Safe deserialization with strict options
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false, // Strict case matching
            AllowTrailingCommas = false,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 10, // Prevent deeply nested objects
            NumberHandling = JsonNumberHandling.Strict
        };
        
        var result = JsonSerializer.Deserialize<T>(json, options);
        
        // Additional validation
        ValidateBusinessRules(result);
        
        return result ?? new T();
    }
}
```

### 2. Input Sanitization
```csharp
public class ConfigurationSanitizer
{
    private readonly string[] _dangerousPatterns = 
    {
        "<script",
        "javascript:",
        "data:",
        "../",
        "\\\\",
        "eval(",
        "function("
    };
    
    public string SanitizeConfiguration(string json)
    {
        // Check for dangerous patterns
        foreach (var pattern in _dangerousPatterns)
        {
            if (json.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                throw new SecurityException($"Configuration contains potentially dangerous pattern: {pattern}");
        }
        
        // Normalize and validate JSON structure
        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch (JsonException ex)
        {
            throw new SecurityException("Invalid JSON structure", ex);
        }
    }
}
```

### 3. Security Policies
```csharp
public class ConfigurationSecurityPolicy
{
    public int MaxConfigurationSize { get; set; } = 1024 * 1024; // 1MB
    public int MaxDepth { get; set; } = 10;
    public int MaxArrayLength { get; set; } = 1000;
    public bool AllowComments { get; set; } = false;
    public bool RequireSchemaValidation { get; set; } = true;
    public string[] BlockedPropertyNames { get; set; } = 
    {
        "eval", "script", "function", "__proto__", "constructor"
    };
}
```

## JSON Schema Examples
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "ConnectionString": {
      "type": "string",
      "pattern": "^Server=[^;]+;Database=[^;]+;.*$",
      "maxLength": 500
    },
    "Timeout": {
      "type": "integer",
      "minimum": 1,
      "maximum": 300
    },
    "EnableFeature": {
      "type": "boolean"
    }
  },
  "required": ["ConnectionString"],
  "additionalProperties": false
}
```

## Monitoring and Alerting
- Log all configuration validation failures
- Monitor for suspicious configuration patterns
- Alert on repeated validation failures
- Track configuration change frequency

## Implementation Steps
1. **Create schema registry** for all provider configurations
2. **Implement validation pipeline** with size and pattern checks
3. **Add sanitization layer** to clean potentially dangerous input
4. **Deploy monitoring** for configuration security events

## Priority
**Medium** - Security vulnerability requiring attention

## Labels
- security
- medium-priority
- configuration
- json
- injection
- validation
