using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantasma.Core.Utils;
using Phantasma.Cryptography;
using Phantasma.Numerics;
using Phantasma.Pay;
using Phantasma.Pay.Chains;

namespace Phantasma.Tests
{
    public class TestWalletManager : WalletManager
    {
        public TestWalletManager(KeyPair keys, params WalletKind[] kinds) : base(keys, kinds)
        {
        }

        public override void FetchURL(string url, Action<string> callback)
        {
            throw new NotImplementedException();
        }
    }

    [TestClass]
    public class PayTests
    {
        [TestMethod]
        public void TestEthereumWallet()
        {
            var keys = new KeyPair(Base16.Decode("a95bd75a7b3b1c0a2a14595e8065a95cb06417f6aaedcc3bc45fda52900ab9e8"));
            var wallet = new TestWalletManager(keys);
            var address = wallet.GetAddress(WalletKind.Ethereum);
            Assert.IsTrue(address.Equals("0xe57a6c074d1db5ed7c98228df71ce5fa35b6bc72", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void TestEOSWallet()
        {
            var wif = "5KA2AqEoo7jqepqeEqK2FjjjgG5nxQN6vfuiSZqgJM79ej6eo4Q";
            byte[] data = wif.Base58CheckDecode();

            byte[] privateKey = new byte[32];
            ByteArrayUtils.CopyBytes(data, 1, privateKey, 0, privateKey.Length);

            var keys = new KeyPair(privateKey);
            var wallet = new TestWalletManager(keys);
            var address = wallet.GetAddress(WalletKind.EOS);
            Assert.IsTrue(address.Equals("EOS8dBKtG9fbhC1wi1SscL32iFRsSi4PsZDT2EHJcYXwV5dAMiBcK", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void TestNeoWallet()
        {
            var keys = KeyPair.FromWIF("L1nuBmNJ2HvLat5xyvpqmHpmXNe6rGGdAzGJgLjDLECaTCVgqjdx");
            var wallet = new TestWalletManager(keys);
            var address = wallet.GetAddress(WalletKind.Neo);

            string expectedAddress = "AU2eYJkpZ2nG81RyqnzF5UL2qjdkpPEJqN";
            Assert.IsTrue(address.Equals(expectedAddress, StringComparison.OrdinalIgnoreCase));

            var decodedAddress = NeoWallet.DecodeAddress(expectedAddress);
            var encodedAddress = NeoWallet.EncodeAddress(decodedAddress);
            Assert.IsTrue(encodedAddress.Equals(expectedAddress, StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void TestBitcoinWallet()
        {
            var keys = new KeyPair(Base16.Decode("60cf347dbc59d31c1358c8e5cf5e45b822ab85b79cb32a9f3d98184779a9efc2"));
            var wallet = new TestWalletManager(keys);
            var address = wallet.GetAddress(WalletKind.Bitcoin);
            Assert.IsTrue(address.Equals("17JsmEygbbEUEpvt4PFtYaTeSqfb9ki1F1", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void TestEndian()
        {
            var n = new BigInteger(100000000);
            var bytes = n.ToUnsignedByteArray();
            Assert.IsTrue(bytes.Length == 4);
            Assert.IsTrue(bytes[0] == 00);
            Assert.IsTrue(bytes[1] == 0xe1);
            Assert.IsTrue(bytes[2] == 0xf5);
            Assert.IsTrue(bytes[3] == 05);
        }

        [TestMethod]
        public void TestDecodeScriptHash()
        {
            var targetAddress = "2N8bXfrWTzqZoV89dosge2JxvE38VnHurqD";
            var temp = targetAddress.Base58CheckDecode().Skip(1).ToArray();

            byte OP_HASH160 = 0xa9;
            byte OP_EQUAL = 0x87;
            var outputKeyScript = ByteArrayUtils.ConcatBytes(new byte[] { OP_HASH160, 0x14 }, ByteArrayUtils.ConcatBytes(temp, new byte[] { OP_EQUAL }));
            var hex = Base16.Encode(outputKeyScript).ToLower();
        }

    }
}
