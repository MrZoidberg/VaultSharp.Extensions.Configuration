namespace VaultSharp.Extensions.Configuration
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using VaultSharp.V1.SecretsEngines;

    /// <summary>
    /// Vault configuration source.
    /// </summary>
    public sealed class VaultConfigurationSource : IConfigurationSource
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
        /// <param name="useV1Engine">Use v1 secrets engine.</param>
        public VaultConfigurationSource(VaultOptions options, string basePath, string? mountPoint = null, ILogger? logger = null, bool? useV1Engine = null)
        {
            this._logger = logger;
            this.Options = options;
            this.BasePath = basePath;

            if (useV1Engine == true)
            {
                this.UseV1Engine = true;
            }

            this.MountPoint = mountPoint ?? (!this.UseV1Engine ? SecretsEngineMountPoints.Defaults.KeyValueV2 : SecretsEngineMountPoints.Defaults.KeyValueV1);
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
        /// Gets a value indicating whether gets base path for vault URLs.
        /// </summary>
        public bool UseV1Engine { get; }

        /// <summary>
        /// Build configuration provider.
        /// </summary>
        /// <param name="builder">Configuration builder.</param>
        /// <returns>Instance of <see cref="IConfigurationProvider"/>.</returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder) => new VaultConfigurationProvider(this, this._logger);
    }
}
