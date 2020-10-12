namespace VaultSharp.Extensions.Configuration.Test
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DotNet.Testcontainers.Containers.Builders;
    using DotNet.Testcontainers.Containers.Modules;
    using DotNet.Testcontainers.Containers.WaitStrategies;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using VaultSharp.V1.AuthMethods.Token;
    using Xunit;

    public class IntegrationTests
    {
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

        private async Task LoadData(Dictionary<string, string> values)
        {
            var authMethod = new TokenAuthMethodInfo("root");

            var vaultClientSettings = new VaultClientSettings("http://localhost:8200", authMethod);
            IVaultClient vaultClient = new VaultClient(vaultClientSettings);

            foreach (var pair in values)
            {
                var data = new Dictionary<string, object>() { ["value"] = pair.Value };
                await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(pair.Key, data).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Success_Test()
        {
            // arrange
            Dictionary<string, string> values = new Dictionary<string, string>();
            values.Add("data/test/option1", "value1");
            values.Add("data/test/subsection/option2", "value2");

            var container = this.PrepareVaultContainer();
            try
            {
                await container.StartAsync().ConfigureAwait(false);
                await this.LoadData(values).ConfigureAwait(false);

                // act
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddVaultConfiguration(
                    () => new VaultOptions("http://localhost:8200", "root"), "test", "secret");
                var configurationRoot = builder.Build();

                // assert
                configurationRoot.GetValue<string>("option1").Should().Be(values["data/test/option1"]);
                configurationRoot.GetSection("subsection").GetValue<string>("option2").Should()
                    .Be(values["data/test/subsection/option2"]);
            }
            finally
            {
                await container.DisposeAsync().ConfigureAwait(false);
            }

        }
    }
}
