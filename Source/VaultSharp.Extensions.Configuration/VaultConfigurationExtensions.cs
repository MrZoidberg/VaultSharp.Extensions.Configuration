namespace VaultSharp.Extensions.Configuration
{
    using System;
    using Microsoft.Extensions.Configuration;

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
        /// <returns>Instance of <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddVaultConfiguration(
            this IConfigurationBuilder configuration,
            Func<VaultOptions> options,
            string basePath,
            string? mountPoint = null)
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));

            var vaultOptions = options();
            configuration.Add(new VaultConfigurationSource(vaultOptions, basePath, mountPoint));
            return configuration;
        }

        /// <summary>
        /// Add Vault as a configuration provider.
        /// </summary>
        /// <param name="configuration">Configuration builder instance.</param>
        /// <param name="basePath">Base path for vault keys.</param>
        /// <param name="mountPoint">KV mounting point.</param>
        /// <returns>Instance of <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddVaultConfiguration(
            this IConfigurationBuilder configuration,
            string basePath,
            string? mountPoint = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (basePath == null)
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            var vaultOptions = new VaultOptions(
                Environment.GetEnvironmentVariable(VaultEnvironmentVariableNames.Address) ??
                VaultConfigurationSource.DefaultVaultUrl,
                Environment.GetEnvironmentVariable(VaultEnvironmentVariableNames.Token) ?? VaultConfigurationSource.DefaultVaultToken,
                Environment.GetEnvironmentVariable(VaultEnvironmentVariableNames.Secret),
                Environment.GetEnvironmentVariable(VaultEnvironmentVariableNames.RoleId));
            configuration.Add(new VaultConfigurationSource(vaultOptions, basePath, mountPoint));
            return configuration;
        }
    }
}
