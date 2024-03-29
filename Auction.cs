﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
// ReSharper disable InconsistentNaming

namespace Common
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [PublicAPI]
    public class Auction
    {
        public Player CurrentPlayer { get; set; }
        public int CurrentBiddingRound { get; set; }

        public Dictionary<int, Dictionary<Player, Bid>> Bids { get; set; } = [];

        public Bid currentContract = Bid.PassBid;
        public bool responderHasSignedOff;
        public BidType currentBidType = BidType.pass;

        private string DebuggerDisplay => GetAuctionAll(Environment.NewLine);

        public string GetPrettyAuction(string separator)
        {
            return Bids.Aggregate(new StringBuilder(), (sb, kvp) => sb.AppendJoin(" ", kvp.Value.Where(p => new [] { Player.North, Player.South }.Contains(p.Key))
                .Select(x => x.Value).Where(y => y != Bid.AlignBid)).Append(separator), sb => sb.ToString());
        }

        public string GetAuctionAll(string separator)
        {
            return Bids.Aggregate(new StringBuilder(), (sb, kvp) => sb.AppendJoin(" ", kvp.Value.Values.Where(y => y != Bid.AlignBid)).Append(separator), sb => sb.ToString());
        }

        public Player GetDeclarer()
        {
            foreach (var biddingRound in Bids.Values)
            {
                foreach (var bid in biddingRound)
                {
                    if (bid.Value.bidType == BidType.bid && bid.Value.Suit == currentContract.Suit)
                        return bid.Key;
                }
            }
            return Player.UnKnown;
        }

        public Player GetDeclarer(Suit suit)
        {
            foreach (var biddingRound in Bids.Values)
            {
                foreach (var bid in biddingRound)
                {
                    if (bid.Value.bidType == BidType.bid && bid.Value.Suit == suit)
                        return bid.Key;
                }
            }
            return Player.UnKnown;
        }

        public Player GetDeclarerOrNorth(Suit suit)
        {
            var declarer = GetDeclarer(suit);
            return declarer == Player.UnKnown ? Player.North : declarer;
        }


        public void AddBid(Bid bid)
        {
            if (!Bids.ContainsKey(CurrentBiddingRound))
                Bids[CurrentBiddingRound] = [];
            Bids[CurrentBiddingRound][CurrentPlayer] = bid;

            if (CurrentPlayer == Player.South)
            {
                CurrentPlayer = Player.West;
                ++CurrentBiddingRound;
            }
            else
                ++CurrentPlayer;

            if (bid.bidType == BidType.bid)
                currentContract = bid;
            if (bid.bidType != BidType.pass)
                currentBidType = bid.bidType;
        }

        public void Clear(Player dealer)
        {
            Bids.Clear();
            CurrentPlayer = dealer;
            CurrentBiddingRound = 1;
            currentContract = Bid.PassBid;

            var player = Player.West;
            while (player != dealer)
            {
                if (!Bids.ContainsKey(1))
                    Bids[1] = new Dictionary<Player, Bid>();
                Bids[1][player] = Bid.AlignBid;
                player++;
            }
        }

        public string GetBidsAsString(Player player)
        {
            return Bids.Where(x => x.Value.ContainsKey(player)).Aggregate(string.Empty, (current, biddingRound) => current + biddingRound.Value[player]);
        }

        public string GetBidsAsStringASCII()
        {
            return Bids.SelectMany(x => x.Value.Values)
                .SkipWhile(y => y == Bid.PassBid || y == Bid.AlignBid)
                .Aggregate(string.Empty, (current, bid) => current + bid.ToStringASCII());
        }

        public IEnumerable<Bid> GetBids(Player player)
        {
            return Bids.Where(x => x.Value.ContainsKey(player)).Select(x => x.Value[player]);
        }

        public void SetBids(Player player, IEnumerable<Bid> newBids)
        {
            Bids.Clear();
            var biddingRound = 1;
            foreach (var bid in newBids)
            {
                Bids[biddingRound] = new Dictionary<Player, Bid>(new List<KeyValuePair<Player, Bid>> { new(player, bid) });
                biddingRound++;
            }
        }

        public void CheckConsistency()
        {
            var bidsSouth = GetBids(Player.South).ToList();
            var previousBid = bidsSouth.First();
            foreach (var bid in bidsSouth.Skip(1))
            {
                if (bid.bidType != BidType.bid) continue;
                if (bid <= previousBid)
                    throw new InvalidOperationException("Bid is lower");
                previousBid = bid;
            }
        }

        public Bid GetRelativeBid(Bid currentBid, int level, Player player)
        {
            var biddingRound = Bids.Single(bid => bid.Value.Any(y => y.Value == currentBid));
            return biddingRound.Key + level < 1 ? default : Bids[biddingRound.Key + level].GetValueOrDefault(player);
        }

        public bool IsEndOfBidding()
        {
            var allBids = Bids.SelectMany(x => x.Value).Select(y => y.Value).Where(z => z.bidType != BidType.align).ToList();
            return (allBids.Count == 4 && allBids.All(bid => bid == Bid.PassBid)) ||
                allBids.Count > 3 && allBids.TakeLast(3).Count() == 3 && allBids.TakeLast(3).All(bid => bid == Bid.PassBid);
        }

        public bool BidIsPossible(Bid bid)
        {
            return bid.bidType switch
            {
                BidType.pass => true,
                BidType.bid => currentContract.bidType != BidType.bid || currentContract < bid,
                BidType.dbl => currentBidType == BidType.bid &&
                    !Util.IsSameTeam(CurrentPlayer, GetDeclarer()),
                BidType.rdbl => currentBidType == BidType.dbl &&
                    Util.IsSameTeam(CurrentPlayer, GetDeclarer()),
                _ => throw new InvalidEnumArgumentException(nameof(bid.bidType), (int)bid.bidType, typeof(BidType)),
            };
        }
    }
}
