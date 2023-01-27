namespace ChessAnalyzer.ChessLib;

/// <summary>
/// Information, sent by the engine during analyzing
/// </summary>
public struct UciEngineInfo
{
    /// <summary>
    /// Current analysis depth
    /// </summary>
    public int Depth { get; private set; }
    
    /// <summary>
    /// Current maximal analysis depth reached for selected search branches
    /// </summary>
    public int MaxDepth { get; private set; }
  
    /// <summary>
    /// Current Search Time (in milliseconds)
    /// </summary>
    public int Time { get; private set; }

    /// <summary>
    /// Number of Nodes searched
    /// </summary>
    public long Nodes { get; private set; }

    /// <summary>
    /// Search speed in (nodes/second)
    /// </summary>
    public long NodesPerSecond { get; private set; }

    /// <summary>
    /// Principal Variation 
    /// </summary>
    public string PrincipalVariation { get; private set; } = string.Empty;

    /// <summary>
    /// Mate distance (Mate in x moves)
    /// </summary>
    public int MateDistance { get; private set; } = int.MaxValue;

    /// <summary>
    /// If engine runs in MultiPV mode (analyzing more than one next move) the move's index
    /// </summary>
    public int MoveIndex { get; private set; } = 1;

    /// <summary>
    /// Currently analyzed move (in UCI notation)
    /// </summary>
    public string CurrentMove { get; private set; } = string.Empty;

    /// <summary>
    /// Currently searching move number x, for the first move x should be 1 not 0
    /// </summary>
    public int CurrentMoveNumber { get; private set; }

    /// <summary>
    /// Number of table base hits
    /// </summary>
    public long TableBaseHits { get; private set; }

    /// <summary>
    /// Evaluation Type
    /// <para><see cref="Evaluation"/> might not always be exact. Sometimes engines output a lower or a upper bound only</para>
    /// </summary>
    public enum EvaluationType { Exact, Upperbound, Lowerbound, Mate }

    /// <summary>
    /// Engine score in Centipawns (from engine's point of view)
    /// </summary>
    public int Evaluation { get; private set; }

    /// <summary>
    /// Evaluation Type
    /// </summary>
    public EvaluationType Type { get; private set; } = EvaluationType.Exact;

    /// <summary>
    /// Determines the MultiPV (resp. line) Index from an engine's info message
    /// </summary>
    /// <param name="message">The info message issued by the engine</param>
    /// <returns>the 0-based line index (multipv 1 will give 0)</returns>
    public static int Index(string message)
    {
        var index = message.IndexOf("multipv", StringComparison.Ordinal);
        if (index == -1) return 0;
        var token = message[(index + 8)..].Split();
        return int.Parse(token[0]) - 1;
    }

    /// <summary>
    /// Updates an existing Info object from a new info message
    /// </summary>
    /// <param name="message">The info message issued by the engine</param>
    /// <returns>true, if new evaluation information was part of the info message</returns>
    public bool Update(string message)
    {
        var result = false;
        var token = message.Split();
        var pv = false;
        for (var i = 1; i < token.Length; ++i)
        {
            switch (token[i])
            {
                case "depth":
                    ++i; Depth = int.Parse(token[i]);
                    break;
                case "seldepth":
                    ++i; MaxDepth = int.Parse(token[i]);
                    break;
                case "time":
                    ++i; Time = int.Parse(token[i]);
                    break;
                case "nodes":
                    ++i; Nodes = long.Parse(token[i]);
                    break;
                case "nps":
                    ++i; NodesPerSecond = long.Parse(token[i]);
                    break;
                case "tbhits":
                    ++i; TableBaseHits = long.Parse(token[i]);
                    break;
                case "multipv":
                    ++i; MoveIndex = int.Parse(token[i]);
                    break;
                case "currmovenumber":
                    ++i; CurrentMoveNumber = int.Parse(token[i]);
                    break;
                case "currmove":
                    ++i; CurrentMove = token[i];
                    break;
                case "score":
                {
                    ++i;
                    Type = token[i] switch
                    {
                        "cp" => EvaluationType.Exact,
                        "mate" => EvaluationType.Mate,
                        "upperbound" => EvaluationType.Upperbound,
                        "lowerbound" => EvaluationType.Lowerbound,
                        _ => Type
                    };
                    ++i;
                    if (Type == EvaluationType.Mate) MateDistance = int.Parse(token[i]);
                    else Evaluation = int.Parse(token[i]);
                    result = true;
                    break;
                }
                case "pv":
                    ++i; PrincipalVariation = token[i];
                    pv = true;
                    continue;
                default:
                {
                    if (pv)
                    {
                        PrincipalVariation += " " + token[i];
                        continue;
                    }

                    break;
                }
            }

            pv = false;
        }
        return result;
    }
}