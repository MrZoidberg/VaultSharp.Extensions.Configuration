namespace VaultSharp.Extensions.Configuration.Test
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using Xunit;

    public class VaultChangeWatcherTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task StartAsync_CompletesWithoutReloadEnabledProviders(bool includeDisabledProvider)
        {
            var providers = includeDisabledProvider
                ? new IConfigurationProvider[]
                {
                    new VaultConfigurationProvider(
                        new VaultConfigurationSource(new VaultOptions("http://localhost:8200", "root", reloadOnChange: false), "test"),
                        null),
                }
                : Array.Empty<IConfigurationProvider>();
            var configuration = new Mock<IConfigurationRoot>();
            configuration.SetupGet(root => root.Providers).Returns(providers);

            using var watcher = new VaultChangeWatcher(configuration.Object);
            using var cancellation = new CancellationTokenSource();

            // Run startup separately so a synchronous retry loop cannot hang the test runner.
            var startup = Task.Run(() => watcher.StartAsync(cancellation.Token));
            try
            {
                await startup.WaitAsync(TimeSpan.FromSeconds(2));
                Assert.NotNull(watcher.ExecuteTask);
                Assert.True(watcher.ExecuteTask.IsCompletedSuccessfully);
            }
            finally
            {
                cancellation.Cancel();
                await startup.WaitAsync(TimeSpan.FromSeconds(5));
                await watcher.StopAsync(CancellationToken.None);
            }
        }
    }
}
