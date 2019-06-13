﻿using Phantasma.Cryptography;
using Phantasma.Numerics;
using Phantasma.Storage.Context;
using System;

namespace Phantasma.Blockchain.Contracts.Native
{
    public sealed class SwapContract : SmartContract
    {
        public override string Name => "swap";

        internal StorageMap _balances; //<string, BigInteger> 
        internal BigInteger _total; 

        public SwapContract() : base()
        {
        }

        // returns how many tokens would be obtained by trading from one type of another
        public BigInteger GetRate(string fromSymbol, string toSymbol, BigInteger amount)
        {
            Runtime.Expect(fromSymbol != toSymbol, "invalid pair");

            Runtime.Expect(_balances.ContainsKey<string>(fromSymbol), fromSymbol + " not available in pot");
            Runtime.Expect(_balances.ContainsKey<string>(toSymbol), toSymbol + " not available in pot");

            var fromBalance = _balances.Get<string, BigInteger>(fromSymbol);
            var toBalance = _balances.Get<string, BigInteger>(toSymbol);

            var fromInfo = Runtime.Nexus.GetTokenInfo(fromSymbol);
            Runtime.Expect(fromInfo.IsFungible, "must be fungible");

            var toInfo = Runtime.Nexus.GetTokenInfo(toSymbol);
            Runtime.Expect(toInfo.IsFungible, "must be fungible");
            BigInteger total;

            if (fromBalance < toBalance)
            {
                total = UnitConversion.ToBigInteger((UnitConversion.ToDecimal(amount, fromInfo.Decimals) / UnitConversion.ToDecimal(toBalance, toInfo.Decimals)) * UnitConversion.ToDecimal(fromBalance, fromInfo.Decimals), toInfo.Decimals);
            }
            else
            {
                total = UnitConversion.ToBigInteger((UnitConversion.ToDecimal(amount, fromInfo.Decimals) * UnitConversion.ToDecimal(fromBalance, fromInfo.Decimals)) / UnitConversion.ToDecimal(toBalance, toInfo.Decimals), toInfo.Decimals);
            }

            return total;
        }

        public void DepositTokens(Address from, string symbol, BigInteger amount)
        {
            Runtime.Expect(IsWitness(from), "invalid witness");
            Runtime.Expect(amount > 0, "invalid amount");

            var info = Runtime.Nexus.GetTokenInfo(symbol);
            Runtime.Expect(info.IsFungible, "must be fungible");

            _total += amount;

            var balance = _balances.ContainsKey<string>(symbol) ? _balances.Get<string, BigInteger>(symbol) : 0;
            balance += amount;
            _balances.Set<string, BigInteger>(symbol, balance);

            Runtime.Expect(Runtime.Nexus.TransferTokens(symbol, this.Storage, Runtime.Chain, from, Runtime.Chain.Address, amount), "tokens transfer failed");
            Runtime.Notify(EventKind.TokenSend, from, new TokenEventData() { chainAddress = Runtime.Chain.Address, symbol = symbol, value = amount });
        }

        public void SwapTokens(Address from, string fromSymbol, string toSymbol, BigInteger amount)
        {
            Runtime.Expect(IsWitness(from), "invalid witness");
            Runtime.Expect(amount > 0, "invalid amount");

            var fromInfo = Runtime.Nexus.GetTokenInfo(fromSymbol);
            Runtime.Expect(fromInfo.IsFungible, "must be fungible");

            var toInfo = Runtime.Nexus.GetTokenInfo(toSymbol);
            Runtime.Expect(toInfo.IsFungible, "must be fungible");

            Runtime.Expect(_balances.ContainsKey<string>(toSymbol), toSymbol + " not available in pot");

            var total = GetRate(fromSymbol, toSymbol, amount);
            var balance = _balances.Get<string, BigInteger>(toSymbol);
            var halfBalance = balance / 2; 
            Runtime.Expect(total < halfBalance, "insuficient balance in pot"); // here should be < instead of <= because we can take more than half of the pot at once

            Runtime.Expect(Runtime.Nexus.TransferTokens(fromSymbol, this.Storage, Runtime.Chain, from, Runtime.Chain.Address, amount), "source tokens transfer failed");
            Runtime.Expect(Runtime.Nexus.TransferTokens(toSymbol, this.Storage, Runtime.Chain, Runtime.Chain.Address, from, total), "target tokens transfer failed");
            Runtime.Notify(EventKind.TokenSend, from, new TokenEventData() { chainAddress = Runtime.Chain.Address, symbol = fromSymbol, value = amount });
            Runtime.Notify(EventKind.TokenReceive, from, new TokenEventData() { chainAddress = Runtime.Chain.Address, symbol = toSymbol, value = total });
        }
    }
}
