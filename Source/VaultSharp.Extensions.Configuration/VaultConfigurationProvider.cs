namespace VaultSharp.Extensions.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.Threading;
    using Newtonsoft.Json.Linq;
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
        private IVaultClient? _vaultClient;
        private Dictionary<string, int> _versionsCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="VaultConfigurationProvider"/> class.
        /// </summary>
        /// <param name="source">Vault configuration source.</param>
        /// <param name="logger">Logger instance.</param>
        public VaultConfigurationProvider(VaultConfigurationSource source, ILogger? logger)
        {
            this._logger = logger;
            this._source = source ?? throw new ArgumentNullException(nameof(source));
            this._versionsCache = new Dictionary<string, int>();
        }

        /// <summary>
        /// Gets <see cref="VaultConfigurationSource"/>.
        /// </summary>
        internal VaultConfigurationSource ConfigurationSource => this._source;

        /// <inheritdoc/>
        public override void Load()
        {
            try
            {
                if (this._vaultClient == null)
                {
                    IAuthMethodInfo authMethod;
                    if (!string.IsNullOrEmpty(this._source.Options.VaultRoleId) &&
                        !string.IsNullOrEmpty(this._source.Options.VaultSecret))
                    {
                        this._logger?.LogDebug("VaultConfigurationProvider: using AppRole authentication");
                        authMethod = new AppRoleAuthMethodInfo(
                            this._source.Options.VaultRoleId,
                            this._source.Options.VaultSecret);
                    }
                    else
                    {
                        this._logger?.LogDebug("VaultConfigurationProvider: using Token authentication");
                        authMethod = new TokenAuthMethodInfo(this._source.Options.VaultToken);
                    }

                    var vaultClientSettings = new VaultClientSettings(this._source.Options.VaultAddress, authMethod)
                    {
                        UseVaultTokenHeaderInsteadOfAuthorizationHeader = true,
                        Namespace = this._source.Options.Namespace,
                    };
                    this._vaultClient = new VaultClient(vaultClientSettings);
                }

                using var ctx = new JoinableTaskContext();
                var jtf = new JoinableTaskFactory(ctx);
                var hasChanges = jtf.RunAsync(
                    async () => await this.LoadVaultDataAsync(this._vaultClient).ConfigureAwait(true)).Join();

                if (hasChanges)
                {
                    this.OnReload();
                }

            }
            catch (Exception e) when (e is VaultApiException || e is System.Net.Http.HttpRequestException)
            {
                this._logger?.Log(LogLevel.Error, e, "Cannot load configuration from Vault");
            }
        }

        private async Task<bool> LoadVaultDataAsync(IVaultClient vaultClient)
        {
            var hasChanges = false;
            await foreach (var secretData in this.ReadKeysAsync(vaultClient, this._source.BasePath))
            {
                this._logger?.LogDebug($"VaultConfigurationProvider: got Vault data with key `{secretData.Key}`");

                var key = secretData.Key;
                key = key.TrimStart('/')[this._source.BasePath.TrimStart('/').Length..].TrimStart('/').Replace('/', ':');
                key = this.ReplaceTheAdditionalCharactersForConfigurationPath(key);
                var data = secretData.SecretData.Data;

                var shouldSetValue = true;
                if (this._versionsCache.TryGetValue(key, out var currentVersion))
                {
                    shouldSetValue = secretData.SecretData.Metadata.Version > currentVersion;
                    string keyMsg = shouldSetValue ? "has new version" : "is outdated";
                    this._logger?.LogDebug($"VaultConfigurationProvider: Data for key `{secretData.Key}` {keyMsg}");
                }

                if (shouldSetValue)
                {
                    this.SetData(data, this.ConfigurationSource.Options.OmitVaultKeyName ? string.Empty : key);
                    hasChanges = true;
                    this._versionsCache[key] = secretData.SecretData.Metadata.Version;
                }
            }

            return hasChanges;
        }

        private void SetData<TValue>(IEnumerable<KeyValuePair<string, TValue>> data, string? key)
        {
            foreach (var pair in data)
            {
                var nestedKey = string.IsNullOrEmpty(key) ? pair.Key : $"{key}:{pair.Key}";
                nestedKey = this.ReplaceTheAdditionalCharactersForConfigurationPath(nestedKey);

                var nestedValue = pair.Value;
                switch (nestedValue)
                {
                    case string sValue:
                        this.Set(nestedKey, sValue);
                        break;
                    case int intValue:
                        this.Set(nestedKey, intValue.ToString(CultureInfo.InvariantCulture));
                        break;
                    case long longValue:
                        this.Set(nestedKey, longValue.ToString(CultureInfo.InvariantCulture));
                        break;
                    case bool boolValue:
                        this.Set(nestedKey, boolValue.ToString(CultureInfo.InvariantCulture));
                        break;
                    case JToken token:
                        switch (token.Type)
                        {
                            case JTokenType.Object:
                            {
                                var jObject = token.Value<JObject>();
                                if (jObject != null)
                                {
                                    this.SetData<JToken?>(jObject, nestedKey);
                                }

                                break;
                            }

                            case JTokenType.None:
                            case JTokenType.Array:
                            {
                                var array = (JArray)token;
                                for (var i = 0; i < array.Count; i++)
                                {
                                    var arrElement = array[i];

                                    if (array[i].Type == JTokenType.Array)
                                    {
                                        this.SetData(new[] { new KeyValuePair<string, JToken?>($"{nestedKey}:{i}", arrElement) }, null);
                                    }
                                    else if (array[i].Type == JTokenType.Object)
                                    {
                                        this.SetData(new[] { new KeyValuePair<string, JToken?>($"{nestedKey}:{i}", arrElement) }, null);

                                        // this.SetData<JToken?>(arrElement, $"{nestedKey}:{i}");
                                    }
                                    else
                                    {
                                        this.Set($"{nestedKey}:{i}", array[i].Value<string>());
                                    }
                                }

                                break;
                            }

                            case JTokenType.Property:
                            case JTokenType.Integer:
                            case JTokenType.Float:
                            case JTokenType.Boolean:
                            case JTokenType.Undefined:
                            case JTokenType.Date:
                            case JTokenType.Raw:
                            case JTokenType.Bytes:
                            case JTokenType.Guid:
                            case JTokenType.Uri:
                            case JTokenType.TimeSpan:
                            case JTokenType.String:
                                this.Set(nestedKey, token.Value<string>());
                                break;
                        }

                        break;
                }
            }
        }

        private async IAsyncEnumerable<KeyedSecretData> ReadKeysAsync(IVaultClient vaultClient, string path)
        {
            Secret<ListInfo>? keys = null;
            var folderPath = path;
            if (folderPath.EndsWith("/", StringComparison.InvariantCulture) == false)
            {
                folderPath += "/";
            }

            try
            {
                keys = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(folderPath, this._source.MountPoint).ConfigureAwait(false);
            }
            catch (VaultApiException)
            {
                // this is key, not a folder
            }

            if (keys != null)
            {
                foreach (var key in keys.Data.Keys)
                {
                    var keyData = this.ReadKeysAsync(vaultClient, folderPath + key);
                    await foreach (var secretData in keyData)
                    {
                        yield return secretData;
                    }
                }
            }

            var valuePath = path;
            if (valuePath.EndsWith("/", StringComparison.InvariantCulture) == true)
            {
                valuePath = valuePath.TrimEnd('/');
            }

            KeyedSecretData? keyedSecretData = null;
            try
            {
                var secretData = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(valuePath, null, this._source.MountPoint)
                    .ConfigureAwait(false);
                keyedSecretData = new KeyedSecretData(valuePath, secretData.Data);
            }
            catch (VaultApiException)
            {
                // this is folder, not a key
            }

            if (keyedSecretData != null)
            {
                yield return keyedSecretData;
            }
        }

        private string ReplaceTheAdditionalCharactersForConfigurationPath(string inputKey)
        {
            if (!this._source.Options.AdditionalCharactersForConfigurationPath.Any())
            {
                return inputKey;
            }

            var outputKey = new StringBuilder(inputKey);

            foreach (var c in this._source.Options.AdditionalCharactersForConfigurationPath)
            {
                outputKey.Replace(c, ':');
            }

            return outputKey.ToString();
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
