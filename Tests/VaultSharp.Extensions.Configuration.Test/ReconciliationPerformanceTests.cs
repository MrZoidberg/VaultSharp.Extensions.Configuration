namespace VaultSharp.Extensions.Configuration.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    public class ReconciliationPerformanceTests
    {
        [Fact]
        public void RemovedKeyReconciliationUsesLinearNumberOfComparisons()
        {
            const int count = 10000;
            var current = Enumerable.Range(0, count).Select(i => i.ToString()).ToList();
            var cached = current.Concat(new[] { "removed" }).ToList();
            var comparer = new CountingComparer();
            var method = typeof(VaultConfigurationProvider).GetMethod("GetRemovedKeys", BindingFlags.NonPublic | BindingFlags.Static)!;

            var removed = (List<string>)method.Invoke(null, new object[] { cached, current, comparer })!;

            Assert.Equal(new[] { "removed" }, removed);
            Assert.True(comparer.Operations < count * 10, $"Reconciliation used {comparer.Operations} hash and equality operations.");
        }

        private sealed class CountingComparer : IEqualityComparer<string>
        {
            public int Operations { get; private set; }

            public bool Equals(string? x, string? y)
            {
                this.Operations++;
                return StringComparer.Ordinal.Equals(x, y);
            }

            public int GetHashCode(string value)
            {
                this.Operations++;
                return StringComparer.Ordinal.GetHashCode(value);
            }
        }
    }
}
