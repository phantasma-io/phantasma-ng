﻿using System;
using System.Collections.Generic;
using Phantasma.Business.VM;
using Phantasma.Core.Cryptography;
using Phantasma.Core.Cryptography.Structs;
using Phantasma.Core.Domain;
using Phantasma.Core.Domain.Execution;
using Phantasma.Core.Domain.Execution.Enums;
using Phantasma.Core.Domain.Interfaces;
using Phantasma.Core.Domain.Serializer;
using Phantasma.Core.Domain.VM;
using Phantasma.Core.Domain.VM.Enums;
using Phantasma.Core.Numerics;

namespace Phantasma.Business.Blockchain.VM
{
    public abstract class DescriptionVM : VirtualMachine
    {
        public DescriptionVM(byte[] script, uint offset) : base(script, offset, null)
        {
            RegisterMethod("ABI()", ExtCalls.Constructor_ABI);
            RegisterMethod("Address()", ExtCalls.Constructor_Address);
            RegisterMethod("Hash()", ExtCalls.Constructor_Hash);
            RegisterMethod("Timestamp()", ExtCalls.Constructor_Timestamp);
        }

        private Dictionary<string, Func<VirtualMachine, ExecutionState>> handlers = new Dictionary<string, Func<VirtualMachine, ExecutionState>>();

        internal void RegisterMethod(string name, Func<VirtualMachine, ExecutionState> handler)
        {
            handlers[name] = handler;
        }

        public abstract IToken FetchToken(string symbol);
        public abstract string OutputAddress(Address address);
        public abstract string OutputSymbol(string symbol);

        public override void DumpData(List<string> lines)
        {
            // do nothing
        }

        private static readonly string FormatInteropTag = "Format.";

        public override ExecutionState ExecuteInterop(string method)
        {
            if (method.StartsWith(FormatInteropTag))
            {
                method = method.Substring(FormatInteropTag.Length);
                switch (method)
                {
                    case "Decimals":
                        {
                            var amount = this.PopNumber("amount");
                            var symbol = this.PopString("symbol");

                            var info = FetchToken(symbol);

                            var result = UnitConversion.ToDecimal(amount, info.Decimals);

                            Stack.Push(VMObject.FromObject(result.ToString()));
                            return ExecutionState.Running;
                        }

                    case "Account":
                        {
                            var temp = Stack.Pop();
                            Address addr;
                            if (temp.Type == VMType.String)
                            {
                                var text = temp.AsString();
                                Expect(Address.IsValidAddress(text), $"expected valid address");
                                addr = Address.FromText(text);
                            }
                            else if (temp.Type == VMType.Bytes)
                            {
                                var bytes = temp.AsByteArray();
                                addr = Serialization.Unserialize<Address>(bytes);
                            }
                            else
                            {
                                addr = temp.AsInterop<Address>();
                            }

                            var result = OutputAddress(addr);
                            Stack.Push(VMObject.FromObject(result.ToString()));
                            return ExecutionState.Running;
                        }

                    case "Symbol":
                        {
                            var symbol = this.PopString("symbol");
                            var result = OutputSymbol(symbol);
                            Stack.Push(VMObject.FromObject(result.ToString()));
                            return ExecutionState.Running;
                        }

                    default:
                        throw new VMException(this, $"unknown interop: {FormatInteropTag}{method}");

                }
            }

            if (handlers.ContainsKey(method))
            {
                var interop = handlers[method];
                return interop(this);
            }

            throw new VMException(this, "unknown interop: " + method);
        }

        public override ExecutionContext LoadContext(string contextName)
        {
            throw new NotImplementedException();
        }
    }

}
