namespace VaultSharp.Extensions.Configuration
{
    using System;
    using Microsoft.Extensions.Primitives;

    internal class VaultChangeToken : IChangeToken
    {
        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            throw new NotImplementedException();
        }

        public bool HasChanged { get; internal set; }

        public bool ActiveChangeCallbacks => true;
    }
}
