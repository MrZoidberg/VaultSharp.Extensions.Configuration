namespace VaultSharp.Extensions.Configuration.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

public partial class IntegrationTests
{
    [Fact]
    public async Task SSL_Enabled_InsecureConnection()
    {
        // arrange
        var values =
            new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
            {
                    {
                        "test", new[]
                        {
                            new KeyValuePair<string, object>("option1", "value1"),
                        }
                    },
            };

        var container = this.PrepareVaultContainer(enableSSL: true);
  
        try
        {
            await container.StartAsync();
            await this.LoadDataAsync("https://localhost:8200", values);

            // act
            var builder = new ConfigurationBuilder();
            builder.AddVaultConfiguration(
                () => new VaultOptions("https://localhost:8200", "root", additionalCharactersForConfigurationPath: new[] { '.' }, insecureConnection: true),
                "test",
                "secret",
                this.logger);
            var configurationRoot = builder.Build();

            // assert
            configurationRoot.GetValue<string>("option1").Should().Be("value1");
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    [Fact]
    public async Task SSL_Enabled_CannotVerifyCert()
    {
        // arrange
        var values =
            new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
            {
                    {
                        "test", new[]
                        {
                            new KeyValuePair<string, object>("option1", "value1"),
                        }
                    },
            };

        var container = this.PrepareVaultContainer(enableSSL: true);

        try
        {
            await container.StartAsync();
            await this.LoadDataAsync("https://localhost:8200", values);

            // act
            var builder = new ConfigurationBuilder();
            builder.AddVaultConfiguration(
                () => new VaultOptions("https://localhost:8200", "root", additionalCharactersForConfigurationPath: new[] { '.' }, insecureConnection: false),
                "test",
                "secret",
                this.logger);
            Action act = () => builder.Build();


            // assert
            act.Should().Throw<System.Net.Http.HttpRequestException>("The SSL connection could not be established, see inner exception.");
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    [Fact]
    public async Task SSL_Enabled_ManuallyVerifyCert()
    {
        // arrange
        var values =
            new Dictionary<string, IEnumerable<KeyValuePair<string, object>>>
            {
                    {
                        "test", new[]
                        {
                            new KeyValuePair<string, object>("option1", "value1"),
                        }
                    },
            };

        var container = this.PrepareVaultContainer(enableSSL: true);

        try
        {
            await container.StartAsync();
            await this.LoadDataAsync("https://localhost:8200", values);

            // act
            var builder = new ConfigurationBuilder();
            builder.AddVaultConfiguration(
                () => new VaultOptions("https://localhost:8200", "root", additionalCharactersForConfigurationPath: new[] { '.' }, insecureConnection: false, serverCertificateCustomValidationCallback: (message, cert, chain, errors) => true),
                "test",
                "secret",
                this.logger);
            var configurationRoot = builder.Build();

            // assert
            configurationRoot.GetValue<string>("option1").Should().Be("value1");
        }
        finally
        {
            await container.DisposeAsync();
        }
    }
}
