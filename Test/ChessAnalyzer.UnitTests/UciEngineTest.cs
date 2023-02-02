using ChessAnalyzer.Analyzer;
using ChessAnalyzer.ChessLib;
using System;
using System.Threading;
using ChessAnalyzer.UnitTests.DataFiles;
using Xunit;

namespace ChessAnalyzer.UnitTests;

public class UciEngineTest
{
    [Fact]
    public void Engine_is_SuccessfullStart()
    {
        var enginePath = new EnginePath();
        using var engine = new UciEngine(enginePath.Path);
        engine.StartEngineAsync().Wait();
     
        Assert.NotNull(engine.Name);
        Assert.True(engine.Options.ContainsKey("Threads"));
        Assert.Equal(UciEngineOption.OptionType.SPIN, engine.Options["Threads"].Type);
     
        engine.Parameters.Add("Threads", "2");
        engine.Parameters.Add("Hash", "256");
        engine.SetOptionsAsync().Wait();

        Assert.True(engine.Options.ContainsKey("Threads"));
        Assert.Equal(UciEngineOption.OptionType.SPIN, engine.Options["Threads"].Type);
    }

    [Fact]
    public void Engine_Find_BestMoveInPosition()
    {
        var enginePath = new EnginePath();
        using var engine = new UciEngine(enginePath.Path);
        engine.StartEngineAsync().Wait();
        engine.Parameters.Add("Threads", "2");
        engine.Parameters.Add("Hash", "256");
        engine.SetOptionsAsync().Wait();
        engine.NewGameAsync().Wait();
        engine.SetPositionAsync("r1bqkb1r/ppp1pppp/8/8/3nn3/2N5/PPP2PPP/R1BQKB1R w KQkq - 0 1").Wait();
        engine.StartAnalysisAsync(TimeSpan.FromSeconds(10)).Wait();
        Assert.Equal(new UciMove("c3e4"), engine.BestMove);
    }

    [Fact]
    public void Engine_Get_ScoreForPosition()
    {
        var enginePath = new EnginePath();
        using var engine = new UciEngine(enginePath.Path);
        engine.StartEngineAsync().Wait();
        engine.SetOptionsAsync().Wait();
        engine.NewGameAsync().Wait();
        engine.SetPositionAsync(UciFen.InitialPosition).Wait();
        engine.StartAnalysisAsync().Wait();
        Thread.Sleep(100);

        var score = engine.AnalysisInfo.Evaluation;
        Assert.True(score > 0);

        engine.SetPositionAsync(UciFen.InitialPosition, "e2e4").Wait();
        engine.StartAnalysisAsync().Wait();
        Thread.Sleep(100);
        score = engine.AnalysisInfo.Evaluation;
        Assert.True(score < 0);

        var game = new UciGame();
        game.Add(new UciExtendedMove("e2e4"));
        game.Add(new UciExtendedMove("c7c5"));
        engine.SetPositionAsync(game, 2, Side.White).Wait();
        engine.StartAnalysisAsync().Wait();
        Thread.Sleep(100);
        score = engine.AnalysisInfo.Evaluation;
        Assert.True(score > 0);
    }

    [Fact]
    public void Engine_Get_ScoreForPosition2()
    {
        var enginePath = new EnginePath();
        using var engine = new UciEngine(enginePath.Path);
        engine.PrepareEngineForAnalysisAsync().Wait();
        engine.SetPositionAsync(UciFen.InitialPosition).Wait();
        engine.StartAnalysisAsync().Wait();
        Thread.Sleep(100);

        var score = engine.AnalysisInfo.Evaluation;
        Assert.True(score > 0);

        engine.SetPositionAsync(UciFen.InitialPosition, "e2e4").Wait();
        engine.StartAnalysisAsync().Wait();
        Thread.Sleep(100);
        score = engine.AnalysisInfo.Evaluation;
        Assert.True(score < 0);

        var game = new UciGame();
        game.Add(new UciExtendedMove("e2e4"));
        game.Add(new UciExtendedMove("c7c5"));
        engine.SetPositionAsync(game, 2, Side.White).Wait();
        engine.StartAnalysisAsync().Wait();
        Thread.Sleep(100);
        score = engine.AnalysisInfo.Evaluation;
        Assert.True(score > 0);
    }

    [Fact]
    public void Engine_Get_BadMoveInChessGame()
    {
        var enginePath = new EnginePath();
        var game = UciPgn.Parse(Data.PgnFileData)[1];
        using var engine = new UciEngine(enginePath.Path);
        var beforeBlunder = engine.AnalyzeAsync(game, TimeSpan.FromMilliseconds(100), 25, Side.Black).Result;
        var afterBlunder = engine.AnalyzeAsync(game, TimeSpan.FromMilliseconds(100), 26, Side.White).Result;
        if (beforeBlunder != null)
        {
            var scoreBefore = -beforeBlunder.Evaluation;
            if (afterBlunder != null)
            {
                var scoreAfter = afterBlunder.Evaluation;
                Assert.True(scoreAfter - scoreBefore > 100);
            }
        }
    }

}