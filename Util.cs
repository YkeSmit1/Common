using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Common
{
    public enum Player
    {
        West,
        North,
        East,
        South,
        UnKnown
    };

    public enum Suit
    {
        Clubs = 0,
        Diamonds = 1,
        Hearts = 2,
        Spades = 3,
        NoTrump = 4
    }

    public enum Face
    {
        Ace,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King
    }

    public enum ExpectedContract
    {
        Game,
        SmallSlam,
        GrandSlam,
    }

    public static class Util
    {
        public static string GetSuitDescription(Suit suit)
        {
            return suit switch
            {
                Suit.Clubs => "♣", // \u2663
                Suit.Diamonds => "♦", // \u2666
                Suit.Hearts => "♥", // \u2665
                Suit.Spades => "♠", // \u2660
                Suit.NoTrump => "NT",
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null),
            };
        }

        public static string GetSuitDescriptionASCII(Suit suit)
        {
            return suit switch
            {
                Suit.Clubs => "C",
                Suit.Diamonds => "D",
                Suit.Hearts => "H",
                Suit.Spades => "S",
                Suit.NoTrump => "NT",
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null),
            };
        }

        public static Suit GetSuit(string suit)
        {
            return suit switch
            {
                "♣" => Suit.Clubs, // \u2663
                "♦" => Suit.Diamonds, // \u2666
                "♥" => Suit.Hearts, // \u2665
                "♠" => Suit.Spades, // \u2660
                "NT" => Suit.NoTrump,
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null),
            };
        }

        public static Suit GetSuitASCII(string suit)
        {
            return suit switch
            {
                "C" => Suit.Clubs,
                "D" => Suit.Diamonds,
                "H" => Suit.Hearts,
                "S" => Suit.Spades,
                "NT" => Suit.NoTrump,
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null),
            };
        }

        public static char GetFaceDescription(Face face)
        {
            return face switch
            {
                Face.Ace => 'A',
                Face.Two => '2',
                Face.Three => '3',
                Face.Four => '4',
                Face.Five => '5',
                Face.Six => '6',
                Face.Seven => '7',
                Face.Eight => '8',
                Face.Nine => '9',
                Face.Ten => 'T',
                Face.Jack => 'J',
                Face.Queen => 'Q',
                Face.King => 'K',
                _ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
            };
        }

        public static bool GetHasTrumpQueen(string handsString, Suit playingSuit)
        {
            var hand = handsString.Split(',');
            return hand[3 - (int)playingSuit].Any(x => x == 'Q');
        }

        public static int GetKeyCards(string handsString, Suit playingSuit)
        {
            var hand = handsString.Split(',');
            var trumpKing = hand[3 - (int)playingSuit].Any(x => x == 'K') ? 1 : 0;
            var keyCards = handsString.Count(x => x == 'A') + trumpKing;
            return keyCards;
        }

        public static Face GetFaceFromDescription(char c)
        {
            return c switch
            {
                'A' => Face.Ace,
                '2' => Face.Two,
                '3' => Face.Three,
                '4' => Face.Four,
                '5' => Face.Five,
                '6' => Face.Six,
                '7' => Face.Seven,
                '8' => Face.Eight,
                '9' => Face.Nine,
                'T' => Face.Ten,
                'J' => Face.Jack,
                'Q' => Face.Queen,
                'K' => Face.King,
                _ => throw new ArgumentOutOfRangeException(nameof(c), c, null),
            };
        }

        public static bool IsSameTeam(Player player1, Player player2)
        {
            return Math.Abs(player1 - player2) == 2 || (player1 == player2) &&
                (player2 != Player.UnKnown || player1 != Player.UnKnown);
        }

        public static (Suit, int) GetLongestSuit(string northHand, string southHand)
        {
            var suitLengthNorth = northHand.Split(',').Select(x => x.Length);
            Debug.Assert(suitLengthNorth.Count() == 4);
            var suitLengthSouth = southHand.Split(',').Select(x => x.Length);
            Debug.Assert(suitLengthSouth.Count() == 4);
            var suitLengthNS = suitLengthNorth.Zip(suitLengthSouth, (x, y) => x + y);
            var maxSuitLength = suitLengthNS.Max();
            var longestSuit = suitLengthNS.ToList().IndexOf(maxSuitLength);
            return ((Suit)(3 - longestSuit), maxSuitLength);
        }

        public static (Suit, int) GetLongestSuitShape(string northHand, string southHandShape)
        {
            var suitLengthNorth = northHand.Split(',').Select(x => x.Length);
            Debug.Assert(suitLengthNorth.Count() == 4);
            var suitLengthSouth = southHandShape.Select(x => int.Parse(x.ToString()));
            Debug.Assert(suitLengthSouth.Count() == 4);
            var suitLengthNS = suitLengthNorth.Zip(suitLengthSouth, (x, y) => x + y);
            var maxSuitLength = suitLengthNS.Max();
            var longestSuit = suitLengthNS.ToList().IndexOf(maxSuitLength);
            return ((Suit)(3 - longestSuit), maxSuitLength);
        }


        public static IEnumerable<(Suit suit, int length)> GetSuitsWithFit(string northHand, string southHand)
        {
            var suitLengthNorth = northHand.Split(',').Select(x => x.Length);
            Debug.Assert(suitLengthNorth.Count() == 4);
            var suitLengthSouth = southHand.Split(',').Select(x => x.Length);
            Debug.Assert(suitLengthSouth.Count() == 4);
            var suitLengthNS = suitLengthNorth.Zip(suitLengthSouth, (x, y) => x + y);
            return suitLengthNS.Select((length, index) => ((Suit)(3 - index), length)).OrderByDescending(x => x.length).TakeWhile(x => x.length >= 8);
        }

        public static int GetNumberOfTrumps(Suit suit, string northHand, string southHand)
        {
            Debug.Assert(suit != Suit.NoTrump);
            var suitLengthNorth = northHand.Split(',')[3 - (int)suit].Length;
            var suitLengthSouth = southHand.Split(',')[3 - (int)suit].Length;
            return suitLengthNorth + suitLengthSouth;
        }

        public static int GetHcpCount(string hand)
        {
            return hand.Count(x => x == 'J') + hand.Count(x => x == 'Q') * 2 + hand.Count(x => x == 'K') * 3 + hand.Count(x => x == 'A') * 4;
        }

        public static int GetHcpCount(params string[] suits)
        {
            return suits.Sum(suit => suit.Count(x => x == 'J') + suit.Count(x => x == 'Q') * 2 + suit.Count(x => x == 'K') * 3 + suit.Count(x => x == 'A') * 4);
        }

        public static int GetControlCount(string hand)
        {
            return hand.Count(x => x == 'K') + hand.Count(x => x == 'A') * 2;
        }

        public static Suit GetTrumpSuitShape(string northHand, string southHandShape)
        {
            var southHand = string.Join(',', southHandShape.Select(x => new string('x', int.Parse(x.ToString()))));
            return GetTrumpSuit(northHand, southHand);
        }

        public static Suit GetTrumpSuit(string northHand, string southHand)
        {
            Debug.Assert(northHand.Length == 16);
            Debug.Assert(southHand.Length == 16);
            // TODO Use single dummy analyses to find out the best trump suit
            var (longestSuit, suitLength) = GetLongestSuit(northHand, southHand);
            // If we have a major fit return the major
            if (new List<Suit> { Suit.Spades, Suit.Hearts }.Contains(longestSuit))
                return (suitLength < 8) ? Suit.NoTrump : longestSuit;
            // Only wants to play a minor if we have a singleton and 9 or more trumps
            if (suitLength > 8 && (northHand.Split(',').Select(x => x.Length).Min() <= 1 || southHand.Split(',').Select(x => x.Length).Min() <= 1))
                return longestSuit;
            return Suit.NoTrump;
        }

        public static (ExpectedContract expectedContract, Dictionary<ExpectedContract, int> confidence) GetExpectedContract(IEnumerable<int> scores)
        {
            ExpectedContract expectedContract;
            if (scores.Count(x => x == 13) / (double)scores.Count() > .6)
                expectedContract = ExpectedContract.GrandSlam;
            else if (scores.Count(x => x == 12) / (double)scores.Count() > .6)
                expectedContract = ExpectedContract.SmallSlam;
            else if (scores.Count(x => x == 12 || x == 13) / (double)scores.Count() > .6)
                expectedContract = scores.Count(x => x == 12) >= scores.Count(x => x == 13) ? ExpectedContract.SmallSlam : ExpectedContract.GrandSlam;
            else expectedContract = ExpectedContract.Game;

            return (expectedContract, new Dictionary<ExpectedContract, int> {
                {ExpectedContract.GrandSlam, scores.Count(x => x == 13) },
                { ExpectedContract.SmallSlam, scores.Count(x => x == 12) },
                { ExpectedContract.Game, scores.Count(x => x < 12)}});
        }

        public static Player GetPlayer(char player) => player switch
        {
            'N' => Player.North,
            'E' => Player.East,
            'S' => Player.South,
            'W' => Player.West,
            _ => Player.UnKnown,
        };

        public static char GetPlayerString(Player player) => player switch
        {
            Player.North => 'N',
            Player.East => 'E',
            Player.South => 'S',
            Player.West => 'W',
            _ => throw new ArgumentException("Unknown player"),
        };

        public static string[] GetBoardsTosr(string board)
        {
            var boardNoDealer = board[2..].Replace('.', ',');
            var suits = boardNoDealer.Split(" ");
            var suitsNFirst = suits.ToList().Rotate(3);
            return suitsNFirst.ToArray();
        }

        public static Player GetPartner(Player player)
        {
            return player switch
            {
                Player.West => Player.East,
                Player.North => Player.South,
                Player.East => Player.West,
                Player.South => Player.North,
                _ => throw new ArgumentException(nameof(player)),
            };
        }
    }
}