// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: ByteProviderFactory.cs
// Description:
//     Default IByteProviderFactory implementation.
//     Creates and opens ByteProvider instances in one call.
// ==========================================================

using System.IO;
using WpfHexEditor.Core.Bytes;
using WpfHexEditor.Core.Interfaces;

namespace WpfHexEditor.Core.Factories
{
    /// <summary>
    /// Default <see cref="IByteProviderFactory"/> — creates and opens <see cref="ByteProvider"/>
    /// instances. Register as a singleton in your DI container:
    /// <code>services.AddSingleton&lt;IByteProviderFactory, ByteProviderFactory&gt;();</code>
    /// </summary>
    public sealed class ByteProviderFactory : IByteProviderFactory
    {
        /// <summary>
        /// Create a provider opened on <paramref name="path"/>.
        /// Caller owns the returned instance and must dispose it.
        /// </summary>
        public IByteProvider CreateFromFile(string path, bool readOnly = false, ByteProviderOptions? options = null)
        {
            var provider = new ByteProvider(options ?? ByteProviderOptions.Default);
            provider.OpenFile(path, readOnly);
            return provider;
        }

        /// <summary>
        /// Create a provider opened on <paramref name="stream"/>.
        /// Caller owns both the provider and the stream.
        /// </summary>
        public IByteProvider CreateFromStream(Stream stream, bool readOnly = false, ByteProviderOptions? options = null)
        {
            var provider = new ByteProvider(options ?? ByteProviderOptions.Default);
            provider.OpenStream(stream, readOnly);
            return provider;
        }

        /// <summary>
        /// Create a provider backed by <paramref name="data"/>.
        /// The array is copied internally; mutations do not affect the original.
        /// </summary>
        public IByteProvider CreateFromMemory(byte[] data, bool readOnly = false, ByteProviderOptions? options = null)
        {
            var provider = new ByteProvider(options ?? ByteProviderOptions.Default);
            provider.OpenMemory(data, readOnly);
            return provider;
        }
    }
}
