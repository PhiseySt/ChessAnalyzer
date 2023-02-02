using ChessAnalyzer.ChessLib.EventArgs;
using System.Diagnostics;
using System.Text;
using ChessAnalyzer.ChessLib.Exceptions;

namespace ChessAnalyzer.ChessLib;

public sealed class UciEngine : IDisposable
{
    private Process? _process;
    private TaskCompletionSource<bool>? _tcsExitEngine;
    private TaskCompletionSource<bool>? _tcsStartEngine;
    private TaskCompletionSource<bool>? _tcsSetOptions;
    private TaskCompletionSource<bool>? _tcsNewGame;
    private TaskCompletionSource<bool>? _tcsStopThinking;
    private TaskCompletionSource<bool>? _tcsFiniteAnalysis;

    private List<UciEngineInfo> _infos = new() { new UciEngineInfo() };

    private enum EngineState { Off, Initializing, Ready, Thinking }
    private EngineState _state = EngineState.Off;
    private Side _side = Side.White;

    private UciGame _game = new();
    private int _moveNumber = 1;


    /// <summary>
    /// Raised whenever the engine issues a "info" message (except "info string" messages), by which
    /// the engine sends information about the current state of analysis
    /// </summary>
    public event EventHandler<EngineInfoEventArgs>? OnEngineInfoChanged;

    /// <summary>
    /// Raised whenever the engine outputs anything
    /// </summary>
    public event EventHandler<EngineOutputEventArgs>? OnEngineOutput;

    /// <summary>
    /// Path to the engine executable
    /// </summary>
    public string? Executable { get; init; }

    /// <summary>
    /// UCI options, passed via setoption command
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; }

    /// <summary>
    /// Arguments passed on start of the engine executable
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Working directory of the engine process. If not set directory of <see cref="Executable"/>
    /// is used
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Engine Name (as given by engine process). Only available once engine process is started.
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// Engine Author (as given by engine process). Only available once engine process is started. 
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Options as provided by the engine's option commands
    /// </summary>
    public Dictionary<string, UciEngineOption> Options { get; } = new();

    /// <summary>
    /// Get's the current Analysis result from the engine
    /// </summary>
    public UciEngineInfo AnalysisInfo => _infos[0];

    /// <summary>
    /// Get's the current Analysis result from the engine for a specified line (inMultiPV mode) 
    /// </summary>
    /// <param name="line">Line index (zero-based, so line = 0 will return the infor for the main line)</param>
    /// <returns>Engine's info for this line</returns>
    public UciEngineInfo GetAnalysisInfo(int line = 0) => _infos[0];

    public UciExtendedMove? BestMove { get; internal set; }

    public UciMove? PonderMove { get; internal set; }

    /// <param name="executable">Path to the engine executable</param>
    /// <param name="parameters">Parameters, which will be passed to the engine later on by calling</param>
    /// <param name="arguments">Command line arguments passed to the engine process on startup</param>
    public UciEngine(string? executable, Dictionary<string, string>? parameters = null, string? arguments = null)
    {
        if (!File.Exists(executable)) throw new FileNotFoundException(executable);
        Executable = executable;
        Parameters = parameters ?? new();
        Arguments = arguments;
    }

    /// <summary>
    /// Starts the engine process
    /// </summary>
    /// <returns>true, if engine process could be started</returns>
    public async Task<bool> StartEngineAsync()
    {
        _tcsStartEngine = new TaskCompletionSource<bool>();
        _process = new Process();
        _process.StartInfo.FileName = Executable;
        _process.StartInfo.RedirectStandardInput = true;
        _process.StartInfo.RedirectStandardOutput = true;
        _process.StartInfo.RedirectStandardError = true;
        _process.StartInfo.UseShellExecute = false;
        _process.StartInfo.CreateNoWindow = true;
        _process.EnableRaisingEvents = true;
        if (Arguments != null) _process.StartInfo.Arguments = Arguments;
        _process.StartInfo.WorkingDirectory = !Directory.Exists(Path.GetDirectoryName(Executable)) ? Path.GetDirectoryName(Executable) : WorkingDirectory;
        _process.Exited += Process_Exited;
        _process.Start();
        _process.OutputDataReceived += Process_OutputDataReceived;
        _process.ErrorDataReceived += Process_ErrorDataReceived;
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        _state = EngineState.Initializing;
        Send("uci");
        return await _tcsStartEngine.Task;
    }


    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        Task.WhenAny(Task.Delay(1000), Exit()).Wait();
        _process?.Kill();
    }

    private async Task<bool> Exit()
    {
        _tcsExitEngine = new TaskCompletionSource<bool>();
        Send("quit");
        return await _tcsExitEngine.Task;
    }

    private void Send(string message)
    {
        Trace.WriteLine("=> " + message);
        _process?.StandardInput.WriteLine(message);
    }

    private void Process_Exited(object? sender, System.EventArgs e)
    {
        if (_tcsExitEngine != null && !_tcsExitEngine.Task.IsCompleted) _tcsExitEngine?.SetResult(true);
        Trace.WriteLine("Process exited!");
    }

    private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null || e.Data.Trim().Length == 0) return;
        Trace.WriteLine("<= " + e.Data);
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null || e.Data.Trim().Length == 0) return;
        Trace.WriteLine("<= " + e.Data);
        if (e.Data.StartsWith("info string"))
        {

        }
        else if (e.Data.StartsWith("info "))
        {
            var index = UciEngineInfo.Index(e.Data);
            var info = _infos[index];
            var evaluationUpdate = info.Update(e.Data);
            _infos[index] = info;
            if (evaluationUpdate)
                OnEngineInfoChanged?.Invoke(this, new EngineInfoEventArgs(info));
        }
        else if (e.Data.StartsWith("bestmove "))
        {
            _state = EngineState.Ready;
            var token = e.Data.Split();
            BestMove = new(token[1]);
            BestMove.UsedThinkTime = TimeSpan.FromMilliseconds(_infos[0].Time);
            BestMove.Depth = _infos[0].Depth;
            BestMove.Evaluation = _infos[0].Evaluation;
            BestMove.SideToMove = _side;
            if (token.Length > 3)
                PonderMove = new(token[3]);
            if (_tcsStopThinking != null && !_tcsStopThinking.Task.IsCompleted)
                _tcsStopThinking.SetResult(true);
            if (_tcsFiniteAnalysis != null && !_tcsFiniteAnalysis.Task.IsCompleted)
                _tcsFiniteAnalysis.SetResult(true);
        }
        else if (e.Data == "readyok")
        {
            _state = EngineState.Ready;
            if (_tcsSetOptions != null && !_tcsSetOptions.Task.IsCompleted)
            {
                _tcsSetOptions.SetResult(true);
            }
            if (_tcsNewGame != null && !_tcsNewGame.Task.IsCompleted)
            {
                _tcsNewGame.SetResult(true);
            }
        }
        else if (e.Data == "uciok")
        {
            _tcsStartEngine?.SetResult(true);
        }
        else if (e.Data.StartsWith("id "))
        {
            Process_GetNameAuthorEngine(e.Data);
        }
        else if (e.Data.StartsWith("option "))
        {
            var option = UciEngineOption.CreateOption(e.Data);
            if (option.Name is not null)
            {
                if (Options.ContainsKey(option.Name)) Options[option.Name] = option;
                else Options.Add(option.Name, option);
            }
        }
        OnEngineOutput?.Invoke(this, new EngineOutputEventArgs(e.Data));
    }

    private void Process_GetNameAuthorEngine(string data)
    {
        if (data.StartsWith("id name ")) Name = data[8..];
        if (data.StartsWith("id author ")) Author = data[9..];
    }

    /// <summary>
    /// Calls the setoption commands used to configure the engine. The settings have to be provided before
    /// via the <see cref="Parameters"/> property
    /// </summary>
    /// <returns>true, if everything went right</returns>
    public async Task<bool> SetOptionsAsync()
    {
        _tcsSetOptions = new TaskCompletionSource<bool>();
        foreach (var p in Parameters)
        {
            if (Options.ContainsKey(p.Key))
            {
                Send($"setoption name {p.Key} value {p.Value}");
            }
            else
            {
                Trace.WriteLine($"Unknown parameter {p.Key} will be ignored!");
            }
        }

        var multipv = Parameters.ContainsKey("MultiPV") ? int.Parse(Parameters["MultiPV"]) : 1;
        _infos = new();
        for (var i = 0; i < multipv; ++i) _infos.Add(new());
        Send("isready");
        return await _tcsSetOptions.Task;
    }

    /// <summary>
    /// Sends the "ucinewgame" command to the engine
    /// <para> this shall be sent to the engine when the next search (started with "position" and "go") will be from
    /// a different game.This can be a new game the engine should play or a new game it should analyse but
    /// also the next position from a testsuite with positions only.</para>
    /// <returns>true, if everything went right</returns>
    /// </summary>
    /// <exception cref="EngineException">thrown if engine state doesn't allow to set position. Method must 
    /// not be called while engine is off, initializing or thinking</exception>
    public async Task<bool> NewGameAsync()
    {
        if (_state == EngineState.Thinking) await StopThinkingAsync();
        if (_state != EngineState.Ready)
            throw new EngineException($"Can't start analysis - Engine state is { _state}, should be { EngineState.Ready}");
        _tcsNewGame = new TaskCompletionSource<bool>();
        Send("ucinewgame");
        Send("isready");
        return await _tcsNewGame.Task;
    }

    /// <summary>
    /// Prepares the engine with one method call. It combines calls to <see cref="StartEngineAsync"/>,
    /// <see cref="SetOptionsAsync"/> and <see cref="NewGameAsync"/>.
    /// </summary>
    /// <param name="parameter">Engine options, which will be sent to the engine using the "setoption" command</param>
    /// <returns>true, if everything went right</returns>
    public async Task<bool> PrepareEngineForAnalysisAsync(Dictionary<string, string>? parameter = null)
    {
        var result = true;
        if (_state == EngineState.Thinking) result = result && await StopThinkingAsync();
        else if (_state == EngineState.Off) result = result && await StartEngineAsync();
        var parameterChanged = false;
        if (parameter != null)
        {
            foreach (var key in parameter.Keys)
            {
                if (Parameters.ContainsKey(key) && Parameters[key] != parameter[key])
                {
                    Parameters[key] = parameter[key];
                    parameterChanged = true;
                }
                else if (!Parameters.ContainsKey(key))
                {
                    Parameters.Add(key, parameter[key]);
                }
            }
        }
        if (parameterChanged || _state < EngineState.Ready)
        {
            result = result && await SetOptionsAsync();
            result = result && await NewGameAsync();
        }
        return result;
    }

    /// <summary>
    /// Stops engine analysis (sends UCI "stop" command)
    /// </summary>
    /// <returns>true, if everything went right</returns>
    /// <exception cref="EngineException">thrown if engine isn't analysing</exception>
    public async Task<bool> StopThinkingAsync()
    {
        if (_state != EngineState.Thinking)
            throw new EngineException(
                $"Can't stop thinking - Engine state is {_state}, should be {EngineState.Thinking}");
        _tcsStopThinking = new();
        Send("stop");
        return await _tcsStopThinking.Task;
    }

    /// <summary>
    /// Set's the engine's position 
    /// </summary>
    /// <param name="fen">Start position in <see href="https://de.wikipedia.org/wiki/Forsyth-Edwards-Notation">FEN</see> representation</param>
    /// <param name="movesList">Moves leading from start position to position to be analysed, in UCI notation separated by spaces. 
    /// Example: <c>e2e4 e7e5 g1f3 b8c6 f1b5 a7a6</c></param>
    /// <returns>true, if everything went right</returns>
    /// <exception cref="EngineException">thrown if engine state doesn't allow to set position. Method must 
    /// not be called while engine is off, initializing or thinking</exception>
    public async Task<bool> SetPositionAsync(string fen = UciFen.InitialPosition, string? movesList = null)
    {
        bool result = true;
        if (_state == EngineState.Thinking) result = await StopThinkingAsync();
        if (_state != EngineState.Ready)
            throw new EngineException($"Can't set position - Engine state is { _state}, should be { EngineState.Ready}");
        _game = new(fen);
        if (movesList != null)
        {
            string[] moves = movesList.Split();
            foreach (string move in moves)
            {
                _game.Add(new(move));
            }
        }
        _moveNumber = _game.Position.MoveNumber;
        _side = _game.SideToMove;
        if (_game.Moves.Count > 0)
            Send($"position fen { fen } moves { movesList }");
        else Send($"position fen { fen }");
        return result;
    }

    /// <summary>
    /// Sets the engine's position from a <see cref="Game"/>
    /// </summary>
    /// <param name="game">Game, from which position is taken</param>
    /// <param name="moveNumber">Movenumber of position</param>
    /// <param name="side">Side to move of position</param>
    /// <returns>true, if everything went right</returns>
    /// <exception cref="EngineException">thrown if engine state doesn't allow to set position. Method must 
    /// not be called while engine is off, initializing or thinking</exception>
    public async Task<bool> SetPositionAsync(UciGame game, int moveNumber, Side side)
    {
        bool result = true;
        if (_state == EngineState.Thinking) result = await StopThinkingAsync();
        if (_state != EngineState.Ready)
            throw new EngineException($"Can't set position - Engine state is { _state}, should be { EngineState.Ready}");
        _game = (UciGame)game.Clone();
        _moveNumber = moveNumber;
        _side = side;
        var sb = new StringBuilder();
        UciPosition pos = new(_game.StartPosition);
        foreach (var move in _game.Moves)
        {
            pos.ApplyMove(move);
            sb.Append(move.ToUciString()).Append(' ');
            if (pos.MoveNumber == moveNumber && pos.SideToMove == side) break;
        }
        if (sb.Length > 0)
            Send($"position fen { _game.StartPosition } moves { sb.ToString().Trim() }");
        else
            Send($"position fen { _game.StartPosition }");
        return result;
    }

    /// <summary>
    /// Starts an infinite analysis of the current position
    /// </summary>
    /// <param name="movesToBeAnalyzed">List of moves, which shall be analyzed - if null, all moves will be analyzed</param>
    /// <returns>true, if everything went right</returns>
    /// <exception cref="EngineException">thrown if engine state doesn't allow to set position. Method must 
    /// not be called while engine is off, initializing or thinking</exception>
    public async Task<bool> StartAnalysisAsync(List<UciMove>? movesToBeAnalyzed = null)
    {
        bool result = true;
        if (_state == EngineState.Thinking) result = await StopThinkingAsync();
        if (_state != EngineState.Ready)
            throw new EngineException($"Can't start analysis - Engine state is { _state}, should be { EngineState.Ready}");
        _state = EngineState.Thinking;
        Send($"go infinite{ SearchMoveCommand(movesToBeAnalyzed) }");
        return result;
    }

    /// <summary>
    /// Starts an analysis of the current position up to a specified search depth
    /// </summary>
    /// <param name="depth">search depth (in plies)</param>
    /// <param name="MovesToBeAnalyzed">List of moves, which shall be analyzed - if null, all moves will be analyzed</param>
    /// <exception cref="EngineException">thrown if engine state doesn't allow to set position. Method must 
    /// not be called while engine is off, initializing or thinking</exception>
    public async Task<bool> StartAnalysisAsync(int depth, List<UciMove>? MovesToBeAnalyzed = null)
    {
        bool result = true;
        if (_state == EngineState.Thinking) result = await StopThinkingAsync();
        if (_state != EngineState.Ready)
            throw new EngineException($"Can't start analysis - Engine state is { _state}, should be { EngineState.Ready}");
        _tcsFiniteAnalysis = new();
        _state = EngineState.Thinking;
        Send($"go depth {depth}{ SearchMoveCommand(MovesToBeAnalyzed) }");
        result = await _tcsFiniteAnalysis.Task && result;
        return result;
    }

    /// <summary>
    /// Starts an analysis of the current position for a specified time
    /// </summary>
    /// <param name="thinkTime">the time the engine shall spend</param>
    /// <param name="MovesToBeAnalyzed">List of moves, which shall be analyzed - if null, all moves will be analyzed</param>
    /// <returns>true, if everything went right</returns>
    /// <exception cref="EngineException">thrown if engine state doesn't allow to set position. Method must 
    /// not be called while engine is off, initializing or thinking</exception>
    public async Task<bool> StartAnalysisAsync(TimeSpan thinkTime, List<UciMove>? MovesToBeAnalyzed = null)
    {
        bool result = true;
        if (_state == EngineState.Thinking) result = await StopThinkingAsync();
        if (_state != EngineState.Ready)
            throw new EngineException($"Can't start analysis - Engine state is { _state}, should be { EngineState.Ready}");
        _tcsFiniteAnalysis = new();
        Send($"go movetime {thinkTime.TotalMilliseconds}{ SearchMoveCommand(MovesToBeAnalyzed) }");
        result = await _tcsFiniteAnalysis.Task && result;
        return result;
    }

    /// <summary>
    /// Prepares the engine and executes an analysis for a specified time
    /// </summary>
    /// <param name="game">Game, from which position is taken</param>
    /// <param name="time">Time, the analysis shall take</param>
    /// <param name="moveNumber">Movenumber of position</param>
    /// <param name="side">Side to move of position</param>
    /// <param name="parameter">Engine options, which will be sent to the engine using the "setoption" command</param>
    /// <param name="movesToBeAnalyzed">List of moves, which shall be analyzed - if null, all moves will be analyzed</param>
    /// <returns>The best move determined by the engine (includes Evaluation Info)</returns>
    public async Task<UciExtendedMove?> AnalyzeAsync(UciGame game, TimeSpan time, int moveNumber = 1, Side side = Side.White, Dictionary<string, string>? parameter = null, List<UciMove>? movesToBeAnalyzed = null)
    {
        await PrepareEngineForAnalysisAsync(parameter);
        await SetPositionAsync(game, moveNumber, side);
        await StartAnalysisAsync(time, movesToBeAnalyzed);
        return BestMove;
    }

    private string SearchMoveCommand(List<UciMove>? movesToBeAnalyzed)
    {
        if (movesToBeAnalyzed == null || movesToBeAnalyzed.Count == 0) return string.Empty;
        var sb = new StringBuilder(" searchmoves");
        foreach (var move in movesToBeAnalyzed)
        {
            sb.Append($" ").Append(move.ToUciString());
        }
        return sb.ToString();
    }
}