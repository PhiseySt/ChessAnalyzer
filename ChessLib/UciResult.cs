namespace ChessAnalyzer.ChessLib;

/// <summary>
/// Possible outcomes of a chess game
/// </summary>
public enum UciResult
{
    /// <summary>
    /// Game is still ongoing, resp. result is unknown
    /// </summary>
    Open,
    WhiteWins,
    BlackWins,
    Draw,
    /// <summary>
    /// Game has been abandoned (for example in engine matches by a crash)
    /// </summary>
    Abandoned
}

/// <summary>
/// More detailed info about game result
/// </summary>
public enum UciResultDetail
{
    Unknown, 
    Mate, 
    Abandoned, 
    TimeForfeit, 
    IllegalMove, 
    ThreeFoldRepetition, 
    FiftyMoves,
    NoMatingMaterial, 
    AdjudicationWin, 
    AdjudicationDraw, 
    Stalemate
}