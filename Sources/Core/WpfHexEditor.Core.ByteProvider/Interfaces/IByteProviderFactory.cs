// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: IByteProviderFactory.cs
// Description:
//     Factory abstraction for creating IByteProvider instances.
//     Enregistrable via DI without pulling in Microsoft.Extensions.DependencyInjection.
// ==========================================================

using System.IO;

namespace WpfHexEditor.Core.Interfaces
{
    /// <summary>
    /// Creates and opens <see cref="IByteProvider"/> instances.
    /// Inject this interface to decouple callers from <see cref="Bytes.ByteProvider"/>.
    /// </summary>
    public interface IByteProviderFactory
    {
        /// <summary>Create a provider backed by a file on disk.</summary>
        IByteProvider CreateFromFile(string path, bool readOnly = false, ByteProviderOptions? options = null);

        /// <summary>Create a provider backed by an existing stream.</summary>
        IByteProvider CreateFromStream(Stream stream, bool readOnly = false, ByteProviderOptions? options = null);

        /// <summary>Create a provider backed by an in-memory byte array.</summary>
        IByteProvider CreateFromMemory(byte[] data, bool readOnly = false, ByteProviderOptions? options = null);
    }
}
