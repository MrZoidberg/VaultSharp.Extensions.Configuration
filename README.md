# VaultSharp.Extensions.Configuration

[![GitHub Actions Status](https://github.com/MrZoidberg/VaultSharp.Extensions.Configuration/workflows/Build/badge.svg?branch=master)](https://github.com/MrZoidberg/VaultSharp.Extensions.Configuration/actions) ![Nuget](https://img.shields.io/nuget/v/VaultSharp.Extensions.Configuration) [![license](https://img.shields.io/github/license/MrZoidberg/VaultSharp.Extensions.Configuration.svg)](LICENSE)

VaultSharp.Extensions.Configuration is an extension to [VaultSharp](https://github.com/rajanadar/VaultSharp) that allows reading configuration options from Vault.

## Get Started

VaultSharp.Extensions.Configuration can be installed using the Nuget package manager or the dotnet CLI.

`dotnet add package VaultSharp.Extensions.Configuration`

It can be injected as a [IConfigurationProvider](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationprovider?view=dotnet-plat-ext-3.1)
to load configuration from HashiCorp Vault:

```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddJsonFile("appsettings.json")
                .AddVaultConfiguration(() => new VaultOptions("http://localhost:8200", "root"), "sampleapp", "secret");
        })
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
```

The `AddVaultConfiguration` method accepts several parameters:

1. Function to provide VaultOptions with Vault connection configuration (optional).

2. Application alias in Vault data. It's used a part of the path to read secrets.

3. Mount point of KV secrets. The default value is `secret` (optional).

## Configuration using environmnt variables

Alternatively, you can configure Vault connection using next environmnt variables:

- `VAULT_ADDR` : Address of the Vault instance. Default value is `"http://locahost:8200`.
- `VAULT_TOKEN` : Vault token. Used for token-based authentication. Default value is `root`.
- `VAULT_ROLEID` : Vault AppRole ID. Used for AppRole-based authentication.
- `VAULT_SECRET` : Vault AppRole secret. Used for AppRole-based authentication.

## Preparing secrets in Vault

You need to store your secrets with special naming rules.
First of all, all secrets should use KV2 storage and have prefix `{app_alias}/{env}`.
For example, if your app has alias `sampleapp` and environment `producton` and you want to have configuration option `ConnectionString` your secret path would be `sampleapp/producton`.

All parameters are grouped and arranged in folders and can be managed within the group. All secret data should use JSON format with secret data inside:
```json
{
    "ConnectionString": "secret value",
    "Option1": "secret value 2",
}
```
### Nested secrets

There are two ways to create nested parameters.
1. Description of nesting directly in Json format.:
```json
{
    "DB": 
    {
        "ConnectionString": "secret value"
    }
}
```
2. Creating a parameter on the desired path "sampleapp/producton/DB":
```json
{
    "ConnectionString": "secret value"
}
```

## Limitations

- Currently, only token and AppRole based authentication is supported.
- Reload tokens are not supported.
- TTL of the secrets is not controlled.
