﻿using System;
using System.Numerics;
using Phantasma.Core.Cryptography;
using Phantasma.Core.Cryptography.Structs;
using Phantasma.Core.Domain;
using Phantasma.Core.Domain.Contract;
using Phantasma.Core.Domain.Contract.Enums;
using Phantasma.Core.Domain.Contract.Validator;
using Phantasma.Core.Domain.Contract.Validator.Enums;
using Phantasma.Core.Domain.Contract.Validator.Structs;
using Phantasma.Core.Domain.Events;
using Phantasma.Core.Domain.Events.Structs;
using Phantasma.Core.Domain.Validation;
using Phantasma.Core.Storage.Context;
using Phantasma.Core.Storage.Context.Structs;
using Phantasma.Core.Types;
using Phantasma.Core.Types.Structs;

namespace Phantasma.Business.Blockchain.Contracts.Native
{
    public sealed class ValidatorContract : NativeContract
    {
        public override NativeContractKind Kind => NativeContractKind.Validator;

        public const string ValidatorSlotsTag = "validator.slots";

        public const string ValidatorRotationTimeTag = "validator.rotation.time";
        public static readonly BigInteger ValidatorRotationTimeDefault = 120;

        public const string ValidatorPollTag = "elections";
        public const string ValidatorMaxOfflineTimeTag = "validator.max.offline.time";
        public static BigInteger ValidatorMaxOfflineTimeDefault = 86400; // 24 hours = 86400

#pragma warning disable 0649
        private StorageList _validators; // <ValidatorInfo>
        private StorageMap _validatorsActivity; // <Address, Timestamp>
#pragma warning restore 0649

        private int _initialValidatorCount => DomainSettings.InitialValidatorCount;

        public ValidatorContract() : base()
        {
        }

        /// <summary>
        /// Returns all of the Validators.
        /// </summary>
        /// <returns></returns>
        public ValidatorEntry[] GetValidators()
        {
            return _validators.All<ValidatorEntry>();
        }

        /// <summary>
        /// Returns the current validator set
        /// </summary>
        /// <param name="tendermintAddress"></param>
        /// <returns></returns>
        public ValidatorEntry GetCurrentValidator(string tendermintAddress)
        {
            var validators = GetValidators();

            foreach (var validator in validators)
            {
                if (validator.address.TendermintAddress == tendermintAddress)
                {
                    return validator;
                }
            }

            return new ValidatorEntry()
            {
                address = Address.Null,
                type = ValidatorType.Invalid,
                election = new Timestamp(0)
            };
        }

        /// <summary>
        /// Returns the type of a given validator
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public ValidatorType GetValidatorType(Address address)
        {
            var validators = GetValidators();

            foreach (var validator in validators)
            {
                if (validator.address == address)
                {
                    return validator.type;
                }
            }

            return ValidatorType.Invalid;
        }

        /// <summary>
        /// Returns the index of a given validator
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public BigInteger GetIndexOfValidator(Address address)
        {
            if (!address.IsUser)
            {
                return -1;
            }

            var validators = GetValidators();

            int index = 0;
            foreach (var validator in validators)
            {
                if (validator.address == address)
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        /// <summary>
        /// Returns the maximum number of validators
        /// </summary>
        /// <returns></returns>
        public int GetMaxTotalValidators()
        {
            if (Runtime.HasGenesis)
            {
                return (int)Runtime.GetGovernanceValue(ValidatorSlotsTag);
            }

            return _initialValidatorCount;
        }

        /// <summary>
        /// Returns the validator at a given index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ValidatorEntry GetValidatorByIndex(BigInteger index)
        {
            Runtime.Expect(index >= 0, "invalid validator index");

            var totalValidators = GetMaxTotalValidators();
            Runtime.Expect(index < totalValidators, $"invalid validator index {index} {totalValidators}");

            if (index < _validators.Count())
            {
                return _validators.Get<ValidatorEntry>(index);
            }

            return new ValidatorEntry()
            {
                address = Address.Null,
                type = ValidatorType.Invalid,
                election = new Timestamp(0)
            };
        }

        /// <summary>
        /// Returns the number of validators of a given type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public BigInteger GetValidatorCount(ValidatorType type)
        {
            if (type == ValidatorType.Invalid)
            {
                return 0;
            }

            var count = 0;
            var max = GetMaxPrimaryValidators();

            if (Runtime.ProtocolVersion >= 10)
            {
                max = GetMaxTotalValidators();
            }

            for (int i = 0; i < max; i++)
            {
                var validator = GetValidatorByIndex(i);
                if (validator.type == type)
                {
                    count++;
                }
            }

            return count;

        }

        /// <summary>
        /// Returns the maximum number of primary validators
        /// </summary>
        /// <returns></returns>
        public BigInteger GetMaxPrimaryValidators()
        {
            if (Runtime.HasGenesis)
            {
                var maxValidators = Runtime.GetGovernanceValue(ValidatorSlotsTag);

                if (Runtime.ProtocolVersion <= DomainSettings.Phantasma30Protocol)
                {
                    // This timestamp was 2023-02-07 16:30:00 UTC (1675787400 unix timestamp)
                    // This was needed because it was not using the correct governance value and with this it will be fixed.
                    if (Runtime.Time >= 1675787400)
                    {
                        return maxValidators;
                    }
                    else
                    {
                        var result = maxValidators * 10 / 25;

                        if (maxValidators > 0 && result < 1)
                        {
                            result = 1;
                        }

                        return result;
                    }
                }
                else if (Runtime.ProtocolVersion == 9)
                {
                    // This timestamp was 2023-02-07 16:30:00 UTC (1675787400 unix timestamp)
                    // this was just to make sure that we could still use the old protocol version
                    return maxValidators;
                }
                else
                {
                    var result = maxValidators * 10 / 25;

                    if (maxValidators > 0 && result < 1)
                    {
                        result = 1;
                    }

                    return result;
                }
            }

            return _initialValidatorCount;
        }

        /// <summary>
        /// Returns the maximum number of secondary validators
        /// </summary>
        /// <returns></returns>
        public BigInteger GetMaxSecondaryValidators()
        {
            if (Runtime.HasGenesis)
            {
                var maxValidators = Runtime.GetGovernanceValue(ValidatorSlotsTag);
                return maxValidators - GetMaxPrimaryValidators();
            }

            return 0;
        }

        // NOTE - witness not required, as anyone should be able to call this, permission is granted based on consensus
        public void SetValidator(Address target, BigInteger index, ValidatorType type)
        {
            Runtime.Expect(target.IsUser, "must be user address");
            Runtime.Expect(type == ValidatorType.Primary || type == ValidatorType.Secondary, "invalid validator type");

            Runtime.Expect(!Nexus.IsDangerousAddress(target), "this address can't be used as source");

            var currentType = GetValidatorType(target);
            Runtime.Expect(currentType != type, $"Already a {type} validator");

            var primaryValidators = GetValidatorCount(ValidatorType.Primary);
            var secondaryValidators = GetValidatorCount(ValidatorType.Secondary);

            Runtime.Expect(index >= 0, "invalid index");

            var totalValidators = GetMaxTotalValidators();
            Runtime.Expect(index <= totalValidators, "invalid index " + totalValidators);

            var expectedType = index <= GetMaxPrimaryValidators() ? ValidatorType.Primary : ValidatorType.Secondary;
            Runtime.Expect(type == expectedType, "unexpected validator type");

            if (primaryValidators > _initialValidatorCount) // for initial validators stake is not verified because it doesn't exist yet.
            {
                var requiredStake = Runtime.CallNativeContext(NativeContractKind.Stake, nameof(StakeContract.GetMasterThreshold), target).AsNumber();
                var stakedAmount = Runtime.GetStake(target);

                Runtime.Expect(stakedAmount >= requiredStake, "not enough stake");
            }

            if (index > 0)
            {
                var previous = _validators.Get<ValidatorEntry>(index - 1);
                Runtime.Expect(previous.type != ValidatorType.Invalid, "previous validator has unexpected status");
            }

            // check if we're expanding the validator set
            if (primaryValidators > _initialValidatorCount)
            {
                var isValidatorProposed = _validators.Get<ValidatorEntry>(index).type == ValidatorType.Proposed;

                if (isValidatorProposed)
                {
                    var currentEntry = _validators.Get<ValidatorEntry>(index);
                    if (currentEntry.type != ValidatorType.Proposed)
                    {
                        Runtime.Expect(currentEntry.type == ValidatorType.Invalid, "invalid validator state");
                        isValidatorProposed = false;
                    }
                }

                var firstValidator = GetValidatorByIndex(0).address;
                if (isValidatorProposed)
                {
                    if (primaryValidators > _initialValidatorCount)
                    {
                        Runtime.Expect(Runtime.IsWitness(target), "invalid witness");
                    }
                    else
                    {
                        Runtime.Expect(Runtime.IsWitness(firstValidator), "invalid witness");
                    }
                }
                else
                {
                    if (primaryValidators > _initialValidatorCount)
                    {
                        var pollName = ConsensusContract.SystemPoll + ValidatorPollTag;
                        var obtainedRank = Runtime.CallNativeContext(NativeContractKind.Consensus, "GetRank", pollName, target).AsNumber();
                        Runtime.Expect(obtainedRank >= 0, "no consensus for electing this address");
                        Runtime.Expect(obtainedRank == index, "this address was elected at a different index");
                    }
                    else
                    {
                        Runtime.Expect(Runtime.IsWitness(firstValidator), "invalid validator witness");
                    }

                    type = ValidatorType.Proposed;
                }
            }
            else
            if (primaryValidators > 0)
            {
                var passedWitnessCheck = false;

                var validators = GetValidators();
                foreach (var validator in validators)
                {
                    if (validator.type == ValidatorType.Primary && Runtime.IsWitness(validator.address))
                    {
                        passedWitnessCheck = true;
                        break;
                    }
                }

                Runtime.Expect(passedWitnessCheck, "invalid validator witness");
            }

            var currentSize = _validators.Count();
            Runtime.Expect(currentSize == index, "validator list has unexpected size");

            var entry = new ValidatorEntry()
            {
                address = target,
                election = Runtime.Time,
                type = type,
            };
            _validators.Add(entry);
            _validatorsActivity.Set<Address, Timestamp>(target, Runtime.Time);

            if (type == ValidatorType.Primary)
            {
                var newValidators = GetValidatorCount(ValidatorType.Primary);
                Runtime.Expect(newValidators > primaryValidators, "number of primary validators did not change");
            }
            else if (type == ValidatorType.Secondary)
            {
                var newValidators = GetValidatorCount(ValidatorType.Secondary);
                Runtime.Expect(newValidators > secondaryValidators, "number of secondary validators did not change");
            }

            if (type != ValidatorType.Proposed)
            {
                Runtime.AddMember(DomainSettings.ValidatorsOrganizationName, Address, target);
            }

            Runtime.Notify(type == ValidatorType.Proposed ? EventKind.ValidatorPropose : EventKind.ValidatorElect, Runtime.Chain.Address, target);
        }

        /// <summary>
        /// Demote a validator to a proposed status.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="target"></param>
        public void DemoteValidator(Address from, Address target)
        {
            Runtime.Expect(Runtime.IsWitness(from), "invalid witness");
            Runtime.Expect(target.IsUser, "must be user address");
            Runtime.Expect(Runtime.IsKnownValidator(from), "not a validator");
            Runtime.Expect(Runtime.IsKnownValidator(target), "not a validator");

            var count = _validators.Count();
            Runtime.Expect(count > 3, "cant remove last validator");

            var index = this.GetIndexOfValidator(target);
            var entry = this.GetValidatorByIndex(index);

            bool brokenRules = false;

            var validatorLastActivity = 1;
            var diff = Runtime.Time - Runtime.Chain.Nexus.GetValidatorLastActivity(target, Runtime.Time);
            var governanceValueOfflineTime = Runtime.GetGovernanceValue(ValidatorMaxOfflineTimeTag);
            var maxPeriod = governanceValueOfflineTime != 0 ? governanceValueOfflineTime : ValidatorMaxOfflineTimeDefault; // 24 hours
            if (diff > maxPeriod)
            {
                brokenRules = true;
            }

            var requiredStake = StakeContract.DefaultMasterThreshold;
            
            var stakedAmount = (BigInteger)Runtime.CallNativeContext(NativeContractKind.Stake, nameof(StakeContract.GetStake), target).AsNumber();

            if (stakedAmount < requiredStake)
            {
                brokenRules = true;
            }

            Runtime.Expect(brokenRules, "no rules broken");

            entry.type = ValidatorType.Invalid;
            _validators.Replace(index, entry);

            Runtime.Notify(EventKind.ValidatorRemove, Runtime.Chain.Address, target);
            Runtime.RemoveMember(DomainSettings.ValidatorsOrganizationName, this.Address, target);
        }

        /// <summary>
        /// Register validator activity
        /// </summary>
        /// <param name="from"></param>
        /// <param name="validatorAddress"></param>
        public void RegisterValidatorActivity(Address from, Address validatorAddress)
        {
            Runtime.Expect(Runtime.IsWitness(from), "invalid witness");
            Runtime.Expect(Runtime.IsKnownValidator(from), "not a validator");
            Runtime.Expect(Runtime.IsKnownValidator(validatorAddress), "not a validator");
            Runtime.Expect(from != validatorAddress, "Cannot register activity for yourself");
            
            var lastActivity = GetValidatorLastActivity(validatorAddress);
            var governanceValueOfflineTime = Runtime.GetGovernanceValue(ValidatorMaxOfflineTimeTag);
            uint maxPeriod = uint.Parse(governanceValueOfflineTime != 0 ? governanceValueOfflineTime.ToString() : ValidatorMaxOfflineTimeDefault.ToString()); // 2 hours
            
            if (lastActivity != Timestamp.Null && lastActivity.Value + maxPeriod <= Runtime.Time)
            {
                Runtime.Expect(false, "validator is offline");
            }
            
            _validatorsActivity.Set(validatorAddress, Runtime.Time);
        }

        /// <summary>
        /// Updates the last activity of a validator
        /// </summary>
        /// <param name="from"></param>
        public void UpdateValidatorActivity(Address from)
        {
            Runtime.Expect(Runtime.IsWitness(from), "invalid witness");
            Runtime.Expect(Runtime.IsKnownValidator(from), "not a validator");
            var lastActivity = GetValidatorLastActivity(from);
            var governanceValueOfflineTime = Runtime.GetGovernanceValue(ValidatorMaxOfflineTimeTag);
            uint maxPeriod = uint.Parse(governanceValueOfflineTime != 0 ? governanceValueOfflineTime.ToString() : ValidatorMaxOfflineTimeDefault.ToString()); // 2 hours
            
            if (lastActivity != Timestamp.Null && lastActivity.Value + maxPeriod <= Runtime.Time)
            {
                Runtime.Expect(false, "validator is offline");
            }
            
            _validatorsActivity.Set(from, Runtime.Time);
        }

        /// <summary>
        /// Returns the last activity of a validator
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public Timestamp GetValidatorLastActivity(Address target)
        {
            if ( !Runtime.IsKnownValidator(target) )
                return Timestamp.Null;

            if (!_validatorsActivity.ContainsKey(target))
                return Timestamp.Null;
              
            return _validatorsActivity.Get<Address, Timestamp>(target);
        }

        /// <summary>
        /// Migrate a validator to a new address
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void Migrate(Address from, Address to)
        {
            Runtime.Expect(Runtime.PreviousContext.Name == "account", "invalid context");

            Runtime.Expect(Runtime.IsWitness(from), "witness failed");

            Runtime.Expect(to.IsUser, "destination must be user address");

            var index = GetIndexOfValidator(from);
            Runtime.Expect(index >= 0, "validator index not found");

            var entry = _validators.Get<ValidatorEntry>(index);
            Runtime.Expect(entry.type == ValidatorType.Primary || entry.type == ValidatorType.Secondary, "not active validator");

            entry.address = to;
            _validators.Replace(index, entry);

            Runtime.MigrateMember(DomainSettings.ValidatorsOrganizationName, Address, from, to);

            Runtime.Notify(EventKind.ValidatorRemove, Runtime.Chain.Address, from);
            Runtime.Notify(EventKind.ValidatorElect, Runtime.Chain.Address, to);
            Runtime.Notify(EventKind.AddressMigration, to, from);
        }
    }
}
