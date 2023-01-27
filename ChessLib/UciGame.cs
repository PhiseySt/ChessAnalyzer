using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ChessAnalyzer.ChessLib.Interfaces;

namespace ChessAnalyzer.ChessLib;

/// <summary>
/// Representation of a chess game
/// </summary>
public class UciGame : ICloneable
{
    /// <summary>
    /// Name of the player playing the white pieces
    /// </summary>
    public string White { set; get; }

    /// <summary>
    /// Name of the player playing the black pieces
    /// </summary>
    public string Black { set; get; }

    /// <summary>
    /// The name of the tournament or match event
    /// </summary>
    public string Event { set; get; }

    /// <summary>
    /// The location of the event
    /// </summary>
    public string Site { set; get; }

    /// <summary>
    /// The starting date of the game
    /// </summary>
    public string Date { set; get; }

    /// <summary>
    /// The playing round ordinal of the game
    /// </summary>
    public string Round { set; get; }

    /// <summary>
    /// The result of the game
    /// </summary>
    public UciResult Result { set; get; } = UciResult.Open;

    /// <summary>
    /// The result of the game
    /// </summary>
    public UciResultDetail ResultDetail { set; get; } = UciResultDetail.Unknown;

    /// <summary>
    /// The start position of the game (in FEN representation)
    /// </summary>
    public string StartPosition { private set; get; }

    /// <summary>
    /// Additional Tags
    /// </summary>
    public Dictionary<string, string> Tags { set; get; } = new();

    /// <summary>
    /// The Eco classification of the game
    /// </summary>
    public UciEco Eco => UciEco.Get(this);

    /// <summary>
    /// The Side to Move at the end of the current move list
    /// </summary>
    public Side SideToMove => _position.SideToMove;

    /// <summary>
    /// Enumeration of the moves of the game
    /// </summary>
    public List<UciExtendedMove> Moves => _moves;


    /// <summary>
    /// Creates a new Game 
    /// </summary>
    /// <param name="startPosition">The start position of the game. If skipped the standard initial position is used</param>
    public UciGame(string startPosition = UciFen.InitialPosition)
    {
        StartPosition = startPosition;
        var count = StartPosition.Split(new char[] { ' ' }).Length;
        if (count == 4) StartPosition += " 0 1";
        else if (count == 5) StartPosition += " 1";
        _position = new UciPosition(StartPosition);
        _hashes.Add(_position.PolyglotKey);
    }
  
    ///<summary>
    ///Get's the current Position (after the last move)
    ///</summary>
    public UciPosition Position => (UciPosition)_position.Clone();

    /// <summary>
    /// Outputs the Game in SAN (Standard Algebraic Notation) Notation
    /// </summary>
    /// <returns>A string containing the notation</returns>
    public string SanNotation(bool withComments = true, bool withVariations = false)
    {
        UciPosition pos = new(StartPosition);
        StringBuilder sb = GetMoveText(pos, _moves, withComments, withVariations);
        sb.Append(UciPgn.ResultToString(Result));
        return sb.ToString().Trim();
    }
    /// <summary>
    /// TimeControl used when game was played (usually null)
    /// </summary>
    public TimeControl TimeControl { set; get; } = null;

    /// <summary>
    /// Introduction Comment: When parsing PGN there is sometimes a comment before the 1st move, adding some additional
    /// information about the game or the initial position
    /// </summary>
    public string IntroductionComment { set; get; } = null;

    private StringBuilder GetMoveText(UciPosition pos, List<UciExtendedMove> _moves, bool withComments, bool withVariations)
    {
        StringBuilder sb = new();
        if (pos.SideToMove == Side.Black) sb.Append($"{pos.MoveNumber}... ");
        foreach (UciExtendedMove m in _moves)
        {
            if (pos.SideToMove == Side.White) sb.Append($"{pos.MoveNumber}. ");
            sb.Append(pos.ToSAN(m)).Append(' ');
            if (withComments && m.Comment != null && m.Comment.Length > 0)
            {
                sb.Append('{').Append(m.Comment).Append("} ");
            }
            if (withVariations && m.Variations != null)
            {
                foreach (var variation in m.Variations)
                {
                    sb.Append($"( {GetMoveText((UciPosition)pos.Clone(), variation, withComments, withVariations)} ) ");
                }
            }
            pos.ApplyMove(m);
        }
        return sb;
    }

    /// <summary>
    /// Outputs the Game in PGN (Portable Game Notation) format
    /// </summary>
    /// <param name="formatter">A <see cref="IPgnOutputFormatter"/>, which creates PGN comments for each move</param>
    /// <returns>A string containing the pgn formatted gamse</returns>
    public string ToPgn(IPgnOutputFormatter formatter = null, bool withVariations = false)
    {
        if (formatter != null) foreach (UciExtendedMove m in _moves) m.Comment = formatter.Comment(m);
        StringBuilder sb = new(PgnTagSection());
        sb.AppendLine();
        //split movetext to lines with max 80 characters
        var movetext = SanNotation(true, withVariations);
        while (movetext.Length > 80)
        {
            var m80 = movetext.Substring(0, 81);
            var indx = m80.LastIndexOf(' ');
            sb.AppendLine(movetext.Substring(0, indx));
            movetext = movetext[(indx + 1)..];
        }
        sb.AppendLine(movetext);
        sb.AppendLine();
        return sb.ToString();
    }
    /// <summary>
    /// Position for a given move within the game
    /// </summary>
    /// <param name="moveNumber">Move number for which the position should be determined</param>
    /// <param name="side">Side (White/Black) for which the position should be determined</param>
    /// <returns>The position at that point within the game</returns>
    public UciPosition? GetPosition(int moveNumber, Side side)
    {
        var pos = new UciPosition(StartPosition);
        var i = 0;
        while (i < Moves.Count && (pos.MoveNumber < moveNumber || pos.SideToMove != side))
        {
            pos.ApplyMove(Moves[i]);
            ++i;
        }
        return (pos.MoveNumber == moveNumber && pos.SideToMove == side) ? pos : null;
    }

    /// <summary>
    /// Move for a given movenumber and side
    /// </summary>
    /// <param name="moveNumber">Move number for which the position should be determined</param>
    /// <param name="side">Side (White/Black) for which the position should be determined</param>
    /// <returns>The move at that point within the game</returns>
    public UciExtendedMove? GetMove(int moveNumber, Side side)
    {
        var targetPly = PlyIndex(moveNumber, side);
        var currentPly = PlyIndex(_position.MoveNumber, _position.SideToMove);
        var indx = Moves.Count - (currentPly - targetPly);
        return indx >= 0 && indx < Moves.Count ? Moves[indx] : null;
    }

    public static int PlyIndex(int moveNumber, Side side)
    {
        return 2 * (moveNumber - 1) + (int)side;
    }

    private string PgnTagSection()
    {
        StringBuilder sb = new();
        sb.AppendLine($"[Event \"{Event}\"]");
        sb.AppendLine($"[Site \"{Site}\"]");
        sb.AppendLine($"[Date \"{Date}\"]");
        if (_position.Chess960)
        {
            sb.AppendLine($"[Variant \"Chess960\"]");
        }
        sb.AppendLine($"[Round \"{Round}\"]");
        sb.AppendLine($"[White \"{White}\"]");
        sb.AppendLine($"[Black \"{Black}\"]");
        sb.AppendLine($"[Result \"{UciPgn.ResultToString(Result)}\"]");
        if (StartPosition == UciFen.InitialPosition)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                UciEco eco = Eco;
                sb.AppendLine($"[ECO \"{eco.Key}\"]");
                sb.AppendLine($"[Opening \"{eco.Text}\"]");
            }
        }
        else
        {
            sb.AppendLine($"[SetUp \"1\"]");
            sb.AppendLine($"[FEN \"{StartPosition}\"]");
        }
        string? termination = new UciDetailedResult(Result, ResultDetail).Termination;
        if (termination != null) sb.AppendLine($"[Termination \"{termination}\"]");
        foreach (var tag in Tags.Keys) sb.AppendLine($"[{tag} \"{Tags[tag]}\"]");
        return sb.ToString();
    }

    /// <summary>
    /// Set's the game's result from it's string representation
    /// </summary>
    /// <param name="resultString">The result as string, either "*", "1-0", "0-1", or "1/2-1/2"</param>
    /// <returns>true, if valid result string has been passed</returns>
    public bool SetResult(string resultString)
    {
        for (var i = 0; i < UciPgn.resultStrings.Length; ++i)
        {
            if (UciPgn.resultStrings[i] == resultString)
            {
                Result = (UciResult)i;
                ResultDetail = UciResultDetail.Unknown;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Set's a PGN Tag value
    /// </summary>
    /// <param name="tag">The tag's name</param>
    /// <param name="value">The tag's value</param>
    public void SetTag(string tag, string value)
    {
        if (tag == "Event") Event = value;
        else if (tag == "Site") Site = value;
        else if (tag == "Date") Date = value;
        else if (tag == "Round") Round = value;
        else if (tag == "White") White = value;
        else if (tag == "Black") Black = value;
        else if (tag == "Result") SetResult(value);
        else if (tag == "FEN")
        {
            if (Moves == null || Moves.Count == 0)
            {
                StartPosition = value;
                _position = new UciPosition(value);
                _hashes.Clear();
                _hashes.Add(_position.PolyglotKey);
            }
            Tags[tag] = value;
        }
        else if (tag == "TimeControl")
        {
            TimeControl = new TimeControl(value);
        }
        else
        {
            Tags[tag] = value;
        }
    }

    /// <summary>
    /// Adds a new Move to the game
    /// </summary>
    /// <param name="extendedMove"></param>
    /// <returns>true, if move is legal</returns>
    public bool Add(UciExtendedMove extendedMove)
    {
        List<UciMove> legalMoves = _position.GetMoves();
        var legal = false;
        foreach (UciMove move in legalMoves)
        {
            if (move.Equals(extendedMove))
            {
                legal = true;
                break;
            }
        }
        if (!legal) return false;
        var isPromotion = (extendedMove.To >= Square.A8 || extendedMove.To <= Square.H1) && _position.GetPiece(extendedMove.From) == (Piece)(8 + (int)_position.SideToMove);
        extendedMove.UndoInfo = new UndoInfo(_position.DrawPlyCount, _position.GetPiece(extendedMove.To), _position.EPSquare, _position.castlings, isPromotion);
        extendedMove.SideToMove = _position.SideToMove;
        _position.ApplyMove(extendedMove);
        _moves.Add(extendedMove);
        _hashes.Add(_position.PolyglotKey);
        if (_position.IsMate)
        {
            Result = SideToMove == Side.White ? UciResult.BlackWins :  UciResult.WhiteWins;
            ResultDetail = UciResultDetail.Mate;
        }
        else if (_position.IsStalemate)
        {
            Result = UciResult.Draw;
            ResultDetail = UciResultDetail.Stalemate;
        }
        else if (_position.DrawPlyCount >= 100)
        {
            Result = UciResult.Draw;
            ResultDetail = UciResultDetail.FiftyMoves;
        }
        else if (Check3FoldRepetition())
        {
            Result = UciResult.Draw;
            ResultDetail = UciResultDetail.ThreeFoldRepetition;
        }
        else if (_position.IsDrawnByInsufficientMatingMaterial())
        {
            Result = UciResult.Draw;
            ResultDetail = UciResultDetail.NoMatingMaterial;
        }
        return true;
    }
    /// <summary>
    /// Adds a variation to the current game
    /// </summary>
    /// <param name="variation">List of all moves, which form the variation</param>
    /// <param name="moveNumber">Move number where variation starts</param>
    /// <param name="side">Side to move where variation starts</param>
    public void AddVariation(List<UciExtendedMove> variation, int moveNumber = 1, Side side = Side.White)
    {
        var moves = Moves;
        var plyIndex = 2 * (moveNumber - 1) + (int)side;
        var subIndex = plyIndex;
        for (var i = 0; i < variation.Count; i++)
        {
            if (subIndex == moves.Count)
            {
                moves.Add(variation[i]);
            }
            else if (variation[i].Equals(moves[subIndex]))
            {
                subIndex++;
            }
            else
            {
                if (moves[subIndex].Variations == null) moves[subIndex].Variations = new();
                List<UciExtendedMove> sub = null;
                foreach (var subvariant in moves[subIndex].Variations)
                {
                    if (subvariant[0].Equals(variation[i]))
                    {
                        subIndex = 1;
                        sub = subvariant;
                        moves = subvariant;
                        break;
                    }
                }
                if (sub == null)
                {
                    moves[subIndex].Variations.Add(new List<UciExtendedMove>(variation.GetRange(i, variation.Count - i)));
                    return;
                }
            }

        }
    }

    /// <summary>
    /// Undos the last applied move from the game
    /// </summary>
    /// <returns>true if successful</returns>
    public bool UndoLastMove()
    {
        if (_position.UndoMove(Moves.Last(), _hashes[^2]))
        {
            Result = UciResult.Open;
            _hashes.RemoveAt(_hashes.Count - 1);
            Moves.RemoveAt(Moves.Count - 1);
        }
        else
        {
            Debug.Assert(false); //Shouldn't happen!
            return false;
        }
        return true;
    }
    /// <summary>
    /// Checks if current position has been 3-fold repeated
    /// </summary>
    /// <returns>true, if position has already repeated 3 times</returns>
    private bool Check3FoldRepetition()
    {
        if (Position.DrawPlyCount < 8) return false;
        var checkHash = _hashes.Last();
        var repetitions = 0;
        for (var i = Math.Max(0, _hashes.Count - Position.DrawPlyCount); i < _hashes.Count; ++i)
        {
            if (_hashes[i] == checkHash) ++repetitions;
        }
        return repetitions >= 3;
    }
    /// <inheritdoc/>
    public object Clone()
    {
        UciGame game = new(StartPosition)
        {
            _position = (UciPosition)_position.Clone(),
            _hashes = new List<ulong>(_hashes),
            Black = Black,
            Date = Date,
            Event = Event,
            _moves = new List<UciExtendedMove>(_moves),
            Result = Result,
            ResultDetail = ResultDetail,
            Round = Round,
            Site = Site,
            Tags = new Dictionary<string, string>(Tags),
            White = White
        };
        return game;
    }

    internal List<UciGame> VariationGames()
    {
        List<UciGame> vgames = new();
        if (Moves.Last().Variations != null)
        {
            foreach (var variation in Moves.Last().Variations)
            {
                UciGame g = (UciGame)Clone();
                g.UndoLastMove();
                foreach (var move in variation)
                {
                    g.Add(move);
                }
                vgames.Add(g);
            }
        }
        return vgames;
    }

    private List<UciExtendedMove> _moves = new();
    private UciPosition? _position = null;
    private List<ulong> _hashes = new();

    public override string ToString()
    {
        return $"{White}-{Black} {UciPgn.ResultToString(Result)} ({Event} {Round})";
    }

}
