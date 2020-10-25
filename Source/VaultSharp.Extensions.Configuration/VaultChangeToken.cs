namespace VaultSharp.Extensions.Configuration
{
    using System;
    using System.Threading;
    using Microsoft.Extensions.Primitives;
/*
    /// <summary>
    /// Implements <see cref="IChangeToken"/>.
    /// </summary>
    public class VaultChangeToken : IChangeToken, IDisposable
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();

        /// <inheritdoc />
        public bool HasChanged => this._cts.IsCancellationRequested;

        /// <inheritdoc />
        public bool ActiveChangeCallbacks => true;

        /// <inheritdoc />
        public IDisposable RegisterChangeCallback(Action<object> callback, object state) => this._cts.Token.Register(callback, state);

        /// <summary>
        /// Used to trigger the change token when a reload occurs.
        /// </summary>
        public void OnReload() => this._cts.Cancel();

        /// <inheritdoc/>
        public void Dispose() => this._cts.Dispose();
    }*/
}
