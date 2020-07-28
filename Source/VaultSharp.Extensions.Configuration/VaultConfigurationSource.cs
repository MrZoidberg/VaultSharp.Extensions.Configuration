namespace VaultSharp.Extensions.Configuration
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using VaultSharp.V1.SecretsEngines;

    /// <summary>
    /// Vault configuration source.
    /// </summary>
    public class VaultConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Default Vault URL.
        /// </summary>
        internal const string DefaultVaultUrl = "http://locahost:8200";

        /// <summary>
        /// Default Vault token.
        /// </summary>
        internal const string DefaultVaultToken = "root";

        private readonly ILogger? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="VaultConfigurationSource"/> class.
        /// </summary>
        /// <param name="options">Vault options.</param>
        /// <param name="basePath">Base path.</param>
        /// <param name="mountPoint">Mounting point.</param>
        /// <param name="logger">Logger instance.</param>
        public VaultConfigurationSource(VaultOptions options, string basePath, string? mountPoint = null, ILogger? logger = null)
        {
            this._logger = logger;
            this.Options = options;
            this.BasePath = "data/" + basePath + "/";
            this.MountPoint = mountPoint ?? SecretsEngineDefaultPaths.KeyValueV2;
        }

        /// <summary>
        /// Gets Vault connection options.
        /// </summary>
        public VaultOptions Options { get; }

        /// <summary>
        /// Gets base path for vault URLs.
        /// </summary>
        public string BasePath { get; }

        /// <summary>
        /// Gets mounting point.
        /// </summary>
        public string MountPoint { get; }

        /// <summary>
        /// Build configuration provider.
        /// </summary>
        /// <param name="builder">Configuration builder.</param>
        /// <returns>Instance of <see cref="IConfigurationProvider"/>.</returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder) => new VaultConfigurationProvider(this, this._logger);
    }
}
