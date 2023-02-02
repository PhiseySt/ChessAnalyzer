using ChessAnalyzer.ChessLib;
using ChessAnalyzer.ChessLib.Analyzator;
using ChessAnalyzer.UnitTests.DataFiles;
using Xunit;

namespace ChessAnalyzer.UnitTests;

public class UciAnalyzatorGameTest
{

    [Fact]
    public void ChessPlanetPgnGame_is_NormalFoundCriticalPositions()
    {
        var analyzatorGame = new AnalyzatorGame();
        var games = UciPgn.Parse(ChessPlanetData.PgnFileData(), true, true);
        var game = games[9];
        var listCriticalPositions = analyzatorGame.GetListCriticalPositionsForGame(game, 2000);
        Assert.Equal(2, listCriticalPositions.CountPositions);
    }

    [Fact]
    public void LichessPgnGame_is_NormalFoundCriticalPositions()
    {
        var analyzatorGame = new AnalyzatorGame();
        var games = UciPgn.Parse(LichessData.PgnFileData(), true, true);
        var game = games[9];
        var listCriticalPositions = analyzatorGame.GetListCriticalPositionsForGame(game, 2000);
        Assert.Equal(1, listCriticalPositions.CountPositions);
    }
}