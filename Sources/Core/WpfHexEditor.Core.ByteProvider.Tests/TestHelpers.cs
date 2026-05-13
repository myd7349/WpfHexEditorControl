using WpfHexEditor.Core.Bytes;

namespace WpfHexEditor.Core.ByteProvider.Tests;

internal static class ByteProviderTestHelpers
{
    internal static Bytes.ByteProvider FromBytes(params byte[] data)
    {
        var p = new Bytes.ByteProvider();
        p.OpenMemory(data);
        return p;
    }
}
