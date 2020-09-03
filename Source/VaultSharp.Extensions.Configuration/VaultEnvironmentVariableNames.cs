namespace VaultSharp.Extensions.Configuration
{
    /// <summary>
    /// Class contains all environment variables names related to vault configuration.
    /// </summary>
    public static class VaultEnvironmentVariableNames
    {
        /// <summary>
        /// Environment variable name for Vault address.
        /// </summary>
        public const string Address = "VAULT_ADDR";

        /// <summary>
        ///  Environment variable name for Vault token.
        /// </summary>
        public const string Token = "VAULT_TOKEN";

        /// <summary>
        ///  Environment variable name for Vault RoleId.
        /// </summary>
        public const string RoleId = "VAULT_ROLEID";

        /// <summary>
        ///  Environment variable name for Vault secret.
        /// </summary>
        public const string Secret = "VAULT_SECRET";
    }
}
