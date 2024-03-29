using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;
using Phantasma.Business.Blockchain.Contracts;
using Phantasma.Business.Blockchain.Contracts.Native;
using Phantasma.Business.Blockchain.Tokens;
using Phantasma.Business.Blockchain.Tokens.Structs;
using Phantasma.Business.Blockchain.VM;
using Phantasma.Business.VM.Utils;
using Phantasma.Core;
using Phantasma.Core.Cryptography;
using Phantasma.Core.Cryptography.Structs;
using Phantasma.Core.Domain;
using Phantasma.Core.Domain.Contract;
using Phantasma.Core.Domain.Contract.Enums;
using Phantasma.Core.Domain.Contract.Interop.Structs;
using Phantasma.Core.Domain.Contract.Validator.Enums;
using Phantasma.Core.Domain.Events.Structs;
using Phantasma.Core.Domain.Exceptions;
using Phantasma.Core.Domain.Execution;
using Phantasma.Core.Domain.Execution.Enums;
using Phantasma.Core.Domain.Interfaces;
using Phantasma.Core.Domain.Serializer;
using Phantasma.Core.Domain.Tasks.Enum;
using Phantasma.Core.Domain.Token.Enums;
using Phantasma.Core.Domain.TransactionData;
using Phantasma.Core.Domain.Validation;
using Phantasma.Core.Domain.VM;
using Phantasma.Core.Numerics;
using Phantasma.Core.Storage.Context;
using Phantasma.Core.Storage.Context.Structs;
using Phantasma.Core.Types.Structs;
using Phantasma.Core.Utils;
using Serilog;
using Event = Phantasma.Core.Domain.Structs.Event;
using Transaction = Phantasma.Core.Domain.TransactionData.Transaction;

namespace Phantasma.Business.Blockchain
{
    public class Chain : IChain
    {
        private const string TransactionHashMapTag = ".txs";
        private const string BlockHashMapTag = ".blocks";
        private const string BlockHeightListTag = ".height";
        private const string TxBlockHashMapTag = ".txblmp";
        private const string AddressTxHashMapTag = ".adblmp";
        private const string TaskListTag = ".tasks";

        private List<Transaction> CurrentTransactions = new();

        private Dictionary<string, int> _methodTableForGasExtraction = null;

#region PUBLIC
        public static readonly uint InitialHeight = 1;

        public INexus Nexus { get; private set; }

        public string Name { get; private set; }
        public Address Address { get; private set; }

        public Block CurrentBlock { get; private set; }
        public Timestamp CurrentTime { get; private set; }
        public IEnumerable<Transaction> Transactions => CurrentTransactions;
        public string CurrentProposer { get; private set; }

        public StorageChangeSetContext CurrentChangeSet { get; private set; }

        public PhantasmaKeys ValidatorKeys { get; set; }
        public Address ValidatorAddress => ValidatorKeys != null ? ValidatorKeys.Address : Address.Null;

        public BigInteger Height => GetBlockHeight();

        public StorageContext Storage { get; private set; }

        public bool IsRoot => this.Name == DomainSettings.RootChainName;
#endregion

        public Chain(INexus nexus, string name)
        {
            Throw.IfNull(nexus, "nexus required");

            this.Name = name;
            this.Nexus = nexus;
            this.ValidatorKeys = null;

            this.Address = Address.FromHash(this.Name);

            this.Storage = (StorageContext)new KeyStoreStorage(Nexus.GetChainStorage(this.Name));
        }

        public Chain(INexus nexus, string name, PhantasmaKeys keys)
        {
            Throw.IfNull(nexus, "nexus required");

            this.Name = name;
            this.Nexus = nexus;
            this.ValidatorKeys = keys;

            this.Address = Address.FromHash(this.Name);

            this.Storage = (StorageContext)new KeyStoreStorage(Nexus.GetChainStorage(this.Name));
        }

        /// <summary>
        /// Get Current Protocol Version
        /// </summary>
        /// <returns></returns>
        private uint GetCurrentProtocolVersion()
        {
            var lastBlockHash = this.GetLastBlockHash();
            return GetCurrentProtocolVersion(lastBlockHash);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lastBlockHash"></param>
        /// <returns></returns>
        private uint GetCurrentProtocolVersion(Hash lastBlockHash)
        {
            uint protocol = DomainSettings.Phantasma30Protocol;
            try
            {
                if (lastBlockHash != Hash.Null)
                    protocol = Nexus.GetProtocolVersion(Nexus.RootStorage);
            }
            catch (Exception e)
            {
                Log.Information("Error getting info {Exception}", e);
            }

            return protocol;
        }

        public IEnumerable<Transaction> BeginBlock(string proposerAddress, BigInteger height, BigInteger minimumFee, Timestamp timestamp, IEnumerable<Address> availableValidators)
        {
            // should never happen
            if (this.CurrentBlock != null)
            {
                // TODO error message
                throw new Exception("Cannot begin new block, current block has not been processed yet");
            }

            var lastBlockHash = this.GetLastBlockHash();
            var lastBlock = this.GetBlockByHash(lastBlockHash);
            var isFirstBlock = lastBlock == null;

            var protocol = GetCurrentProtocolVersion(lastBlockHash);
            
            this.CurrentProposer = proposerAddress;
            var validator = Nexus.GetValidator(this.Storage, this.CurrentProposer);

            Address validatorAddress = validator.address;

            if (validator.address == Address.Null)
            {
                foreach (var address in availableValidators)
                {
                    if (address.TendermintAddress == this.CurrentProposer)
                    {
                        validatorAddress = address;
                        break;
                    }
                }

                if (validatorAddress == Address.Null)
                {
                    throw new Exception("Unknown validator");
                }
            }

            this.CurrentBlock = new Block(height
                , this.Address
                , timestamp
                , isFirstBlock ? Hash.Null : lastBlock.Hash
                , protocol
                , validatorAddress
                , new byte[0]
            );
            
            this.CurrentTime = timestamp;

            // create new storage context
            this.CurrentChangeSet = new StorageChangeSetContext(this.Storage);
            List<Transaction> systemTransactions = new ();
            var oracle = Nexus.GetOracleReader();

            if (this.IsRoot)
            {
                bool inflationReady = false;
                if ( protocol <= 12)
                    inflationReady = Filter.Enabled ? false : NativeContract.LoadFieldFromStorage<bool>(this.CurrentChangeSet, NativeContractKind.Gas, nameof(GasContract._inflationReady));
                else
                    inflationReady = NativeContract.LoadFieldFromStorage<bool>(this.CurrentChangeSet, NativeContractKind.Gas, nameof(GasContract._inflationReady));

                if (inflationReady)
                {
                    var senderAddress = this.CurrentBlock.Validator;

                    // NOTE inflation is a expensive transaction so it requires a larger gas limit compared to other transactions
                    int requiredGasLimit = Transaction.DefaultGasLimit * 50;
                    if ( Nexus.GetGovernanceValue(Storage,  Phantasma.Business.Blockchain.Nexus.NexusProtocolVersionTag) <= 8)
                        requiredGasLimit = Transaction.DefaultGasLimit * 4;
                    

                    var script = new ScriptBuilder()
                        .AllowGas(senderAddress, Address.Null, minimumFee, requiredGasLimit)
                        .CallContract(NativeContractKind.Gas, nameof(GasContract.ApplyInflation), this.CurrentBlock.Validator)
                        .SpendGas(senderAddress)
                        .EndScript();

                    Transaction transaction;
                        transaction = new Transaction(
                            this.Nexus.Name,
                            this.Name,
                            script,
                            this.CurrentBlock.Timestamp.Value + 1,
                            "SYSTEM");
                        
                    transaction.Sign(this.ValidatorKeys);
                    systemTransactions.Add(transaction);
                }
            }

            systemTransactions.AddRange(ProcessPendingTasks(this.CurrentBlock, oracle, minimumFee, this.CurrentChangeSet));

            // returns eventual system transactions that need to be broadcasted to tenderm int to be included into the current block
            return systemTransactions;
        }

        private (CodeType, string) HandleWhitelistedMethods(IEnumerable<DisasmMethodCall> methods, Timestamp timestamp, uint protocolVersion)
        {
            if (methods.Any(x => x.MethodName.Equals(nameof(SwapContract.SwapFee)) || x.MethodName.Equals(nameof(ExchangeContract.SwapFee))))
            {
                var existsLPToken = Nexus.TokenExists(Storage, DomainSettings.LiquidityTokenSymbol);
                BigInteger exchangeVersion = 0;
                if (protocolVersion >= 14)
                {
                    try
                    {
                        exchangeVersion = this.InvokeContractAtTimestamp(Storage, timestamp, NativeContractKind.Exchange, nameof(ExchangeContract.GetDexVersion)).AsNumber();
                    }
                    catch ( Exception e)
                    {
                        Log.Error("Error getting exchange version {Exception}", e);
                    }
                }
                else
                {
                    exchangeVersion = this.InvokeContractAtTimestamp(Storage, CurrentBlock.Timestamp, NativeContractKind.Exchange, nameof(ExchangeContract.GetDexVersion)).AsNumber();
                }
                
                if (existsLPToken && exchangeVersion >= 1) // Check for the Exchange contract
                {
                    var exchangePot = GetTokenBalance(Storage, DomainSettings.FuelTokenSymbol, SmartContract.GetAddressForNative(NativeContractKind.Exchange));
                    Log.Information("Exchange Pot Balance: {balance}", exchangePot);
                    var unitValue = UnitConversion.GetUnitValue(DomainSettings.FuelTokenDecimals);
                    if (exchangePot < unitValue) {
                        return (CodeType.Error, $"Empty pot Exchange");
                    }
                }
                else
                {
                    var pot = GetTokenBalance(Storage, DomainSettings.FuelTokenSymbol, SmartContract.GetAddressForNative(NativeContractKind.Swap));
                    Log.Information("Swap Pot Balance: {balance}", pot);
                    var unitValue = UnitConversion.GetUnitValue(DomainSettings.FuelTokenDecimals);
                    if (pot < unitValue) {
                        return (CodeType.Error, $"Empty pot Swap");
                    }
                }
            }
            
            return (CodeType.Ok, "");
        }

        public (CodeType, string) CheckTx(Transaction tx, Timestamp timestamp)
        {
            uint protocolVersion = Nexus.GetProtocolVersion(Storage);
            Log.Information("check tx {Hash}", tx.Hash);

            if (tx.Expiration < timestamp)
            {
                var type = CodeType.Expired;
                Log.Information("check tx error {Expired} {Hash}", type, tx.Hash);
                return (type, "Transaction is expired");
            }

            if (!tx.IsValid(this))
            {
                Log.Information("check tx 2 " + tx.Hash);
                return (CodeType.InvalidChain, "Transaction is not meant to be executed on this chain");
            }

            if (tx.Signatures.Length == 0)
            {
                var type = CodeType.UnsignedTx;
                Log.Information("check tx error {UsignedTx} {Hash}", type, tx.Hash);
                return (type, "Transaction is not signed");
            }

            if (protocolVersion >= 13)
            {
                if (tx.Script.Length > DomainSettings.ArchiveMaxSize)
                {
                    var type = CodeType.Error;
                    Log.Information("check tx error {ScriptTooBig} {Hash}", type, tx.Hash);
                    return (type, "Transaction script is too big");
                }

                if (this.ContainsTransaction(tx.Hash))
                {
                    var type = CodeType.Error;
                    Log.Information("check tx error {Error} {Hash}", type, tx.Hash);
                    return (type, "Transaction already exists in chain");
                }
            }

            if (Nexus.HasGenesis())
            {
                Address from, target;
                BigInteger gasPrice, gasLimit;

                if (_methodTableForGasExtraction == null)
                {
                    _methodTableForGasExtraction = GenerateMethodTable();
                }

                IEnumerable<DisasmMethodCall> methods;

                if (protocolVersion >= 14)
                {
                    try
                    {
                        methods = DisasmUtils.ExtractMethodCalls(tx.Script, protocolVersion, _methodTableForGasExtraction, detectAndUseJumps: true);
                    }
                    catch (Exception ex)
                    {
                        var type = CodeType.Error;
                        Log.Information("check tx error {Error} {Hash}", type, tx.Hash);
                        return (type, "Error pre-processing transaction script contents: " + ex.Message);
                    }
                }
                else
                {
                    methods = DisasmUtils.ExtractMethodCalls(tx.Script, protocolVersion, _methodTableForGasExtraction, detectAndUseJumps: false);
                }
                
                
                /*if (transaction.TransactionGas != TransactionGas.Null)
                    {
                        from = transaction.TransactionGas.GasPayer;
                        target = transaction.TransactionGas.GasTarget;
                        gasPrice = transaction.TransactionGas.GasPrice;
                        gasLimit = transaction.TransactionGas.GasLimit;
                    }
                    else
                    {
                        var result = this.ExtractGasInformation(tx, out from, out target, out gasPrice,
                            out gasLimit, methods, _methodTableForGasExtraction);

                        if (result.Item1 != CodeType.Ok)
                        {
                            return (result.Item1, result.Item2);
                        }
                    }*/

                var result = this.ExtractGasInformation(tx, out from, out target, out gasPrice, out gasLimit, methods, _methodTableForGasExtraction);

                if (result.Item1 != CodeType.Ok)
                {
                    return (result.Item1, result.Item2);
                }

                if (protocolVersion >= 13)
                {
                    /*if (from.IsNull  || gasPrice <= 0 || gasLimit <= 0)
                    {
                        var type = CodeType.NoSystemAddress;
                        Log.Information("check tx error {type} {Hash}", type, tx.Hash);
                        return (type, "AllowGas or GasPayer / GasTarget / GasPrice / GasLimit call not found in transaction script");
                    }*/

                    if (!tx.IsSignedBy(from))
                    {
                        var type = CodeType.Error;
                        Log.Information("check tx error {Error} {Hash}", type, tx.Hash);
                        return (type, "Transaction was not signed by the caller address");
                    }
                }
                
                var whitelisted = TransactionExtensions.IsWhitelisted(methods);
                if (whitelisted)
                {
                    var cosmicResult = HandleWhitelistedMethods(methods, timestamp, protocolVersion);
                    if ( cosmicResult.Item1 != CodeType.Ok)
                    {
                        return (cosmicResult.Item1, cosmicResult.Item2);
                    }
                }
                else
                {
                    var minFee = Nexus.GetGovernanceValue(Nexus.RootStorage, GovernanceContract.GasMinimumFeeTag);
                    if (gasPrice < minFee)
                    {
                        var type = CodeType.GasFeeTooLow;
                        Log.Information("check tx error {type} {Hash}", type, tx.Hash);
                        return (type, "Gas fee too low");
                    }

                    var minGasRequired = gasPrice * gasLimit;
                    var balance = GetTokenBalance(this.Storage, DomainSettings.FuelTokenSymbol, from);
                    if (balance < minGasRequired)
                    {
                        var type = CodeType.MissingFuel;
                        Log.Information("check tx error {MissingFuel} {Hash}", type, tx.Hash);

                        if (balance == 0)
                        {
                            return (type, $"Missing fuel, {from} has 0 {DomainSettings.FuelTokenSymbol}");
                        }
                        else
                        {
                            return (type, $"Missing fuel, {from} has {UnitConversion.ToDecimal(balance, DomainSettings.FuelTokenDecimals)} {DomainSettings.FuelTokenSymbol} expected at least {UnitConversion.ToDecimal(minGasRequired, DomainSettings.FuelTokenDecimals)} {DomainSettings.FuelTokenSymbol}");
                        }
                    }
                }
            }

            if (tx.Script.Length == 0)
            {
                var type = CodeType.InvalidScript;
                Log.Information("check tx error {type} {Hash}", type, tx.Hash);
                return (type, "Script attached to tx is invalid");
            }

            Log.Information("check tx Successful {Hash}", tx.Hash);
            return (CodeType.Ok, "");
        }

        internal void FlushExtCalls()
        {
            // make it null here to force next txs received to rebuild it
            _methodTableForGasExtraction = null;
        }

        public Dictionary<string, int> GenerateMethodTable(uint? ProtocolVersion = null)
        {
            if ( ProtocolVersion == null ) ProtocolVersion = GetCurrentProtocolVersion();
            var table = DisasmUtils.GetDefaultDisasmTable(ProtocolVersion.Value);

            var contracts = GetContracts(this.Storage);

            foreach (var contract in contracts)
            {
                var nativeKind = contract.Name.FindNativeContractKindByName();
                if (nativeKind != NativeContractKind.Unknown)
                {
                    continue; // we skip native contracts as those are already in the dictionary from GetDefaultDisasmTable()
                }

                table.AddContractToTable(contract);
            }

            var tokens = this.Nexus.GetAvailableTokenSymbols(Nexus.RootStorage);
            foreach (var symbol in tokens)
            {
                if (Nexus.IsSystemToken(symbol) && symbol != DomainSettings.LiquidityTokenSymbol)
                {
                    continue;
                }

                var token = Nexus.GetTokenInfo(Nexus.RootStorage, symbol);
                table.AddTokenToTable(token);
            }

            return table;
        }

        public IEnumerable<T> EndBlock<T>() where T : class
        {
            
            //if (Height == 1)
            //{
            //    throw new ChainException("genesis transaction failed");
            //}
            
            // TODO currently the managing of the ABI cache is broken so we have to call this at end of the block
            ((Chain)Nexus.RootChain).FlushExtCalls();
            
            // TODO return block events
            if (typeof(T) == typeof(Block))
            {
                this.CurrentBlock.AddOraclesEntries(Nexus.GetOracleReader().Entries);
                //var blocks = new List<Block>() { CurrentBlock };
                //return (List<T>) Convert.ChangeType(blocks, typeof(List<T>));
            }
            
            // TODO validator update - Only be allowed after extensive testing.
            /*try
            {
                if (typeof(T) == typeof(ValidatorUpdate))
                {
                    return HandleValidatorUpdates() as List<T>;
                }
            }
            catch (Exception e)
            {
                //Webhook.Notify("Error in HandleValidatorUpdates");
                Log.Error(e, "Error in HandleValidatorUpdates");
            }*/
            
            return new List<T>();
        }

        public TransactionResult DeliverTx(Transaction tx)
        {
            TransactionResult result = new();

            Log.Information("Deliver tx {Hash}", tx);

            try
            {
                if (CurrentTransactions.Any(x => x.Hash == tx.Hash))
                {
                    throw new ChainException("Duplicated transaction hash");
                }

                CurrentTransactions.Add(tx);
                var txIndex = CurrentTransactions.Count - 1;
                var oracle = Nexus.GetOracleReader();

                // create snapshot
                var snapshot = this.CurrentChangeSet.Clone();

                result = ExecuteTransaction(txIndex, tx, tx.Script, this.CurrentBlock.Validator,
                    this.CurrentBlock.Timestamp, snapshot, this.CurrentBlock.Notify, oracle,
                    ChainTask.Null);

                if (result.State == ExecutionState.Halt)
                {
                    if (result.Result != null)
                    {
                        var resultBytes = Serialization.Serialize(result.Result);
                        this.CurrentBlock.SetResultForHash(tx.Hash, resultBytes);
                    }

                    snapshot.Execute();
                }
                else
                {
                    snapshot = null;
                }

                this.CurrentBlock.SetStateForHash(tx.Hash, result.State);
                
            }
            catch (Exception e)
            {
                // log original exception, throwing it again kills the call stack!
                Log.Error("Exception for {Hash} in DeliverTx {Exception}", tx.Hash, e);
                result.Code = 1;
                result.Codespace = e.Message;
                result.State = ExecutionState.Fault;
                this.CurrentBlock.SetStateForHash(tx.Hash, result.State);

                ProcessFilteredExceptions(e.Message);
            }

            return result;
        }

        private void ProcessFilteredExceptions(string exceptionMessage)
        {
            var filteredAddress = Filter.ExtractFilteredAddress(exceptionMessage);

            if (!filteredAddress.IsNull)
            {
                Filter.AddRedFilteredAddress(Nexus.RootStorage, filteredAddress);
            }
        }

        public byte[] Commit()
        {
            Log.Information("Committing block {Height}", this.CurrentBlock.Height);
            if (!this.CurrentBlock.IsSigned)
            {
                if ( this.CurrentBlock.Validator == ValidatorKeys.Address)
                {
                    this.CurrentBlock.Sign(ValidatorKeys);
                }
            }
            Block lastBlock = this.CurrentBlock;
            
            try
            {
                AddBlock(this.CurrentBlock, this.CurrentTransactions, this.CurrentChangeSet);
            }
            catch (Exception e)
            {
                // Commit cannot throw anything, an error in this phase has to stop the node!
                Log.Error("Critical failure {Error}", e);
                Webhook.Notify($"[{((DateTime) CurrentBlock.Timestamp).ToLongDateString()}] reason -> Critical failure by [{e.Message}]");
                Environment.Exit(-1);
            }

            this.CurrentBlock = null;
            this.CurrentTransactions.Clear();
            return lastBlock.Hash.ToByteArray();
        }

        public IContract[] GetContracts(StorageContext storage)
        {
            var contractList = new StorageList(SmartContractSheet.GetContractListKey(), storage);
            var addresses = contractList.All<Address>();
            return addresses.Select(x => this.GetContractByAddress(storage, x)).ToArray();
        }

        public override string ToString()
        {
            return $"{Name} ({Address})";
        }

        private bool VerifyBlockBeforeAdd(Block block)
        {
            if (block.TransactionCount >= DomainSettings.MaxTxPerBlock)
            {
                return false;
            }

            /* THOSE DONT WORK because the block is still empty!
            
            if (block.OracleData.Count() >= DomainSettings.MaxOracleEntriesPerBlock)
            {
                return false;
            }

            if (block.Events.Count() >= DomainSettings.MaxEventsPerBlock)
            {
                return false;
            }*/

            return true;
        }

        public void AddBlock(Block block, IEnumerable<Transaction> transactions, StorageChangeSetContext changeSet)
        {
            block.AddAllTransactionHashes(transactions.Select (x => x.Hash).ToArray());
            
            this.SetBlock(block, transactions, changeSet);
        }

        public byte[] SetBlock(Block block, IEnumerable<Transaction> transactions, StorageChangeSetContext changeSet)
        {

            // Validate block 
            if (!VerifyBlockBeforeAdd(block))
            {
                throw new ChainException("Invalid block");
            }
            
            if (!block.IsSigned)
            {
                throw new ChainException("Block is not signed");
            }
                
            if ( block.PreviousHash != this.CurrentBlock.PreviousHash)
            {
                throw new ChainException("Block previous hash is not the same as the current block");
            }
                
            if ( block.Height != this.CurrentBlock.Height)
            {
                throw new ChainException("Block height is not the same as the current block");
            }

            if (block.Timestamp != this.CurrentBlock.Timestamp)
            {
                throw new ChainException("Block timestamp is not the same as the current block");
            }

            if (block.ChainAddress != this.CurrentBlock.ChainAddress)
            {
                throw new ChainException("Block chain address is not the same as the current block");
            }
                
            if ( block.Events.Count() != this.CurrentBlock.Events.Count())
            {
                throw new ChainException("Block events are not the same as the current block");
            }
            
            if ( block.Events.Except(this.CurrentBlock.Events).Count() != 0 && this.CurrentBlock.Events.Except(block.Events).Count() != 0 )
            {
                var blockEvents = block.Events.ToArray();
                var currentBlockEvents = this.CurrentBlock.Events.ToArray();
                
                for(int i = 0; i < blockEvents.Length; i++)
                {
                    if (!blockEvents[i].Equals(currentBlockEvents[i]))
                    {
                        throw new ChainException($"Block events are not the same as the current block\n {blockEvents[i]}\n {currentBlockEvents[i]}");
                    }
                }
            }
                
            if ( block.Protocol != this.CurrentBlock.Protocol)
            {
                throw new ChainException("Block protocol is not the same as the current block");
            }
            
            if (Nexus.HasGenesis())
            {
                if ( !Nexus.IsPrimaryValidator(block.Validator, block.Timestamp) )
                {
                    throw new ChainException("Block validator is not a valid validator");
                }

                var transactionHashs = transactions.Select(x => x.Hash).ToArray();
                if ( block.TransactionHashes.Count() != transactionHashs.Count())
                {
                    throw new ChainException("Block transaction hashes are not the same as the current block");
                }
                
                if (transactions.Select(tx => !this.ContainsTransaction(tx.Hash)).All(tx => !tx))
                {
                    foreach (var tx in transactions)
                    {
                        if (this.ContainsTransaction(tx.Hash))
                        {
                            throw new ChainException($"Tx Hash already in the chain, cannot add block : {tx.Hash}");
                        }
                    }
                }
            
                if ( this.CurrentBlock.TransactionCount == 0)
                    this.CurrentBlock.AddAllTransactionHashes(transactionHashs);
                
                if (block.TransactionHashes.Except(transactionHashs).Count() != 0 &&
                    transactionHashs.Except(block.TransactionHashes).Count() != 0)
                {
                    var blockTransactionHashes = block.TransactionHashes;
                    var currentBlockTransactionHashes = transactionHashs.ToArray();

                    for (int i = 0; i < currentBlockTransactionHashes.Length; i++)
                    {
                        if (!blockTransactionHashes.Contains(currentBlockTransactionHashes[i]))
                        {
                            throw new ChainException(
                                $"Block transaction hashes are not the same as the current block\n {blockTransactionHashes[i]}\n {currentBlockTransactionHashes[i]}");
                        }
                    }
                }
                
                if (transactions.Select(tx => tx.IsValid(this)).Any(valid => !valid))
                {
                    throw new ChainException("Block transactions are not valid");
                }
                
                if (transactions.Count() != this.Transactions.Count())
                {
                    throw new ChainException(
                        $"Block transactions are not the same as the current block, {transactions.Count()} != {this.Transactions.Count()} | {this.CurrentBlock.TransactionCount}");
                }
                
                if (transactions.Except(this.Transactions).Count() != 0 &&
                    this.Transactions.Except(transactions).Count() != 0)
                {
                    var blockTransactions = transactions.ToArray();
                    var currentBlockTransactions = this.Transactions.ToArray();

                    for (int i = 0; i < blockTransactions.Length; i++)
                    {
                        if (!blockTransactions[i].Equals(currentBlockTransactions[i]))
                        {
                            throw new ChainException(
                                $"Block transactions are not the same as the current block\n {blockTransactions[i]}\n {currentBlockTransactions[i]}");
                        }
                    }
                }
            }

            // from here on, the block is accepted
            changeSet.Execute();

            var hashList = new StorageList(BlockHeightListTag, this.Storage);
            hashList.Add<Hash>(block.Hash);
            
            // persist genesis hash at height 1
            if (block.Height == 1)
            {
                var genesisHash = block.Hash;
                Nexus.CommitGenesis(genesisHash);
            }
            
            var blockMap = new StorageMap(BlockHashMapTag, this.Storage);
            
            var blockBytes = block.ToByteArray(true);

            var blk = Block.Unserialize(blockBytes);
            blockBytes = CompressionUtils.Compress(blockBytes);
            blockMap.Set<Hash, byte[]>(block.Hash, blockBytes);

            var txMap = new StorageMap(TransactionHashMapTag, this.Storage);
            var txBlockMap = new StorageMap(TxBlockHashMapTag, this.Storage);

            foreach (Transaction tx in transactions)
            {
                var txBytes = tx.ToByteArray(true);
                txBytes = CompressionUtils.Compress(txBytes);
                txMap.Set<Hash, byte[]>(tx.Hash, txBytes);
                txBlockMap.Set<Hash, Hash>(tx.Hash, block.Hash);
            }
            
            foreach (var transaction in transactions)
            {
                var addresses = new HashSet<Address>();
                var events = block.GetEventsForTransaction(transaction.Hash);

                foreach (var evt in events)
                {
                    if (evt.Contract == "gas" && (evt.Address.IsSystem || evt.Address == block.Validator))
                    {
                        continue;
                    }

                    addresses.Add(evt.Address);
                }

                var addressTxMap = new StorageMap(AddressTxHashMapTag, this.Storage);
                foreach (var address in addresses)
                {
                    var addressList = addressTxMap.Get<Address, StorageList>(address);
                    addressList.Add<Hash>(transaction.Hash);
                }
            }
            
            Block lastBlock = this.CurrentBlock;

            this.CurrentBlock = null;
            this.CurrentTransactions.Clear();
            
            Log.Information("Committed block {Height}", lastBlock.Height);

            return lastBlock.Hash.ToByteArray();
        }

        private TransactionResult ExecuteTransaction(int index, Transaction transaction, byte[] script, Address validator, Timestamp time, StorageChangeSetContext changeSet
                , Action<Hash, Event> onNotify, IOracleReader oracle, IChainTask task)
        {
            var result = new TransactionResult();

            result.Hash = transaction.Hash;

            uint offset = 0;

            RuntimeVM runtime;
            runtime = new RuntimeVM(index, script, offset, this, validator, time, transaction, changeSet, oracle, task);
            
            result.State = runtime.Execute();

            result.Events = runtime.Events.ToArray();
            result.GasUsed = (long)runtime.UsedGas;

            foreach (var evt in runtime.Events)
            {
                onNotify(transaction.Hash, evt);
            }

            if (result.State != ExecutionState.Halt)
            {
                result.Code = 1;
                result.Codespace = runtime.ExceptionMessage ?? "Execution Unsuccessful";
                ProcessFilteredExceptions(result.Codespace);
                return result;
            }

            if (runtime.Stack.Count > 0)
            {
                result.Result = runtime.Stack.Pop();
            }

            // merge transaction oracle data
            oracle.MergeTxData();

            result.Code = 0;
            result.Codespace = "Execution Successful";
            return result;
        }

        // NOTE should never be used directly from a contract, instead use Runtime.GetBalance!
        public BigInteger GetTokenBalance(StorageContext storage, IToken token, Address address)
        {
            if (token.Flags.HasFlag(TokenFlags.Fungible))
            {
                var balances = new BalanceSheet(token);
                return balances.Get(storage, address);
            }
            else
            {
                var ownerships = new OwnershipSheet(token.Symbol);
                var items = ownerships.Get(storage, address);
                return items.Length;
            }
        }

        public BigInteger GetTokenBalance(StorageContext storage, string symbol, Address address)
        {
            var token = Nexus.GetTokenInfo(storage, symbol);
            return GetTokenBalance(storage, token, address);
        }

        public BigInteger GetTokenSupply(StorageContext storage, string symbol)
        {
            var supplies = new SupplySheet(symbol, this, Nexus);
            return supplies.GetTotal(storage);
        }

        // NOTE this lists only nfts owned in this chain
        public BigInteger[] GetOwnedTokens(StorageContext storage, string tokenSymbol, Address address)
        {
            var ownership = new OwnershipSheet(tokenSymbol);
            return ownership.Get(storage, address).ToArray();
        }

        /// <summary>
        /// Deletes all blocks starting at the specified hash.
        /// </summary>
        /*
        public void DeleteBlocks(Hash targetHash)
        {
            var targetBlock = FindBlockByHash(targetHash);
            Throw.IfNull(targetBlock, nameof(targetBlock));

            var currentBlock = this.LastBlock;
            while (true)
            {
                Throw.IfNull(currentBlock, nameof(currentBlock));

                var changeSet = _blockChangeSets[currentBlock.Hash];
                changeSet.Undo();

                _blockChangeSets.Remove(currentBlock.Hash);
                _blockHeightMap.Remove(currentBlock.Height);
                _blocks.Remove(currentBlock.Hash);

                currentBlock = FindBlockByHash(currentBlock.PreviousHash);

                if (currentBlock.PreviousHash == targetHash)
                {
                    break;
                }
            }
        }*/

        public ExecutionContext GetContractContext(StorageContext storage, SmartContract contract)
        {
            if (!IsContractDeployed(storage, contract.Address))
            {
                throw new ChainException($"contract '{contract.Name}' not deployed on '{Name}' chain");
            }

            var context = new ChainExecutionContext(contract);
            return context;
        }

        public VMObject InvokeContractAtTimestamp(StorageContext storage, Timestamp time, NativeContractKind nativeContract, string methodName, params object[] args)
        {
            return InvokeContractAtTimestamp(storage, time, nativeContract.GetContractName(), methodName, args);
        }

        public VMObject InvokeContractAtTimestamp(StorageContext storage, Timestamp time, string contractName, string methodName, params object[] args)
        {
            var script = ScriptUtils.BeginScript().CallContract(contractName, methodName, args).EndScript();

            var result = InvokeScript(storage, script, time);

            if (result == null)
            {
                throw new ChainException($"Invocation of method '{methodName}' of contract '{contractName}' failed");
            }

            return result;
        }

        public VMObject InvokeScript(StorageContext storage, byte[] script, Timestamp time)
        {
            var oracle = Nexus.GetOracleReader();
            var changeSet = new StorageChangeSetContext(storage);
            uint offset = 0;
            var vm = new RuntimeVM(-1, script, offset, this, Address.Null, time, Transaction.Null, changeSet, oracle, ChainTask.Null);

            var state = vm.Execute();

            if (state != ExecutionState.Halt)
            {
                return null;
            }

            if (vm.Stack.Count == 0)
            {
                throw new ChainException($"No result, vm stack is empty");
            }

            var result = vm.Stack.Pop();

            return result;
        }

        // generates incremental ID (unique to this chain)
        public BigInteger GenerateUID(StorageContext storage)
        {
            var key = Encoding.ASCII.GetBytes("_uid");

            var lastID = storage.Has(key) ? storage.Get<BigInteger>(key) : 0;

            lastID++;
            storage.Put<BigInteger>(key, lastID);

            return lastID;
        }

#region FEES
        public BigInteger GetBlockReward(Block block)
        {
            if (block.TransactionCount == 0)
            {
                return 0;
            }

            var lastTxHash = block.TransactionHashes[block.TransactionHashes.Length - 1];
            var evts = block.GetEventsForTransaction(lastTxHash);

            BigInteger total = 0;
            foreach (var evt in evts)
            {
                if (evt.Kind == EventKind.TokenClaim && evt.Contract == "block")
                {
                    var data = evt.GetContent<TokenEventData>();
                    total += data.Value;
                }
            }

            return total;
        }

        public BigInteger GetTransactionFee(Transaction tx)
        {
            Throw.IfNull(tx, nameof(tx));
            return GetTransactionFee(tx.Hash);
        }

        public BigInteger GetTransactionFee(Hash transactionHash)
        {
            Throw.IfNull(transactionHash, nameof(transactionHash));

            BigInteger fee = 0;

            var blockHash = GetBlockHashOfTransaction(transactionHash);
            var block = GetBlockByHash(blockHash);
            Throw.IfNull(block, nameof(block));

            var events = block.GetEventsForTransaction(transactionHash);
            foreach (var evt in events)
            {
                if (evt.Kind == EventKind.GasPayment && evt.Contract == "gas")
                {
                    var info = evt.GetContent<GasEventData>();
                    fee += info.amount * info.price;
                }
            }

            return fee;
        }
#endregion

#region Contracts
        private byte[] GetContractListKey()
        {
            return Encoding.ASCII.GetBytes("contracts.");
        }

        private byte[] GetContractKey(Address contractAddress, string field)
        {
            var bytes = Encoding.ASCII.GetBytes(field);
            var key = ByteArrayUtils.ConcatBytes(bytes, contractAddress.ToByteArray());
            return key;
        }

        public bool IsContractDeployed(StorageContext storage, string name)
        {
            if (ValidationUtils.IsValidTicker(name))
            {
                return Nexus.TokenExists(storage, name);
            }

            return IsContractDeployed(storage, SmartContract.GetAddressFromContractName(name));
        }

        public bool IsContractDeployed(StorageContext storage, Address contractAddress)
        {
            if (contractAddress == SmartContract.GetAddressForNative(NativeContractKind.Gas))
            {
                return true;
            }

            if (contractAddress == SmartContract.GetAddressForNative(NativeContractKind.Block))
            {
                return true;
            }

            if (contractAddress == SmartContract.GetAddressForNative(NativeContractKind.Unknown))
            {
                return false;
            }

            var contract = new SmartContractSheet(contractAddress);
            if (contract.HasScript(storage))
            {
                return true;
            }

            var token = Nexus.GetTokenInfo(storage, contractAddress);
            return (token != null);
        }

        public bool DeployContractScript(StorageContext storage, Address contractOwner, string name, Address contractAddress, byte[] script, ContractInterface abi)
        {
            var contract = new SmartContractSheet(name, contractAddress);
            if (contract.HasScript(storage))
            {
                return false;
            }

            contract.PutScript(storage, script);
            
            var ownerBytes = contractOwner.ToByteArray();
            contract.PutOwner(storage, ownerBytes);


            var abiBytes = abi.ToByteArray();
            contract.PutABI(storage, abiBytes);

            var nameBytes = Encoding.ASCII.GetBytes(name);
            contract.PutName(storage, nameBytes);
            
            contract.AddToList(storage, contractAddress);

            FlushExtCalls();

            return true;
        }

        public SmartContract GetContractByAddress(StorageContext storage, Address contractAddress)
        {
            var contract = new SmartContractSheet(contractAddress);

            if (contract.HasName(storage))
            {
                var nameBytes = contract.GetName(storage);
                var name = Encoding.ASCII.GetString(nameBytes);
                return GetContractByName(storage, name);
            }

            var symbols = Nexus.GetAvailableTokenSymbols(storage);
            foreach (var symbol in symbols)
            {
                var tokenAddress = TokenUtils.GetContractAddress(symbol);

                if (tokenAddress == contractAddress)
                {
                    var token = Nexus.GetTokenInfo(storage, symbol);
                    return new CustomContract(token.Symbol, token.Script, token.ABI);
                }
            }

            return NativeContract.GetNativeContractByAddress(contractAddress);
        }

        public SmartContract GetContractByName(StorageContext storage, string name)
        {
            if (Blockchain.Nexus.IsNativeContract(name) || ValidationUtils.IsValidTicker(name))
            {
                return Nexus.GetContractByName(storage, name);
            }

            var address = SmartContract.GetAddressFromContractName(name);
            var contract = new SmartContractSheet(address);
            if (!contract.HasScript(storage))
            {
                return null;
            }

            var script = contract.GetScript(storage);

            var abiBytes = contract.GetABI(storage);
            var abi = ContractInterface.FromBytes(abiBytes);

            return new CustomContract(name, script, abi);
        }

        public void UpgradeContract(StorageContext storage, string name, byte[] script, ContractInterface abi)
        {
            if (Blockchain.Nexus.IsNativeContract(name) || ValidationUtils.IsValidTicker(name))
            {
                throw new ChainException($"Cannot upgrade this type of contract: {name}");
            }

            if (!IsContractDeployed(storage, name))
            {
                throw new ChainException($"Cannot upgrade non-existing contract: {name}");
            }

            var address = SmartContract.GetAddressFromContractName(name);
            var contract = new SmartContractSheet(address);

            contract.PutScript(storage, script);

            var abiBytes = abi.ToByteArray();
            contract.PutABI(storage, abiBytes);

            FlushExtCalls();
        }

        public void KillContract(StorageContext storage, string name)
        {
            if (Blockchain.Nexus.IsNativeContract(name) || ValidationUtils.IsValidTicker(name))
            {
                throw new ChainException($"Cannot kill this type of contract: {name}");
            }

            if (!IsContractDeployed(storage, name))
            {
                throw new ChainException($"Cannot kill non-existing contract: {name}");
            }

            var address = SmartContract.GetAddressFromContractName(name);
            var contract = new SmartContractSheet(address);
            
            contract.DeleteScript(storage);
            contract.DeleteABI(storage);
            //contract.DeleteName(storage);
            //contract.DeleteOwner(storage);
            

            // TODO clear other storage used by contract (global variables, maps, lists, etc)
            // contract.DeleteContract(storage);
        }

        public Address GetContractOwner(StorageContext storage, Address contractAddress)
        {
            if (contractAddress.IsSystem)
            {
                var contract = new SmartContractSheet(contractAddress);
                var owner = contract.GetOwner(storage);
                if (owner != Address.Null)
                {
                    return owner;
                }

                var token = Nexus.GetTokenInfo(storage, contractAddress);
                if (token != null)
                {
                    return token.Owner;
                }
            }

            return Address.Null;
        }

#endregion

        private BigInteger GetBlockHeight()
        {
            var hashList = new StorageList(BlockHeightListTag, this.Storage);
            return hashList.Count();
        }

        public Hash GetLastBlockHash()
        {
            var lastHeight = GetBlockHeight();
            if (lastHeight <= 0)
            {
                return Hash.Null;
            }

            return GetBlockHashAtHeight(lastHeight);
        }

        public Hash GetBlockHashAtHeight(BigInteger height)
        {
            if (height <= 0)
            {
                throw new ChainException("invalid block height");
            }

            if (height > this.Height)
            {
                return Hash.Null;
            }

            var hashList = new StorageList(BlockHeightListTag, this.Storage);
            // NOTE chain heights start at 1, but list index start at 0
            var hash = hashList.Get<Hash>(height - 1);
            return hash;
        }

        public Block GetBlockByHash(Hash hash)
        {
            if (hash == Hash.Null)
            {
                return null;
            }

            var blockMap = new StorageMap(BlockHashMapTag, this.Storage);

            if (blockMap.ContainsKey<Hash>(hash))
            {
                var bytes = blockMap.Get<Hash, byte[]>(hash);
                bytes = CompressionUtils.Decompress(bytes);
                var block = Block.Unserialize(bytes);

                if (block.Hash != hash)
                {
                    throw new ChainException("data corruption on block: " + hash);
                }

                return block;
            }

            return null;
        }

        public bool ContainsBlockHash(Hash hash)
        {
            return GetBlockByHash(hash) != null;
        }

        public BigInteger GetTransactionCount()
        {
            var txMap = new StorageMap(TransactionHashMapTag, this.Storage);
            return txMap.Count();
        }

        public bool ContainsTransaction(Hash hash)
        {
            var txMap = new StorageMap(TransactionHashMapTag, this.Storage);
            return txMap.ContainsKey(hash);
        }

        public Transaction GetTransactionByHash(Hash hash)
        {
            var txMap = new StorageMap(TransactionHashMapTag, this.Storage);
            if (txMap.ContainsKey<Hash>(hash))
            {
                var bytes = txMap.Get<Hash, byte[]>(hash);
                bytes = CompressionUtils.Decompress(bytes);
                var tx = Transaction.Unserialize(bytes);

                if (tx.Hash != hash)
                {
                    throw new ChainException("data corruption on transaction: " + hash);
                }

                return tx;
            }

            return null;
        }

        public Hash GetBlockHashOfTransaction(Hash transactionHash)
        {
            var txBlockMap = new StorageMap(TxBlockHashMapTag, this.Storage);

            if (txBlockMap.ContainsKey(transactionHash))
            {
                var blockHash = txBlockMap.Get<Hash, Hash>(transactionHash);
                return blockHash;
            }

            return Hash.Null;
        }

        public IEnumerable<Transaction> GetBlockTransactions(Block block)
        {
            return block.TransactionHashes.Select(hash => GetTransactionByHash(hash));
        }

        public Hash[] GetTransactionHashesForAddress(Address address)
        {
            var addressTxMap = new StorageMap(AddressTxHashMapTag, this.Storage);
            var addressList = addressTxMap.Get<Address, StorageList>(address);
            return addressList.All<Hash>();
        }

        public Timestamp GetLastActivityOfAddress(Address address)
        {
            var addressTxMap = new StorageMap(AddressTxHashMapTag, this.Storage);
            var addressList = addressTxMap.Get<Address, StorageList>(address);
            var count = addressList.Count();
            if (count <= 0)
            {
                return Timestamp.Null;
            }

            var lastTxHash = addressList.Get<Hash>(count - 1);
            var blockHash = GetBlockHashOfTransaction(lastTxHash);

            var block = GetBlockByHash(blockHash);

            if (block == null) // should never happen
            {
                return Timestamp.Null;
            }

            return block.Timestamp;
        }

#region SWAPS
        /// <summary>
        /// Get Swap list for an address
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        private StorageList GetSwapListForAddress(StorageContext storage, Address address)
        {
            var key = ByteArrayUtils.ConcatBytes(Encoding.UTF8.GetBytes(".swapaddr"), address.ToByteArray());
            return new StorageList(key, storage);
        }
        
        /// <summary>
        /// Get the swap map
        /// </summary>
        /// <param name="storage"></param>
        /// <returns></returns>
        private StorageMap GetSwapMap(StorageContext storage)
        {
            var key = Encoding.UTF8.GetBytes(".swapmap");
            return new StorageMap(key, storage);
        }

        /// <summary>
        /// Register a swap
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="from"></param>
        /// <param name="swap"></param>
        public void RegisterSwap(StorageContext storage, Address from, ChainSwap swap)
        {
            var list = GetSwapListForAddress(storage, from);
            list.Add<Hash>(swap.sourceHash);

            var map = GetSwapMap(storage);
            map.Set<Hash, ChainSwap>(swap.sourceHash, swap);
        }

        /// <summary>
        /// Get a swap
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="sourceHash"></param>
        /// <returns></returns>
        /// <exception cref="ChainException"></exception>
        public ChainSwap GetSwap(StorageContext storage, Hash sourceHash)
        {
            var map = GetSwapMap(storage);

            if (map.ContainsKey<Hash>(sourceHash))
            {
                return map.Get<Hash, ChainSwap>(sourceHash);
            }

            throw new ChainException("invalid chain swap hash: " + sourceHash);
        }

        /// <summary>
        /// Get Swap Hashs for address
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public Hash[] GetSwapHashesForAddress(StorageContext storage, Address address)
        {
            var list = GetSwapListForAddress(storage, address);
            return list.All<Hash>();
        }
#endregion

#region TASKS
        private byte[] GetTaskKey(BigInteger taskID, string field)
        {
            var bytes = Encoding.ASCII.GetBytes(field);
            var key = ByteArrayUtils.ConcatBytes(bytes, taskID.ToUnsignedByteArray());
            return key;
        }

        public IChainTask StartTask(StorageContext storage, Address from, string contractName, ContractMethod method, uint frequency, uint delay, TaskFrequencyMode mode, BigInteger gasLimit)
        {
            if (!IsContractDeployed(storage, contractName))
            {
                return null;
            }

            var taskID = GenerateUID(storage);
            var task = new ChainTask(taskID, from, contractName, method.name, frequency, delay, mode, gasLimit, this.Height + 1, true);

            var taskKey = GetTaskKey(taskID, "task_info");

            var taskBytes = task.ToByteArray();

            storage.Put(taskKey, taskBytes);

            var taskList = new StorageList(TaskListTag, this.Storage);
            taskList.Add<BigInteger>(taskID);

            return task;
        }

        public bool StopTask(StorageContext storage, BigInteger taskID)
        {
            var taskKey = GetTaskKey(taskID, "task_info");

            if (this.Storage.Has(taskKey))
            {
                this.Storage.Delete(taskKey);

                taskKey = GetTaskKey(taskID, "task_run");
                if (this.Storage.Has(taskKey))
                {
                    this.Storage.Delete(taskKey);
                }

                var taskList = new StorageList(TaskListTag, this.Storage);
                taskList.Remove<BigInteger>(taskID);

                return true;
            }

            return false;
        }

        public IChainTask GetTask(StorageContext storage, BigInteger taskID)
        {
            var taskKey = GetTaskKey(taskID, "task_info");

            var taskBytes = this.Storage.Get(taskKey);

            var task = ChainTask.FromBytes(taskID, taskBytes);

            return task;

        }

        private IEnumerable<Transaction> ProcessPendingTasks(Block block, IOracleReader oracle, BigInteger minimumFee, StorageChangeSetContext changeSet)
        {
            var taskList = new StorageList(TaskListTag, changeSet);
            var taskCount = taskList.Count();

            List<Transaction> transactions = null;

            int i = 0;
            while (i < taskCount)
            {
                var taskID = taskList.Get<BigInteger>(i);
                var task = GetTask(changeSet, taskID);

                Transaction tx;

                var taskResult = ProcessPendingTask(block, oracle, minimumFee, changeSet, task, out tx);
                if (taskResult == TaskResult.Running)
                {
                    i++;
                }
                else
                {
                    taskList.RemoveAt(i);
                }

                if (tx != null)
                {
                    if (transactions == null)
                    {
                        transactions = new List<Transaction>();
                    }

                    transactions.Add(tx);
                }
            }

            if (transactions != null)
            {
                return transactions;
            }

            return Enumerable.Empty<Transaction>();
        }

        private BigInteger GetTaskTimeFromBlock(TaskFrequencyMode mode, Block block)
        {
            switch (mode)
            {
                case TaskFrequencyMode.Blocks:
                    {
                        return block.Height;
                    }

                case TaskFrequencyMode.Time:
                    {
                        return block.Timestamp.Value;
                    }

                default:
                    throw new ChainException("Unknown task mode: " + mode);
            }
        }

        private TaskResult ProcessPendingTask(Block block, IOracleReader oracle, BigInteger minimumFee,
                StorageChangeSetContext changeSet, IChainTask task, out Transaction transaction)
        {
            transaction = null;

            BigInteger currentRun = GetTaskTimeFromBlock(task.Mode, block);
            var taskKey = GetTaskKey(task.ID, "task_run");

            if (task.Mode != TaskFrequencyMode.Always)
            {
                bool isFirstRun = !changeSet.Has(taskKey);

                if (isFirstRun)
                {
                    var taskBlockHash = GetBlockHashAtHeight(task.Height);
                    var taskBlock = GetBlockByHash(taskBlockHash);

                    BigInteger firstRun = GetTaskTimeFromBlock(task.Mode, taskBlock) + task.Delay;

                    if (currentRun < firstRun)
                    {
                        return TaskResult.Skipped; // skip execution for now
                    }
                }
                else
                {
                    BigInteger lastRun = isFirstRun ? changeSet.Get<BigInteger>(taskKey) : 0;

                    var diff = currentRun - lastRun;
                    if (diff < task.Frequency)
                    {
                        return TaskResult.Skipped; // skip execution for now
                    }
                }
            }
            else
            {
                currentRun = 0;
            }
            
            var taskScript = new ScriptBuilder()
                .AllowGas(task.Owner, Address.Null, minimumFee, task.GasLimit)
                .CallContract(task.ContextName, task.Method)
                .SpendGas(task.Owner)
                .EndScript();

            transaction = new Transaction(this.Nexus.Name, this.Name, taskScript, block.Timestamp.Value + 1, "TASK");

            var txResult = ExecuteTransaction(-1, transaction, transaction.Script, block.Validator, block.Timestamp, changeSet,
                        block.Notify, oracle, task);
            if (txResult.Code == 0)
            {
                var resultBytes = Serialization.Serialize(txResult.Result);
                block.SetResultForHash(transaction.Hash, resultBytes);

                block.SetStateForHash(transaction.Hash, txResult.State);

                // update last_run value in storage
                if (currentRun > 0)
                {
                    changeSet.Put<BigInteger>(taskKey, currentRun);
                }

                var shouldStop = txResult.Result.AsBool();
                return shouldStop ? TaskResult.Halted : TaskResult.Running;
            }

            block.SetStateForHash(transaction.Hash, txResult.State);
            return TaskResult.Crashed;
            
        }
#endregion

#region block validation
        public void CloseBlock(Block block, StorageChangeSetContext storage)
        {
            var rootStorage = this.IsRoot ? storage : Nexus.RootStorage;

            if (block.Height > 1)
            {
                var prevBlock = GetBlockByHash(block.PreviousHash);

                if (prevBlock.Validator != block.Validator)
                {
                    block.Notify(new Event(EventKind.ValidatorSwitch, block.Validator, "block", Serialization.Serialize(prevBlock)));
                }
            }

            var tokenStorage = this.Name == DomainSettings.RootChainName ? storage : Nexus.RootStorage;
            var token = this.Nexus.GetTokenInfo(tokenStorage, DomainSettings.FuelTokenSymbol);
            var balance = new BalanceSheet(token);
            var blockAddress = Address.FromHash("block");
            var totalAvailable = balance.Get(storage, blockAddress);

            var targets = new List<Address>();

            if (Nexus.HasGenesis())
            {
                var validators = Nexus.GetValidators(block.Timestamp);

                var totalValidators = Nexus.GetPrimaryValidatorCount(block.Timestamp);

                for (int i = 0; i < totalValidators; i++)
                {
                    var validator = validators[i];
                    if (validator.type != ValidatorType.Primary)
                    {
                        continue;
                    }

                    targets.Add(validator.address);
                }
            }

            if (targets.Count > 0)
            {
                if (!balance.Subtract(storage, blockAddress, totalAvailable))
                {
                    throw new BlockGenerationException("could not subtract balance from block address");
                }

                var amountPerValidator = totalAvailable / targets.Count;
                var leftOvers = totalAvailable - (amountPerValidator * targets.Count);

                foreach (var address in targets)
                {
                    BigInteger amount = amountPerValidator;

                    if (address == block.Validator)
                    {
                        amount += leftOvers;
                    }

                    // TODO this should use triggers when available...
                    if (!balance.Add(storage, address, amount))
                    {
                        throw new BlockGenerationException($"could not add balance to {address}");
                    }

                    var eventData = Serialization.Serialize(new TokenEventData(DomainSettings.FuelTokenSymbol, amount, this.Name));
                    block.Notify(new Event(EventKind.TokenClaim, address, "block", eventData));
                }
            }
        }
#endregion

        public Address LookUpName(StorageContext storage, string name, Timestamp timestamp)
        {
            if (IsContractDeployed(storage, name))
            {
                return SmartContract.GetAddressFromContractName(name);
            }

            return this.Nexus.LookUpName(storage, name, timestamp);
        }

        public string GetNameFromAddress(StorageContext storage, Address address, Timestamp timestamp)
        {
            if (address.IsNull)
            {
                return ValidationUtils.NULL_NAME;
            }

            if (address.IsSystem)
            {
                if (address == DomainSettings.InfusionAddress)
                {
                    return DomainSettings.InfusionName;
                }

                var contract = this.GetContractByAddress(storage, address);
                if (contract != null)
                {
                    return contract.Name;
                }
                else
                {
                    var tempChain = Nexus.GetChainByAddress(address);
                    if (tempChain != null)
                    {
                        return tempChain.Name;
                    }

                    var org = Nexus.GetOrganizationByAddress(storage, address);
                    if (org != null)
                    {
                        return org.ID;
                    }

                    return ValidationUtils.ANONYMOUS_NAME;
                }
            }

            return Nexus.RootChain.InvokeContractAtTimestamp(storage, timestamp, NativeContractKind.Account, nameof(AccountContract.LookUpAddress), address).AsString();
        }

    }
}
