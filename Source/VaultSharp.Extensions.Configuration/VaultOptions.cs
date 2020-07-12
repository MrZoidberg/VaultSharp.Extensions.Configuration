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
        public VaultOptions(string vaultAddress, string vaultToken, string vaultSecret, string vaultRoleId)
        {
            this.VaultAddress = vaultAddress;
            this.VaultToken = vaultToken;
            this.VaultSecret = vaultSecret;
            this.VaultRoleId = vaultRoleId;
        }

        /// <summary>
        /// Gets Vault URL address.
        /// </summary>
        public string VaultAddress { get; }

        /// <summary>
        /// Gets Vault access token.
        /// </summary>
        public string VaultToken { get; }

        /// <summary>
        /// Gets Vault secret.
        /// </summary>
        public string VaultSecret { get; }

        /// <summary>
        /// Gets Vault role identifier.
        /// </summary>
        public string VaultRoleId { get; }
    }
}
