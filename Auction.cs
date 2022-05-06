using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Common
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Auction
    {
        public Player CurrentPlayer { get; set; }
        public int CurrentBiddingRound { get; set; }

        public Dictionary<int, Dictionary<Player, Bid>> bids { get; set; } = new Dictionary<int, Dictionary<Player, Bid>>();
        public Bid currentContract = Bid.PassBid;
        public bool responderHasSignedOff = false;
        public BidType currentBidType = BidType.pass;

        private string DebuggerDisplay
        {
            get { return GetAuctionAll(Environment.NewLine); }
        }

        public string GetPrettyAuction(string separator)
        {
            return bids.Aggregate(new StringBuilder(), (sb, kvp) => sb.AppendJoin(" ", kvp.Value.Where(p => new [] { Player.North, Player.South }.Contains(p.Key))
                .Select(x => x.Value).Where(y => y != Bid.AlignBid)).Append(separator), sb => sb.ToString());
        }

        public string GetAuctionAll(string separator)
        {
            return bids.Aggregate(new StringBuilder(), (sb, kvp) => sb.AppendJoin(" ", kvp.Value.Values.Where(y => y != Bid.AlignBid)).Append(separator), sb => sb.ToString());
        }

        public Player GetDeclarer()
        {
            foreach (var biddingRoud in bids.Values)
            {
                foreach (var bid in biddingRoud)
                {
                    if (bid.Value.bidType == BidType.bid && bid.Value.suit == currentContract.suit)
                        return bid.Key;
                }
            }
            return Player.UnKnown;
        }

        public Player GetDeclarer(Suit suit)
        {
            foreach (var biddingRoud in bids.Values)
            {
                foreach (var bid in biddingRoud)
                {
                    if (bid.Value.bidType == BidType.bid && bid.Value.suit == suit)
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
            if (!bids.ContainsKey(CurrentBiddingRound))
                bids[CurrentBiddingRound] = new Dictionary<Player, Bid>();
            bids[CurrentBiddingRound][CurrentPlayer] = bid;

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
            bids.Clear();
            CurrentPlayer = dealer;
            CurrentBiddingRound = 1;
            currentContract = Bid.PassBid;

            var player = Player.West;
            while (player != dealer)
            {
                if (!bids.ContainsKey(1))
                    bids[1] = new Dictionary<Player, Bid>();
                bids[1][player] = Bid.AlignBid;
                player++;
            }
        }

        public string GetBidsAsString(Player player)
        {
            return bids.Where(x => x.Value.ContainsKey(player)).Aggregate(string.Empty, (current, biddingRound) => current + biddingRound.Value[player]);
        }

        public string GetBidsAsStringASCII()
        {
            return bids.SelectMany(x => x.Value.Values)
                .SkipWhile(y => y == Bid.PassBid || y == Bid.AlignBid)
                .Aggregate(string.Empty, (string current, Bid bid) => current + bid.ToStringASCII());
        }

        public IEnumerable<Bid> GetBids(Player player)
        {
            return bids.Where(x => x.Value.ContainsKey(player)).Select(x => x.Value[player]);
        }

        public void SetBids(Player player, IEnumerable<Bid> newBids)
        {
            bids.Clear();
            var biddingRound = 1;
            foreach (var bid in newBids)
            {
                bids[biddingRound] = new Dictionary<Player, Bid>(new List<KeyValuePair<Player, Bid>> { new KeyValuePair<Player, Bid>(player, bid) });
                biddingRound++;
            }
        }

        public bool Used4ClAsRelay()
        {
            var previousBiddingRound = bids.First();
            foreach (var biddingRound in bids.Skip(1))
            {
                if (biddingRound.Value.TryGetValue(Player.North, out var bid) && bid == Bid.fourClubBid)
                    return previousBiddingRound.Value[Player.South] == Bid.threeSpadeBid;

                previousBiddingRound = biddingRound;
            }
            return false;
        }

        public void CheckConsistency()
        {
            var bidsSouth = GetBids(Player.South);
            var previousBid = bidsSouth.First();
            foreach (var bid in bidsSouth.Skip(1))
            {
                if (bid.bidType == BidType.bid)
                {
                    if (bid <= previousBid)
                        throw new InvalidOperationException("Bid is lower");
                    previousBid = bid;
                }
            }
        }

        public bool IsEndOfBidding()
        {
            var allBids = bids.SelectMany(x => x.Value).Select(y => y.Value).Where(z => z.bidType != BidType.align);
            return (allBids.Count() == 4 && allBids.All(bid => bid == Bid.PassBid)) ||
                allBids.Count() > 3 && allBids.TakeLast(3).Count() == 3 && allBids.TakeLast(3).All(bid => bid == Bid.PassBid);
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
                _ => throw new InvalidEnumArgumentException(nameof(bid.bidType), (int)bid.bidType, null),
            };
        }
    }
}
