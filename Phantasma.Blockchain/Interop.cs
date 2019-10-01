﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Phantasma.Cryptography;
using Phantasma.Domain;
using Phantasma.Storage;
using Phantasma.Storage.Utils;

namespace Phantasma.Blockchain
{
    public struct PlatformInfo : IPlatform, ISerializable
    {
        public string Name { get; private set; }
        public string Symbol { get; private set; } // for fuel
        public PlatformSwapAddress[] InteropAddresses { get; private set; }

        public PlatformInfo(string name, string symbol, IEnumerable<PlatformSwapAddress> interopAddresses) : this()
        {
            Name = name;
            Symbol = symbol;
            InteropAddresses = interopAddresses.ToArray();
        }

        public void SerializeData(BinaryWriter writer)
        {
            writer.WriteVarString(Name);
            writer.WriteVarString(Symbol);
            writer.WriteVarInt(InteropAddresses.Length);
            foreach (var address in InteropAddresses)
            {
                writer.WriteVarString(address.ExternalAddress);
                writer.WriteAddress(address.LocalAddress);
            }
        }

        public void UnserializeData(BinaryReader reader)
        {
            this.Name = reader.ReadVarString();
            this.Symbol = reader.ReadVarString();
            var interopCount = (int)reader.ReadVarInt();
            this.InteropAddresses = new PlatformSwapAddress[interopCount];
            for (int i = 0; i < interopCount; i++)
            {
                var temp = new PlatformSwapAddress();
                temp.ExternalAddress = reader.ReadVarString();
                temp.LocalAddress = reader.ReadAddress();
                InteropAddresses[i] = temp;
            }
        }
    }

    public static class InteropUtils
    {
        public static string Seed = "";

        public static PhantasmaKeys GenerateInteropKeys(PhantasmaKeys genesisKeys, string platformName)
        {
            var temp = $"{platformName.ToUpper()}!{genesisKeys.ToWIF()}{Seed}";
            var privateKey = CryptoExtensions.Sha256(temp);
            var key = new PhantasmaKeys(privateKey);
            return key;
        }
    }
}
