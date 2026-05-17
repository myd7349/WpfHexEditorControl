using WpfHexEditor.Core.Bytes;
using WpfHexEditor.Core.Search.Models;
using WpfHexEditor.Core.Search.Services;
using Xunit;
using static WpfHexEditor.Core.ByteProvider.Tests.ByteProviderTestHelpers;

namespace WpfHexEditor.Core.ByteProvider.Tests;

public class SearchTests
{

    [Fact]
    public void FindFirst_PatternExists_ReturnsPosition()
    {
        using var p = FromBytes(1, 2, 3, 2, 3);
        long pos = p.FindFirst(new byte[] { 2, 3 });
        Assert.Equal(1, pos);
    }

    [Fact]
    public void FindFirst_PatternMissing_ReturnsMinusOne()
    {
        using var p = FromBytes(1, 2, 3);
        long pos = p.FindFirst(new byte[] { 0xFF });
        Assert.Equal(-1, pos);
    }

    [Fact]
    public void FindNext_ReturnsNextOccurrence()
    {
        using var p = FromBytes(1, 2, 1, 2, 1);
        long first = p.FindFirst(new byte[] { 1, 2 });
        long next = p.FindNext(new byte[] { 1, 2 }, first + 1);
        Assert.Equal(2, next);
    }

    [Fact]
    public void FindAll_MultipleOccurrences_ReturnsAll()
    {
        using var p = FromBytes(0xAA, 1, 0xAA, 1, 0xAA);
        var all = p.FindAll(new byte[] { 0xAA });
        Assert.Equal(3, System.Linq.Enumerable.Count(all));
    }

    [Fact]
    public void CountOccurrences_Correct()
    {
        using var p = FromBytes(1, 2, 1, 2, 1, 2);
        int count = p.CountOccurrences(new byte[] { 1, 2 });
        Assert.Equal(3, count);
    }

    [Fact]
    public void SearchRegex_FindsMzHeader()
    {
        // MZ header: 0x4D 0x5A
        using var p = FromBytes(0x00, 0x4D, 0x5A, 0x00);
        var results = p.SearchRegex(@"\x4D\x5A");
        Assert.Single(results);
        Assert.Equal(1, results[0].Position);
    }

    [Fact]
    public void KnownPatterns_PeHeader_CorrectBytes()
    {
        Assert.Equal(new byte[] { 0x4D, 0x5A }, KnownPatterns.PeHeader.ToArray());
    }

    [Fact]
    public void KnownPatterns_PngMagic_CorrectLength()
    {
        Assert.Equal(8, KnownPatterns.PngMagic.Length);
    }

    [Fact]
    public void Search_TextMode_FindsAscii()
    {
        var data = System.Text.Encoding.ASCII.GetBytes("Hello World Hello");
        using var p = new Bytes.ByteProvider();
        p.OpenMemory(data);

        var opts = new SearchOptions
        {
            Pattern = System.Text.Encoding.ASCII.GetBytes("Hello")
        };
        var result = p.Search(opts);
        Assert.True(result.Matches.Count >= 2);
    }
}
