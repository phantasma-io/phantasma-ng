﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Phantasma.Business.Blockchain.Contracts.Native;
using Phantasma.Business.VM.Utils;
using Phantasma.Core.Cryptography;
using Phantasma.Core.Cryptography.Structs;
using Phantasma.Core.Domain;
using Phantasma.Core.Domain.Contract;
using Phantasma.Core.Domain.Contract.Enums;
using Phantasma.Core.Domain.Interfaces;
using Phantasma.Core.Domain.TransactionData;

namespace Phantasma.Business.Blockchain
{
    public static class TransactionExtensions
    {
        private static Dictionary<string, List<string>> WhiteListedCalls = new Dictionary<string, List<string>>
        {
            {
                NativeContractKind.Swap.GetContractName(),
                new List<string>()
                {
                    nameof(SwapContract.SwapFee),
                    nameof(SwapContract.SwapReverse)
                }
            },
            { 
                NativeContractKind.Exchange.GetContractName(),
               new List<string>()
               {
                   nameof(ExchangeContract.SwapFee),
                   nameof(ExchangeContract.SwapReverse)
               }
            },
            { 
                NativeContractKind.Stake.GetContractName(),
                new List<string>()
                {
                    nameof(StakeContract.Claim)
                }
            }
        };
        
        private static string GasContractName = NativeContractKind.Gas.GetContractName();
        private static string GasSpendGasMethod = nameof(GasContract.SpendGas);
        
        public static bool ExtractGasDetailsFromScript(byte[] script, uint ProtocolVersion, out Address from, out Address target, out BigInteger gasPrice, out BigInteger gasLimit, Dictionary<string, int> methodArgumentCountTable = null)
        {
            var methods = DisasmUtils.ExtractMethodCalls(script, ProtocolVersion, methodArgumentCountTable);
            return ExtractGasDetailsFromMethods(methods, out from, out target, out gasPrice, out gasLimit, methodArgumentCountTable);
        }
        
        public static bool ExtractGasDetailsFromMethods(IEnumerable<DisasmMethodCall> methods, out Address from, out Address target, out BigInteger gasPrice, out BigInteger gasLimit, Dictionary<string, int> methodArgumentCountTable = null)
        {
            var allowGas = methods.FirstOrDefault(x => x.ContractName.Equals("gas") && x.MethodName.Equals(nameof(GasContract.AllowGas)));

            if (allowGas == null || allowGas.Arguments.Length != 4)
            {
                from = Address.Null;
                target = Address.Null;
                gasPrice = 0;
                gasLimit = 0;
                return false;
            }

            from = allowGas.Arguments[0].AsAddress();
            target = allowGas.Arguments[1].AsAddress();
            gasPrice = allowGas.Arguments[2].AsNumber();
            gasLimit = allowGas.Arguments[3].AsNumber();
            return true;
        }

        public static bool IsWhitelisted(IEnumerable<DisasmMethodCall> methods)
        {
            var method = methods.First();
            
            if (!WhiteListedCalls.ContainsKey(method.ContractName))
                return false;

            if (!WhiteListedCalls.TryGetValue(method.ContractName, out List<string> whitelistedMethods))
            {
                return false; 
            }

            return whitelistedMethods.Contains(method.MethodName);
        }
        
        public static bool HasSpendGas(IEnumerable<DisasmMethodCall> methods)
        {
            foreach (var method in methods)
            {
                if (!method.ContractName.Equals(GasContractName))
                    continue;

                if ( method.MethodName.Equals(GasSpendGasMethod))
                    return true;
            }

            return false;
        }
        
        public static bool IsValid(this Transaction tx, IChain chain)
        {
            return (chain.Name == tx.ChainName && chain.Nexus.Name == tx.NexusName);
        }
    }
}
