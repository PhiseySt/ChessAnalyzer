﻿using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChessAnalyzer.ChessLib;

/// <summary>
/// Class representing ECO codes to classify opening positions
/// </summary>
[Serializable]
public class UciEco : IComparable<UciEco>
{
    /// <summary>
    /// Description of the Opening
    /// </summary>
    public string? Text { init; get; }

    /// <summary>
    /// The ECO code
    /// </summary>
    public string? Key { init; get; }

    /// <summary>
    /// Series of moves defining the opening
    /// </summary>
    public string? MoveString { init; get; }

    /// <summary>
    /// Determines the opening key for a position
    /// </summary>
    /// <param name="fen">The position in FEN representation</param>
    /// <returns>The ECO or null if position is not found</returns>
    public static UciEco Get(string fen)
    {
        Initialize();
        UciPosition pos = new(fen);
        if (EcoList.ContainsKey(pos.PolyglotKey)) return EcoList[pos.PolyglotKey]; else return null;
    }
    /// <summary>
    /// Determines the opening key for a game
    /// </summary>
    /// <param name="game">The game</param>
    /// <returns>The ECO or null if game opening is irregular</returns>
    public static UciEco Get(UciGame game)
    {
        Initialize();
        UciEco eco = null;
        UciPosition pos = new();
        int moveCount = 0;
        foreach (UciMove m in game.Moves)
        {
            pos.ApplyMove(m);
            if (EcoList.ContainsKey(pos.PolyglotKey)) eco = EcoList[pos.PolyglotKey];
            ++moveCount;
            if (moveCount > 40) return eco;
        }
        return eco;
    }

    /// <summary>
    /// Returns a short game, which results in a position matching the provided ECO object
    /// </summary>
    /// <param name="eco">ECO object for which the Game, shall be provided</param>
    /// <returns>A game, with moves leading to the position defined by the passed ECO object</returns>
    public static UciGame GetGame(UciEco eco)
    {
        Initialize();
  
        UciGame game = new();
        MatchCollection mc = UciPgn.regexPGNMove.Matches(eco.MoveString);
        foreach (Match mm in mc)
        {
            UciMove move = UciPgn.ParseMove(game.Position, mm.Value, mm);
            game.Add(new UciExtendedMove(move));
        }
        return game;
    }
    /// <summary>
    /// Determines the polyglot key of the position defined by the Eco object
    /// </summary>
    /// <param name="eco"></param>
    /// <returns>Polyglot Key</returns>
    public static ulong GetID(UciEco eco)
    {
        Initialize();
        return SortedEcos[eco];
    }
    /// <summary>
    /// Returns Eco object for a polyglot key
    /// </summary>
    /// <param name="polyglotkey"></param>
    /// <returns></returns>
    public static UciEco Get(ulong polyglotkey)
    {
        Initialize();
        return EcoList.ContainsKey(polyglotkey) ? EcoList[polyglotkey] : null;
    }
    /// <summary>
    /// Returns a list of Eco objects for subvariants based on a given Eco
    /// </summary>
    /// <param name="eco">Variant for which subvariants shall be provided</param>
    /// <returns>List of all Eco objects defining subvariants of eco</returns>
    public static List<UciEco> Subvariants(UciEco eco)
    {
        Initialize();
        List<UciEco> subvariants = new();
        if (eco == null) return subvariants;
        int index = SortedEcos.IndexOfKey(eco);
        for (int i = index + 1; i < SortedEcos.Count; ++i)
        {
            UciEco neco = SortedEcos.Keys[i];
            if (neco.MoveString.StartsWith(eco.MoveString))
                subvariants.Add(neco);
            else
                break;
        }
        return subvariants;
    }
    /// <summary>
    /// Returns for each ECO-key the basic variant
    /// </summary>
    /// <returns>List of basic variants</returns>
    public static SortedSet<UciEco> Keyvariants()
    {
        return Keyvariants(' ');
    }
    /// <summary>
    /// Returns for each ECO-key the basic variant
    /// </summary>
    /// <param name="volume">Volume to which result set shall be restricted</param>
    /// <returns>List of basic variants</returns>
    public static SortedSet<UciEco> Keyvariants(char volume, string fromKey = "A00", string toKey = "E99")
    {
        Initialize();
        SortedSet<UciEco> keyvariants = new();
        if (fromKey == null || toKey == null) return keyvariants;
        string key = "";
        foreach (var entry in Get(fromKey, toKey))
        {
            if (entry.Key != key)
            {
                keyvariants.Add(entry);
                key = entry.Key;
            }
        }
        return volume == ' ' ? keyvariants : new SortedSet<UciEco>(keyvariants.Where(k => k.Key[0] == volume));
    }
    /// <summary>
    /// Get Ecos from key interval
    /// </summary>
    /// <param name="fromKey">From key (e.g. "B20")</param>
    /// <param name="toKey">To key (e.g. "B39")</param>
    /// <returns>Set of Ecos matching the key interval</returns>
    public static SortedSet<UciEco> Get(string fromKey, string toKey)
    {
        Initialize();
        return new SortedSet<UciEco>(SortedEcos.Keys.Where(k => k.Key.CompareTo(fromKey) >= 0 && k.Key.Substring(0, 3).CompareTo(toKey) <= 0));
    }
    /// <summary>
    /// ECO Volume Titles
    /// </summary>
    public static SortedDictionary<char, string> Volumes => new()
    {
        { 'A', "Flank openings" },
        { 'B', "Semi-Open Games other than the French Defense" },
        { 'C', "Open Games and the French Defense" },
        { 'D', "Closed Games and Semi-Closed Games" },
        { 'E', "Indian Defenses" }
    };
    /// <summary>
    /// Chapter (Subgroups) grouping related openings
    /// </summary>
    public static SortedDictionary<Tuple<string, string>, string> Chapter => new()
    {
        { new("A00", "A39"), "White first moves other than 1.e4, 1.d4" },
        { new("A40", "A44"), "1.d4 without 1...d5, 1...Nf6 or 1...f5: Atypical replies to 1.d4" },
        { new("A45", "A49"), "1.d4 Nf6 without 2.c4: Atypical replies to 1...Nf6" },
        { new("A50", "A79"), "1.d4 Nf6 2.c4 without 2...e6 or 2...g6: Atypical Indian systems" },
        { new("A80", "A89"), "1.d4 f5: Dutch Defence" },
        { new("B00", "B19"), "1.e4 without 1...c5, 1...e6 or 1...e5 (B00–B19)" },
        { new("B20", "B99"), "1.e4 c5: Sicilian Defence" },
        { new("C00", "C19"), "1.e4 e6: French Defence" },
        { new("C20", "C99"), "1.e4 e5: Open Game" },
        { new("D00", "D69"), "1.d4 d5: Closed Game" },
        { new("D70", "D99"), "1.d4 Nf6 2.c4 g6 with 3...d5: Grünfeld Defence" },
        { new("E00", "E59"), "1.d4 Nf6 2.c4 e6: Indian systems with ...e6" },
        { new("E60", "E99"), "1.d4 Nf6 2.c4 g6 without 3...d5: Indian systems with ...g6 (except Grünfeld)" },
    };
    /// <summary>
    /// Determines the base variant (the shortest variant having the same key) for
    /// a given variant
    /// </summary>
    /// <param name="eco">Eco for which the base variant shall be determined</param>
    /// <returns>The base variant</returns>
    public static UciEco Keyvariant(UciEco eco)
    {
        UciEco keyvariant = eco;
        int indx = SortedEcos.Keys.IndexOf(eco);
        for (int i = indx - 1; i >= 0; --i)
        {
            UciEco e = SortedEcos.Keys[i];
            if (e.Key == keyvariant.Key) keyvariant = e; else break;
        }
        return keyvariant;
    }

    /// <inheritdoc/>
    public int CompareTo(UciEco other)
    {
        int result = Key.CompareTo(other.Key);
        if (result == 0) result = MoveString.CompareTo(other.MoveString);
        return result;
    }

    internal static Dictionary<UInt64, UciEco> EcoList { private set; get; } = null;
    internal static SortedList<UciEco, UInt64> SortedEcos { private set; get; } = null;

    internal static void Initialize()
    {
        if (EcoList != null && EcoList.Count > 0) return;
        try
        {
            EcoList = JsonSerializer.Deserialize<Dictionary<ulong, UciEco>>(ecojson);
            SortedEcos = new SortedList<UciEco, ulong>();
            foreach (var entry in EcoList)
            {
                try
                {
                    SortedEcos.Add(entry.Value, entry.Key);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }
    }


}