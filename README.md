# VaultSharp.Extensions.Configuration

[![GitHub Actions Status](https://github.com/MrZoidberg/VaultSharp.Extensions.Configuration/workflows/Build/badge.svg?branch=master)](https://github.com/MrZoidberg/VaultSharp.Extensions.Configuration/actions) ![Nuget](https://img.shields.io/nuget/v/VaultSharp.Extensions.Configuration)

VaultSharp.Extensions.Configuration is an extension to VaultSharp that allows reading configuration options from Vault.

## Get Started

VaultSharp.Extensions.Configuration can be installed using the Nuget package manager or the dotnet CLI.

`dotnet add package VaultSharp.Extensions.Configuration`

It can be injected as a [IConfigurationProvider](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationprovider?view=dotnet-plat-ext-3.1)
to load configuration from HashiCorp Vault.



## Limitations

Currently, only token and AppRole based authentication is supported.
