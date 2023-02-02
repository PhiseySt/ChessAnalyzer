namespace ChessAnalyzer.ChessLib.Analyzator;

public class CriticalPosition
{
    // Fen critical position 
    public string? Fen { get; set;}

    // Side for move
    public Side Side { get; set;}

    // Best move from engine analyze
    public UciExtendedMove? BestMove { get; set; }

}