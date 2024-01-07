using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace Common
{
    public static class ListExtensions
    {
        public static IEnumerable<T> Rotate<T>(this List<T> list, int offset)
        {
            return list.Skip(offset).Concat(list.Take(offset));
        }
    }

    [PublicAPI]
    public class BoardDto
    {
        public string Event { get; set; }
        public DateTime? Date { get; set; }
        public int BoardNumber { get; set; }
        public Player Dealer { get; set; }
        public string Vulnerable { get; set; }
        public Dictionary<Player, string> Deal { get; set; }
        public Player Declarer { get; set; }
        public Auction Auction { get; set; }

        public string Description { get; set; }
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"""[Event "{Event}"]""");
            if (Date != null)
                sb.AppendLine($"""[Date "{Date:yyyy.MM.dd}"]""");
            sb.AppendLine($"""[Board "{BoardNumber}"]""");
            sb.AppendLine($"""[Dealer "{Util.GetPlayerString(Dealer)}"]""");
            sb.AppendLine($"""[Vulnerable "{Vulnerable}"]""");
            if (!string.IsNullOrWhiteSpace(Description))
                sb.AppendLine($"""[Description "{Description}"]""");
            var sortedDeal = new SortedDictionary<Player, string>(Deal);
            sb.AppendLine($"""[Deal "{Util.GetPlayerString(Dealer)}:{string.Join(' ', sortedDeal.ToList().Rotate((int)Dealer)
                .Select(x => x.Value.Replace(',', '.')))}"]""");
            if (Declarer != Player.UnKnown)
                sb.AppendLine($"""[Declarer "{Util.GetPlayerString(Declarer)}"]""");
            if (Auction == null) return sb.ToString();
            sb.AppendLine($"""[Auction "{Util.GetPlayerString(Dealer)}"]""");
            foreach (var biddingRound in Auction.Bids.Values)
                    sb.AppendLine(string.Join('\t', biddingRound.Values.Select(x => x.ToStringASCII())));
            return sb.ToString();
        }

        public static BoardDto FromString(string pbnString)
        {
            var board = new BoardDto();
            using var sr = new StringReader(pbnString);
            while (sr.ReadLine() is { } line)
            {
                var firstQuoteIndex = line.IndexOf('"');
                if (firstQuoteIndex == -1)
                    continue;
                var key = line[1..firstQuoteIndex].Trim();
                var value = line[firstQuoteIndex..].Trim().TrimEnd(']').Trim('"');

                switch (key)
                {
                    case "Event":
                        board.Event = value;
                        break;
                    case "Date":
                        if (DateTime.TryParse(value, out var date))
                            board.Date = date;
                        break;
                    case "Board":
                        if (int.TryParse(value, out var boardNumber))
                            board.BoardNumber = boardNumber;
                        break;
                    case "Dealer":
                        board.Dealer = value.Length > 0 ? Util.GetPlayer(value[0]) : Player.UnKnown;
                        break;
                    case "Vulnerable":
                        board.Vulnerable = value;
                        break;
                    case "Deal":
                        var firstPlayer = value.Length > 0 ? Util.GetPlayer(value[0]) : Player.UnKnown;
                        board.Deal = value.Replace('.', ',')[2..].Split(" ").ToList().Rotate(4 - (int)firstPlayer).Select((suit, index) => (suit, Index: index))
                            .ToDictionary(x => (Player)x.Index, x => x.suit);
                        break;
                    case "Declarer":
                        board.Declarer = value.Length > 0 ? Util.GetPlayer(value[0]) : Player.UnKnown;
                        break;
                    case "Auction":
                        {
                            board.Auction = new Auction();
                            var biddingRound = 1;
                            while ((line = sr.ReadLine()) != null && line[0] != '[')
                            {
                                var bids = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries).Where(x => !x.StartsWith('=')).ToList().Select((bid, index) => (bid, index));
                                board.Auction.Bids[biddingRound] = bids.ToDictionary(x => (Player)x.index, x => Bid.FromStringASCII(x.bid));
                                biddingRound++;
                            }
                        }
                        break;
                    case "Description":
                        board.Description = value;
                        break;
                }
            }
            return board;
        }
    }
}
