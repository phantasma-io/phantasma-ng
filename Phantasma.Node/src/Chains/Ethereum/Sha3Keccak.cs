using System.Linq;
using System.Text;
using Nethereum.Hex.HexConvertors.Extensions;
using Org.BouncyCastle.Crypto.Digests;

namespace Phantasma.Node.Chains.Ethereum;

public class Sha3Keccak
{
    public static Sha3Keccak Current { get; } = new Sha3Keccak();

    public string CalculateHash(string value)
    {
        var input = Encoding.UTF8.GetBytes(value);
        var output = CalculateHash(input);
        return output.ToHex();
    }

    public string CalculateHashFromHex(params string[] hexValues)
    {
        var joinedHex = string.Join("", hexValues.Select(x => HexByteConvertorExtensions.RemoveHexPrefix(x)).ToArray());
        return CalculateHash(joinedHex.HexToByteArray()).ToHex();
    }

    public byte[] CalculateHash(byte[] value)
    {
        var digest = new KeccakDigest(256);
        var output = new byte[digest.GetDigestSize()];
        digest.BlockUpdate(value, 0, value.Length);
        digest.DoFinal(output, 0);
        return output;
    }
}
