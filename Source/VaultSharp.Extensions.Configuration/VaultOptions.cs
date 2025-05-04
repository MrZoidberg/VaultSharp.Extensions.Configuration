namespace VaultSharp.Extensions.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
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
        /// <param name="keyPrefix">Store all Vault keys under this prefix </param>
        /// <param name="additionalCharactersForConfigurationPath">Additional characters for the Configuration path.</param>
        /// <param name="additionalStringsForConfigurationPath">Additional strings for the Configuration path.</param>
        /// <param name="namespace">Vault namespace.</param>
        /// <param name="alwaysAddTrailingSlashToBasePath">Should a trailing slash be added to the base path. See AlwaysAddTrailingSlashToBasePath property for details </param>
        /// <param name="insecureConnection">(Dangerous!) Ignore certificate validation. This implies self-signed certificates are accepted.</param>
        /// <param name="serverCertificateCustomValidationCallback">An optional action to post-process the HttpClientHandler. Used to manually validate the server certificate. Ignored if AcceptInsecureConnection is true.</param>
        public VaultOptions(
            string vaultAddress,
            string? vaultToken,
            string? vaultSecret = null,
            string? vaultRoleId = null,
            bool reloadOnChange = false,
            int reloadCheckIntervalSeconds = 300,
            bool omitVaultKeyName = false,
            string? keyPrefix = null,
            IEnumerable<char>? additionalCharactersForConfigurationPath = null,
            IEnumerable<string>? additionalStringsForConfigurationPath = null,
            string? @namespace = null,
            bool alwaysAddTrailingSlashToBasePath = true,
            bool insecureConnection = false,
            Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? serverCertificateCustomValidationCallback = null)
        {
            this.VaultAddress = vaultAddress;
            this.VaultToken = vaultToken;
            this.VaultSecret = vaultSecret;
            this.VaultRoleId = vaultRoleId;
            this.ReloadOnChange = reloadOnChange;
            this.ReloadCheckIntervalSeconds = reloadCheckIntervalSeconds;
            this.OmitVaultKeyName = omitVaultKeyName;
            this.KeyPrefix = keyPrefix;
            this.AdditionalCharactersForConfigurationPath = additionalCharactersForConfigurationPath ?? Array.Empty<char>();
            this.AdditionalStringsForConfigurationPath = [..additionalStringsForConfigurationPath ?? [], ..(additionalCharactersForConfigurationPath ?? []).Select(x => x.ToString())];
            this.Namespace = @namespace;
            this.AlwaysAddTrailingSlashToBasePath = alwaysAddTrailingSlashToBasePath;
            this.AcceptInsecureConnection = insecureConnection;
            this.ServerCertificateCustomValidationCallback = serverCertificateCustomValidationCallback;
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
        /// <param name="additionalStringsForConfigurationPath">Additional strings for the Configuration path.</param>
        /// <param name="namespace">Vault namespace.</param>
        /// <param name="alwaysAddTrailingSlashToBasePath">Should a trailing slash be added to the base path. See AlwaysAddTrailingSlashToBasePath property for details </param>
        /// <param name="insecureConnection">(Dangerous!) Ignore certificate validation. This implies self-signed certificates are accepted.</param>
        /// <param name="serverCertificateCustomValidationCallback">An optional action to post-process the HttpClientHandler. Used to manually validate the server certificate. Ignored if AcceptInsecureConnection is true.</param>
        public VaultOptions(
            string vaultAddress,
            IAuthMethodInfo authMethod,
            bool reloadOnChange = false,
            int reloadCheckIntervalSeconds = 300,
            bool omitVaultKeyName = false,
            IEnumerable<char>? additionalCharactersForConfigurationPath = null,
            IEnumerable<string>? additionalStringsForConfigurationPath = null,
            string? @namespace = null,
            bool alwaysAddTrailingSlashToBasePath = true,
            bool insecureConnection = false,
            Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? serverCertificateCustomValidationCallback = null)
        {
            this.VaultAddress = vaultAddress;
            this.AuthMethod = authMethod;
            this.ReloadOnChange = reloadOnChange;
            this.ReloadCheckIntervalSeconds = reloadCheckIntervalSeconds;
            this.OmitVaultKeyName = omitVaultKeyName;
            this.AdditionalCharactersForConfigurationPath = additionalCharactersForConfigurationPath ?? Array.Empty<char>();
            this.AdditionalStringsForConfigurationPath = [..additionalStringsForConfigurationPath ?? [], ..(additionalCharactersForConfigurationPath ?? []).Select(x => x.ToString())];
            this.Namespace = @namespace;
            this.AlwaysAddTrailingSlashToBasePath = alwaysAddTrailingSlashToBasePath;
            this.AcceptInsecureConnection = insecureConnection;
            this.ServerCertificateCustomValidationCallback = serverCertificateCustomValidationCallback;
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
        public bool OmitVaultKeyName { get; }

        /// <summary>
        /// Store all read keys under this Configuration key name prefix.
        /// </summary>
        public string? KeyPrefix { get; }

        /// <summary>
        /// Gets an array of characters that will be used as a path to form the Configuration.
        /// This may not be equal to <see cref="AdditionalStringsForConfigurationPath"/>.
        /// </summary>
        public IEnumerable<char> AdditionalCharactersForConfigurationPath { get; }

        /// <summary>
        /// Gets an array of strings that will be used as a path to form the Configuration.
        /// Contains string values of <see cref="AdditionalCharactersForConfigurationPath"/>.
        /// </summary>
        public IEnumerable<string> AdditionalStringsForConfigurationPath { get; }

        /// <summary>
        /// Gets Vault namespace.
        /// </summary>
        public string? Namespace { get; }

        /// <summary>
        /// Gets the value indicating whether to always add a trailing slash to the base path.
        /// If it is true, the base path is considered to be a "folder" with nested keys, otherwise the base path is considered to be a key itself.
        /// It is true by default. Set to false if you don't have permissions to list keys in the base path.
        /// </summary>
        public bool AlwaysAddTrailingSlashToBasePath { get; }

        /// <summary>
        /// Indicates whether we should disregard the certificate validation (for examples, servers behind Internet aren't likely to have a strong certs but we can't afford to use HTTP either)
        /// Previously, the certificate behavior can be set globally, but subsequently removed in .NET Core and onwards due to security reasons.
        /// We need to set the behavior to each HttpClient on a case-by-case basis. As such, this option is provided as a resolution.
        /// If it is true, a custom PostProcessHttpClientHandlerAction will be injected to the VaultClientSettings to accept any server certificate.
        /// Default value: false. Hashicorp also recommend using a proper CA to setup Vault access due to security concerns.
        /// </summary>
        public bool AcceptInsecureConnection { get; }

        /// <summary>
        /// An optional action to post-process the HttpClientHandler. Used to manually validate the server certificate. Ignored if AcceptInsecureConnection is true.
        /// </summary>
        public Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? ServerCertificateCustomValidationCallback { get; set;}

        /// <summary>
        /// An optional hook to allow custom configuration of the HttpClientHandler.
        /// This is useful if you need to customize the HTTP client's proxy settings, for example.
        /// </summary>
        /// <remarks>
        /// The action will be invoked after the VaultSharp provider applies the AcceptInsecureConnection and ServerCertificateCustomValidationCallback
        /// customizations, if you enabled them.  Be aware that if you overwrite the HttpMessageHandler's ServerCertificateCustomValidationCallback
        /// in your action-handler method, you will cancel out the effect of enabling the AcceptInsecureConnection and/or
        /// ServerCertificateCustomValidationCallback options.
        /// </remarks>
        public Action<HttpMessageHandler>? PostProcessHttpClientHandlerAction { get; set; }
    }
}
