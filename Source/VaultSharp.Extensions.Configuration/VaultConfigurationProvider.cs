namespace VaultSharp.Extensions.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.Threading;
    using VaultSharp;
    using VaultSharp.Core;
    using VaultSharp.V1.AuthMethods;
    using VaultSharp.V1.AuthMethods.AppRole;
    using VaultSharp.V1.AuthMethods.Token;
    using VaultSharp.V1.Commons;

    /// <summary>
    /// Vault configuration provider.
    /// </summary>
    public class VaultConfigurationProvider : ConfigurationProvider
    {
        private readonly ILogger? _logger;

        private VaultConfigurationSource _source;

        /// <summary>
        /// Initializes a new instance of the <see cref="VaultConfigurationProvider"/> class.
        /// </summary>
        /// <param name="source">Vault configuration source.</param>
        /// <param name="logger">Logger instance.</param>
        public VaultConfigurationProvider(VaultConfigurationSource source, ILogger? logger)
        {
            this._logger = logger;
            this._source = source;
        }

        /// <inheritdoc/>
        public override void Load()
        {
            try
            {
                IAuthMethodInfo authMethod;
                if (!string.IsNullOrEmpty(this._source.Options.VaultRoleId) &&
                    !string.IsNullOrEmpty(this._source.Options.VaultSecret))
                {
                    authMethod = new AppRoleAuthMethodInfo(
                        this._source.Options.VaultRoleId,
                        this._source.Options.VaultSecret);
                }
                else
                {
                    authMethod = new TokenAuthMethodInfo(this._source.Options.VaultToken);
                }

                var vaultClientSettings = new VaultClientSettings(this._source.Options.VaultAddress, authMethod)
                {
                    UseVaultTokenHeaderInsteadOfAuthorizationHeader = true,
                };
                IVaultClient vaultClient = new VaultClient(vaultClientSettings);

                using var ctx = new JoinableTaskContext();
                var jtf = new JoinableTaskFactory(ctx);
                jtf.RunAsync(
                    async () => { await this.LoadVaultDataAsync(vaultClient).ConfigureAwait(true); }).Join();
            }
            catch (Exception e) when (e is VaultApiException || e is System.Net.Http.HttpRequestException)
            {
                this._logger?.Log(LogLevel.Error, e, "Cannot load configuration from Vault");
            }
        }

        private async Task LoadVaultDataAsync(IVaultClient vaultClient)
        {
            await foreach (var secretData in this.ReadKeysAsync(vaultClient, this._source.BasePath))
            {
                var key = secretData.Key;
                key = key.Replace(this._source.BasePath, string.Empty, StringComparison.InvariantCultureIgnoreCase)
                    .Replace('/', ':');
                if (secretData.SecretData.Data.ContainsKey("value"))
                {
                    this.Set(key, secretData.SecretData.Data["value"].ToString());
                }
            }
        }

        private async IAsyncEnumerable<KeyedSecretData> ReadKeysAsync(IVaultClient vaultClient, string path)
        {
            Secret<ListInfo>? keys = null;
            try
            {
                keys = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(path, this._source.MountPoint).ConfigureAwait(false);
            }
            catch (VaultApiException)
            {
                // this is key, not a folder
            }

            if (keys != null)
            {
                foreach (var key in keys.Data.Keys)
                {
                    var keyData = this.ReadKeysAsync(vaultClient, path + key);
                    await foreach (var secretData in keyData)
                    {
                        yield return secretData;
                    }
                }
            }
            else
            {
                var secretData =
                    await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, null, this._source.MountPoint).ConfigureAwait(false);
                yield return new KeyedSecretData(path, secretData.Data);
            }
        }

        private class KeyedSecretData
        {
            public KeyedSecretData(string key, SecretData secretData)
            {
                this.Key = key;
                this.SecretData = secretData;
            }

            public string Key { get; }

            public SecretData SecretData { get; }
        }
    }
}
