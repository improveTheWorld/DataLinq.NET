# DataLinq.Snowflake

[![NuGet](https://img.shields.io/nuget/v/DataLinq.Snowflake.svg)](https://www.nuget.org/packages/DataLinq.Snowflake) [![NuGet Downloads](https://img.shields.io/nuget/dt/DataLinq.Snowflake.svg)](https://www.nuget.org/packages/DataLinq.Snowflake) ![Tests](https://img.shields.io/badge/tests-418%20passing-brightgreen) ![Code Coverage](https://img.shields.io/badge/code%20coverage-85%25-blue) ![LINQ Coverage](https://img.shields.io/badge/LINQ%20coverage-~90%25-blue) ![SQL Injection](https://img.shields.io/badge/SQL%20injection-100%25%20parameterized-green) ![.NET](https://img.shields.io/badge/.NET-8.0+-purple)

LINQ-native Snowflake integration for DataLinq.NET.

```bash
dotnet add package DataLinq.Snowflake
```

> **Free dev tier included** — 1,000 rows, no license key, no credit card. The [core DataLinq.NET package](https://www.nuget.org/packages/DataLinq.NET) (streaming, SUPRA pattern, Cases, EF Core) is **Apache 2.0 free** and a dependency.

📖 **[Full Documentation](https://github.com/improveTheWorld/DataLinq.NET/blob/main/docs/LINQ-to-Snowflake.md)**  | **[DataLinq.NET on GitHub](https://github.com/improveTheWorld/DataLinq.NET)** | 🌐 **[Product Website](https://get-datalinq.net/)**

## Features

- **Native LINQ Translation** - Write C# LINQ, execute Snowflake SQL
- **Streaming Results** - Row-by-row processing with `IAsyncEnumerable`
- **Type Safety** - Strong typing with automatic column mapping
- **SQL Injection Prevention** - Parameterized queries by default
- **O(1) Memory Writes** - Native streaming via PUT + COPY INTO
- **Cases Pattern** - Multi-output conditional routing
- **Auto-UDF** — Custom methods in Where/Select/OrderBy/GroupBy auto-translate to Snowflake UDFs (static, instance, lambda, entity-param)
- **ForEach** — Server-side iteration via stored procedures with static field sync-back
- **Pull() Escape Hatch** - Switch to client-side streaming for edge cases

## Quick Start

```csharp
using DataLinq.SnowflakeQuery;

// Connect to Snowflake
using var context = Snowflake.Connect(
    account: "xy12345.us-east-1",
    user: "myuser",
    password: "mypass",
    database: "MYDB",
    warehouse: "COMPUTE_WH"
);

// Query with LINQ (server-side SQL)
var orders = await context.Read.Table<Order>("orders")
    .Where(o => o.Amount > 1000)
    .OrderByDescending(o => o.OrderDate)
    .Take(100)
    .ToList();

// Client-side processing requires explicit Pull()
await context.Read.Table<Order>("orders")
    .Where(o => o.Status == "Active")    // Server-side SQL
    .Pull()                               // ← Switch to client
    .ForEach(o => Console.WriteLine(o))   // Client-side C#
    .Do();

// Update specific columns only (compile-time safe expression)
await records.MergeTable(context, "ORDERS", o => o.OrderId,
    updateOnly: o => new { o.Status, o.UpdatedAt });
```

## Nested Objects (VARIANT)

Access Snowflake VARIANT columns with natural C# property syntax:

```csharp
// Model with nested properties
public class Order {
    public int Id { get; set; }
    
    [Variant]  // Marks column as VARIANT
    public OrderData Data { get; set; }
}

// Query nested properties - translates to colon syntax
var parisOrders = await context.Read.Table<Order>("ORDERS")
    .Where(o => o.Data.Customer.City == "Paris")
    .ToList();
// SQL: WHERE data:customer:city = 'Paris'
```

## Write Operations

Snowflake uses native `IAsyncEnumerable` streaming - O(1) memory, no config needed:

```csharp
// Bulk insert (streams via PUT + COPY INTO)
await records.WriteTable(context, "ORDERS");
await records.WriteTable(context, "ORDERS", createIfMissing: true);
await records.WriteTable(context, "ORDERS", overwrite: true);
await records.WriteTable(context, "ORDERS", createIfMissing: true, overwrite: true);

// Upsert (merge) on key — all columns updated
await records.MergeTable(context, "ORDERS", o => o.OrderId);

// Upsert — only update specific columns (compile-time safe expression)
await records.MergeTable(context, "ORDERS", o => o.OrderId,
    updateOnly: o => new { o.Status, o.UpdatedAt });

// Multi-column single-property shorthand
await records.MergeTable(context, "ORDERS", o => o.OrderId,
    updateOnly: o => o.Status);
```

## Requirements

- .NET 8.0+
- [DataLinq.Net](https://www.nuget.org/packages/DataLinq.NET) 1.0.0+

## Support & Issues

📧 **Contact**: support@get-datalinq.net  
🐛 **Report Issues**: [github.com/improveTheWorld/DataLinq.NET/issues](https://github.com/improveTheWorld/DataLinq.NET/issues)

## License

### Free Tier (No Setup Required)

DataLinq.Snowflake works out of the box with **no license and no configuration**. The free tier caps queries at **1,000 rows** — no environment variables, no debugger detection, no opt-in needed. Just install and run.

### Production License

For unlimited rows, obtain a license at:
- 🌐 **Pricing**: https://get-datalinq.net/pricing
- 📧 **Contact**: support@get-datalinq.net

Set your license key as an environment variable (auto-detected at runtime):
```bash
# PowerShell
$env:DATALINQ_LICENSE_KEY="your-license-key"

# Bash/Linux/macOS
export DATALINQ_LICENSE_KEY="your-license-key"

# Docker / Kubernetes
ENV DATALINQ_LICENSE_KEY=your-license-key
```

> **Security**: The license key is never in source code. Set it in your deployment environment (CI/CD secrets, Azure Key Vault, AWS Secrets Manager, etc.)
