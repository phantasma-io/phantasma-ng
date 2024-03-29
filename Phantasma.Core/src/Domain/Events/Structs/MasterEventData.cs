using System.Numerics;
using Phantasma.Core.Types.Structs;

namespace Phantasma.Core.Domain.Events.Structs;

public struct MasterEventData
{
    public readonly string Symbol;
    public readonly BigInteger Value;
    public readonly string ChainName;
    public readonly Timestamp ClaimDate;

    public MasterEventData(string symbol, BigInteger value, string chainName, Timestamp claimDate)
    {
        this.Symbol = symbol;
        this.Value = value;
        this.ChainName = chainName;
        this.ClaimDate = claimDate;
    }
}
