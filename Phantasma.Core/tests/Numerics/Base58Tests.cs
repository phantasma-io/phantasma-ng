using System;
using System.IO;
using System.Text;
using Shouldly;
using Xunit;

namespace Phantasma.Core.Tests.Numerics;

using Phantasma.Core.Numerics;

[Collection("Numerics")]
public class Base58
{
    [Fact]
    public void encode_decode_test_numbers()
    {
        const string testString = "0815";
        var byteArray = Encoding.ASCII.GetBytes(testString);
        var encodedString = Phantasma.Core.Numerics.Base58.Encode(byteArray);
        var decodedBytes = Phantasma.Core.Numerics.Base58.Decode(encodedString);
        decodedBytes.ShouldBe(byteArray);
        GetStringFromByteArray(decodedBytes).ShouldBe(testString);
    }

    [Fact]
    public void encode_decode_test_string()
    {
        const string testString = "Sepp";
        var byteArray = Encoding.ASCII.GetBytes(testString);
        var encodedString = Phantasma.Core.Numerics.Base58.Encode(byteArray);
        var decodedBytes = Phantasma.Core.Numerics.Base58.Decode(encodedString);
        decodedBytes.ShouldBe(byteArray);
        GetStringFromByteArray(decodedBytes).ShouldBe(testString);
    }
    
    [Fact]
    public void decode_digit_0_error()
    {
        const string testString = "0";
        Should.Throw<FormatException>(() => Phantasma.Core.Numerics.Base58.Decode(testString));
    }
    
    [Fact]
    public void encode_add_0_bytes()
    {
        const string testString = "  Sepp";
        var byteArray = new byte[] { 0x00, 0x00, 0x53, 0x65, 0x70, 0x70, };
        var encodedString = Phantasma.Core.Numerics.Base58.Encode(byteArray);
        var decodedBytes = Phantasma.Core.Numerics.Base58.Decode(encodedString);
        decodedBytes.ShouldBe(byteArray);
        GetStringFromByteArray(decodedBytes).ShouldBe(testString);
    }

    private static string GetStringFromByteArray(byte[] array)
    {
        using var stream = new MemoryStream(array);
        using var streamReader = new StreamReader(stream);

        return streamReader.ReadToEnd();
    }
}
