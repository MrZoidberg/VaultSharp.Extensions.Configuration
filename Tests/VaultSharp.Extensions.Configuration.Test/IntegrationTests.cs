namespace VaultSharp.Extensions.Configuration.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNet.Testcontainers.Builders;
    using DotNet.Testcontainers.Containers;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using Serilog.Extensions.Logging;
    using VaultSharp.Core;
    using VaultSharp.V1.AuthMethods.AppRole;
    using VaultSharp.V1.AuthMethods.Token;
    using Xunit;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    [CollectionDefinition("VaultSharp.Extensions.Configuration.Tests", DisableParallelization = true)]
    public partial class IntegrationTests
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

        private IContainer PrepareVaultContainer(bool enableSSL = false, string? script = null)
        {
            var builder = new ContainerBuilder()
                .WithImage("vault")
                .WithName("vaultsharptest_"+Guid.NewGuid().ToString().Substring(0,8))
                .WithPortBinding(8200, 8200)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8200))
                .WithEnvironment("VAULT_UI", "true")
                .WithEnvironment("VAULT_DEV_ROOT_TOKEN_ID", "root")
                .WithEnvironment("VAULT_DEV_LISTEN_ADDRESS", "0.0.0.0:8200");

            if (enableSSL)
            {
                // docker run -p 8200:8200 -v "${PWD}/certs:/tmp/certs" vault server -dev-tls=true -dev-tls-cert-dir=tmp/certs
                builder = builder.WithCommand(new[] { "server", "-dev-tls=true", "-dev-tls-cert-dir=/tmp/certs" });
                builder = builder.WithBindMount(Path.Combine(Environment.CurrentDirectory, "certs"), "/tmp/certs");
            }

            if (!string.IsNullOrEmpty(script))
            {
                var appRoleScript = Path.Combine(Environment.CurrentDirectory, script);
                builder = builder
                    .WithBindMount(appRoleScript, "/tmp/script.sh");
            }

            return builder.Build();
        }

        private async Task LoadDataAsync(string server, Dictionary<string, IEnumerable<KeyValuePair<string, object>>> values)
        {
            var authMethod = new TokenAuthMethodInfo("root");

            var vaultClientSettings = new VaultClientSettings(server, authMethod)
            {
                SecretsEngineMountPoints = { KeyValueV2 = "secret" },
                PostProcessHttpClientHandlerAction = handler =>
                {
                    if (handler is HttpClientHandler clientHandler)
                    {
                        clientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    }
                }
            };
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

        private async Task<(string RoleId, string SecretId)> GetAppRoleCreds(string roleName)
        {
            var authMethod = new TokenAuthMethodInfo("root");

            var vaultClientSettings = new VaultClientSettings("http://localhost:8200", authMethod) { SecretsEngineMountPoints = { KeyValueV2 = "secret" } };
            IVaultClient vaultClient = new VaultClient(vaultClientSettings);

            var roleId = await vaultClient.V1.Auth.AppRole.GetRoleIdAsync(roleName);
            var secretId = await vaultClient.V1.Auth.AppRole.PullNewSecretIdAsync(roleName);
            return (RoleId: roleId.Data.RoleId, SecretId: secretId.Data.SecretId);
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
                            new KeyValuePair<string, object>("option5", new[] { "v1", "v2", "v3" }),
                            new KeyValuePair<string, object>(
                                "option6",
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
                    {
                        "test/otherSubsection.otherSubsection2/otherSubsection3.otherSubsection4.otherSubsection5", new[]
                        {
                            new KeyValuePair<string, object>("option7", "value7"),
                        }
                    },
                    {
                        "test/subsection/testsection", new[]
                        {
                            new KeyValuePair<string, object>("option8", "value8"),
                        }
                    },
                };

            var container = this.PrepareVaultContainer();
            try
            {
                await container.StartAsync().ConfigureAwait(false);
                await this.LoadDataAsync("http://localhost:8200", values).ConfigureAwait(false);

                // act
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddVaultConfiguration(
                    () => new VaultOptions("http://localhost:8200", "root", additionalCharactersForConfigurationPath: new[] { '.' }),
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
                configurationRoot.GetSection("otherSubsection")
                    .GetSection("otherSubsection2")
                    .GetSection("otherSubsection3")
                    .GetSection("otherSubsection4")
                    .GetSection("otherSubsection5")
                    .GetValue<string>("option7").Should().Be("value7");
                configurationRoot.GetSection("subsection").GetSection("testsection").GetValue<string>("option8").Should().Be("value8");
            }
            finally
            {
                await container.DisposeAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Success_SimpleTestOmitVaultKey_TokenAuth()
        {
            var values =
               new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
               {
                    {
                        "myservice-config", new[]
                        {
                            new KeyValuePair<string, object>("option1", "value1"),
                            new KeyValuePair<string, object>("subsection", new {option2 = "value2"}),
                        }
                    },
               };

            var container = this.PrepareVaultContainer();
            try
            {
                await container.StartAsync().ConfigureAwait(false);
                await this.LoadDataAsync("http://localhost:8200", values).ConfigureAwait(false);

                // act
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddVaultConfiguration(
                    () => new VaultOptions("http://localhost:8200", "root", omitVaultKeyName: true),
                    "myservice-config",
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

            var values =
                new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
                {
                    { "test", new[] { new KeyValuePair<string, object>("option1", "value1") } },
                    { "test/subsection", new[] { new KeyValuePair<string, object>("option2", "value2") } },
                };

            var container = this.PrepareVaultContainer();
            try
            {
                await container.StartAsync().ConfigureAwait(false);
                await this.LoadDataAsync("http://localhost:8200", values).ConfigureAwait(false);


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
                values = new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
                {
                    { "test", new[] { new KeyValuePair<string, object>("option1", "value1_new") } },
                    { "test/subsection", new[] { new KeyValuePair<string, object>("option2", "value2_new") } },
                    { "test/subsection3", new[] { new KeyValuePair<string, object>("option3", "value3_new") } },
                    { "test/testsection", new[] { new KeyValuePair<string, object>("option4", "value4_new") } },
                };
                await this.LoadDataAsync("http://localhost:8200", values).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(15), cts.Token).ConfigureAwait(true);

                reloadToken.HasChanged.Should().BeTrue();
                configurationRoot.GetValue<string>("option1").Should().Be("value1_new");
                configurationRoot.GetSection("subsection").GetValue<string>("option2").Should().Be("value2_new");
                configurationRoot.GetSection("subsection3").GetValue<string>("option3").Should().Be("value3_new");
                configurationRoot.GetSection("testsection").GetValue<string>("option4").Should().Be("value4_new");

                changeWatcher.Dispose();
            }
            finally
            {
                cts.Cancel();
                await container.DisposeAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Success_WatcherTest_NoChanges()
        {
            // arrange
            using CancellationTokenSource cts = new CancellationTokenSource();

            var values =
                new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
                {
                    { "test", new[] { new KeyValuePair<string, object>("option1", "value1") } },
                    { "test/subsection", new[] { new KeyValuePair<string, object>("option2", "value2") } },
                };

            var container = this.PrepareVaultContainer();
            try
            {
                await container.StartAsync().ConfigureAwait(false);
                await this.LoadDataAsync("http://localhost:8200", values).ConfigureAwait(false);


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
                //await this.LoadDataAsync(values).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(20), cts.Token).ConfigureAwait(true);

                reloadToken.HasChanged.Should().BeFalse();
                configurationRoot.GetValue<string>("option1").Should().Be("value1");
                configurationRoot.GetSection("subsection").GetValue<string>("option2").Should().Be("value2");

                changeWatcher.Dispose();
            }
            finally
            {
                cts.Cancel();
                await container.DisposeAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Success_WatcherTest_OmitVaultKey_TokenAuth()
        {
            // arrange
            using CancellationTokenSource cts = new CancellationTokenSource();
            var values =
             new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
             {
                    {
                        "myservice-config", new[]
                        {
                            new KeyValuePair<string, object>("option1", "value1"),
                            new KeyValuePair<string, object>("subsection", new {option2 = "value2"}),
                        }
                    },
             };

            var container = this.PrepareVaultContainer();
            try
            {
                await container.StartAsync(cts.Token).ConfigureAwait(false);
                await this.LoadDataAsync("http://localhost:8200", values).ConfigureAwait(false);


                // act
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddVaultConfiguration(
                    () => new VaultOptions("http://localhost:8200", "root", reloadOnChange: true, reloadCheckIntervalSeconds: 10, omitVaultKeyName: true),
                    "myservice-config",
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
                values =
                 new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
                 {
                        {
                            "myservice-config", new[]
                            {
                                new KeyValuePair<string, object>("option1", "value1_new"),
                                new KeyValuePair<string, object>("subsection", new {option2 = "value2_new"}),
                                new KeyValuePair<string, object>("subsection3", new {option3 = "value3_new"}),
                            }
                        },
                 };

                await this.LoadDataAsync("http://localhost:8200", values).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(15), cts.Token).ConfigureAwait(true);

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

        [Fact]
        public async Task Success_TokenAuthMethod()
        {
            // arrange
            using CancellationTokenSource cts = new CancellationTokenSource();
            var values =
              new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
              {
                    {
                        "myservice-config", new[]
                        {
                            new KeyValuePair<string, object>("option1", "value1"),
                            new KeyValuePair<string, object>("subsection", new {option2 = "value2"}),
                        }
                    },
              };

            var container = this.PrepareVaultContainer();
            try
            {
                await container.StartAsync(cts.Token).ConfigureAwait(false);
                await this.LoadDataAsync("http://localhost:8200", values).ConfigureAwait(false);

                // act
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddVaultConfiguration(
                    () => new VaultOptions("http://localhost:8200", new TokenAuthMethodInfo("root"), reloadOnChange: true, reloadCheckIntervalSeconds: 10, omitVaultKeyName: true),
                    "myservice-config",
                    "secret",
                    this._logger);
                var configurationRoot = builder.Build();

                // assert
                configurationRoot.GetValue<string>("option1").Should().Be("value1");
                configurationRoot.GetSection("subsection").GetValue<string>("option2").Should().Be("value2");
            }
            finally
            {
                cts.Cancel();
                await container.DisposeAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Success_AppRoleAuthMethod()
        {
            // arrange
            using CancellationTokenSource cts = new CancellationTokenSource();
            var values =
             new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
             {
                    {
                        "myservice-config", new[]
                        {
                            new KeyValuePair<string, object>("option1", "value1"),
                            new KeyValuePair<string, object>("subsection", new {option2 = "value2"}),
                        }
                    },
             };

            var container = this.PrepareVaultContainer(script: "approle.sh");
            try
            {
                await container.StartAsync(cts.Token).ConfigureAwait(false);
                var execResult = await container.ExecAsync(new[] { "/tmp/script.sh" });
                if (execResult.ExitCode != 0)
                {
                    string msg = execResult.Stdout + Environment.NewLine + execResult.Stderr;
                    throw new Exception(msg);
                }
                var (RoleId, SecretId) = await this.GetAppRoleCreds("test-role");
                await this.LoadDataAsync("http://localhost:8200", values).ConfigureAwait(false);

                // act
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddVaultConfiguration(
                    () => new VaultOptions("http://localhost:8200", new AppRoleAuthMethodInfo(RoleId, SecretId), reloadOnChange: true, reloadCheckIntervalSeconds: 10, omitVaultKeyName: true),
                    "myservice-config",
                    "secret",
                    this._logger);
                var configurationRoot = builder.Build();

                // assert
                configurationRoot.GetValue<string>("option1").Should().Be("value1");
                configurationRoot.GetSection("subsection").GetValue<string>("option2").Should().Be("value2");
            }
            finally
            {
                cts.Cancel();
                await container.DisposeAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Success_AppRoleAuthMethodNoListPermissions()
        {
            // arrange
            using CancellationTokenSource cts = new CancellationTokenSource();
            var values =
             new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
             {
                    {
                        "myservice-config", new[]
                        {
                            new KeyValuePair<string, object>("option1", "value1"),
                            new KeyValuePair<string, object>("subsection", new {option2 = "value2"}),
                        }
                    },
             };

            var container = this.PrepareVaultContainer(script: "approle_nolist.sh");
            try
            {
                await container.StartAsync(cts.Token).ConfigureAwait(false);
                var execResult = await container.ExecAsync(new[] { "/tmp/script.sh" });
                if (execResult.ExitCode != 0)
                {
                    string msg = execResult.Stdout + Environment.NewLine + execResult.Stderr;
                    throw new Exception(msg);
                }
                var (RoleId, SecretId) = await this.GetAppRoleCreds("test-role");
                await this.LoadDataAsync("http://localhost:8200", values).ConfigureAwait(false);

                // act
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddVaultConfiguration(
                    () => new VaultOptions("http://localhost:8200", new AppRoleAuthMethodInfo(RoleId, SecretId), reloadOnChange: true, reloadCheckIntervalSeconds: 10, omitVaultKeyName: true, alwaysAddTrailingSlashToBasePath: false),
                    "myservice-config",
                    "secret",
                    this._logger);
                var configurationRoot = builder.Build();

                // assert
                configurationRoot.GetValue<string>("option1").Should().Be("value1");
                configurationRoot.GetSection("subsection").GetValue<string>("option2").Should().Be("value2");
            }
            finally
            {
                cts.Cancel();
                await container.DisposeAsync().ConfigureAwait(false);
            }
        }


        [Fact]
        public async Task Failure_PermissionDenied()
        {
            // arrange
            using var cts = new CancellationTokenSource();
            var values =
              new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
              {
                    {
                        "myservice-config", new[]
                        {
                            new KeyValuePair<string, object>("option1", "value1"),
                            new KeyValuePair<string, object>("subsection", new {option2 = "value2"}),
                        }
                    },
              };
            var loggerMock = new Mock<ILogger<IntegrationTests>>();
            var container = this.PrepareVaultContainer();
            try
            {
                await container.StartAsync(cts.Token).ConfigureAwait(false);
                await this.LoadDataAsync("http://localhost:8200", values).ConfigureAwait(false);

                // act
                var builder = new ConfigurationBuilder();
                builder.AddVaultConfiguration(
                    () => new VaultOptions("http://localhost:8200", new TokenAuthMethodInfo("NON VALID TOKEN"), reloadOnChange: true, reloadCheckIntervalSeconds: 10, omitVaultKeyName: true),
                    "myservice-config",
                    "secret",
                    loggerMock.Object);
                var configurationRoot = builder.Build();

                // assert
                loggerMock.Verify(
                    x => x.Log(
                        It.Is<LogLevel>(l => l == LogLevel.Error),
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString() == "Cannot load configuration from Vault"),
                        It.Is<VaultApiException>(exception => exception.HttpStatusCode == HttpStatusCode.Forbidden),
                        It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);
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
