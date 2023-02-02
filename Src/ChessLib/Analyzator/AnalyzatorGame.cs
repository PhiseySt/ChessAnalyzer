using ChessAnalyzer.Analyzer;

namespace ChessAnalyzer.ChessLib.Analyzator;

public class AnalyzatorGame
{
    public CriticalPositions GetListCriticalPositionsForGame(UciGame game, int timeAnalyzeOneMoveMSec)
    {
        var criticalPositions = new CriticalPositions
        {
            Positions = new List<CriticalPosition>()
        };
        var enginePath = new EnginePath();
        using var engine = new UciEngine(enginePath.Path);
        engine.PrepareEngineForAnalysisAsync().Wait();

        decimal scorePrevMove = 0;
        decimal scoreCurrentMove = 0;
        var currentMovesForGame = string.Empty;
        var currentSide = Side.Black;
        var prevCurrentSide = Side.Black;
        var currentAnalyzeGame = new UciGame();


        foreach (var move in game.Moves)
        {
            scorePrevMove = scoreCurrentMove;
            currentMovesForGame += $" {move.ToUciString()}";
            currentSide = currentSide == Side.White ? Side.Black : Side.White;
            currentAnalyzeGame.Add(new UciExtendedMove(move.ToUciString()));

            engine.SetPositionAsync(UciFen.InitialPosition, currentMovesForGame).Wait();
            engine.StartAnalysisAsync().Wait();
            Thread.Sleep(timeAnalyzeOneMoveMSec);
            engine.StopThinkingAsync().Wait();
            scoreCurrentMove = engine.AnalysisInfo.Evaluation;
            if (Math.Abs(Math.Abs(scorePrevMove) - Math.Abs(scoreCurrentMove)) > 300)
            {
                var resultCriticalPositions = new CriticalPosition
                {
                    Side = prevCurrentSide,
                    BestMove = engine.BestMove,
                    Fen = currentAnalyzeGame.Position.FEN
                };
                criticalPositions.Positions.Add(resultCriticalPositions);
                criticalPositions.CountPositions++;
            }

            prevCurrentSide = currentSide;
        }

        return criticalPositions;
    }
}