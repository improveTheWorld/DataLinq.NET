# DataLinq.Snowflake v1.2.0

**Release Date:** March 19, 2026
**Requires:** DataLinq.NET 1.0.0+

## ✨ Key-Pair Authentication

Connect using RSA key-pair auth for service accounts — no password required:

```csharp
await using var context = Snowflake.Connect(
    account: "xy12345.us-east-1",
    user: "svc_account",
    privateKeyFile: new FileInfo("path/to/rsa_key.p8"),
    database: "MY_DB",
    warehouse: "MY_WAREHOUSE"
);
```

Supports PKCS#8 undecrypted private key files (`.pem` or `.p8`). Also available via the `configure` callback: `opts.PrivateKey` (raw string) or `opts.PrivateKeyFile` (file path).

## ✨ Simplified Licensing

The free tier is now the default behavior — **no environment variables required**. The 1,000-row cap applies automatically until a production license key is set.

## 🔧 Bug Fixes

- **Column naming**: Fixed `ToSnakeCase` handling of consecutive uppercase letters (`IPAddress` → `ip_address`, `HTTPSUrl` → `https_url`)
- **Write result type**: Write and merge operations now return `long` (instead of `int`) for affected row counts, matching Snowflake's native `NUMBER` return type
- **Parameter contamination**: Fixed bind variables leaking across set operations (`Union`, `Intersect`, `Except`)

## Validation

540 integration tests passed with zero failures across 44 batch suites.
