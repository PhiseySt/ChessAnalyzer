using System.Text;

namespace ChessAnalyzer.ChessLib;


/// <summary>
/// The detailed result, containing the information about the game's outcome
/// </summary>
public struct UciDetailedResult
{
    /// <summary>
    /// Creates a new Detailed result
    /// </summary>
    /// <param name="result">The result (white wins, black wins or draw)</param>
    /// <param name="detail">The result's detail giving the reason for the result</param>
    /// <param name="additionalInfo">additional info (like which illegal move was played)</param>
    public UciDetailedResult(UciResult result, UciResultDetail detail = UciResultDetail.Unknown, object additionalInfo = null) { Result = result; Detail = detail; AdditionalInfo = additionalInfo; }
    /// <summary>
    /// The result (white wins, black wins or draw)
    /// </summary>
    public UciResult Result { private set; get; }
    /// <summary>
    /// The result's detail giving the reason for the result
    /// </summary>
    public UciResultDetail Detail { private set; get; }
    /// <summary>
    /// Additional info (like which illegal move was played)
    /// </summary>
    public object AdditionalInfo { private set; get; }
    /// <summary>
    /// Creates a string representation, mimicing the result info from cutechess
    /// </summary>
    /// <returns>the string representation</returns>
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append(UciPgn.ResultToString(Result));
        if (Result !=  UciResult.Open && Detail != UciResultDetail.Unknown)
        {
            string resultPhrase = ResultPhrase[(int)Detail];
            if (Result == UciResult.WhiteWins)
            {
                resultPhrase = resultPhrase.Replace("<winner>", "White");
                resultPhrase = resultPhrase.Replace("<looser>", "Black");
            }
            else if (Result == UciResult.BlackWins)
            {
                resultPhrase = resultPhrase.Replace("<winner>", "Black");
                resultPhrase = resultPhrase.Replace("<looser>", "White");
            }
            if (AdditionalInfo != null) resultPhrase = resultPhrase.Replace("<info>", AdditionalInfo.ToString());
            sb.Append(resultPhrase);
        }
        return sb.ToString();
    }
    /// <summary>
    /// The termination tag value as used in PGN Termination tag
    /// </summary>
    public string? Termination
    {
        get
        {
            if (Result == UciResult.Open) return "unterminated";
            else return PgnTerminationTerms[Detail];
        }
    }

    private static readonly string[] ResultPhrase = new string[11] { "", " {<winner> mates}", " {<looser> resigns}", " {<looser> loses on time}", " {<looser> makes an illegal move: <info>}",
                                                                  " {Draw by 3-fold repetition}", " {Draw by fifty moves rule}", " {Draw by insufficient mating material}",
                                                                  " {<winner> wins by adjudication}", " {Draw by adjudication}", " {Draw by stalemate}"};
    private static readonly Dictionary<UciResultDetail, string?> PgnTerminationTerms = new()
    {
        { UciResultDetail.Abandoned, "abandoned" },
        { UciResultDetail.AdjudicationWin, "adjudication" },
        { UciResultDetail.AdjudicationWin, "adjudication" },
        { UciResultDetail.TimeForfeit, "time forfeit" },
        { UciResultDetail.IllegalMove, "rules infraction" },
        { UciResultDetail.Mate, "normal" },
        { UciResultDetail.Stalemate, "normal" },
        { UciResultDetail.FiftyMoves, "normal" },
        { UciResultDetail.NoMatingMaterial, "normal" },
        { UciResultDetail.ThreeFoldRepetition, "normal" },
        { UciResultDetail.Unknown, null }
    };
}