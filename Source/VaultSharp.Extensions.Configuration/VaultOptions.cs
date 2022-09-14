namespace VaultSharp.Extensions.Configuration
{
    using System;
    using System.Collections.Generic;
    using VaultSharp.V1.AuthMethods;

    /// <summary>
    /// Vault options class.
    /// </summary>
    public class VaultOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VaultOptions"/> class.
        /// </summary>
        /// <param name="vaultAddress">Vault address.</param>
        /// <param name="vaultToken">Vault token.</param>
        /// <param name="vaultSecret">Vault secret.</param>
        /// <param name="vaultRoleId">Vault Role ID.</param>
        /// <param name="reloadOnChange">Reload secrets if changed in Vault.</param>
        /// <param name="reloadCheckIntervalSeconds">Interval in seconds to check Vault for any changes.</param>
        /// <param name="omitVaultKeyName">Omit Vault Key Name in Configuration Keys.</param>
        /// <param name="additionalCharactersForConfigurationPath">Additional characters for the Configuration path.</param>
        /// <param name="namespace">Vault namespace.</param>
        public VaultOptions(
            string vaultAddress,
            string? vaultToken,
            string? vaultSecret = null,
            string? vaultRoleId = null,
            bool reloadOnChange = false,
            int reloadCheckIntervalSeconds = 300,
            bool omitVaultKeyName = false,
            IEnumerable<char>? additionalCharactersForConfigurationPath = null,
            string? @namespace = null)
        {
            this.VaultAddress = vaultAddress;
            this.VaultToken = vaultToken;
            this.VaultSecret = vaultSecret;
            this.VaultRoleId = vaultRoleId;
            this.ReloadOnChange = reloadOnChange;
            this.ReloadCheckIntervalSeconds = reloadCheckIntervalSeconds;
            this.OmitVaultKeyName = omitVaultKeyName;
            this.AdditionalCharactersForConfigurationPath = additionalCharactersForConfigurationPath ?? Array.Empty<char>();
            this.Namespace = @namespace;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VaultOptions"/> class.
        /// </summary>
        /// <param name="vaultAddress">Vault address.</param>
        /// <param name="authMethod">Vault auth method.</param>
        /// <param name="reloadOnChange">Reload secrets if changed in Vault.</param>
        /// <param name="reloadCheckIntervalSeconds">Interval in seconds to check Vault for any changes.</param>
        /// <param name="omitVaultKeyName">Omit Vault Key Name in Configuration Keys.</param>
        /// <param name="additionalCharactersForConfigurationPath">Additional characters for the Configuration path.</param>
        /// <param name="namespace">Vault namespace.</param>
        public VaultOptions(
            string vaultAddress,
            IAuthMethodInfo authMethod,
            bool reloadOnChange = false,
            int reloadCheckIntervalSeconds = 300,
            bool omitVaultKeyName = false,
            IEnumerable<char>? additionalCharactersForConfigurationPath = null,
            string? @namespace = null)
        {
            this.VaultAddress = vaultAddress;
            this.AuthMethod = authMethod;
            this.ReloadOnChange = reloadOnChange;
            this.ReloadCheckIntervalSeconds = reloadCheckIntervalSeconds;
            this.OmitVaultKeyName = omitVaultKeyName;
            this.AdditionalCharactersForConfigurationPath = additionalCharactersForConfigurationPath ?? Array.Empty<char>();
            this.Namespace = @namespace;
        }

        /// <summary>
        /// Gets Vault Auth method
        /// </summary>
        public IAuthMethodInfo? AuthMethod { get; }

        /// <summary>
        /// Gets Vault URL address.
        /// </summary>
        public string VaultAddress { get; }

        /// <summary>
        /// Gets Vault access token. Used for token-based authentication.
        /// </summary>
        public string? VaultToken { get; }

        /// <summary>
        /// Gets Vault secret. Used for role-based authentication.
        /// </summary>
        public string? VaultSecret { get; }

        /// <summary>
        /// Gets Vault role identifier. Used for role-based authentication.
        /// </summary>
        public string? VaultRoleId { get; }

        /// <summary>
        /// Gets a value indicating whether gets value indicating that secrets should be re-read when they are changed in Vault.
        /// In this case Reload token will be triggered.
        /// </summary>
        public bool ReloadOnChange { get; }

        /// <summary>
        /// Gets interval in seconds to check Vault for any changes.
        /// </summary>
        public int ReloadCheckIntervalSeconds { get; }

        /// <summary>
        /// Gets a value indicating whether the Vault key should be ommited when generation Configuration key names.
        /// </summary>
        public bool OmitVaultKeyName { get;  }

        /// <summary>
        /// Gets an array of characters that will be used as a path to form the Configuration.
        /// </summary>
        public IEnumerable<char> AdditionalCharactersForConfigurationPath { get; }

        /// <summary>
        /// Gets Vault namespace.
        /// </summary>
        public string? Namespace { get; }
    }
}
