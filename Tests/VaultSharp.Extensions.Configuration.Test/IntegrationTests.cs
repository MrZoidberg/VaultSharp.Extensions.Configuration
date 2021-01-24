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

        private async Task LoadDataAsync(Dictionary<string, IEnumerable<KeyValuePair<string, object>>> values)
        {
            var authMethod = new TokenAuthMethodInfo("root");

            var vaultClientSettings = new VaultClientSettings("http://localhost:8200", authMethod);
            IVaultClient vaultClient = new VaultClient(vaultClientSettings);

            foreach (var sectionPair in values)
            {
                var data = new Dictionary<string, object>();
                foreach (var pair in sectionPair.Value)
                {
                    data.Add(pair.Key, pair.Value);
                }

                await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(sectionPair.Key, data)
                    .ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Success_SimpleTest_TokenAuth()
        {
            // arrange
            var values =
                new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
                {
                    {
                        "test", new[]
                        {
                            new KeyValuePair<string, object>("option1", "value1"),
                            new KeyValuePair<string, object>("option3", 5),
                            new KeyValuePair<string, object>("option4", true),
                            new KeyValuePair<string, object>("option5", new[] {"v1", "v2", "v3"}),
                            new KeyValuePair<string, object>("option6",
                                new[]
                                {
                                    new TestConfigObject() {OptionA = "a1", OptionB = "b1"},
                                    new TestConfigObject() {OptionA = "a2", OptionB = "b2"},
                                }),
                        }
                    },
                    {
                        "test/subsection", new[]
                        {
                            new KeyValuePair<string, object>("option2", "value2"),
                        }
                    },
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
                configurationRoot.GetValue<int>("option3").Should().Be(5);
                configurationRoot.GetValue<bool>("option4").Should().Be(true);
                configurationRoot.GetValue<string>("option5:0").Should().Be("v1");
                configurationRoot.GetValue<string>("option5:1").Should().Be("v2");
                configurationRoot.GetValue<string>("option5:2").Should().Be("v3");
                var t1 = new TestConfigObject();
                configurationRoot.Bind("option6:0", t1);
                t1.OptionA.Should().Be("a1");
                t1.OptionB.Should().Be("b1");
                var t2 = new TestConfigObject();
                configurationRoot.Bind("option6:1", t2);
                t2.OptionA.Should().Be("a2");
                t2.OptionB.Should().Be("b2");
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

            var values =
                new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
                {
                    {"test", new[] {new KeyValuePair<string, object>("option1", "value1"),}},
                    {"test/subsection", new[] {new KeyValuePair<string, object>("option2", "value2"),}},
                };

            var container = this.PrepareVaultContainer();
            try
            {
                await container.StartAsync().ConfigureAwait(false);
                await this.LoadDataAsync(values).ConfigureAwait(false);


                // act
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddVaultConfiguration(
                    () => new VaultOptions("http://localhost:8200", "root", reloadOnChange: true,
                        reloadCheckIntervalSeconds: 10),
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
                values = new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
                {
                    {"test", new[] {new KeyValuePair<string, object>("option1", "value1_new"),}},
                    {"test/subsection", new[] {new KeyValuePair<string, object>("option2", "value2_new"),}},
                    {"test/subsection3", new[] {new KeyValuePair<string, object>("option3", "value3_new"),}},
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

    public class TestConfigObject
    {
        public string OptionA { get; set; }

        public string OptionB { get; set; }
    }
}
