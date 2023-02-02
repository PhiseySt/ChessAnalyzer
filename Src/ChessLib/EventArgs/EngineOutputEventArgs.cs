namespace ChessAnalyzer.ChessLib.EventArgs;

public class EngineOutputEventArgs : System.EventArgs
{
    public EngineOutputEventArgs(string message)
    {
        Message = message;
    }

    public string Message { get; set; }
}