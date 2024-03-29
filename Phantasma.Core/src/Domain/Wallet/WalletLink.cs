using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Phantasma.Core.Cryptography;
using Phantasma.Core.Cryptography.Enums;
using Phantasma.Core.Cryptography.Structs;
using Phantasma.Core.Numerics;

namespace Phantasma.Core.Domain.Wallet
{
    public abstract class WalletLink
    {
        public const int WebSocketPort = 7090;
        public const int LinkProtocol = 2;

        public class ErrorDTO
        {
            public string message { get; set; }
        }

        public class AuthorizationDTO
        {
            public string wallet { get; set; }
            public string nexus { get; set; }
            public string dapp { get; set; }
            public string token { get; set; }
            public int version { get; set; }
        }

        public class BalanceDTO
        {
            public string symbol { get; set; }
            public string value { get; set; }
            public int decimals { get; set; }
        }

        public class FileDTO
        {
            public string name { get; set; }
            public int size { get; set; }
            public uint date { get; set; }
            public string hash { get; set; }
        }

        public class AccountDTO
        {
            public string alias { get; set; }
            public string address { get; set; }
            public string name { get; set; }
            public string avatar { get; set; }
            public string platform { get; set; }
            public string external { get; set; }
            public BalanceDTO[] balances { get; set; }
            public FileDTO[] files { get; set; }
        }

        public class InvocationDTO
        {
            public string result { get; set; }
        }

        public class TransactionDTO
        {
            public string hash { get; set; }
        }

        public class SignatureDTO
        {
            public string signature { get; set; }
            public string random { get; set; }
        }

        public class ConnectionDTO
        {
            public string Token { get; }
            public int Version { get; }

            public ConnectionDTO(string token, int version)
            {
                this.Token = token;
                this.Version = version;
            }
        }

        private RandomNumberGenerator rnd = RandomNumberGenerator.Create();

        private Dictionary<string, ConnectionDTO> _connections = new Dictionary<string, ConnectionDTO>();

        protected abstract WalletStatus Status { get; }

        public abstract string Nexus { get; }

        public abstract string Name { get; }

        private bool _isPendingRequest;

        public WalletLink()
        {
        }

        private ConnectionDTO ValidateRequest(string[] args)
        {
            if (args.Length >= 3)
            {
                string dapp = args[args.Length - 2];
                string token = args[args.Length - 1];

                if (_connections.ContainsKey(dapp))
                {
                    var connection = _connections[dapp];
                    if (connection.Token == token)
                    {
                        return connection;
                    }
                }
            }

            return null;
        }

        protected abstract void Authorize(string dapp, string token, int version, Action<bool, string> callback);

        protected abstract void GetAccount(string platform, Action<AccountDTO, string> callback);

        protected abstract void InvokeScript(string chain, byte[] script, int id, Action<byte[], string> callback);

        // NOTE for security, signData should not be usable as a way of signing transaction. That way the wallet is responsible for appending random bytes to the message, and return those in callback
        protected abstract void SignData(string platform, SignatureKind kind, byte[] data, int id, Action<string, string, string> callback);

        protected abstract void SignTransaction(string platform, SignatureKind kind, string chain, byte[] script, byte[] payload, int id, Action<Hash, string> callback);

        protected abstract void WriteArchive(Hash hash, int blockIndex, byte[] data, Action<bool, string> callback);

        /*private bool HandleAuthorize(int id, string[] args, Connection connection, bool success, JsonNode answer, Action<int, JsonNode, bool> callback)
        {
            if (args.Length == 1 || args.Length == 2)
            {
                string token;
                var dapp = args[0];

                int version;
                
                if (args.Length == 2)
                {
                    var str = args[1];
                    if (!int.TryParse(str, out version))
                    {
                        answer = APIUtils.FromAPIResult(new Error() { message = $"authorize: Invalid version: {str}"});
                        callback(id, answer, false);
                        _isPendingRequest = false;
                        return;
                    }
                }
                else 
                { 
                    version = 1; 
                }

                if (_connections.ContainsKey(dapp))
                {
                    connection = _connections[dapp];
                    success = true;
                    answer = APIUtils.FromAPIResult(new Authorization() { wallet = this.Name, nexus = this.Nexus, dapp = dapp, token = connection.Token, version = connection.Version });
                }
                else
                {
                    var bytes = new byte[32];
                    rnd.NextBytes(bytes);
                    token = Base16.Encode(bytes);

                    this.Authorize(dapp, token, version, (authorized, error) =>
                    {
                        if (authorized)
                        {
                            _connections[dapp] = new Connection(token, version);

                            success = true;
                            answer = APIUtils.FromAPIResult(new Authorization() { wallet = this.Name, nexus = this.Nexus, dapp = dapp, token = token });
                        }
                        else
                        {
                            answer = APIUtils.FromAPIResult(new Error() { message = error});
                        }

                        callback(id, answer, success);
                        _isPendingRequest = false;
                    });

                    return;
                }

            }
            else
            {
                answer = APIUtils.FromAPIResult(new Error() { message = $"authorize: Invalid amount of arguments: {args.Length}" });
            }
        }*/

        private void HandleGetAccount()
        {
            /*int expectedLength;

            switch (connection.Version)
            {
                case 1:
                    expectedLength = 0;
                    break;

                default:
                    expectedLength = 1;
                    break;
            }

            if (args.Length == expectedLength)
            {
                string platform;

                if (connection.Version >= 2)
                {
                    platform = args[0].ToLower();
                }
                else
                {
                    platform = "phantasma";
                }

                GetAccount(platform, (account, error) => {
                    if (error == null)
                    {
                        success = true;
                        answer = APIUtils.FromAPIResult(account);
                    }
                    else
                    {
                        answer = APIUtils.FromAPIResult(new Error() { message = error });
                    }

                    callback(id, answer, success);
                    _isPendingRequest = false;
                });

                return;
            }
            else
            {
                answer = APIUtils.FromAPIResult(new Error() { message = $"getAccount: Invalid amount of arguments: {args.Length}" });
            }*/

        }

        private void HandleSignData()
        {
            /*
             * int expectedLength;

                        switch (connection.Version)
                        {
                            case 1:
                                expectedLength = 2;
                                break;

                            default:
                                expectedLength = 3;
                                break;
                        }

                        if (args.Length == expectedLength)
                        {
                            var data = Base16.Decode(args[0], false);
                            if (data == null)
                            {
                                answer = APIUtils.FromAPIResult(new Error() { message = $"signTx: Invalid input received" });
                            }
                            else
                            {
                                SignatureKind signatureKind;

                                if (!Enum.TryParse<SignatureKind>(args[1], out signatureKind))
                                {
                                    answer = APIUtils.FromAPIResult(new Error() { message = $"signData: Invalid signature: " + args[1] });
                                    callback(id, answer, false);
                                    _isPendingRequest = false;
                                    return;
                                }

                                var platform = connection.Version >= 2 ? args[2].ToLower() : "phantasma";

                                SignData(platform, signatureKind, data, id, (signature, random, txError) => {
                                    if (signature != null)
                                    {
                                        success = true;
                                        answer = APIUtils.FromAPIResult(new Signature() { signature = signature, random = random });
                                    }
                                    else
                                    {
                                        answer = APIUtils.FromAPIResult(new Error() { message = txError });
                                    }

                                    callback(id, answer, success);
                                    _isPendingRequest = false;
                                });
                            }

             */
        }
        
        private void HandleSignTransaction()
        {
            /*
             * int expectedLength;

                        switch (connection.Version)
                        {
                            case 1:
                                expectedLength = 4;
                                break;

                            default:
                                expectedLength = 5;
                                break;
                        }

                        if (args.Length == expectedLength)
                        {
                            int index = 0;

                            if (connection.Version == 1)
                            {
                                var txNexus = args[index]; index++;
                                if (txNexus != this.Nexus)
                                {
                                    answer = APIUtils.FromAPIResult(new Error() { message = $"signTx: Expected nexus {this.Nexus}, instead got {txNexus}" });
                                    callback(id, answer, false);
                                    _isPendingRequest = false;
                                    return;
                                }
                            }

                            var chain = args[index]; index++;
                            var script = Base16.Decode(args[index], false); index++;

                            if (script == null)
                            {
                                answer = APIUtils.FromAPIResult(new Error() { message = $"signTx: Invalid script data" });
                            }
                            else
                            {
                                byte[] payload = args[index].Length > 0 ? Base16.Decode(args[index], false) : null;
                                index++;

                                string platform;
                                SignatureKind signatureKind;

                                if (connection.Version >= 2) {
                                    if (!Enum.TryParse<SignatureKind>(args[index], out signatureKind))
                                    {
                                        answer = APIUtils.FromAPIResult(new Error() { message = $"signTx: Invalid signature: " + args[index] });
                                        callback(id, answer, false);
                                        _isPendingRequest = false;
                                        return;
                                    }
                                    index++;

                                    platform = args[index].ToLower();
                                    index++;
                                }
                                else {
                                    platform = "phantasma";
                                    signatureKind = SignatureKind.Ed25519;
                                }

                                SignTransaction(platform, signatureKind, chain, script, payload, id, (hash, txError) => {
                                    if (hash != Hash.Null)
                                    {
                                        success = true;
                                        answer = APIUtils.FromAPIResult(new Transaction() { hash = hash.ToString() });
                                    }
                                    else
                                    {
                                        answer = APIUtils.FromAPIResult(new Error() { message = txError });
                                    }

                                    callback(id, answer, success);
                                    _isPendingRequest = false;
                                });
                            }

                            return;
                        }
                        else
                        {
                            answer = APIUtils.FromAPIResult(new Error() { message = $"signTx: Invalid amount of arguments: {args.Length}" });
                        }
             */
        }
        
        private void HandleInvokeScript()
        {
            /*
             * int expectedLength;

                        switch (connection.Version)
                        {
                            case 1:
                                expectedLength = 4;
                                break;

                            default:
                                expectedLength = 5;
                                break;
                        }

                        if (args.Length == expectedLength)
                        {
                            int index = 0;

                            if (connection.Version == 1)
                            {
                                var txNexus = args[index]; index++;
                                if (txNexus != this.Nexus)
                                {
                                    answer = APIUtils.FromAPIResult(new Error() { message = $"signTx: Expected nexus {this.Nexus}, instead got {txNexus}" });
                                    callback(id, answer, false);
                                    _isPendingRequest = false;
                                    return;
                                }
                            }

                            var chain = args[index]; index++;
                            var script = Base16.Decode(args[index], false); index++;

                            if (script == null)
                            {
                                answer = APIUtils.FromAPIResult(new Error() { message = $"signTx: Invalid script data" });
                            }
                            else
                            {
                                byte[] payload = args[index].Length > 0 ? Base16.Decode(args[index], false) : null;
                                index++;

                                string platform;
                                SignatureKind signatureKind;

                                if (connection.Version >= 2) {
                                    if (!Enum.TryParse<SignatureKind>(args[index], out signatureKind))
                                    {
                                        answer = APIUtils.FromAPIResult(new Error() { message = $"signTx: Invalid signature: " + args[index] });
                                        callback(id, answer, false);
                                        _isPendingRequest = false;
                                        return;
                                    }
                                    index++;

                                    platform = args[index].ToLower();
                                    index++;
                                }
                                else {
                                    platform = "phantasma";
                                    signatureKind = SignatureKind.Ed25519;
                                }

                                SignTransaction(platform, signatureKind, chain, script, payload, id, (hash, txError) => {
                                    if (hash != Hash.Null)
                                    {
                                        success = true;
                                        answer = APIUtils.FromAPIResult(new Transaction() { hash = hash.ToString() });
                                    }
                                    else
                                    {
                                        answer = APIUtils.FromAPIResult(new Error() { message = txError });
                                    }

                                    callback(id, answer, success);
                                    _isPendingRequest = false;
                                });
                            }

                            return;
                        }
                        else
                        {
                            answer = APIUtils.FromAPIResult(new Error() { message = $"signTx: Invalid amount of arguments: {args.Length}" });
                        }
                        break;
                    }

                case "invokeScript":
                    {
                        if (args.Length == 2)
                        {
                            var chain = args[0];
                            var script = Base16.Decode(args[1], false);

                            if (script == null)
                            {
                                answer = APIUtils.FromAPIResult(new Error() { message = $"signTx: Invalid script data" });
                            }
                            else
                            {
                                InvokeScript(chain, script, id, (invokeResult, invokeError) =>
                                {
                                    if (invokeResult != null)
                                    {
                                        success = true;
                                        answer = APIUtils.FromAPIResult(new Invocation() { result = Base16.Encode(invokeResult) });
                                    }
                                    else
                                    {
                                        answer = APIUtils.FromAPIResult(new Error() { message = invokeError });
                                    }

                                    callback(id, answer, success);
                                    _isPendingRequest = false;
                                });
                                return;
                            }
                        }
                        else
                        {
                            answer = APIUtils.FromAPIResult(new Error() { message = $"invokeScript: Invalid amount of arguments: {args.Length}"});
                        }
             */
        }
        
        private void HandleWriteArchive()
        {
            /*if (args.Length == 3)
            {
                var archiveHash = Hash.Parse(args[0]);
                var blockIndex = int.Parse(args[1]);
                var bytes = Base16.Decode(args[2], false);

                if (bytes == null)
                {
                    answer = APIUtils.FromAPIResult(new Error() { message = $"invokeScript: Invalid archive data"});
                }
                else
                {
                    WriteArchive(archiveHash, blockIndex, bytes, (result, error) =>
                    {
                        if (result)
                        {
                            success = true;
                            answer = APIUtils.FromAPIResult(new Transaction() { hash = archiveHash.ToString() });
                        }
                        else
                        {
                            answer = APIUtils.FromAPIResult(new Error() { message = error });
                        }

                        callback(id, answer, success);
                        _isPendingRequest = false;
                    });
                }

                return;
            }
            else
            {
                answer = APIUtils.FromAPIResult(new Error() { message = $"writeArchive: Invalid amount of arguments: {args.Length}" });
            }*/
        }

        public void Execute(string cmd, Action<int, JsonNode, bool> callback)
        {
            var args = cmd.Split(',');

            JsonNode answer;

            int id = 0;

            if (!int.TryParse(args[0], out id))
            {
                answer = APIUtils.FromAPIResult(new ErrorDTO() { message = "Invalid request id" });
                callback(id, answer, false);
                return;
            }

            if (args.Length != 2)
            {
                answer = APIUtils.FromAPIResult(new ErrorDTO() { message = "Malformed request" });
                callback(id, answer, false);
                return;
            }

            cmd = args[1];
            args = cmd.Split('/');

            bool success = false;

            var requestType = args[0];

            if (requestType != "authorize")
            {
                var status = this.Status;
                if (status != WalletStatus.Ready)
                {
                    answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"Wallet is {status}" });
                    callback(id, answer, false);
                    return;
                }
            }

            if (_isPendingRequest)
            {
                answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"A previous request is still pending" });
                callback(id, answer, false);
                return;
            }

            _isPendingRequest = true;

            ConnectionDTO connectionDto = null;

            if (requestType != "authorize")
            {
                connectionDto = ValidateRequest(args);
                if (connectionDto == null)
                {
                    answer = APIUtils.FromAPIResult(new ErrorDTO() { message = "Invalid or missing API token" });
                    callback(id, answer, false);
                    _isPendingRequest = false;
                    return;
                }

                // exclude dapp/token args
                args = args.Take(args.Length - 2).ToArray();
            }

            args = args.Skip(1).ToArray();

            switch (requestType)
            {
                case "authorize":
                    {
                        if (args.Length == 1 || args.Length == 2)
                        {
                            string token;
                            var dapp = args[0];

                            int version;
                            
                            if (args.Length == 2)
                            {
                                var str = args[1];
                                if (!int.TryParse(str, out version))
                                {
                                    answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"authorize: Invalid version: {str}"});
                                    callback(id, answer, false);
                                    _isPendingRequest = false;
                                    return;
                                }
                            }
                            else 
                            { 
                                version = 1; 
                            }

                            if (_connections.ContainsKey(dapp))
                            {
                                connectionDto = _connections[dapp];
                                success = true;
                                answer = APIUtils.FromAPIResult(new AuthorizationDTO() { wallet = this.Name, nexus = this.Nexus, dapp = dapp, token = connectionDto.Token, version = connectionDto.Version });
                            }
                            else
                            {
                                var bytes = new byte[32];

                                rnd.GetBytes(bytes);

                                token = Base16.Encode(bytes);

                                this.Authorize(dapp, token, version, (authorized, error) =>
                                {
                                    if (authorized)
                                    {
                                        _connections[dapp] = new ConnectionDTO(token, version);

                                        success = true;
                                        answer = APIUtils.FromAPIResult(new AuthorizationDTO() { wallet = this.Name, nexus = this.Nexus, dapp = dapp, token = token });
                                    }
                                    else
                                    {
                                        answer = APIUtils.FromAPIResult(new ErrorDTO() { message = error});
                                    }

                                    callback(id, answer, success);
                                    _isPendingRequest = false;
                                });

                                return;
                            }

                        }
                        else
                        {
                            answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"authorize: Invalid amount of arguments: {args.Length}" });
                        }

                        break;
                    }

                case "getAccount":
                    {
                        int expectedLength;

                        switch (connectionDto.Version)
                        {
                            case 1:
                                expectedLength = 0;
                                break;

                            default:
                                expectedLength = 1;
                                break;
                        }

                        if (args.Length == expectedLength)
                        {
                            string platform;

                            if (connectionDto.Version >= 2)
                            {
                                platform = args[0].ToLower();
                            }
                            else
                            {
                                platform = "phantasma";
                            }

                            GetAccount(platform, (account, error) => {
                                if (error == null)
                                {
                                    success = true;
                                    answer = APIUtils.FromAPIResult(account);
                                }
                                else
                                {
                                    answer = APIUtils.FromAPIResult(new ErrorDTO() { message = error });
                                }

                                callback(id, answer, success);
                                _isPendingRequest = false;
                            });

                            return;
                        }
                        else
                        {
                            answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"getAccount: Invalid amount of arguments: {args.Length}" });
                        }

                        break;
                    }

                case "signData":
                    {
                        int expectedLength;

                        switch (connectionDto.Version)
                        {
                            case 1:
                                expectedLength = 2;
                                break;

                            default:
                                expectedLength = 3;
                                break;
                        }

                        if (args.Length == expectedLength)
                        {
                            var data = Base16.Decode(args[0], false);
                            if (data == null)
                            {
                                answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"signTx: Invalid input received" });
                                callback(id, answer, success);
                                _isPendingRequest = false;
                            }
                            else
                            {
                                SignatureKind signatureKind;

                                if (!Enum.TryParse<SignatureKind>(args[1], out signatureKind))
                                {
                                    answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"signData: Invalid signature: " + args[1] });
                                    callback(id, answer, false);
                                    _isPendingRequest = false;
                                    return;
                                }

                                var platform = connectionDto.Version >= 2 ? args[2].ToLower() : "phantasma";

                                SignData(platform, signatureKind, data, id, (signature, random, txError) => {
                                    if (signature != null)
                                    {
                                        success = true;
                                        answer = APIUtils.FromAPIResult(new SignatureDTO() { signature = signature, random = random });
                                    }
                                    else
                                    {
                                        answer = APIUtils.FromAPIResult(new ErrorDTO() { message = txError });
                                    }

                                    callback(id, answer, success);
                                    _isPendingRequest = false;
                                });
                            }

                            return;
                        }
                        else
                        {
                            answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"signTx: Invalid amount of arguments: {args.Length}" });
                        }
                        break;
                    }

                case "signTx":
                    {
                        int expectedLength;

                        switch (connectionDto.Version)
                        {
                            case 1:
                                expectedLength = 4;
                                break;

                            default:
                                expectedLength = 5;
                                break;
                        }

                        if (args.Length == expectedLength)
                        {
                            int index = 0;

                            if (connectionDto.Version == 1)
                            {
                                var txNexus = args[index]; index++;
                                if (txNexus != this.Nexus)
                                {
                                    answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"signTx: Expected nexus {this.Nexus}, instead got {txNexus}" });
                                    callback(id, answer, false);
                                    _isPendingRequest = false;
                                    return;
                                }
                            }

                            var chain = args[index]; index++;
                            var script = Base16.Decode(args[index], false); index++;

                            if (script == null)
                            {
                                answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"signTx: Invalid script data" });
                            }
                            else
                            {
                                byte[] payload = args[index].Length > 0 ? Base16.Decode(args[index], false) : null;
                                index++;

                                string platform;
                                SignatureKind signatureKind;

                                if (connectionDto.Version >= 2) {
                                    if (!Enum.TryParse<SignatureKind>(args[index], out signatureKind))
                                    {
                                        answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"signTx: Invalid signature: " + args[index] });
                                        callback(id, answer, false);
                                        _isPendingRequest = false;
                                        return;
                                    }
                                    index++;

                                    platform = args[index].ToLower();
                                    index++;
                                }
                                else {
                                    platform = "phantasma";
                                    signatureKind = SignatureKind.Ed25519;
                                }

                                SignTransaction(platform, signatureKind, chain, script, payload, id, (hash, txError) => {
                                    if (hash != Hash.Null)
                                    {
                                        success = true;
                                        answer = APIUtils.FromAPIResult(new TransactionDTO() { hash = hash.ToString() });
                                    }
                                    else
                                    {
                                        answer = APIUtils.FromAPIResult(new ErrorDTO() { message = txError });
                                    }

                                    callback(id, answer, success);
                                    _isPendingRequest = false;
                                });
                                return;

                            }

                        }
                        else
                        {
                            answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"signTx: Invalid amount of arguments: {args.Length}" });
                        }
                        break;
                    }

                case "invokeScript":
                    {
                        if (args.Length == 2)
                        {
                            var chain = args[0];
                            var script = Base16.Decode(args[1], false);

                            if (script == null)
                            {
                                answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"signTx: Invalid script data" });
                            }
                            else
                            {
                                InvokeScript(chain, script, id, (invokeResult, invokeError) =>
                                {
                                    if (invokeResult != null)
                                    {
                                        success = true;
                                        answer = APIUtils.FromAPIResult(new InvocationDTO() { result = Base16.Encode(invokeResult) });
                                    }
                                    else
                                    {
                                        answer = APIUtils.FromAPIResult(new ErrorDTO() { message = invokeError });
                                    }

                                    callback(id, answer, success);
                                    _isPendingRequest = false;
                                });
                                return;
                            }
                        }
                        else
                        {
                            answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"invokeScript: Invalid amount of arguments: {args.Length}"});
                        }

                        break;
                    }

                case "writeArchive":
                    {
                        if (args.Length == 3)
                        {
                            var archiveHash = Hash.Parse(args[0]);
                            var blockIndex = int.Parse(args[1]);
                            var bytes = Base16.Decode(args[2], false);

                            if (bytes == null)
                            {
                                answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"invokeScript: Invalid archive data"});
                            }
                            else
                            {
                                WriteArchive(archiveHash, blockIndex, bytes, (result, error) =>
                                {
                                    if (result)
                                    {
                                        success = true;
                                        answer = APIUtils.FromAPIResult(new TransactionDTO() { hash = archiveHash.ToString() });
                                    }
                                    else
                                    {
                                        answer = APIUtils.FromAPIResult(new ErrorDTO() { message = error });
                                    }

                                    callback(id, answer, success);
                                    _isPendingRequest = false;
                                });
                                return;
                            }
                        }
                        else
                        {
                            answer = APIUtils.FromAPIResult(new ErrorDTO() { message = $"writeArchive: Invalid amount of arguments: {args.Length}" });
                        }

                        break;
                    }

                default:
                    answer = APIUtils.FromAPIResult(new ErrorDTO() { message = "Invalid request type" });
                    break;
            }

            callback(id, answer, success);
            _isPendingRequest = false;
        }

        public void Revoke(string dapp, string token)
        {
            Throw.If(!_connections.ContainsKey(dapp), "unknown dapp");

            var connection = _connections[dapp];
            Throw.If(connection.Token != token, "invalid token");

            _connections.Remove(dapp);
        }
    }
}
