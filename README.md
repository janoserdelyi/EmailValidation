# EmailValidation

[![NuGet](https://img.shields.io/nuget/v/com.janoserdelyi.EmailValidation.svg)](https://www.nuget.org/packages/com.janoserdelyi.EmailValidation/)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL%20v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)

A comprehensive .NET library for email validation, sanitization, and ranking with a functional programming approach. This library consolidates various email validation techniques into a fluent, chainable API that prioritizes low-complexity checks first for optimal performance.

## Features

- **Fluent API**: Chain validation operations in a readable, functional style
- **Comprehensive Validation**: Format validation, parsing, MX record verification
- **Sanitization**: Automatic trimming, case normalization
- **Typo Detection**: Built-in common typo detection and correction
- **Domain Management**: Allow/block specific domains and TLDs
- **Temporary Email Detection**: Block disposable/temporary email services
- **Ranking System**: Score emails based on quality and trustworthiness
- **Result Pattern**: Type-safe error handling with detailed error messages
- **Performance Optimized**: Low-complexity validations first

## Installation

```bash
dotnet add package com.janoserdelyi.EmailValidation
```

## Quick Start

### Basic Validation

```csharp
using com.janoserdelyi.EmailValidation;

// Simple validation
var result = Email.Validator("user@example.com")
    .ValidateFormat()
    .Parse();

if (result.IsSuccess)
{
    var email = result.Value;
    Console.WriteLine($"Valid email: {email.Address}");
    Console.WriteLine($"Local part: {email.LocalPart}");
    Console.WriteLine($"Domain: {email.Domain}");
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

### Full Validation Pipeline

```csharp
var result = Email.Validator(" FOO@EXAMPLE.COM ")
    .Lower()                    // Convert to lowercase
    .Trim()                     // Remove whitespace
    .Ensure(                    // Custom validation
        e => Email.IsLongEnough(e.Address, 5),
        (int)Error.TooShort,
        "Email is too short"
    )
    .ValidateFormat()           // RFC format validation
    .Parse()                    // Parse local and domain parts
    .DisallowTld("edu")         // Block specific TLDs
    .LocalIsValid()             // Validate local part rules
    .CommonTypos()              // Detect and suggest corrections
    .Rank()                     // Calculate quality score
    .VerifyMxRecords(new MxConfig {
        DnsServers = new List<string> { "1.1.1.1" },
        BypassDomains = new List<string> { "gmail.com", "yahoo.com" }
    });

if (result.IsSuccess)
{
    var email = result.Value;
    Console.WriteLine($"Validated: {email.Address}");
    Console.WriteLine($"Rank: {email.StaticRank}");
}
```

### One-Shot Full Validation

```csharp
// Shortcut for full validation pipeline
var result = Email.FullValidation("user@example.com");
```

## API Reference

### Core Methods

| Method | Description |
|--------|-------------|
| `Validator(string)` | Initialize validation pipeline |
| `FullValidation(string)` | Complete validation with all checks |
| `ValidateFormat()` | RFC-compliant format validation |
| `Parse()` | Extract local and domain parts |
| `Lower()` | Convert to lowercase |
| `Trim()` | Remove leading/trailing whitespace |
| `LocalIsValid()` | Validate local part rules |
| `CommonTypos()` | Detect common typos |
| `Rank()` | Calculate quality score |

### Domain Management

| Method | Description |
|--------|-------------|
| `DisallowTld(string)` | Block specific top-level domains |
| `DisallowDomains(List<string>)` | Block specific domains (blocklist mode) |
| `AllowDomains(List<string>)` | Allow only specific domains (allowlist mode) |
| `DisallowTemporaryServiceDomains()` | Block disposable email services |

### Network Validation

| Method | Description |
|--------|-------------|
| `VerifyMxRecords(MxConfig?)` | Verify domain has MX records |

### Custom Validation

```csharp
var result = Email.Validator("test@example.com")
    .Ensure(
        email => email.Address.Contains("@"),
        (int)Error.InvalidFormat,
        "Email must contain @ symbol"
    )
    .Ensure(
        email => !email.Address.StartsWith("."),
        (int)Error.InvalidFormat,
        "Email cannot start with a dot"
    );
```

## Configuration

### MX Record Verification

```csharp
var config = new MxConfig
{
    DnsServers = new List<string> { "8.8.8.8", "1.1.1.1" },
    BypassDomains = new List<string> { "gmail.com", "outlook.com" }
};

var result = Email.Validator("user@example.com")
    .VerifyMxRecords(config);
```

### Temporary Service Detection

```csharp
var config = new TemporaryServiceConfig
{
    // Configuration options for temporary email detection
};

var result = await Email.Validator("user@tempmail.com")
    .DisallowTemporaryServiceDomains(config);
```

## Result Pattern

All validation methods return a `Result<Email>` type that provides:

```csharp
public class Result<Email>
{
    public bool IsSuccess { get; }
    public Email? Value { get; }         // Available when IsSuccess = true
    public int ErrorCode { get; }        // Available when IsSuccess = false  
    public string ErrorMessage { get; }  // Available when IsSuccess = false
}
```

## Error Handling

```csharp
var result = Email.Validator("invalid-email")
    .ValidateFormat();

if (!result.IsSuccess)
{
    Console.WriteLine($"Validation failed:");
    Console.WriteLine($"Error Code: {result.ErrorCode}");
    Console.WriteLine($"Error Message: {result.ErrorMessage}");
}
```

## Performance Considerations

The library is designed for performance:

- **Fast-fail approach**: Low-complexity validations run first
- **Lazy evaluation**: Network checks only when explicitly called
- **Caching**: Results can be cached for repeated validations
- **Async support**: Network operations use async/await

## Best Practices

1. **Order matters**: Place fast validations before slow ones
2. **Use appropriate validation level**: Don't over-validate for your use case
3. **Handle errors gracefully**: Always check `IsSuccess` before accessing `Value`
4. **Cache results**: Store validation results for expensive operations
5. **Choose allowlist vs blocklist**: Use `AllowDomains` for restrictive scenarios

## Examples

### Registration Form Validation

```csharp
public async Task<bool> ValidateRegistrationEmail(string email)
{
    var result = await Email.Validator(email)
        .Trim()
        .Lower()
        .ValidateFormat()
        .Parse()
        .LocalIsValid()
        .DisallowTemporaryServiceDomains()
        .VerifyMxRecords();
    
    return result.IsSuccess;
}
```

### Newsletter Signup with Typo Detection

```csharp
public ValidationResult ValidateNewsletterEmail(string email)
{
    var result = Email.Validator(email)
        .Trim()
        .Lower()
        .ValidateFormat()
        .Parse()
        .CommonTypos()
        .Rank();
    
    if (result.IsSuccess)
    {
        return new ValidationResult 
        { 
            IsValid = true, 
            Email = result.Value.Address,
            Quality = result.Value.StaticRank 
        };
    }
    
    return new ValidationResult 
    { 
        IsValid = false, 
        Error = result.ErrorMessage 
    };
}
```

## Dependencies

- [com.janoserdelyi.Validation](https://www.nuget.org/packages/com.janoserdelyi.Validation/) - Result pattern implementation
- [com.janoserdelyi.MailVerifier](https://www.nuget.org/packages/com.janoserdelyi.MailVerifier/) - MX record verification

## License

This project is licensed under the AGPL v3 License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Changelog

### Version 1.5.3
- Current stable release
- Full .NET 8.0 support
- Improved performance and error handling
