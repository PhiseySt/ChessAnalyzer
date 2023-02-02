using System.IO;

namespace ChessAnalyzer.UnitTests.DataFiles;

public class ChessPlanetData
{
    private static readonly string PgnFileFullDataName = Directory.GetCurrentDirectory() + PgnFileDataName;
    private const string PgnFileDataName = "\\DataFiles\\СhessPlanet.pgn";

    public  static string PgnFileData() => File.ReadAllText(PgnFileFullDataName);
}