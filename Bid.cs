using System;
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace Common
{
    public enum BidType
    {
        bid,
        pass,
        dbl,
        rdbl,
        invalid,
        align
    }

    [PublicAPI]
    public class Bid : IEquatable<Bid>, IComparable<Bid>
    {
        public static readonly Bid AlignBid = new(BidType.align);
        public static readonly Bid InvalidBid = new(BidType.invalid);
        public static readonly Bid PassBid = new(BidType.pass);
        public static readonly Bid Dbl = new(BidType.dbl);
        public static readonly Bid Rdbl = new(BidType.rdbl);

        public readonly BidType bidType;

        public int Rank { get; }

        public Suit Suit { get; }
        public string description = string.Empty;

        public Bid(int rank, Suit suit)
        {
            bidType = BidType.bid;
            this.Suit = suit;
            this.Rank = rank;
        }

        public Bid(BidType bidType)
        {
            this.bidType = bidType;
            Suit = default;
            Rank = default;
        }

        public override string ToString()
        {
            return bidType switch
            {
                BidType.bid => Rank + Util.GetSuitDescription(Suit),
                BidType.pass => "Pass",
                BidType.dbl => "Dbl",
                BidType.rdbl => "Rdbl",
                BidType.invalid => "Invalid",
                BidType.align => "",
                _ => throw new ArgumentOutOfRangeException(nameof(bidType)),
            };
        }

        public string ToStringASCII()
        {
            return bidType switch
            {
                BidType.bid => Rank + Util.GetSuitDescriptionASCII(Suit),
                BidType.pass => "Pass",
                BidType.dbl => "X",
                BidType.rdbl => "XX",
                BidType.align => "",
                _ => throw new ArgumentOutOfRangeException(nameof(bidType)),
            };
        }

        public static Bid FromStringASCII(string bid)
        {
            return bid switch
            {
                "Pass" => PassBid,
                "X" => Dbl,
                "XX" => Rdbl,
                _ => new Bid(int.Parse(bid.Substring(0, 1)), Util.GetSuitASCII(bid[1..])),
            };
        }

        public static Bid GetBid(int bidId)
        {
            return bidId switch
            {
                -1 => Dbl,
                0 => PassBid,
                _ => new Bid((bidId - 1) / 5 + 1, (Suit)((bidId - 1) % 5))
            };
        }

        public static int GetBidId(Bid bid)
        {
            return bid == Dbl ? -1 : bid == PassBid ? 0 : ((bid.Rank - 1) * 5) + (int)bid.Suit + 1;
        }

        public static Bid NextBid(Bid bid)
        {
            if (bid == PassBid)
                return new Bid(1, Suit.Clubs);
            return bid.Suit == Suit.NoTrump ? new Bid(bid.Rank + 1, Suit.Clubs) : new Bid(bid.Rank, bid.Suit + 1);
        }

        public static Bid GetGameContractSafe(Suit trumpSuit, Bid currentBid, bool canUseNextBid)
        {
            var bid = GetGameContract(trumpSuit, currentBid, canUseNextBid);
            return bid == InvalidBid ? PassBid : bid;
        }

        public static Bid GetGameContract(Suit trumpSuit, Bid currentBid, bool canUseNextBid)
        {
            var bid = trumpSuit switch
            {
                Suit.Spades => new Bid(4, Suit.Spades),
                Suit.Hearts => new Bid(4, Suit.Hearts),
                Suit.Diamonds => new Bid(5, Suit.Diamonds),
                Suit.Clubs => new Bid(5, Suit.Clubs),
                Suit.NoTrump => new Bid(3, Suit.NoTrump),
                _ => throw new ArgumentException(nameof(trumpSuit)),
            };
            var contract = CheapestContract(currentBid, bid, canUseNextBid);
            return contract.Rank <= 5 ? contract : InvalidBid;
        }

        private static Bid CheapestContract(Bid currentBid, Bid bid, bool canUseNextBid)
        {
            if (currentBid.Suit == bid.Suit && currentBid.Rank < bid.Rank)
                return bid;
            if (currentBid.Suit == bid.Suit)
                return PassBid;
            if (currentBid + (canUseNextBid ? 0 : 1) < bid)
                return bid;
            return bid + (5 * (((currentBid + 1 - bid) / 5) + 1));
        }

        public static Bid CheapestContract(Bid currentBid, Suit suit)
        {
            if (currentBid.Suit == suit)
                return PassBid;
            var bid = new Bid(currentBid.Rank + (currentBid.Suit > suit ? 1 : 0), suit);
            return bid;
        }

        public static Bid GetBestContract(ExpectedContract expectedContract, Suit item1, Bid currentBid)
        {
            return expectedContract switch
            {
                ExpectedContract.Game => GetGameContract(item1, currentBid, false),
                ExpectedContract.SmallSlam => new Bid(6, item1),
                ExpectedContract.GrandSlam => new Bid(7, item1),
                _ => throw new ArgumentException(nameof(expectedContract)),
            };
        }

        // Operators
        public bool Equals(Bid other) => !(other is null) && Suit == other.Suit && bidType == other.bidType && Rank == other.Rank;
        public override bool Equals(object obj) => obj is Bid other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(bidType, Rank, Suit);

        public int CompareTo(Bid other)
        {
            var bidTypeComparison = bidType.CompareTo(other.bidType);
            if (bidTypeComparison != 0) return bidTypeComparison;

            var rankComparison = Rank.CompareTo(other.Rank);
            if (rankComparison != 0) return rankComparison;

            return Suit.CompareTo(other.Suit);
        }
        public static bool operator ==(Bid a, Bid b) => a?.Equals(b) ?? b is null;
        public static bool operator !=(Bid a, Bid b) => !(a == b);
        public static bool operator <(Bid a, Bid b) => a.CompareTo(b) < 0;
        public static bool operator >(Bid a, Bid b) => a.CompareTo(b) > 0;
        public static bool operator <=(Bid a, Bid b) => a.CompareTo(b) <= 0;
        public static bool operator >=(Bid a, Bid b) => a.CompareTo(b) >= 0;
        public static int operator -(Bid a, Bid b) => GetBidId(a) - GetBidId(b);
        public static Bid operator -(Bid a, int i) => a.bidType == BidType.bid ? DecreaseBid(a, i) : a;
        public static Bid operator --(Bid a) => a.bidType == BidType.bid ? DecreaseBid(a, 1) : a;
        public static Bid operator +(Bid a, int i) => a.bidType == BidType.bid ? IncreaseBid(a, i) : a;
        public static Bid operator ++(Bid a) => a.bidType == BidType.bid ? IncreaseBid(a, 1) : a;

        private static Bid IncreaseBid(Bid a, int i)
        {
            var bid = GetBid(GetBidId(a) + i);
            bid.description = a.description;
            return bid;
        }

        private static Bid DecreaseBid(Bid a, int i)
        {
            var bid = GetBid(GetBidId(a) - i);
            bid.description = a.description;
            return bid;
        }
    }
}