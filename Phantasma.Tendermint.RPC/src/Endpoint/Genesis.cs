﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tendermint.RPC.Endpoint
{
    /*
     * Get genesis file. Return Parameters return round states
     *
        // Genesis file
        type ResultGenesis struct {
            Genesis *types.GenesisDoc 
        }
        // GenesisDoc defines the initial conditions for a tendermint blockchain, in particular its validator set.
        type GenesisDoc struct {
            GenesisTime     time.Time          
            ChainID         string             
            ConsensusParams *ConsensusParams   
            Validators      []GenesisValidator 
            AppHash         cmn.HexBytes      
            AppState        json.RawMessage    
        }
    */

    /*
        {
          "jsonrpc": "2.0",
          "id": "",
          "result": {
            "genesis": {
              "genesis_time": "2019-03-07T01:52:07.500913003Z",
              "chain_id": "Binance-Chain-Nile",
              "consensus_params": {
                "block_size": {
                  "max_bytes": "1048576",
                  "max_gas": "-1"
                },
                "evidence": {
                  "max_age": "100000"
                },
                "validator": {
                  "pub_key_types": [
                    "ed25519"
                  ]
                }
              },
              "app_hash": "",
              "app_state": {
                "tokens": [
                  {
                    "name": "Binance Chain Native Token",
                    "symbol": "BNB",
                    "total_supply": "20000000000000000",
                    "owner": "tbnb12hlquylu78cjylk5zshxpdj6hf3t0tahwjt3ex",
                    "mintable": false
                  }
                ],
                "accounts": [
                  {
                    "name": "Fuji",
                    "address": "tbnb12hlquylu78cjylk5zshxpdj6hf3t0tahwjt3ex",
                    "valaddr": "7B343E041CA130000A8BC00C35152BD7E7740037"
                  },
                  {
                    "name": "Kita",
                    "address": "tbnb167yp9jkv6uaqnyq62gfkx82xmfny0cl9xe04zj",
                    "valaddr": "E0DD72609CC106210D1AA13936CB67B93A0AEE21"
                  },
                  {
                    "name": "Everest",
                    "address": "tbnb1earfwcjre04hp7phqnkw8ts04tkumdn0cyzun0",
                    "valaddr": "FC3108DC3814888F4187452182BC1BAF83B71BC9"
                  },
                  {
                    "name": "Seoraksan",
                    "address": "tbnb1hexqyu3m8uuudqdnnpnsnlwe6xg0n3078lx68l",
                    "valaddr": "62633D9DB7ED78E951F79913FDC8231AA77EC12B"
                  },
                  {
                    "name": "Elbrus",
                    "address": "tbnb135mqtf9gef879nmjlpwz6u2fzqcw4qlzrqwgvw",
                    "valaddr": "B6F20C7FAA2B2F6F24518FA02B71CB5F4A09FBA3"
                  },
                  {
                    "name": "Ararat",
                    "address": "tbnb1q82g2h9q0kfe7sysnj5w7nlak92csfjztymp39",
                    "valaddr": "06FD60078EB4C2356137DD50036597DB267CF616"
                  },
                  {
                    "name": "Carrauntoohil",
                    "address": "tbnb183nch8pn3f698vurrqypq3s254slcane2t66aj",
                    "valaddr": "37EF19AF29679B368D2B9E9DE3F8769B35786676"
                  },
                  {
                    "name": "Scafell",
                    "address": "tbnb1r6l0c0fxu458hlq6m7amkcltj8nufyl9mr2wm5",
                    "valaddr": "18E69CC672973992BB5F76D049A5B2C5DDF77436"
                  },
                  {
                    "name": "Aconcagua",
                    "address": "tbnb193t8pkhm2sxw5uy5ypesygda8rzsk25ge3e9y7",
                    "valaddr": "344C39BB8F4512D6CAB1F6AAFAC1811EF9D8AFDF"
                  },
                  {
                    "name": "Zugspitze",
                    "address": "tbnb108drn8exhv72tp40e6lq9z949nnjj54yzqrr2f",
                    "valaddr": "91844D296BD8E591448EFC65FD6AD51A888D58FA"
                  },
                  {
                    "name": "Gahinga",
                    "address": "tbnb1vehecekrsks5sshcwvxyeyrd469j9wvcqm37yu",
                    "valaddr": "B3727172CE6473BC780298A2D66C12F1A14F5B2A"
                  }
                ],
                "dex": {},
                "param": {
                  "fees": [
                    {
                      "type": "params/FixedFeeParams",
                      "value": {
                        "msg_type": "submit_proposal",
                        "fee": "1000000000",
                        "fee_for": 1
                      }
                    },
                    {
                      "type": "params/FixedFeeParams",
                      "value": {
                        "msg_type": "deposit",
                        "fee": "125000",
                        "fee_for": 1
                      }
                    },
                    {
                      "type": "params/FixedFeeParams",
                      "value": {
                        "msg_type": "vote",
                        "fee": "0",
                        "fee_for": 3
                      }
                    },
                    {
                      "type": "params/FixedFeeParams",
                      "value": {
                        "msg_type": "dexList",
                        "fee": "80000000000",
                        "fee_for": 2
                      }
                    },
                    {
                      "type": "params/FixedFeeParams",
                      "value": {
                        "msg_type": "orderNew",
                        "fee": "0",
                        "fee_for": 3
                      }
                    },
                    {
                      "type": "params/FixedFeeParams",
                      "value": {
                        "msg_type": "orderCancel",
                        "fee": "0",
                        "fee_for": 3
                      }
                    },
                    {
                      "type": "params/FixedFeeParams",
                      "value": {
                        "msg_type": "issueMsg",
                        "fee": "40000000000",
                        "fee_for": 2
                      }
                    },
                    {
                      "type": "params/FixedFeeParams",
                      "value": {
                        "msg_type": "mintMsg",
                        "fee": "20000000000",
                        "fee_for": 2
                      }
                    },
                    {
                      "type": "params/FixedFeeParams",
                      "value": {
                        "msg_type": "tokensBurn",
                        "fee": "100000000",
                        "fee_for": 1
                      }
                    },
                    {
                      "type": "params/FixedFeeParams",
                      "value": {
                        "msg_type": "tokensFreeze",
                        "fee": "1000000",
                        "fee_for": 1
                      }
                    },
                    {
                      "type": "params/TransferFeeParams",
                      "value": {
                        "fixed_fee_params": {
                          "msg_type": "send",
                          "fee": "125000",
                          "fee_for": 1
                        },
                        "multi_transfer_fee": "100000",
                        "lower_limit_as_multi": "2"
                      }
                    },
                    {
                      "type": "params/DexFeeParam",
                      "value": {
                        "dex_fee_fields": [
                          {
                            "fee_name": "ExpireFee",
                            "fee_value": "100000"
                          },
                          {
                            "fee_name": "ExpireFeeNative",
                            "fee_value": "20000"
                          },
                          {
                            "fee_name": "CancelFee",
                            "fee_value": "100000"
                          },
                          {
                            "fee_name": "CancelFeeNative",
                            "fee_value": "20000"
                          },
                          {
                            "fee_name": "FeeRate",
                            "fee_value": "1000"
                          },
                          {
                            "fee_name": "FeeRateNative",
                            "fee_value": "400"
                          },
                          {
                            "fee_name": "IOCExpireFee",
                            "fee_value": "50000"
                          },
                          {
                            "fee_name": "IOCExpireFeeNative",
                            "fee_value": "10000"
                          }
                        ]
                      }
                    }
                  ]
                },
                "stake": {
                  "pool": {
                    "loose_tokens": "4000000000000000",
                    "bonded_tokens": "0"
                  },
                  "params": {
                    "unbonding_time": "604800000000000",
                    "max_validators": 15,
                    "bond_denom": "BNB"
                  },
                  "validators": null,
                  "bonds": null
                },
                "gov": {
                  "starting_proposalID": "1",
                  "deposit_period": {
                    "min_deposit": [
                      {
                        "denom": "BNB",
                        "amount": "200000000000"
                      }
                    ],
                    "max_deposit_period": "1209600000000000"
                  },
                  "voting_period": {
                    "voting_period": "14400000000000"
                  },
                  "tallying_procedure": {
                    "threshold": "50000000",
                    "veto": "33400000",
                    "governance_penalty": "1000000"
                  }
                },
                "gentxs": [
                  {
                    "type": "auth/StdTx",
                    "value": {
                      "msg": [
                        {
                          "type": "cosmos-sdk/MsgCreateValidatorProposal",
                          "value": {
                            "MsgCreateValidator": {
                              "Description": {
                                "moniker": "Fuji",
                                "identity": "",
                                "website": "",
                                "details": ""
                              },
                              "Commission": {
                                "rate": "0",
                                "max_rate": "0",
                                "max_change_rate": "0"
                              },
                              "delegator_address": "tbnb12hlquylu78cjylk5zshxpdj6hf3t0tahwjt3ex",
                              "validator_address": "bva12hlquylu78cjylk5zshxpdj6hf3t0tahqmr98n",
                              "pubkey": {
                                "type": "tendermint/PubKeyEd25519",
                                "value": "Sl1HU+t5+S6A7+It96yk9mak9Ev4HFNsSgnUucW2VLU="
                              },
                              "delegation": {
                                "denom": "BNB",
                                "amount": "100000000000"
                              }
                            },
                            "proposal_id": "0"
                          }
                        }
                      ],
                      "signatures": [
                        {
                          "pub_key": {
                            "type": "tendermint/PubKeySecp256k1",
                            "value": "A+gcCBsoefY1d9TnkIOPV8IX5+/i/BTrMvFU7vG9RXIk"
                          },
                          "signature": "oWWGy2kN9yQDVJ/aLE7N/Si/lTTsce3k8VRsdtzO6doSw2eFL9v8wB3GdTaOBvuJGJti73WPGaEN8fbUjao5hw==",
                          "account_number": "0",
                          "sequence": "0"
                        }
                      ],
                      "memo": "1bca643058c56f9c20ebaaad1739522ee7d11cd6@172.18.10.204:26656",
                      "source": "0",
                      "data": null
                    }
                  },
                  {
                    "type": "auth/StdTx",
                    "value": {
                      "msg": [
                        {
                          "type": "cosmos-sdk/MsgCreateValidatorProposal",
                          "value": {
                            "MsgCreateValidator": {
                              "Description": {
                                "moniker": "Kita",
                                "identity": "",
                                "website": "",
                                "details": ""
                              },
                              "Commission": {
                                "rate": "0",
                                "max_rate": "0",
                                "max_change_rate": "0"
                              },
                              "delegator_address": "tbnb167yp9jkv6uaqnyq62gfkx82xmfny0cl9xe04zj",
                              "validator_address": "bva167yp9jkv6uaqnyq62gfkx82xmfny0cl9gs8pu8",
                              "pubkey": {
                                "type": "tendermint/PubKeyEd25519",
                                "value": "BCJDOWiPAS5kneSOJBiACS6qj2qg9PFL/Pngx2kXwLY="
                              },
                              "delegation": {
                                "denom": "BNB",
                                "amount": "100000000000"
                              }
                            },
                            "proposal_id": "0"
                          }
                        }
                      ],
                      "signatures": [
                        {
                          "pub_key": {
                            "type": "tendermint/PubKeySecp256k1",
                            "value": "Axu55ox7sJ1YsxZsdtUGU7Is5xCzEfs0rT5nQ1JnCkuh"
                          },
                          "signature": "Mnvxh3LIiclOLlIN1N1vrOA7igL6pdo5EwKT/JzwQbNAPLQA9CgArrMaH+GW+m+wjxEezCDC9tDqe3KB0NwI1w==",
                          "account_number": "0",
                          "sequence": "0"
                        }
                      ],
                      "memo": "7bbe02b44f45fb8f73981c13bb21b19b30e2658d@172.18.10.205:26656",
                      "source": "0",
                      "data": null
                    }
                  },
                  {
                    "type": "auth/StdTx",
                    "value": {
                      "msg": [
                        {
                          "type": "cosmos-sdk/MsgCreateValidatorProposal",
                          "value": {
                            "MsgCreateValidator": {
                              "Description": {
                                "moniker": "Everest",
                                "identity": "",
                                "website": "",
                                "details": ""
                              },
                              "Commission": {
                                "rate": "0",
                                "max_rate": "0",
                                "max_change_rate": "0"
                              },
                              "delegator_address": "tbnb1earfwcjre04hp7phqnkw8ts04tkumdn0cyzun0",
                              "validator_address": "bva1earfwcjre04hp7phqnkw8ts04tkumdn0kd2gd6",
                              "pubkey": {
                                "type": "tendermint/PubKeyEd25519",
                                "value": "QDSzfO2ooL8Tsauu7nqPk4NUIJmlVNIZuT0M5p45cOg="
                              },
                              "delegation": {
                                "denom": "BNB",
                                "amount": "100000000000"
                              }
                            },
                            "proposal_id": "0"
                          }
                        }
                      ],
                      "signatures": [
                        {
                          "pub_key": {
                            "type": "tendermint/PubKeySecp256k1",
                            "value": "A7H2pGbdLT9YJQwlqNd3dWfq6fGs5Xk8v7h3Ckp+AU2e"
                          },
                          "signature": "8tLKWXxMc6HmOTovnRGD3i8xhX572wn3Kj8Kkd6ND9I/dOveZxfrvsmE6bXFRcyBvIXxFTSEef4fwuVKjNgWUw==",
                          "account_number": "0",
                          "sequence": "0"
                        }
                      ],
                      "memo": "0d46d1e6b1103d33765e209a7da0943156291fcb@172.18.10.206:26656",
                      "source": "0",
                      "data": null
                    }
                  },
                  {
                    "type": "auth/StdTx",
                    "value": {
                      "msg": [
                        {
                          "type": "cosmos-sdk/MsgCreateValidatorProposal",
                          "value": {
                            "MsgCreateValidator": {
                              "Description": {
                                "moniker": "Seoraksan",
                                "identity": "",
                                "website": "",
                                "details": ""
                              },
                              "Commission": {
                                "rate": "0",
                                "max_rate": "0",
                                "max_change_rate": "0"
                              },
                              "delegator_address": "tbnb1hexqyu3m8uuudqdnnpnsnlwe6xg0n3078lx68l",
                              "validator_address": "bva1hexqyu3m8uuudqdnnpnsnlwe6xg0n307fkwwe2",
                              "pubkey": {
                                "type": "tendermint/PubKeyEd25519",
                                "value": "j0p0oHNRiV3fNzBXuY+ubfryzSHzegY+GWAQeP5HDVM="
                              },
                              "delegation": {
                                "denom": "BNB",
                                "amount": "100000000000"
                              }
                            },
                            "proposal_id": "0"
                          }
                        }
                      ],
                      "signatures": [
                        {
                          "pub_key": {
                            "type": "tendermint/PubKeySecp256k1",
                            "value": "Aygd4/N9zvf/rKnWCMCqbt0O2pad5ZXyiPeBZgbhE7GV"
                          },
                          "signature": "d7g5NIda45dOpTT+k/rVOqXrxilPI1t6E0qT9YbTzVBKNSOb2uAWy2hlMt32bNIFW5W5/d7czFWnmqEaY/BQmg==",
                          "account_number": "0",
                          "sequence": "0"
                        }
                      ],
                      "memo": "64b29930674c02dd4a45968759173a3c546fb57c@172.18.10.207:26656",
                      "source": "0",
                      "data": null
                    }
                  },
                  {
                    "type": "auth/StdTx",
                    "value": {
                      "msg": [
                        {
                          "type": "cosmos-sdk/MsgCreateValidatorProposal",
                          "value": {
                            "MsgCreateValidator": {
                              "Description": {
                                "moniker": "Elbrus",
                                "identity": "",
                                "website": "",
                                "details": ""
                              },
                              "Commission": {
                                "rate": "0",
                                "max_rate": "0",
                                "max_change_rate": "0"
                              },
                              "delegator_address": "tbnb135mqtf9gef879nmjlpwz6u2fzqcw4qlzrqwgvw",
                              "validator_address": "bva135mqtf9gef879nmjlpwz6u2fzqcw4qlzdfxujm",
                              "pubkey": {
                                "type": "tendermint/PubKeyEd25519",
                                "value": "SbKI5Ou7OigcLVRvwwJT1brwiZO25dKV+3h6WzFKKY4="
                              },
                              "delegation": {
                                "denom": "BNB",
                                "amount": "100000000000"
                              }
                            },
                            "proposal_id": "0"
                          }
                        }
                      ],
                      "signatures": [
                        {
                          "pub_key": {
                            "type": "tendermint/PubKeySecp256k1",
                            "value": "A0130hq3qdHzfPUkU/ZQ4s2jynvhy7uOrtWnCpCmjasJ"
                          },
                          "signature": "NYgG1u8fayGSTStgwfioxDemDS+8H16DC7+s/DRD1rBannYUs8cUAn2Lfrqg0leRhhNrWGPgD4qQv9IU2Smh/w==",
                          "account_number": "0",
                          "sequence": "0"
                        }
                      ],
                      "memo": "7d290fae6845d53f7ffbb2aabc528b29650bee6c@172.18.10.208:26656",
                      "source": "0",
                      "data": null
                    }
                  },
                  {
                    "type": "auth/StdTx",
                    "value": {
                      "msg": [
                        {
                          "type": "cosmos-sdk/MsgCreateValidatorProposal",
                          "value": {
                            "MsgCreateValidator": {
                              "Description": {
                                "moniker": "Ararat",
                                "identity": "",
                                "website": "",
                                "details": ""
                              },
                              "Commission": {
                                "rate": "0",
                                "max_rate": "0",
                                "max_change_rate": "0"
                              },
                              "delegator_address": "tbnb1q82g2h9q0kfe7sysnj5w7nlak92csfjztymp39",
                              "validator_address": "bva1q82g2h9q0kfe7sysnj5w7nlak92csfjz9dn40s",
                              "pubkey": {
                                "type": "tendermint/PubKeyEd25519",
                                "value": "4Xy+nCDNz9+HazsSl40yZKAH/KqnHEzbcB2evAMj9E8="
                              },
                              "delegation": {
                                "denom": "BNB",
                                "amount": "100000000000"
                              }
                            },
                            "proposal_id": "0"
                          }
                        }
                      ],
                      "signatures": [
                        {
                          "pub_key": {
                            "type": "tendermint/PubKeySecp256k1",
                            "value": "AlG4f0se3Ok1EbsvzMtDIQsSGBslR+eqy9uSBIgXQToP"
                          },
                          "signature": "pMLebkHE2hnuHv+AjIdMdnm6G5kzheCFs+V1+NZV12p+yfK3T7UPy/2mDFVkmIUfwWaBtDHD//+G8eyvZDD5Ew==",
                          "account_number": "0",
                          "sequence": "0"
                        }
                      ],
                      "memo": "7cf465f3c351f9f0873be9a7396a5438208b9546@172.18.10.209:26656",
                      "source": "0",
                      "data": null
                    }
                  },
                  {
                    "type": "auth/StdTx",
                    "value": {
                      "msg": [
                        {
                          "type": "cosmos-sdk/MsgCreateValidatorProposal",
                          "value": {
                            "MsgCreateValidator": {
                              "Description": {
                                "moniker": "Carrauntoohil",
                                "identity": "",
                                "website": "",
                                "details": ""
                              },
                              "Commission": {
                                "rate": "0",
                                "max_rate": "0",
                                "max_change_rate": "0"
                              },
                              "delegator_address": "tbnb183nch8pn3f698vurrqypq3s254slcane2t66aj",
                              "validator_address": "bva183nch8pn3f698vurrqypq3s254slcaneyzjwr8",
                              "pubkey": {
                                "type": "tendermint/PubKeyEd25519",
                                "value": "vQPen4qynigACU4VP6xvaWz6USU2ycL4BNyywsTkrtY="
                              },
                              "delegation": {
                                "denom": "BNB",
                                "amount": "100000000000"
                              }
                            },
                            "proposal_id": "0"
                          }
                        }
                      ],
                      "signatures": [
                        {
                          "pub_key": {
                            "type": "tendermint/PubKeySecp256k1",
                            "value": "A6+/HDC0uLx/9Z5N+Gc+qWOIUaRpKsZYoDlRb41EUryy"
                          },
                          "signature": "BRG3lQeEWiamvVHnf30YeFqsK+TIt0qfYhLhSZnyYwh4b3AwsHQcTzFfr/wezfDa7C/OnxinngXXCAy5zLAhPg==",
                          "account_number": "0",
                          "sequence": "0"
                        }
                      ],
                      "memo": "32769f58a63d25e4a0b9d793ce80626506213727@172.18.10.210:26656",
                      "source": "0",
                      "data": null
                    }
                  },
                  {
                    "type": "auth/StdTx",
                    "value": {
                      "msg": [
                        {
                          "type": "cosmos-sdk/MsgCreateValidatorProposal",
                          "value": {
                            "MsgCreateValidator": {
                              "Description": {
                                "moniker": "Scafell",
                                "identity": "",
                                "website": "",
                                "details": ""
                              },
                              "Commission": {
                                "rate": "0",
                                "max_rate": "0",
                                "max_change_rate": "0"
                              },
                              "delegator_address": "tbnb1r6l0c0fxu458hlq6m7amkcltj8nufyl9mr2wm5",
                              "validator_address": "bva1r6l0c0fxu458hlq6m7amkcltj8nufyl942z69p",
                              "pubkey": {
                                "type": "tendermint/PubKeyEd25519",
                                "value": "GE57ED00xBAD+bhk1fjBrdqb0ENrJTuzyES8c5wed8k="
                              },
                              "delegation": {
                                "denom": "BNB",
                                "amount": "100000000000"
                              }
                            },
                            "proposal_id": "0"
                          }
                        }
                      ],
                      "signatures": [
                        {
                          "pub_key": {
                            "type": "tendermint/PubKeySecp256k1",
                            "value": "ArnjgWSGbDDJmYuIYbE97ZShYNCf0AlVjeNINmmDyYa0"
                          },
                          "signature": "un+GYFlzBtV9lDapslHxwHbsVi0Ng8YzAv8UK4OgSNcRU4FUX69r2ujkx6Zx8EIsgPlxCja9xgGuK9qYJwPZKw==",
                          "account_number": "0",
                          "sequence": "0"
                        }
                      ],
                      "memo": "c1bcf51e6022010ebb93288bd5d932a3894c999e@172.18.10.211:26656",
                      "source": "0",
                      "data": null
                    }
                  },
                  {
                    "type": "auth/StdTx",
                    "value": {
                      "msg": [
                        {
                          "type": "cosmos-sdk/MsgCreateValidatorProposal",
                          "value": {
                            "MsgCreateValidator": {
                              "Description": {
                                "moniker": "Aconcagua",
                                "identity": "",
                                "website": "",
                                "details": ""
                              },
                              "Commission": {
                                "rate": "0",
                                "max_rate": "0",
                                "max_change_rate": "0"
                              },
                              "delegator_address": "tbnb193t8pkhm2sxw5uy5ypesygda8rzsk25ge3e9y7",
                              "validator_address": "bva193t8pkhm2sxw5uy5ypesygda8rzsk25ghc336t",
                              "pubkey": {
                                "type": "tendermint/PubKeyEd25519",
                                "value": "TUIK6oQ+kqDP5p2JaW3/aCd2n5y1KiSa9TfOib8qS3Q="
                              },
                              "delegation": {
                                "denom": "BNB",
                                "amount": "100000000000"
                              }
                            },
                            "proposal_id": "0"
                          }
                        }
                      ],
                      "signatures": [
                        {
                          "pub_key": {
                            "type": "tendermint/PubKeySecp256k1",
                            "value": "Az0wT4xmeI7a7sEIFKcGLkiICkFBS1Fl4/hFMGV1QjL6"
                          },
                          "signature": "T+Jg3b6p0IOd/J0tChygDOnQjKJXl2m6K1zyyLMM2E82woc9eL7nR6j7jr00SuU5dJ/Z+UuYfeinv4R0pbGpmA==",
                          "account_number": "0",
                          "sequence": "0"
                        }
                      ],
                      "memo": "dd2adba52ad9c830fe16a53fe81dac6880a91218@172.18.10.212:26656",
                      "source": "0",
                      "data": null
                    }
                  },
                  {
                    "type": "auth/StdTx",
                    "value": {
                      "msg": [
                        {
                          "type": "cosmos-sdk/MsgCreateValidatorProposal",
                          "value": {
                            "MsgCreateValidator": {
                              "Description": {
                                "moniker": "Zugspitze",
                                "identity": "",
                                "website": "",
                                "details": ""
                              },
                              "Commission": {
                                "rate": "0",
                                "max_rate": "0",
                                "max_change_rate": "0"
                              },
                              "delegator_address": "tbnb108drn8exhv72tp40e6lq9z949nnjj54yzqrr2f",
                              "validator_address": "bva108drn8exhv72tp40e6lq9z949nnjj54yvfth5u",
                              "pubkey": {
                                "type": "tendermint/PubKeyEd25519",
                                "value": "yA6avvf/Q5wQxo/o8TA97d/FJ3GMOzfYumgHRG48gno="
                              },
                              "delegation": {
                                "denom": "BNB",
                                "amount": "100000000000"
                              }
                            },
                            "proposal_id": "0"
                          }
                        }
                      ],
                      "signatures": [
                        {
                          "pub_key": {
                            "type": "tendermint/PubKeySecp256k1",
                            "value": "A28N2eZXepmh+2enXvdAPqbbPf9yFCqYZleFjUMRJe0g"
                          },
                          "signature": "egp4GjM/8PEVeFJiopen35eZzy/5NKjGKmK3MGpfmAFGQvjN6G4HyGX+6eigOuw40qpMdT9HYmvzSoa+jgXURQ==",
                          "account_number": "0",
                          "sequence": "0"
                        }
                      ],
                      "memo": "c4d94f29e765ecfe81c940e11c2e997321aa8e0f@172.18.10.213:26656",
                      "source": "0",
                      "data": null
                    }
                  },
                  {
                    "type": "auth/StdTx",
                    "value": {
                      "msg": [
                        {
                          "type": "cosmos-sdk/MsgCreateValidatorProposal",
                          "value": {
                            "MsgCreateValidator": {
                              "Description": {
                                "moniker": "Gahinga",
                                "identity": "",
                                "website": "",
                                "details": ""
                              },
                              "Commission": {
                                "rate": "0",
                                "max_rate": "0",
                                "max_change_rate": "0"
                              },
                              "delegator_address": "tbnb1vehecekrsks5sshcwvxyeyrd469j9wvcqm37yu",
                              "validator_address": "bva1vehecekrsks5sshcwvxyeyrd469j9wvcwje26f",
                              "pubkey": {
                                "type": "tendermint/PubKeyEd25519",
                                "value": "kUKvzGkbfMBdJsewvgyLRkGClBcXMOB584T94vpQuvw="
                              },
                              "delegation": {
                                "denom": "BNB",
                                "amount": "100000000000"
                              }
                            },
                            "proposal_id": "0"
                          }
                        }
                      ],
                      "signatures": [
                        {
                          "pub_key": {
                            "type": "tendermint/PubKeySecp256k1",
                            "value": "AsS8HffgT0IIai/sesaWtW5wurpu7eBDkhu0esmwjsnc"
                          },
                          "signature": "k6LegehVpGnjQ4ePBwJajrbKlPg5tXQMkBtIZ+nbMNAHp4Z2IihYrUGMAoKu0B0LJbbNH/7Gq7b0AK5HfYEByg==",
                          "account_number": "0",
                          "sequence": "0"
                        }
                      ],
                      "memo": "4119f9f689f62734bcf3757f916639bc480bb8ce@172.18.10.214:26656",
                      "source": "0",
                      "data": null
                    }
                  }
                ]
              }
            }
          }
        }
    */

    public class ConsensusParamsBlockSize
    {
        [JsonProperty("max_bytes")]
        public string MaxBytes { get; set; }

        [JsonProperty("max_gas")]
        public string MaxGas { get; set; }
    }

    public class ConsensusParamsEvidence
    {
        [JsonProperty("max_age")]
        public string MaxAge { get; set; }
    }

    public class ConsensusParamsValidator
    {
        [JsonProperty("pub_key_types")]
        public List<string> PubKeyTypes { get; set; }
    }

    public class ConsensusParams
    {
        [JsonProperty("block_size")]
        public ConsensusParamsBlockSize BlockSize { get; set; }

        [JsonProperty("evidence")]
        public ConsensusParamsEvidence Evidence { get; set; }

        [JsonProperty("validator")]
        public ConsensusParamsValidator Validator { get; set; }
    }

    public class ResultGenesisAppStateToken
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("total_supply")]
        public string TotalSupply { get; set; }

        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("mintable")]
        public bool Mintable { get; set; }

    }

    public class ResultGenesisAppStateAccount
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("valaddr")]
        public string Valaddr { get; set; }
    }

    public class ResultGenesisAppStateDex
    {
        // TODO  No example available
    }

    public class ResultGenesisAppStateParamFeeValueFixedFeeParams
    {
        [JsonProperty("msg_type")]
        public string MsgType { get; set; }

        [JsonProperty("fee")]
        public string Fee { get; set; }

        [JsonProperty("fee_for")]
        public long FeeFor { get; set; }
    }

    public class ResultGenesisAppStateParamFeeValueDexFeeField
    {
        [JsonProperty("fee_name")]
        public string FeeName { get; set; }

        [JsonProperty("fee_value")]
        public string FeeValue { get; set; }
    }

    public class ResultGenesisAppStateParamFeeValue
    {
        [JsonProperty("msg_type")]
        public string MsgType { get; set; }

        [JsonProperty("fee")]
        public string Fee { get; set; }

        [JsonProperty("fee_for")]
        public long? FeeFor { get; set; }

        [JsonProperty("fixed_fee_params")]
        public ResultGenesisAppStateParamFeeValueFixedFeeParams FixedFeeParams { get; set; }

        [JsonProperty("multi_transfer_fee")]
        public string multi_transfer_fee { get; set; }

        [JsonProperty("lower_limit_as_multi")]
        public string LowerLimitAsMulti { get; set; }

        [JsonProperty("dex_fee_fields")]
        public List<ResultGenesisAppStateParamFeeValueDexFeeField> DexFeeFields { get; set; }
    }

    public class ResultGenesisAppStateParamFee
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public ResultGenesisAppStateParamFeeValue Value { get; set; }
    }

    public class ResultGenesisAppStateParam
    {
        [JsonProperty("fees")]
        public List<ResultGenesisAppStateParamFee> Fees { get; set; }

    }

    public class ResultGenesisAppStateStakePool
    {
        [JsonProperty("loose_tokens")]
        public string LooseTokens { get; set; }

        [JsonProperty("bonded_tokens")]
        public string BondedTokens { get; set; }

    }

    public class ResultGenesisAppStateStakeParams
    {
        [JsonProperty("unbonding_time")]
        public string UnbondingTime { get; set; }

        [JsonProperty("max_validators")]
        public string MaxValidators { get; set; }

        [JsonProperty("bond_denom")]
        public string BondDenom { get; set; }
    }

    public class ResultGenesisAppStateStake
    {
        [JsonProperty("pool")]
        public ResultGenesisAppStateStakePool Pool { get; set; }

        [JsonProperty("params")]
        public ResultGenesisAppStateStakeParams Params { get; set; }

        [JsonProperty("validators")]
        public string Validators { get; set; } // TODO no example data

        [JsonProperty("bonds")]
        public string Bonds { get; set; } // TODO no example data
    }

    public class ResultGenesisAppStateGovDepositPeriodMinDeposit
    {
        [JsonProperty("denom")]
        public string Denom { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }
    }

    public class ResultGenesisAppStateGovDepositPeriod
    {
        [JsonProperty("min_deposit")]
        public List<ResultGenesisAppStateGovDepositPeriodMinDeposit> MinDeposit { get; set; }

        [JsonProperty("max_deposit_period")]
        public string MaxDepositPeriod { get; set; }
    }

    public class ResultGenesisAppStateGovVotingPeroid
    {
        [JsonProperty("voting_period")]
        public string VotingPeriod { get; set; }
    }

    public class ResultGenesisAppStateGovTallyingProcedure
    {
        [JsonProperty("threshold")]
        public string Threshold { get; set; }

        [JsonProperty("veto")]
        public string Veto { get; set; }

        [JsonProperty("governance_penalty")]
        public string GovernancePenalty { get; set; }
    }

    public class ResultGenesisAppStateGov
    {
        [JsonProperty("starting_proposalID")]
        public string StartingProposalID { get; set; }

        [JsonProperty("deposit_period")]
        public ResultGenesisAppStateGovDepositPeriod DepositPeriod { get; set; }

        [JsonProperty("voting_period")]
        public ResultGenesisAppStateGovVotingPeroid VotingPeriod { get; set; }

        [JsonProperty("tallying_procedure")]
        public ResultGenesisAppStateGovTallyingProcedure TallyingProcedure { get; set; }
    }

    public class ResultGenesisAppStateGentxValueMsg
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonExtensionData, JsonProperty("value")]
        public IDictionary<string, JToken> Value;
    }

    public class ResultGenesisAppStateGentxValueSignaturePubKey
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class ResultGenesisAppStateGentxValueSignature
    {
        [JsonProperty("pub_key")]
        public ResultGenesisAppStateGentxValueSignaturePubKey PubKey { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }

        [JsonProperty("account_number")]
        public string AccountNumber { get; set; }

        [JsonProperty("sequence")]
        public string Sequence { get; set; }
    }

    public class ResultGenesisAppStateGentxValue
    {
        [JsonProperty("msg")]
        public List<ResultGenesisAppStateGentxValueMsg> Msg { get; set; }

        [JsonProperty("signatures")]
        public List<ResultGenesisAppStateGentxValueSignature> Signatures { get; set; }

        [JsonProperty("memo")]
        public string Memo { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }
    }

    public class ResultGenesisAppStateGentx
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public ResultGenesisAppStateGentxValue Value { get; set; }
    }

    public class ResultGenesisAppState
    {
        [JsonProperty("tokens")]
        public List<ResultGenesisAppStateToken> Tokens { get; set; }

        [JsonProperty("accounts")]
        public List<ResultGenesisAppStateAccount> Accounts { get; set; }

        [JsonProperty("dex")]
        public ResultGenesisAppStateDex Dex { get; set; }

        [JsonProperty("param")]
        public ResultGenesisAppStateParam Param { get; set; }

        [JsonProperty("stake")]
        public ResultGenesisAppStateStake Stake { get; set; }

        [JsonProperty("gov")]
        public ResultGenesisAppStateGov Gov { get; set; }

        [JsonProperty("gentxs")]
        public List<ResultGenesisAppStateGentx> Gentxs { get; set; }
    }

    public class GenesisDoc
    {
        [JsonProperty("genesis_time")]
        public string GenesisTime { get; set; }

        [JsonProperty("chain_id")]
        public string ChainId { get; set; }

        [JsonProperty("consensus_params")]
        public ConsensusParams ConsensusParams { get; set; }

        [JsonProperty("app_hash")]
        public string AppHash { get; set; }

        [JsonProperty("app_state")]
        public ResultGenesisAppState AppState { get; set; }
    }

    public class ResultGenesis : IEndpointResponse
    {
        [JsonProperty("genesis")]
        public GenesisDoc Genesis { get; set; }
    }
}
