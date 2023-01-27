namespace ChessAnalyzer.ChessLib.Interfaces;

/// <summary>
/// Allows to extend the PGN output by custom comments
/// </summary>
public interface IPgnOutputFormatter
{
    /// <summary>
    /// Allows to enhance a move by a comment, which will be stored in PGN output
    /// </summary>
    /// <param name="move">The extended move</param>
    string Comment(UciExtendedMove move);
}