namespace ChessAnalyzer.Analyzer;

public class EnginePath
{
    public string? Path { get; } 

    public EnginePath()
    {
        Path = @"c:\Stas\Projects\ChessAnalyzer\ChessAnalyzer\Engines\stockfish_14.1_win_x64_avx2.exe";
    }
    public EnginePath(string path)
    {
        Path = path;
    }
}