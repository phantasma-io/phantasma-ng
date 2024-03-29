﻿using Phantasma.Core.Cryptography;
using Phantasma.Core.Numerics;
using Phantasma.Core.Domain;
using System.Linq;
using Phantasma.Business.VM.Utils;
using Phantasma.Core.Cryptography.Structs;
using Xunit;
namespace Phantasma.Business.Tests;



[Collection("WalletTests")]
[CollectionDefinition(nameof(WalletTests), DisableParallelization = true)]
public class WalletTests
{
    [Fact]
    public void TransferScriptMethodExtraction()
    {
        var source = PhantasmaKeys.Generate();
        var dest = PhantasmaKeys.Generate();
        var amount = UnitConversion.GetUnitValue(DomainSettings.StakingTokenDecimals);
        var script = ScriptUtils.BeginScript().AllowGas(source.Address, Address.Null, 1, 999).TransferTokens(DomainSettings.StakingTokenSymbol, source.Address, dest.Address, amount).SpendGas(source.Address).EndScript();

        var table = DisasmUtils.GetDefaultDisasmTable();
        var methods = DisasmUtils.ExtractMethodCalls(script, DomainSettings.LatestKnownProtocol, table);

        Assert.True(methods != null && methods.Count() == 3);
    }
}

