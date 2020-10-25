namespace VaultSharp.Extensions.Configuration.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNet.Testcontainers.Containers.Builders;
    using DotNet.Testcontainers.Containers.Modules;
    using DotNet.Testcontainers.Containers.WaitStrategies;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.VisualStudio.TestPlatform.TestHost;
    using Serilog;
    using Serilog.Core;
    using Serilog.Extensions.Logging;
    using VaultSharp.V1.AuthMethods.Token;
    using Xunit;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    [CollectionDefinition("VaultSharp.Extensions.Configuration.Tests", DisableParallelization = true)]
    public class IntegrationTests
    {
        private ILogger _logger;

        public IntegrationTests()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();
            this._logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(IntegrationTests));
        }

        private TestcontainersContainer PrepareVaultContainer()
        {
            var testcontainersBuilder = new TestcontainersBuilder<TestcontainersContainer>()
                .WithImage("vault")
                .WithName("vaultsharp_test")
                .WithPortBinding(8200, 8200)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8200))
                .WithEnvironment("VAULT_DEV_ROOT_TOKEN_ID", "root")
                .WithEnvironment("VAULT_DEV_LISTEN_ADDRESS", "0.0.0.0:8200");

            return testcontainersBuilder.Build();
        }

        private async Task LoadDataAsync(Dictionary<string, KeyValuePair<string,string>> values)
        {
            var authMethod = new TokenAuthMethodInfo("root");

            var vaultClientSettings = new VaultClientSettings("http://localhost:8200", authMethod);
            IVaultClient vaultClient = new VaultClient(vaultClientSettings);

            foreach (var pair in values)
            {
                var data = new Dictionary<string, object>() { [pair.Value.Key] = pair.Value.Value };
                await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(pair.Key, data).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Success_SimpleTest_TokenAuth()
        {
            // arrange
            Dictionary<string, KeyValuePair<string, string>> values =
                new Dictionary<string, KeyValuePair<string, string>>
                {
                    { "test", new KeyValuePair<string, string>("option1", "value1") },
                    { "test/subsection", new KeyValuePair<string, string>("option2", "value2") },
                };

            var container = this.PrepareVaultContainer();
            try
            {
                await container.StartAsync().ConfigureAwait(false);
                await this.LoadDataAsync(values).ConfigureAwait(false);

                // act
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddVaultConfiguration(
                    () => new VaultOptions("http://localhost:8200", "root"),
                    "test",
                    "secret",
                    this._logger);
                var configurationRoot = builder.Build();

                // assert
                configurationRoot.GetValue<string>("option1").Should().Be("value1");
                configurationRoot.GetSection("subsection").GetValue<string>("option2").Should().Be("value2");
            }
            finally
            {
                await container.DisposeAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Success_WatcherTest_TokenAuth()
        {
            // arrange
            using CancellationTokenSource cts = new CancellationTokenSource();

            Dictionary<string, KeyValuePair<string, string>> values =
                new Dictionary<string, KeyValuePair<string, string>>
                {
                    { "test", new KeyValuePair<string, string>("option1", "value1") },
                    { "test/subsection", new KeyValuePair<string, string>("option2", "value2") },
                };

            var container = this.PrepareVaultContainer();
            try
            {
                await container.StartAsync().ConfigureAwait(false);
                await this.LoadDataAsync(values).ConfigureAwait(false);


                // act
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddVaultConfiguration(
                    () => new VaultOptions("http://localhost:8200", "root", reloadOnChange: true, reloadCheckIntervalSeconds: 10),
                    "test",
                    "secret",
                    this._logger);
                var configurationRoot = builder.Build();
                VaultChangeWatcher changeWatcher = new VaultChangeWatcher(configurationRoot, this._logger);
                await changeWatcher.StartAsync(cts.Token).ConfigureAwait(false);
                var reloadToken = configurationRoot.GetReloadToken();

                // assert
                configurationRoot.GetValue<string>("option1").Should().Be("value1");
                configurationRoot.GetSection("subsection").GetValue<string>("option2").Should().Be("value2");
                reloadToken.HasChanged.Should().BeFalse();

                // load new data and wait for reload
                values = new Dictionary<string, KeyValuePair<string, string>>
                {
                    { "test", new KeyValuePair<string, string>("option1", "value1_new") },
                    { "test/subsection", new KeyValuePair<string, string>("option2", "value2_new") },
                    { "test/subsection3", new KeyValuePair<string, string>("option3", "value3_new") },
                };
                await this.LoadDataAsync(values).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(true);

                reloadToken.HasChanged.Should().BeTrue();
                configurationRoot.GetValue<string>("option1").Should().Be("value1_new");
                configurationRoot.GetSection("subsection").GetValue<string>("option2").Should().Be("value2_new");
                configurationRoot.GetSection("subsection3").GetValue<string>("option3").Should().Be("value3_new");

                changeWatcher.Dispose();
            }
            finally
            {
                cts.Cancel();
                await container.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
