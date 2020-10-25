namespace VaultSharp.Extensions.Configuration
{
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
        public VaultOptions(
            string vaultAddress,
            string? vaultToken,
            string? vaultSecret = null,
            string? vaultRoleId = null,
            bool reloadOnChange = false,
            int reloadCheckIntervalSeconds = 300)
        {
            this.VaultAddress = vaultAddress;
            this.VaultToken = vaultToken;
            this.VaultSecret = vaultSecret;
            this.VaultRoleId = vaultRoleId;
            this.ReloadOnChange = reloadOnChange;
            this.ReloadCheckIntervalSeconds = reloadCheckIntervalSeconds;
        }

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
    }
}
