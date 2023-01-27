using ChessAnalyzer.Analyzer;
using ChessAnalyzer.ChessLib;

// Find best move in position by fen
var enginePath = new EnginePath();
using var engine = new UciEngine(enginePath.Path);
engine.StartEngineAsync().Wait();
engine.Parameters.Add("Threads", "2");
engine.Parameters.Add("Hash", "256");
engine.SetOptionsAsync().Wait();
engine.NewGameAsync().Wait();
engine.SetPositionAsync("r3k2r/pppb1ppp/5n2/8/1b6/2N2P2/PPP1P1PP/R1B1KB1R w KQkq - 0 1").Wait();
engine.StartAnalysisAsync(TimeSpan.FromSeconds(10)).Wait();
if (engine.BestMove != null) Console.WriteLine($"{engine.BestMove.From} - {engine.BestMove.To}");
Console.ReadLine();
