using ChessAnalyzer.ChessLib;
using ChessAnalyzer.UnitTests.DataFiles;
using Xunit;

namespace ChessAnalyzer.UnitTests;

public class UciPgnTest
{

    [Fact]
    public void ChessPlanetPgn_is_NormalParsed()
    {
        var games = UciPgn.Parse(ChessPlanetData.PgnFileData(), true, true);
        Assert.Equal(19, games.Count);
        Assert.Equal(83, games[0].Moves.Count);
    }

    [Fact]
    public void LichessPgn_is_NormalParsed()
    {
        var games = UciPgn.Parse(LichessData.PgnFileData(), true, true);
        Assert.Equal(54, games.Count);
        Assert.Equal(75, games[10].Moves.Count);
    }
}