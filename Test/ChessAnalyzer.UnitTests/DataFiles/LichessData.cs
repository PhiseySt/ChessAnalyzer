using System.IO;

namespace ChessAnalyzer.UnitTests.DataFiles;

public class LichessData
{
    private static readonly string PgnFileFullDataName = Directory.GetCurrentDirectory() + PgnFileDataName;
    private const string PgnFileDataName = "\\DataFiles\\Lichess.pgn";

    public static string PgnFileData() => File.ReadAllText(PgnFileFullDataName);
}