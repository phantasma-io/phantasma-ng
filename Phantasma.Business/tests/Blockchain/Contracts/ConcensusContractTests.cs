using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using Phantasma.Business.Blockchain;
using Phantasma.Business.Tests.Simulator;
using Phantasma.Business.VM.Utils;
using Phantasma.Core.Cryptography;
using Phantasma.Core.Cryptography.Enums;
using Phantasma.Core.Cryptography.Structs;
using Phantasma.Core.Domain;
using Phantasma.Core.Domain.Contract;
using Phantasma.Core.Domain.Contract.Consensus;
using Phantasma.Core.Domain.Contract.Consensus.Enums;
using Phantasma.Core.Domain.Contract.Consensus.Structs;
using Phantasma.Core.Domain.Contract.Enums;
using Phantasma.Core.Domain.Exceptions;
using Phantasma.Core.Domain.Interfaces;
using Phantasma.Core.Domain.Serializer;
using Phantasma.Core.Domain.TransactionData;
using Phantasma.Core.Domain.VM;
using Phantasma.Core.Numerics;
using Phantasma.Core.Types;
using Phantasma.Core.Types.Structs;

namespace Phantasma.Business.Tests.Blockchain.Contracts;

using Xunit;
using Phantasma.Business.Blockchain.Contracts.Native;

[Collection(nameof(SystemTestCollectionDefinition))]
public class ConcensusContractTests
{
    PhantasmaKeys user;
    PhantasmaKeys user2;
    PhantasmaKeys user3;
    PhantasmaKeys owner;
    PhantasmaKeys owner2;
    PhantasmaKeys owner3;
    PhantasmaKeys owner4;
    Nexus nexus;
    NexusSimulator simulator;
    int amountRequested;
    int gas;
    BigInteger initialAmount;
    BigInteger initialFuel;
    BigInteger startBalance;

    private const string testSubject = "test_subject";

    public ConcensusContractTests()
    {
        Initialize();
    }

    public void Initialize()
    {
        user = PhantasmaKeys.Generate();
        user2 = PhantasmaKeys.Generate();
        user3 = PhantasmaKeys.Generate();
        owner = PhantasmaKeys.Generate();
        owner2 = PhantasmaKeys.Generate();
        owner3 = PhantasmaKeys.Generate();
        owner4 = PhantasmaKeys.Generate();
        amountRequested = 100000000;
        gas = 99999;
        initialAmount = UnitConversion.ToBigInteger(10, DomainSettings.StakingTokenDecimals);
        initialFuel = UnitConversion.ToBigInteger(10, DomainSettings.FuelTokenDecimals);
        InitializeSimulator();

        startBalance = nexus.RootChain.GetTokenBalance(simulator.Nexus.RootStorage, DomainSettings.StakingTokenSymbol, user.Address);
    }
    
    protected void InitializeSimulator()
    {
        simulator = new NexusSimulator(new []{owner, owner2, owner3, owner4}, DomainSettings.LatestKnownProtocol);
        nexus = simulator.Nexus;
        nexus.SetOracleReader(new OracleSimulator(nexus));
        SetInitialBalance(user.Address);
        SetInitialBalance(user2.Address);
        SetInitialBalance(user3.Address);
    }

    protected void SetInitialBalance(Address address)
    {
        simulator.BeginBlock();
        simulator.GenerateTransfer(owner, address, nexus.RootChain, DomainSettings.FuelTokenSymbol, initialFuel);
        simulator.GenerateTransfer(owner, address, nexus.RootChain, DomainSettings.StakingTokenSymbol, initialAmount);
        simulator.EndBlock();
        Assert.True(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        
        simulator.BeginBlock();
        simulator.GenerateTransfer(owner, address, nexus.RootChain, DomainSettings.FuelTokenSymbol, initialFuel);
        simulator.GenerateTransfer(owner, address, nexus.RootChain, DomainSettings.StakingTokenSymbol, 100000000000);
        simulator.EndBlock();
        Assert.True(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
    }
    
    [Fact]
    public void TestMigrate()
    {
        simulator.BeginBlock();
        simulator.GenerateCustomTransaction(user, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(user.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.Migrate), user.Address, user2.Address)
                .SpendGas(user.Address)
                .EndScript());
        simulator.EndBlock();
        Assert.False(simulator.LastBlockWasSuccessful());
    }
    
    [Fact]
    public void TestConsensus()
    {
        //  InitPoll(Address from, string subject, string organization, ConsensusMode mode, Timestamp startTime, Timestamp endTime, byte[] serializedChoices, BigInteger votesPerUser)
        var subject = "subject_test";
        var organization = DomainSettings.ValidatorsOrganizationName;
        var mode = ConsensusMode.Majority;
        Timestamp startTime = ((Timestamp)simulator.CurrentTime).Value + 100;
        Timestamp endTime = startTime.Value + 100000;
        // Choices PollChoice
        var choices = new PollChoice[]
        {
            new PollChoice(Encoding.UTF8.GetBytes("choice1")),
            new PollChoice(Encoding.UTF8.GetBytes("choice2")),
            new PollChoice(Encoding.UTF8.GetBytes("choice3")),
        };
        var serializedChoices = choices.Serialize();
        var votesPerUser = 1;
        
        // FAIL: Init Pool (Not on the organization)
        InitPoll(user, subject, organization, mode, startTime, endTime, serializedChoices, votesPerUser, true);
        
        // Init Pool (On the organization)
        InitPoll(owner, subject, organization, mode, startTime, endTime, serializedChoices, votesPerUser);
        
        // FAIL: Try to Init Again to check the Fetch pool
        InitPoll(user, subject, organization, mode, startTime, endTime, serializedChoices, votesPerUser, true);

        simulator.TimeSkipHours(1);
        Thread.Sleep(1000);
        
        var getConsensus = simulator.InvokeContract(NativeContractKind.Consensus, nameof(ConsensusContract.GetConsensusPoll), subject).AsStruct<ConsensusPoll>();
        Assert.Equal(subject, getConsensus.subject);
        Assert.Equal(organization, getConsensus.organization);
        Assert.Equal(mode, getConsensus.mode);
        Assert.Equal(startTime, getConsensus.startTime);
        Assert.Equal(endTime, getConsensus.endTime);
        
        // Let's vote with owner
        SingleVote(owner, subject, 0);

        Assert.Throws<ChainException>(() =>
            simulator.InvokeContract(NativeContractKind.Consensus,
            nameof(ConsensusContract.HasConsensus), subject, choices[0].value));
        
        simulator.TimeSkipDays(2);

        // Check consensus it needs to be a transaction so it can alter the state of the chain
        HasConsensus(owner, subject, choices[0].value);
        
        var hasConsensus = simulator.InvokeContract(NativeContractKind.Consensus,
            nameof(ConsensusContract.HasConsensus), subject, choices[0].value).AsBool();
        Assert.True(hasConsensus);
        
        var allConsensus = simulator.InvokeContract(NativeContractKind.Consensus,
            nameof(ConsensusContract.GetConsensusPolls), subject).ToArray<ConsensusPoll>();
        
        Assert.Equal(1, allConsensus.Length);
        Assert.Equal(subject, allConsensus[0].subject);
        Assert.Equal(organization, allConsensus[0].organization);
        Assert.Equal(mode, allConsensus[0].mode);
        Assert.Equal(startTime, allConsensus[0].startTime);
        Assert.Equal(endTime, allConsensus[0].endTime);
    }

    [Fact]
    public void TestUpdatingVote()
    {
        var subject = "subject_test";
        var organization = DomainSettings.StakersOrganizationName;
        var mode = ConsensusMode.Popularity;
        Timestamp startTime = ((Timestamp)simulator.CurrentTime).Value + 100;
        Timestamp endTime = startTime.Value + 100000;
        // Choices PollChoice
        var choices = new PollChoice[]
        {
            new PollChoice(Encoding.UTF8.GetBytes("choice1")),
            new PollChoice(Encoding.UTF8.GetBytes("choice2")),
            new PollChoice(Encoding.UTF8.GetBytes("choice3")),
        };
        
        var serializedChoices = choices.Serialize();
        var votesPerUser = 2;

        Stake(user, UnitConversion.ToBigInteger(1001,DomainSettings.StakingTokenDecimals));

        simulator.TimeSkipDays(30);
        
        startTime = ((Timestamp)simulator.CurrentTime).Value + 100;
        endTime = startTime.Value + 100000;
        
        // Init Pool
        InitPoll(user, subject, organization, mode, startTime, endTime, serializedChoices, votesPerUser);

        simulator.TimeSkipHours(1);
        
        // FAIL: Let's vote with owner
        MultiVote(user, subject, new PollVote[] { new PollVote
        {
            index = 0,
            percentage = 1000,
        }, new PollVote
        {
            index = 1,
            percentage = 1000,
        } }, true);
        
        // FAIL: Not 100% SUM
        MultiVote(user, subject, new PollVote[] { new PollVote
        {
            index = 0,
            percentage = 10,
        }, new PollVote
        {
            index = 0,
            percentage = 10,
        } }, true);
        
        // FAIL: 100% SUM But twice the votes in the same.
        MultiVote(user, subject, new PollVote[] { new PollVote
        {
            index = 0,
            percentage = 90,
        }, new PollVote
        {
            index = 0,
            percentage = 10,
        } }, true);
        
        // FAIL: 100% and 0 in another
        MultiVote(user, subject, new PollVote[] { new PollVote
        {
            index = 0,
            percentage = 100,
        }, new PollVote
        {
            index = 1,
            percentage = 0,
        } }, true);
        
        // Success - Vote
        MultiVote(user, subject, new PollVote[] { new PollVote
        {
            index = 0,
            percentage = 75,
        }, new PollVote
        {
            index = 1,
            percentage = 25,
        } });
        
        // Check the vote
        var consensusPoll = simulator.InvokeContract(NativeContractKind.Consensus, nameof(ConsensusContract.GetConsensusPoll), subject).AsStruct<ConsensusPoll>();
        
        Assert.Equal(subject, consensusPoll.subject);
        Assert.Equal(organization, consensusPoll.organization);
        Assert.Equal(mode, consensusPoll.mode);
        Assert.Equal(startTime, consensusPoll.startTime);
        Assert.Equal(endTime, consensusPoll.endTime);
        Assert.Equal(1, consensusPoll.totalVotes);
        
        // Get the Stacking power
        var votePower = simulator.InvokeContract(NativeContractKind.Stake, nameof(StakeContract.GetAddressVotingPower), user.Address).AsNumber();
        
        Assert.Equal(consensusPoll.entries[0].votes, votePower * 75 / 100);
        Assert.Equal(consensusPoll.entries[1].votes, votePower * 25 / 100);
        
        // Change the vote
        MultiVote(user, subject, new PollVote[] { new PollVote
        {
            index = 0,
            percentage = 50,
        }, new PollVote
        {
            index = 1,
            percentage = 50,
        } });
        
        // Re-check the vote
        consensusPoll = simulator.InvokeContract(NativeContractKind.Consensus, nameof(ConsensusContract.GetConsensusPoll), subject).AsStruct<ConsensusPoll>();

        Assert.Equal(consensusPoll.entries[0].votes, votePower * 50 / 100);
        Assert.Equal(consensusPoll.entries[1].votes, votePower * 50 / 100);
    }

    [Fact]
    public void TestRemovingVote()
    {
        // Init a Poll
        var subject = "subject_test";
        var organization = DomainSettings.ValidatorsOrganizationName;
        var mode = ConsensusMode.Majority;
        Timestamp startTime = ((Timestamp)simulator.CurrentTime).Value + 100;
        Timestamp endTime = startTime.Value + 100000;
        // Choices PollChoice
        var choices = new PollChoice[]
        {
            new PollChoice(Encoding.UTF8.GetBytes("choice1")),
            new PollChoice(Encoding.UTF8.GetBytes("choice2")),
            new PollChoice(Encoding.UTF8.GetBytes("choice3")),
        };
        var serializedChoices = choices.Serialize();
        var votesPerUser = 1;
        
        // Choices PollChoice
        
        // Init Pool
        InitPoll(owner, subject, organization, mode, startTime, endTime, serializedChoices, votesPerUser);
        
        // Vote before the poll starts
        SingleVote(owner, subject, 0, true);
        
        // wait 1 hour
        simulator.TimeSkipHours(1);
        
        // Let's vote with owner
        SingleVote(owner, subject, 0);
        
        // FAIL: Let's remove the vote
        RemoveVote(owner, "notTheSubject", true);
        
        // Let's remove the vote
        RemoveVote(owner, subject);
    }
    
    [Fact]
    public void PollChoice_Value_IsSet()
    {
        // Arrange
        byte[] expectedValue = new byte[] { 0x01, 0x02, 0x03 };
        PollChoice pollChoice = new PollChoice(expectedValue);

        // Act
        byte[] actualValue = pollChoice.value;

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void TestMultisignature()
    {
        var subject = "subject_test";
        var nexusName = "simnet";
        var chainName = "main";
        var script = ScriptUtils.BeginScript()
            .AllowGas(owner3.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
            .CallInterop("Runtime.TransferTokens", owner.Address, owner2.Address, DomainSettings.StakingTokenSymbol, amountRequested )
            .SpendGas(owner3.Address)
            .EndScript(); // TODO: Change to a valid script to test if they have permission to perform this.
        var time = simulator.CurrentTime;
        var payload = "Consensus";
        time = time + TimeSpan.FromHours(12);

        ITransaction transaction = new Transaction(nexusName, chainName, script, time, payload);
        transaction.Sign(owner);
        List<Address> addresses = new List<Address>();
        addresses.Add(owner.Address);
        addresses.Add(owner2.Address);
        addresses.Add(owner3.Address);
        addresses.Add(owner4.Address);

        var scriptCreateTransaction = ScriptUtils.BeginScript()
            .AllowGas(owner.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
            .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.CreateTransaction), owner.Address,
                subject, Serialization.Serialize(transaction), addresses.ToArray())
            .SpendGas(owner.Address)
            .EndScript();
        
        // Create Transaction
        simulator.BeginBlock();
        simulator.GenerateCustomTransaction(owner, ProofOfWork.None, () =>
            scriptCreateTransaction);
        simulator.EndBlock();
        Assert.True(simulator.LastBlockWasSuccessful());

        var signature = transaction.GetTransactionSignature(owner2);
        transaction.AddSignature(signature);

        // Try to Init Again to check the Fetch pool
        simulator.BeginBlock();
        simulator.GenerateCustomTransaction(owner2, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(owner2.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.AddSignatureTransaction), owner2.Address, subject, signature.Serialize())
                .SpendGas(owner2.Address)
                .EndScript());
        simulator.EndBlock();
        Assert.True(simulator.LastBlockWasSuccessful());
        
        signature = transaction.GetTransactionSignature(owner3);
        transaction.AddSignature(signature);

        simulator.TimeSkipHours(1);
        
        simulator.BeginBlock();
        simulator.GenerateCustomTransaction(owner3, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(owner3.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.AddSignatureTransaction), owner3.Address, subject, Serialization.Serialize(signature))
                .SpendGas(owner3.Address)
                .EndScript());
        simulator.EndBlock();
        Assert.True(simulator.LastBlockWasSuccessful());
        
        signature = transaction.GetTransactionSignature(owner4);
        transaction.AddSignature(signature);

        simulator.BeginBlock();
        simulator.GenerateCustomTransaction(owner4, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(owner4.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.AddSignatureTransaction), owner4.Address, subject, Serialization.Serialize(signature))
                .SpendGas(owner4.Address)
                .EndScript());
        simulator.EndBlock();
        Assert.True(simulator.LastBlockWasSuccessful());

        // Get the transaction
        simulator.BeginBlock();
        var tx = simulator.GenerateCustomTransaction(owner4, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(owner4.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.GetTransaction), owner4.Address, subject)
                .SpendGas(owner4.Address)
                .EndScript());
        var block = simulator.EndBlock().First();
        Assert.True(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        var txResult = block.GetResultForTransaction(tx.Hash);
        Assert.NotNull(txResult);

        var test = Serialization.Unserialize<VMObject>(txResult);
        var toTransactionBytes = test.AsByteArray();
        var result = Transaction.Unserialize(toTransactionBytes);
        Assert.NotNull(result);
        
        Assert.Equal(transaction.Expiration, result.Expiration);
        Assert.Equal(transaction.Payload, result.Payload);
        Assert.Equal(transaction.Script, result.Script);
        Assert.Equal(transaction.NexusName, result.NexusName);
        Assert.Equal(transaction.ChainName, result.ChainName);
        Assert.Equal(transaction.Signatures.Length, result.Signatures.Length);
        Assert.Equal(transaction.Signatures[0].Kind, result.Signatures[0].Kind);
        Assert.Equal(transaction.Signatures[1].Kind, result.Signatures[1].Kind);
        Assert.Equal(transaction.Signatures[2].Kind, result.Signatures[2].Kind);
        Assert.Equal(transaction.Signatures[3].Kind, result.Signatures[3].Kind);

        simulator.BeginBlock();
        simulator.SendRawTransaction(result);
        simulator.EndBlock();
        Assert.True(simulator.LastBlockWasSuccessful());
        
        // Delete transaction
        simulator.BeginBlock();
        simulator.GenerateCustomTransaction(owner4, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(owner4.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.DeleteTransaction), addresses.ToArray(), subject)
                .SpendGas(owner4.Address)
                .EndScript());
        simulator.EndBlock();
        Assert.True(simulator.LastBlockWasSuccessful());
        
        
        // Validate transaction is deleted
        // Get the transaction
        simulator.BeginBlock();
        tx = simulator.GenerateCustomTransaction(owner4, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(owner4.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.GetTransaction), owner4.Address, subject)
                .SpendGas(owner4.Address)
                .EndScript());
        block = simulator.EndBlock().First();
        Assert.False(simulator.LastBlockWasSuccessful());
        txResult = block.GetResultForTransaction(tx.Hash);
        Assert.Null(txResult);
    }

    /// <summary>
    /// Init a Poll
    /// </summary>
    /// <param name="_user"></param>
    /// <param name="subject"></param>
    /// <param name="organization"></param>
    /// <param name="mode"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="serializedChoices"></param>
    /// <param name="votesPerUser"></param>
    /// <param name="shouldFail"></param>
    private void InitPoll(PhantasmaKeys _user, string subject, string organization, ConsensusMode mode, Timestamp startTime, Timestamp endTime, byte[] serializedChoices, BigInteger votesPerUser, bool shouldFail = false)
    {
        simulator.BeginBlock();
        simulator.GenerateCustomTransaction(_user, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(_user.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.InitPoll), _user.Address,subject, organization, mode, startTime, endTime, serializedChoices, votesPerUser)
                .SpendGas(_user.Address)
                .EndScript());
        simulator.EndBlock();
        
        if ( shouldFail )
        {
            Assert.False(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
        else
        {
            Assert.True(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
    }

    /// <summary>
    /// Single Vote
    /// </summary>
    /// <param name="_user"></param>
    /// <param name="subject"></param>
    /// <param name="index"></param>
    /// <param name="shouldFail"></param>
    private void SingleVote(PhantasmaKeys _user, string subject, int index, bool shouldFail = false)
    {
        simulator.BeginBlock();
        simulator.GenerateCustomTransaction(_user, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(_user.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.SingleVote), _user.Address, subject, index)
                .SpendGas(_user.Address)
                .EndScript());
        simulator.EndBlock();
        if (shouldFail)
        {
            Assert.False(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
        else
        {
            Assert.True(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
    }

    /// <summary>
    /// Multi Vote
    /// </summary>
    /// <param name="_user"></param>
    /// <param name="subject"></param>
    /// <param name="index"></param>
    /// <param name="shouldFail"></param>
    private void MultiVote(PhantasmaKeys _user, string subject, PollVote[] votes, bool shouldFail = false)
    {
        simulator.BeginBlock();
        simulator.GenerateCustomTransaction(_user, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(_user.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.MultiVote), _user.Address, subject, votes)
                .SpendGas(_user.Address)
                .EndScript());
        simulator.EndBlock();
        
        if (shouldFail)
        {
            Assert.False(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
        else
        {
            Assert.True(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
    }

    /// <summary>
    /// Remove vote from poll
    /// </summary>
    /// <param name="_user"></param>
    /// <param name="subject"></param>
    /// <param name="shouldFail"></param>
    private void RemoveVote(PhantasmaKeys _user, string subject, bool shouldFail = false)
    {
        simulator.BeginBlock();
        simulator.GenerateCustomTransaction(_user, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(_user.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.RemoveVotes), _user.Address, subject)
                .SpendGas(_user.Address)
                .EndScript());
        simulator.EndBlock();
        
        if (shouldFail)
        {
            Assert.False(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
        else
        {
            Assert.True(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
    }

    /// <summary>
    /// Get the rank for that value.
    /// </summary>
    /// <param name="_user"></param>
    /// <param name="subject"></param>
    /// <param name="value"></param>
    /// <param name="shouldFail"></param>
    private void GetRank(PhantasmaKeys _user, string subject, byte[] value, bool shouldFail = false)
    {
        simulator.BeginBlock();
        simulator.GenerateCustomTransaction(_user, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(_user.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.GetRank), _user.Address, subject, value)
                .SpendGas(_user.Address)
                .EndScript());
        simulator.EndBlock();
        
        if (shouldFail)
        {
            Assert.False(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
        else
        {
            Assert.True(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
    }
    
    /// <summary>
    /// Has Consensus
    /// </summary>
    /// <param name="_user"></param>
    /// <param name="subject"></param>
    /// <param name="value"></param>
    /// <param name="shouldFail"></param>
    private void HasConsensus(PhantasmaKeys _user, string subject, byte[] value, bool shouldFail = false)
    {
        simulator.BeginBlock();
        simulator.GenerateCustomTransaction(_user, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(_user.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Consensus, nameof(ConsensusContract.HasConsensus), subject, value)
                .SpendGas(_user.Address)
                .EndScript());
        simulator.EndBlock();
        if ( shouldFail )
        {
            Assert.False(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
        else
        {
            Assert.True(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
    }

    /// <summary>
    /// Stake
    /// </summary>
    /// <param name="_user"></param>
    /// <param name="amount"></param>
    /// <param name="shouldFail"></param>
    private void Stake(PhantasmaKeys _user, BigInteger amount, bool shouldFail = false)
    {
        simulator.BeginBlock();
        simulator.GenerateCustomTransaction(_user, ProofOfWork.None, () =>
            ScriptUtils.BeginScript()
                .AllowGas(_user.Address, Address.Null, simulator.MinimumFee, simulator.MinimumGasLimit)
                .CallContract(NativeContractKind.Stake, nameof(StakeContract.Stake), _user.Address, amount)
                .SpendGas(_user.Address)
                .EndScript());
        simulator.EndBlock();
        
        if ( shouldFail )
        {
            Assert.False(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
        else
        {
            Assert.True(simulator.LastBlockWasSuccessful(), simulator.FailedTxReason);
        }
    }
}
