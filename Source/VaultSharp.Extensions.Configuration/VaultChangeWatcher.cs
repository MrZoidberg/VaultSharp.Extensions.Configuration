namespace VaultSharp.Extensions.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// Background service to notify about Vault data changes.
    /// </summary>
    public class VaultChangeWatcher : BackgroundService
    {
        private readonly ILogger? _logger;
        private VaultConfigurationProvider? _configProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="VaultChangeWatcher"/> class.
        /// test.
        /// </summary>
        /// <param name="configurationRoot">sdfsdf.</param>
        /// <param name="logger">sdlvlkdfgf.</param>
        public VaultChangeWatcher(IConfigurationRoot configurationRoot, ILogger? logger = null)
        {
            if (configurationRoot == null)
            {
                throw new ArgumentNullException(nameof(configurationRoot));
            }

            this._logger = logger;

            this._configProvider = (VaultConfigurationProvider?)configurationRoot.Providers.FirstOrDefault(p =>
                    p is VaultConfigurationProvider);
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (this._configProvider == null || !this._configProvider.ConfigurationSource.Options.ReloadOnChange)
            {
                this._logger?.LogInformation(
                    "VaultChangeWatcher won't work because configuration provider is null or ReloadOnChange is disabled");
                return;
            }

            int waitForSec = this._configProvider.ConfigurationSource.Options.ReloadCheckIntervalSeconds;

            while (!stoppingToken.IsCancellationRequested)
            {
                this._logger?.LogInformation(
                    $"VaultChangeWatcher will wait for {waitForSec} seconds");
                await Task.Delay(TimeSpan.FromSeconds(waitForSec), stoppingToken).ConfigureAwait(false);
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                this._logger?.LogInformation(
                    "Vault configuration reload is triggered by VaultChangeWatcher");
                this._configProvider.Load();
            }
        }
    }
}
