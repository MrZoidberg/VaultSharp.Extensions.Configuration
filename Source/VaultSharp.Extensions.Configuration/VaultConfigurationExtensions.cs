namespace VaultSharp.Extensions.Configuration
{
#pragma warning disable CA2000

    using System;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Vault configuration extensions.
    /// </summary>
    public static class VaultConfigurationExtensions
    {
        /// <summary>
        /// Add Vault as a configuration provider.
        /// </summary>
        /// <param name="configuration">Configuration builder instance.</param>
        /// <param name="options">Vault options provider action.</param>
        /// <param name="basePath">Base path for vault keys.</param>
        /// <param name="mountPoint">KV mounting point.</param>
        /// <param name="logger">Logger instance.</param>
        /// <returns>Instance of <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddVaultConfiguration(
            this IConfigurationBuilder configuration,
            Func<VaultOptions> options,
            string basePath,
            string? mountPoint = null,
            ILogger? logger = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _ = options ?? throw new ArgumentNullException(nameof(options));

            var vaultOptions = options();
            configuration.Add(new VaultConfigurationSource(vaultOptions, basePath, mountPoint, logger));
            return configuration;
        }

        /// <summary>
        /// Add Vault as a configuration provider.
        /// </summary>
        /// <param name="configuration">Configuration builder instance.</param>
        /// <param name="basePath">Base path for vault keys.</param>
        /// <param name="mountPoint">KV mounting point.</param>
        /// <param name="logger">Logger instance.</param>
        /// <returns>Instance of <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddVaultConfiguration(
            this IConfigurationBuilder configuration,
            string basePath,
            string? mountPoint = null,
            ILogger? logger = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (basePath == null)
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            var insecureOk = bool.TryParse(Environment.GetEnvironmentVariable(VaultEnvironmentVariableNames.InsecureConnection), out var insecure);

            var vaultOptions = new VaultOptions(
                Environment.GetEnvironmentVariable(VaultEnvironmentVariableNames.Address) ??
                VaultConfigurationSource.DefaultVaultUrl,
                Environment.GetEnvironmentVariable(VaultEnvironmentVariableNames.Token) ?? VaultConfigurationSource.DefaultVaultToken,
                Environment.GetEnvironmentVariable(VaultEnvironmentVariableNames.Secret),
                Environment.GetEnvironmentVariable(VaultEnvironmentVariableNames.RoleId),
                insecureOk && insecure);
            configuration.Add(new VaultConfigurationSource(vaultOptions, basePath, mountPoint, logger));
            return configuration;
        }
    }
#pragma warning restore CA2000
}
