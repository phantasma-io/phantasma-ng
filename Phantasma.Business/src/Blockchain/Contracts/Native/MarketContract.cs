using System;
using System.Numerics;
using Phantasma.Business.Blockchain.Tokens;
using Phantasma.Core.Cryptography;
using Phantasma.Core.Cryptography.Structs;
using Phantasma.Core.Domain;
using Phantasma.Core.Domain.Contract;
using Phantasma.Core.Domain.Contract.Enums;
using Phantasma.Core.Domain.Contract.Market;
using Phantasma.Core.Domain.Contract.Market.Enums;
using Phantasma.Core.Domain.Contract.Market.Structs;
using Phantasma.Core.Domain.Events;
using Phantasma.Core.Domain.Events.Structs;
using Phantasma.Core.Domain.Token;
using Phantasma.Core.Domain.Token.Enums;
using Phantasma.Core.Domain.VM;
using Phantasma.Core.Domain.VM.Enums;
using Phantasma.Core.Storage.Context;
using Phantasma.Core.Storage.Context.Structs;
using Phantasma.Core.Types;
using Phantasma.Core.Types.Structs;

namespace Phantasma.Business.Blockchain.Contracts.Native
{
    public sealed class MarketContract : NativeContract
    {
        public override NativeContractKind Kind => NativeContractKind.Market;

        private const int fiveMinutes = 86400 / 24 / 12;
        private const int oneHour = 3600;

#pragma warning disable 0649
        private StorageMap _auctionMap; //<string, MarketAuction>
        private StorageMap _auctionIds; //<string, MarketAuction>
#pragma warning restore 0649

        public MarketContract() : base()
        {
        }

        /// <summary>
        /// Edit an auction
        /// </summary>
        /// <param name="from"></param>
        /// <param name="baseSymbol"></param>
        /// <param name="quoteSymbol"></param>
        /// <param name="tokenID"></param>
        /// <param name="price"></param>
        /// <param name="endPrice"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="extensionPeriod"></param>
        public void EditAuction(Address from, string baseSymbol, string quoteSymbol, BigInteger tokenID, BigInteger price, BigInteger endPrice, Timestamp startDate, Timestamp endDate, BigInteger extensionPeriod)
        {
            Runtime.Expect(Runtime.IsWitness(from), "invalid witness");

            Runtime.Expect(Runtime.TokenExists(quoteSymbol), "invalid quote token");
            var quoteToken = Runtime.GetToken(quoteSymbol);
            Runtime.Expect(quoteToken.Flags.HasFlag(TokenFlags.Fungible), "quote token must be fungible");

            var nft = Runtime.ReadToken(baseSymbol, tokenID);
            Runtime.Expect(nft.CurrentChain == Runtime.Chain.Name, "token not currently in this chain");
            var marketAddress = GetAddressForNative(NativeContractKind.Market);
            Runtime.Expect(nft.CurrentOwner == marketAddress, "invalid owner");

            Runtime.Expect(price >= 0, "price has to be >= 0");
            Runtime.Expect(endPrice >= 0, "final price has to be >= 0");

            var auctionID = baseSymbol + "." + tokenID;

            Runtime.Expect(_auctionMap.ContainsKey(auctionID), "invalid auction");

            var auction = _auctionMap.Get<string, MarketAuction>(auctionID);

            Runtime.Expect(auction.Creator == from, "invalid auction creator");

            if (auction.Type != TypeAuction.Fixed) // prevent edit already started auctions
            {
                Runtime.Expect(auction.StartDate > Runtime.Time, "EditAuction can only be used before listing start");
            }

            if (price == 0)
            {
                price = auction.Price;
            }

            if (endPrice == 0)
            {
                endPrice = auction.EndPrice;
            }

            if (startDate == 0)
            {
                startDate = auction.StartDate;
            }

            if (endDate == 0)
            {
                endDate = auction.EndDate;
            }
            else
            {
                Runtime.Expect(endDate > startDate, "invalid end date");
            }

            if (extensionPeriod == 0 || auction.Type == TypeAuction.Fixed)
            {
                extensionPeriod = auction.ExtensionPeriod;
            }

            if (auction.Type == TypeAuction.Classic || auction.Type == TypeAuction.Reserve)
            {
                Runtime.Expect(extensionPeriod <= oneHour, "extensionPeriod must be <= 1 hour");
            }

            var auctionNew = new MarketAuction(from, startDate, endDate, baseSymbol, quoteSymbol, tokenID, price, endPrice, extensionPeriod, auction.Type, auction.ListingFee, auction.ListingFeeAddress, auction.BuyingFee, auction.BuyingFeeAddress, auction.CurrentBidWinner);
            _auctionMap.Set(auctionID, auctionNew);

            Runtime.Notify(EventKind.OrderCancelled, auctionNew.Creator, new MarketEventData() { ID = auction.TokenID, BaseSymbol = auction.BaseSymbol, QuoteSymbol = auction.QuoteSymbol, Price = auction.Price, EndPrice = auction.EndPrice, Type = auction.Type });
            Runtime.Notify(EventKind.OrderCreated, auctionNew.Creator, new MarketEventData() { ID = auctionNew.TokenID, BaseSymbol = auctionNew.BaseSymbol, QuoteSymbol = auctionNew.QuoteSymbol, Price = auctionNew.Price, EndPrice = auctionNew.EndPrice, Type = auctionNew.Type });
        }

        /// <summary>
        /// List a token for sale
        /// </summary>
        /// <param name="from"></param>
        /// <param name="baseSymbol"></param>
        /// <param name="quoteSymbol"></param>
        /// <param name="tokenID"></param>
        /// <param name="price"></param>
        /// <param name="endPrice"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="extensionPeriod"></param>
        /// <param name="typeAuction"></param>
        /// <param name="listingFee"></param>
        /// <param name="listingFeeAddress"></param>
        public void ListToken(Address from, string baseSymbol, string quoteSymbol, BigInteger tokenID, BigInteger price, BigInteger endPrice, Timestamp startDate, Timestamp endDate, BigInteger extensionPeriod, BigInteger typeAuction, BigInteger listingFee, Address listingFeeAddress)
        {
            Runtime.Expect(Runtime.IsWitness(from), "invalid witness");

            if (startDate < Runtime.Time) // initialize start date to Runtime.Time if its before that
            {
                startDate = Runtime.Time;
            }

            Runtime.Expect(Runtime.TokenExists(quoteSymbol), "invalid quote token");
            var quoteToken = Runtime.GetToken(quoteSymbol);
            Runtime.Expect(quoteToken.Flags.HasFlag(TokenFlags.Fungible), "quote token must be fungible");

            Runtime.Expect(Runtime.TokenExists(baseSymbol), "invalid base token");
            var baseToken = Runtime.GetToken(baseSymbol);
            Runtime.Expect(!baseToken.Flags.HasFlag(TokenFlags.Fungible), "base token must be non-fungible");

            var nft = Runtime.ReadToken(baseSymbol, tokenID);
            Runtime.Expect(nft.CurrentChain == Runtime.Chain.Name, "token not currently in this chain");
            Runtime.Expect(nft.CurrentOwner == from, $"invalid owner {nft.CurrentOwner} {from} {tokenID}");

            Runtime.Expect(price >= 0, "price has to be >= 0");

            Runtime.Expect(listingFee <= 5 && listingFee >= 0, "listingFee has to be <= 5% and >= 0%");

            Runtime.Expect(listingFee == 0 || listingFeeAddress != Address.Null, "Fee receiving address cannot be null");

            TypeAuction type;

            if (typeAuction == 1) // Classic
            {
                Runtime.Expect(endDate > startDate, "end date must be later than start date");
                Runtime.Expect(extensionPeriod <= oneHour, "extensionPeriod must be <= 1 hour");
                var maxAllowedDate = Runtime.Time + TimeSpan.FromDays(30);
                Runtime.Expect(endDate <= maxAllowedDate, "end date is too distant, max: " + maxAllowedDate + ", received: " + endDate);
                endPrice = 0;
                type = TypeAuction.Classic;
            }
            else if (typeAuction == 2) // Reserve
            {
                Runtime.Expect(extensionPeriod <= oneHour, "extensionPeriod must be <= 1 hour");
                endPrice = 0;
                startDate = 0;
                endDate = 0;
                type = TypeAuction.Reserve;
            }
            else if (typeAuction == 3) // Dutch
            {
                Runtime.Expect(endDate > startDate, "end date must be later than start date");
                Runtime.Expect(endPrice < price, "final price has to be lower than initial price");
                Runtime.Expect(endPrice >= 0, "final price has to be >= 0");
                var maxAllowedDate = Runtime.Time + TimeSpan.FromDays(30);
                Runtime.Expect(endDate <= maxAllowedDate, "end date is too distant, max: " + maxAllowedDate + ", received: " + endDate);
                extensionPeriod = 0;
                type = TypeAuction.Dutch;
            }
            else // Default - Fixed
            {
                if (endDate != 0)
                {
                    Runtime.Expect(endDate > Runtime.Time, "invalid end date");
                }
                endPrice = 0;
                extensionPeriod = 0;
                type = TypeAuction.Fixed;
            }

            Runtime.TransferToken(baseToken.Symbol, from, Address, tokenID);

            var auction = new MarketAuction(from, startDate, endDate, baseSymbol, quoteSymbol, tokenID, price, endPrice, extensionPeriod, type, listingFee, listingFeeAddress, 0, Address.Null, Address.Null);
            var auctionID = baseSymbol + "." + tokenID;
            _auctionMap.Set(auctionID, auction);
            _auctionIds.Set(auctionID, auctionID);

            Runtime.Notify(EventKind.OrderCreated, from, new MarketEventData() { ID = tokenID, BaseSymbol = baseSymbol, QuoteSymbol = quoteSymbol, Price = price, EndPrice = endPrice, Type = type });
        }
        
        /// <summary>
        /// Bid on a token (Auction)
        /// </summary>
        /// <param name="from"></param>
        /// <param name="symbol"></param>
        /// <param name="tokenID"></param>
        /// <param name="price"></param>
        /// <param name="buyingFee"></param>
        /// <param name="buyingFeeAddress"></param>
        public void BidToken(Address from, string symbol, BigInteger tokenID, BigInteger price, BigInteger buyingFee, Address buyingFeeAddress)
        {
            Runtime.Expect(Runtime.IsWitness(from), "invalid witness");

            var auctionID = symbol + "." + tokenID;

            Runtime.Expect(_auctionMap.ContainsKey(auctionID), "invalid auction");
            var auction = _auctionMap.Get<string, MarketAuction>(auctionID);

            Runtime.Expect(price >= 0, "price has to be >= 0");

            Runtime.Expect(buyingFee <= 5 && buyingFee >= 0, "buyingFee has to be <= 5% and >= 0%");

            Runtime.Expect(auction.StartDate < Runtime.Time, "you can not bid on an auction which has not started");


            MarketAuction auctionNew;

            if (Runtime.Time >= auction.EndDate && auction.EndDate != 0) // if auction ended trigger sale end
            {
                if (auction.Type == TypeAuction.Dutch || auction.Type == TypeAuction.Fixed || auction.CurrentBidWinner == Address.Null)
                {
                    // no winners, cancel the auction
                    CancelSale(auction.BaseSymbol, auction.TokenID);
                }
                else
                {
                    // current bid is winner
                    EndSaleInternal(auction.CurrentBidWinner, auction.BaseSymbol, auction.TokenID, auction);
                    Runtime.Notify(EventKind.OrderFilled, auction.CurrentBidWinner, new MarketEventData() { ID = auction.TokenID, BaseSymbol = auction.BaseSymbol, QuoteSymbol = auction.QuoteSymbol, Price = auction.Price, EndPrice = auction.EndPrice, Type = auction.Type });
                }
            }
            else
            {
                if (auction.Type == TypeAuction.Classic || auction.Type == TypeAuction.Reserve)
                {
                    Runtime.Expect(from != auction.Creator, "you can not bid on your own auctions");

                    if (auction.EndPrice == 0)
                    {
                        Runtime.Expect(price >= auction.Price, "bid has to be higher or equal to starting price");
                    }
                    else
                    {
                        var minBid = auction.EndPrice / 100 + auction.EndPrice;
                        if (minBid == auction.EndPrice)
                            minBid = minBid + 1;

                        Runtime.Expect(price >= minBid, "bid has to be minimum 1% higher than last bid");
                    }

                    Timestamp startDateNew = auction.StartDate;
                    Timestamp endDateNew = auction.EndDate;

                    if (auction.StartDate == 0) // if reserve auction not started
                    {
                        startDateNew = Runtime.Time;
                        endDateNew = Runtime.Time + TimeSpan.FromDays(1);
                    }
                    else if (auction.EndDate - Runtime.Time < auction.ExtensionPeriod) // extend timer if < extensionPeriod
                    {
                        endDateNew = Runtime.Time + TimeSpan.FromSeconds((double)auction.ExtensionPeriod);
                    }

                    // calculate listing & buying & refund fees
                    BigInteger combinedFees = 0;
                    BigInteger combinedRefund = 0;
                    if (auction.ListingFee != 0)
                    {
                        combinedFees += GetFee(auction.QuoteSymbol, price, auction.ListingFee);
                        combinedRefund += GetFee(auction.QuoteSymbol, auction.EndPrice, auction.ListingFee);
                    }
                    if (buyingFee != 0)
                    {
                        combinedFees += GetFee(auction.QuoteSymbol, price, buyingFee);
                    }
                    if (auction.BuyingFee != 0)
                    {
                        combinedRefund += GetFee(auction.QuoteSymbol, auction.EndPrice, auction.BuyingFee);
                    }
                    combinedFees += price;
                    combinedRefund += auction.EndPrice;

                    // transfer price + listing + buying fees to contract
                    Runtime.TransferTokens(auction.QuoteSymbol, from, Address, combinedFees);

                    // refund old bid amount + listing + buying fees to previous current winner if any
                    if (auction.CurrentBidWinner != Address.Null)
                    {
                        Runtime.TransferTokens(auction.QuoteSymbol, Address, auction.CurrentBidWinner, combinedRefund);
                    }

                    auctionNew = new MarketAuction(auction.Creator, startDateNew, endDateNew, auction.BaseSymbol, auction.QuoteSymbol, auction.TokenID, auction.Price, price, auction.ExtensionPeriod, auction.Type, auction.ListingFee, auction.ListingFeeAddress, buyingFee, buyingFeeAddress, from);
                    _auctionMap.Set(auctionID, auctionNew);
                    Runtime.Notify(EventKind.OrderBid, from, new MarketEventData() { ID = auctionNew.TokenID, BaseSymbol = auctionNew.BaseSymbol, QuoteSymbol = auctionNew.QuoteSymbol, Price = auctionNew.Price, EndPrice = auctionNew.EndPrice, Type = auctionNew.Type });
                }

                if (auction.Type == TypeAuction.Dutch)
                {
                    Runtime.Expect(from != auction.Creator, "you can not bid on your own auctions");

                    var priceDiff = auction.Price - auction.EndPrice;
                    var timeDiff = auction.EndDate - auction.StartDate;
                    var timeSinceStart = Runtime.Time - auction.StartDate;
                    var priceDiffSinceStart = timeSinceStart * priceDiff / timeDiff;
                    var currentPrice = auction.Price - priceDiffSinceStart;

                    if (currentPrice < auction.EndPrice)
                    {
                        currentPrice = auction.EndPrice;
                    }

                    // calculate listing & buying fees then transfer them to contract
                    BigInteger combinedFees = 0;
                    if (auction.ListingFee != 0)
                    {
                        combinedFees += GetFee(auction.QuoteSymbol, currentPrice, auction.ListingFee);
                    }
                    if (buyingFee != 0)
                    {
                        combinedFees += GetFee(auction.QuoteSymbol, currentPrice, buyingFee);
                    }
                    combinedFees += currentPrice;

                    Runtime.TransferTokens(auction.QuoteSymbol, from, Address, combinedFees);

                    auctionNew = new MarketAuction(auction.Creator, auction.StartDate, auction.EndDate, auction.BaseSymbol, auction.QuoteSymbol, auction.TokenID, auction.Price, currentPrice, auction.ExtensionPeriod, auction.Type, auction.ListingFee, auction.ListingFeeAddress, buyingFee, buyingFeeAddress, from);
                    _auctionMap.Set(auctionID, auctionNew);
                    EndSaleInternal(from, auction.BaseSymbol, auction.TokenID, auctionNew);
                    Runtime.Notify(EventKind.OrderBid, auctionNew.CurrentBidWinner, new MarketEventData() { ID = auctionNew.TokenID, BaseSymbol = auctionNew.BaseSymbol, QuoteSymbol = auctionNew.QuoteSymbol, Price = auctionNew.Price, EndPrice = auctionNew.EndPrice, Type = auctionNew.Type });
                    Runtime.Notify(EventKind.OrderFilled, auctionNew.CurrentBidWinner, new MarketEventData() { ID = auctionNew.TokenID, BaseSymbol = auctionNew.BaseSymbol, QuoteSymbol = auctionNew.QuoteSymbol, Price = auctionNew.Price, EndPrice = auctionNew.EndPrice, Type = auctionNew.Type });
                }

                if (auction.Type == TypeAuction.Fixed)
                {
                    Runtime.Expect(price == auction.Price, "bid has to be equal to current price");

                    auctionNew = new MarketAuction(auction.Creator, auction.StartDate, auction.EndDate, auction.BaseSymbol, auction.QuoteSymbol, auction.TokenID, auction.Price, 0, auction.ExtensionPeriod, auction.Type, auction.ListingFee, auction.ListingFeeAddress, buyingFee, buyingFeeAddress, from);
                    _auctionMap.Set(auctionID, auctionNew);
                    EndSaleInternal(from, auction.BaseSymbol, auction.TokenID, auctionNew);
                    Runtime.Notify(EventKind.OrderFilled, auctionNew.CurrentBidWinner, new MarketEventData() { ID = auctionNew.TokenID, BaseSymbol = auctionNew.BaseSymbol, QuoteSymbol = auctionNew.QuoteSymbol, Price = auctionNew.Price, EndPrice = auctionNew.EndPrice, Type = auctionNew.Type });
                }
            }
        }

        /*public void MakeOffer(Address from, string symbol, BigInteger tokenID, BigInteger price, BigInteger buyingFee,
            Address buyingFeeAddress)
        {
            Runtime.Expect(Runtime.IsWitness(from), "invalid witness");
            Runtime.Expect(Runtime.TokenExists(symbol), "invalid token");
            Runtime.Expect(Runtime.TokenExists(symbol), "invalid token");
            var token = Runtime.GetToken(symbol);
            Runtime.Expect(!token.Flags.HasFlag(TokenFlags.Fungible), "token must be non-fungible");
            Runtime.Expect(Runtime.TokenExists(symbol), "invalid token");
        }*/

        /// <summary>
        /// Sell a token on the market
        /// </summary>
        /// <param name="from"></param>
        /// <param name="baseSymbol"></param>
        /// <param name="quoteSymbol"></param>
        /// <param name="tokenID"></param>
        /// <param name="price"></param>
        /// <param name="endDate"></param>
        public void SellToken(Address from, string baseSymbol, string quoteSymbol, BigInteger tokenID, BigInteger price, Timestamp endDate)
        {
            Runtime.Expect(Runtime.IsWitness(from), "invalid witness");
            Runtime.Expect(endDate > Runtime.Time, "invalid end date");

            var maxAllowedDate = Runtime.Time + TimeSpan.FromDays(30);
            Runtime.Expect(endDate <= maxAllowedDate, "end date is too distant, max: " + maxAllowedDate + ", received: " + endDate);

            Runtime.Expect(Runtime.TokenExists(quoteSymbol), "invalid quote token");
            var quoteToken = Runtime.GetToken(quoteSymbol);
            Runtime.Expect(quoteToken.Flags.HasFlag(TokenFlags.Fungible), "quote token must be fungible");

            Runtime.Expect(Runtime.TokenExists(baseSymbol), "invalid base token");
            var baseToken = Runtime.GetToken(baseSymbol);
            Runtime.Expect(!baseToken.Flags.HasFlag(TokenFlags.Fungible), "base token must be non-fungible");

            var nft = Runtime.ReadToken(baseSymbol, tokenID);
            Runtime.Expect(nft.CurrentChain == Runtime.Chain.Name, "token not currently in this chain");
            Runtime.Expect(nft.CurrentOwner == from, "invalid owner");

            Runtime.Expect(price >= 0, "price has to be >= 0");

            Runtime.TransferToken(baseToken.Symbol, from, Address, tokenID);

            var auction = new MarketAuction(from, Runtime.Time, endDate, baseSymbol, quoteSymbol, tokenID, price, 0, 0, TypeAuction.Fixed, 0, Address.Null, 0, Address.Null, Address.Null);
            var auctionID = baseSymbol + "." + tokenID;
            _auctionMap.Set(auctionID, auction);
            _auctionIds.Set(auctionID, auctionID);

            Runtime.Notify(EventKind.OrderCreated, from, new MarketEventData() { ID = tokenID, BaseSymbol = baseSymbol, QuoteSymbol = quoteSymbol, Price = price, EndPrice = 0, Type = TypeAuction.Fixed });
        }

        /// <summary>
        /// Buy a token on the market
        /// </summary>
        /// <param name="from"></param>
        /// <param name="symbol"></param>
        /// <param name="tokenID"></param>
        public void BuyToken(Address from, string symbol, BigInteger tokenID)
        {
            Runtime.Expect(Runtime.IsWitness(from), "invalid witness");

            var auctionID = symbol + "." + tokenID;

            Runtime.Expect(_auctionMap.ContainsKey(auctionID), "invalid auction");
            var auction = _auctionMap.Get<string, MarketAuction>(auctionID);

            Runtime.Expect(auction.Type == TypeAuction.Fixed, "BuyToken only supports fixed price listings");

            Runtime.Expect(auction.StartDate <= Runtime.Time, "you can not buy a nft for which the sale has not started");

            if (auction.Creator == from)
            {
                CancelSale(symbol, tokenID);
                return;
            }

            EndSaleInternal(from, symbol, tokenID, auction);

            Runtime.Notify(EventKind.OrderFilled, from, new MarketEventData() { ID = auction.TokenID, BaseSymbol = auction.BaseSymbol, QuoteSymbol = auction.QuoteSymbol, Price = auction.Price, EndPrice = 0, Type = auction.Type });
        }

        /// <summary>
        /// Cancel a sale on the market
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="tokenID"></param>
        public void CancelSale(string symbol, BigInteger tokenID)
        {
            var auctionID = symbol + "." + tokenID;

            Runtime.Expect(_auctionMap.ContainsKey(auctionID), "invalid auction");
            var auction = _auctionMap.Get<string, MarketAuction>(auctionID);

            var from = auction.Creator;

            if (Runtime.Time < auction.EndDate)
            {
                Runtime.Expect(Runtime.IsWitness(from), "invalid witness");
            }

            if (Runtime.ProtocolVersion < 8)
            {
                if (auction.Type == TypeAuction.Reserve)
                {
                    Runtime.Expect(auction.EndDate == 0, "reserve auction can not be cancelled once it started");
                }
                else if (auction.Type != TypeAuction.Fixed)
                {
                    Runtime.Expect(Runtime.Time < auction.StartDate || Runtime.Time > auction.EndDate, "auction can not be cancelled once it started, until it ends");
                }
            }
            else
            {
                if (auction.Type != TypeAuction.Fixed)
                {
                    Runtime.Expect(auction.CurrentBidWinner == Address.Null, "auction can not be cancelled once it has received bids");
                }
            }

            EndSaleInternal(from, symbol, tokenID, auction);
            Runtime.Notify(EventKind.OrderCancelled, from, new MarketEventData() { ID = auction.TokenID, BaseSymbol = auction.BaseSymbol, QuoteSymbol = auction.QuoteSymbol, Price = auction.Price, EndPrice = 0, Type = auction.Type });
        }

        /// <summary>
        /// End a sale on the market
        /// </summary>
        /// <param name="from"></param>
        /// <param name="symbol"></param>
        /// <param name="tokenID"></param>
        /// <param name="auction"></param>
        private void EndSaleInternal(Address from, string symbol, BigInteger tokenID, MarketAuction auction)
        {
            Runtime.Expect(Runtime.TokenExists(auction.BaseSymbol), "invalid base token");
            var baseToken = Runtime.GetToken(auction.BaseSymbol);
            Runtime.Expect(!baseToken.Flags.HasFlag(TokenFlags.Fungible), "token must be non-fungible");

            var nft = Runtime.ReadToken(symbol, tokenID);
            Runtime.Expect(nft.CurrentChain == Runtime.Chain.Name, "token not currently in this chain");
            Runtime.Expect(nft.CurrentOwner == Address, "invalid owner");

            // if not a cancellation
            if (auction.Creator != from)
            {
                Runtime.Expect(Runtime.TokenExists(auction.QuoteSymbol), "invalid quote token");
                var quoteToken = Runtime.GetToken(auction.QuoteSymbol);
                Runtime.Expect(quoteToken.Flags.HasFlag(TokenFlags.Fungible), "quote token must be fungible");

                var price = auction.Price;

                // if new auctions type, use EndPrice
                if (auction.Type != TypeAuction.Fixed)
                {
                    price = auction.EndPrice;
                }

                // calculate fees on full amount
                var listFee = GetFee(auction.QuoteSymbol, price, auction.ListingFee);
                var buyFee = GetFee(auction.QuoteSymbol, price, auction.BuyingFee);

                var finalAmount = price;

                if (auction.Type == TypeAuction.Fixed) // if fixed type auction, transfer to contract done in EndSaleInternal to account for original BuyToken and new BidToken
                {
                    // calculate total amount then transfer them to contract
                    BigInteger combinedFees = buyFee + listFee + price;

                    // check that we have enough balance first
                    var balance = Runtime.GetBalance(quoteToken.Symbol, from);

                    Runtime.Expect(balance >= combinedFees, "not enough balance to pay the fees");

                    Runtime.TransferTokens(auction.QuoteSymbol, from, Address, combinedFees);
                }

                // handle royalties
                var nftSymbol = auction.BaseSymbol;
                var nftData = Runtime.ReadToken(nftSymbol, tokenID);
                var series = Runtime.GetTokenSeries(nftSymbol, nftData.SeriesID);

                var royaltyProperty = new ContractMethod("getRoyalties", VMType.Number, -1);

                if (series.ABI.Implements(royaltyProperty))
                {
                    var nftRoyalty = Runtime.CallNFT(nftSymbol, nftData.SeriesID, royaltyProperty, tokenID).AsNumber();
                    if (nftRoyalty > 50)
                    {
                        nftRoyalty = 50; // we don't allow more than 50% royalties fee
                    }
                    var royaltyFee = finalAmount * nftRoyalty / 100;
                    Runtime.TransferTokens(quoteToken.Symbol, Address, nftData.Creator, royaltyFee);
                    finalAmount -= royaltyFee;
                }

                // transfer sale amount
                Runtime.TransferTokens(quoteToken.Symbol, Address, auction.Creator, finalAmount);

                // transfer listing fees
                if (listFee != 0)
                {
                    Runtime.TransferTokens(quoteToken.Symbol, Address, auction.ListingFeeAddress, listFee);
                }

                // transfer buying fees
                if (buyFee != 0)
                {
                    Runtime.TransferTokens(quoteToken.Symbol, Address, auction.BuyingFeeAddress, buyFee);
                }

            }

            // send nft to buyer
            Runtime.TransferToken(baseToken.Symbol, Address, from, auction.TokenID);

            var auctionID = symbol + "." + tokenID;
            _auctionMap.Remove(auctionID);
            _auctionIds.Remove(auctionID);
        }

        /// <summary>
        /// Get the fee amount
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="price"></param>
        /// <param name="fee"></param>
        /// <returns></returns>
        private BigInteger GetFee(string symbol, BigInteger price, BigInteger fee)
        {
            if (fee <= 0) return 0;

            var listFee = price * fee / 100;
            var quoteToken = Runtime.GetToken(symbol);
            if (!quoteToken.Flags.HasFlag(TokenFlags.Divisible) && listFee == 0)
            {
                listFee = 1;
            }
            return listFee;
        }

        /// <summary>
        /// Get all auctions
        /// </summary>
        /// <returns></returns>
        public MarketAuction[] GetAuctions()
        {
            var ids = _auctionIds.AllValues<string>();
            var auctions = new MarketAuction[ids.Length];
            for (int i = 0; i < auctions.Length; i++)
            {
                auctions[i] = _auctionMap.Get<string, MarketAuction>(ids[i]);
            }
            return auctions;
        }

        /// <summary>
        /// Check if the address is a seller
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsSeller(Address target)
        {
            var ids = _auctionIds.AllValues<string>();
            for (int i = 0; i < ids.Length; i++)
            {
                var auction = _auctionMap.Get<string, MarketAuction>(ids[i]);

                if (auction.Creator == target)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if there's an auction for the token
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="tokenID"></param>
        /// <returns></returns>
        public bool HasAuction(string symbol, BigInteger tokenID)
        {
            var auctionID = symbol + "." + tokenID;
            return _auctionMap.ContainsKey(auctionID);
        }

        /// <summary>
        /// Get the auction for the token
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="tokenID"></param>
        /// <returns></returns>
        public MarketAuction GetAuction(string symbol, BigInteger tokenID)
        {
            var auctionID = symbol + "." + tokenID;

            Runtime.Expect(_auctionMap.ContainsKey(auctionID), "invalid auction");
            var auction = _auctionMap.Get<string, MarketAuction>(auctionID);
            return auction;
        }
    }
}
