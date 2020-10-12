namespace VaultSharp.Extensions.Configuration
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;

    public class VaultChangeWatcher: IDisposable
    {
        private readonly VaultOptions _options;
        private readonly string _basePath;
        private readonly string _mountPoint;
        private readonly ILogger? _logger;
        private readonly List<VaultChangeToken> _changeTokens;

        /// <summary>
        /// Initializes a new instance of the <see cref="VaultChangeWatcher"/> class.
        /// </summary>
        /// <param name="source">Configuration source.</param>
        /// <param name="logger">Logger instance.</param>
        public VaultChangeWatcher(VaultConfigurationSource source, ILogger? logger = null)
        {
            this._options = source.Options;
            this._basePath = source.BasePath;
            this._mountPoint = source.MountPoint;
            this._logger = logger;
            this._changeTokens = new List<VaultChangeToken>();
        }

        /// <summary>
        ///     <para>Creates a <see cref="IChangeToken" />.</para>
        /// </summary>
        /// <returns>
        /// An <see cref="IChangeToken" /> that is notified when a Vault data matching configuration is added,
        /// modified or deleted.
        /// </returns>
        public IChangeToken Watch()
        {
            VaultChangeToken changeToken = new VaultChangeToken();
            this._changeTokens.Add(changeToken);
            return changeToken;
        }
    }
}
