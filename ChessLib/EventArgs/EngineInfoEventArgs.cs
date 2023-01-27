namespace ChessAnalyzer.ChessLib.EventArgs;


public class EngineInfoEventArgs : System.EventArgs
{
    public EngineInfoEventArgs(UciEngineInfo info)
    {
        Info = info;
    }

    public UciEngineInfo Info { get; set; }
}