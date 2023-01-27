using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ChessAnalyzer.ChessLib;

/// <summary>
/// Represents a <see href="http://www.saremba.de/chessgml/standards/pgn/pgn-complete.htm"/>PGN (portable game notation) file. 
/// </summary>
public class UciPgn
{
    /// <summary>
    /// The parsed games contained in the PGN File
    /// </summary>
    /// <remarks>The List is only filled after <see cref="LoadAsync"/> has been executed</remarks>
    public List<UciGame> Games { private set; get; } = new List<UciGame>();
    /// <summary>
    /// The PGN file's name
    /// </summary>
    public string Filename { private set; get; }
    /// <summary>
    /// Creates a new PGN object for File <paramref name="filename"/>
    /// </summary>
    /// <param name="filename">The PGN file's name</param>
    public UciPgn(string filename)
    {
        Filename = filename;
    }

    private int _gameCount = -1;
    /// <summary>
    /// The number of games contained in the PGN File
    /// </summary>
    public int GameCount { get { if (_gameCount < 0) IndexFile(); return _gameCount; } }
    /// <summary>
    /// Loads the file's content into memory and parses it
    /// </summary>
    /// <returns>The number of games</returns>
    public async Task<int> LoadAsync(bool comments = false)
    {
        Stopwatch sw = new();
        sw.Start();
        Games.Clear();
        if (Filename == null || !File.Exists(Filename)) return 0;
        string content = null;
        try
        {
            using var reader = new StreamReader(new FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            content = await reader.ReadToEndAsync();
        }
        catch (Exception)
        {
            return 0;
        }
        Games.AddRange(Parse(content, comments));
        sw.Stop();
        Console.WriteLine(sw.Elapsed);
        return Games.Count;
    }
    /// <summary>
    /// Returns an Enumerable which returns one parsed Game after the other while reading the input PGN file
    /// </summary>
    /// <param name="comments">if true, comments are parsed as well</param>
    /// <returns>Enumerable with parsed Games</returns>
    public IEnumerable<UciGame> GetGames(bool comments = false)
    {
        using var reader = new StreamReader(new FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        string line;
        bool inMoveText = false;
        StringBuilder sb = new();
        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith("["))
            {
                if (inMoveText)
                {
                    Match m = regexPGNGame.Match(sb.ToString());
                    if (m.Success)
                    {
                        UciGame game = ParseGame(m, comments);
                        yield return game;
                        sb.Clear();
                    }
                }
                inMoveText = false;
            }
            else if (line.Trim().Length == 0)
            {
                //do nothing
            }
            else
            {
                inMoveText = true;
            }
            sb.Append(line).Append(Environment.NewLine);
        }
        if (inMoveText)
        {
            Match m = regexPGNGame.Match(sb.ToString());
            if (m.Success)
            {
                UciGame game = ParseGame(m, comments);
                yield return game;
            }
        }
    }
    /// <summary>
    /// Reads and parses the game with index <paramref name="index"/> from the PGN-File
    /// </summary>
    /// <param name="index">the index of the game to be read</param>
    /// <returns>The parsed Game</returns>
    public UciGame GetGame(int index)
    {
        if (IndexFile() && index < indices.Count)
        {
            StringBuilder sb = new();
            using (var reader = new StreamReader(new FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.BaseStream.Seek(indices[index], SeekOrigin.Begin);
                long end = index < indices.Count - 1 ? indices[index + 1] : long.MaxValue;
                string line;
                long offset = indices[index];
                while ((line = reader.ReadLine()) != null
                    && (offset += reader.CurrentEncoding.GetBytes(line + NewLine).Length) <= end)
                {
                    sb.Append(line).Append(Environment.NewLine);
                }
                Match m = regexPGNGame.Match(sb.ToString());
                if (m.Success)
                {
                    UciGame game = ParseGame(m);
                    return game;
                }
            }
            return null;
        }
        else return null;
    }

    private List<string> _players = null;
    public List<String> Players { get { return GetPlayers(); } }

    /// <summary>
    /// Determines a list of players from the PGN-File
    /// </summary>
    /// <returns>List of Players</returns>
    private List<string> GetPlayers()
    {
        if (_players != null) return _players;
        else if (Games.Count == 0) return null;
        else
        {
            HashSet<string> players = new();
            foreach (UciGame game in Games)
            {
                players.Add(game.White);
                players.Add(game.Black);
            }
            _players = new List<string>(players);
            _players.Sort();
            return _players;
        }

    }

    private List<long> indices = null;
    private string NewLine = Environment.NewLine;
    private bool IndexFile()
    {
        if (indices != null) return true;
        indices = new List<long>();
        try
        {
            using (var reader = new StreamReader(new FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                //Detect line breaks
                char[] buffer = new char[1024];
                reader.Read(buffer, 0, 1024);
                for (int i = 0; i < 1024; ++i)
                {
                    if (i > 0 && buffer[i] == '\n')
                    {
                        if (buffer[i - 1] == '\r') NewLine = "\r\n"; else NewLine = "\n";
                        break;
                    }
                }
            }
            using (var reader = new StreamReader(new FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                string line;
                bool inMoveText = false;
                long offset = 0;
                long position = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    position += reader.CurrentEncoding.GetBytes(line + NewLine).Length;
                    if (line.StartsWith("["))
                    {
                        if (inMoveText || indices.Count == 0) indices.Add(offset);
                        inMoveText = false;
                    }
                    else if (line.Trim().Length >= 0)
                    {
                        inMoveText = true;
                    }
                    offset = position;
                }
                _gameCount = indices.Count;
                return indices.Count > 0;
            }
        }
        catch (Exception)
        {
            indices = null;
            return false;
        }
    }

    /// <summary>
    /// Parses a pgn-file into a List of Games
    /// </summary>
    /// <param name="pgn">The PGN input</param>
    /// <param name="comments">if true, comments are parsed as well</param>
    /// <param name="variations">if true, variations are parsed as well</param>
    /// <param name="count">Number of games to be parsed (default: all)</param>
    /// <param name="offset">Index of first game to be parsed</param>
    /// <returns>List of parsed Games</returns>
    public static List<UciGame> Parse(string pgn, bool comments = false, bool variations = false, int count = Int32.MaxValue, int offset = 0)
    {
        MatchCollection mcGames = regexPGNGame.Matches(pgn);
        List<UciGame> games = new(mcGames.Count);
        int indx = 0;
        foreach (Match mGame in mcGames)
        {
            if (indx < offset) continue;
            try
            {
                var game = ParseGame(mGame, comments, variations);
                games.Add(game);
                if (indx >= count) break;
                indx++;
            }
            catch (Exception)
            {
                continue;
            }
        }
        return games;
    }

    private static UciGame ParseGame(Match mGame, bool comments = false, bool variations = false)
    {
        string tags = mGame.Groups[1].Value;
        string moveText = mGame.Groups[3].Value;
        UciGame game = new();
        MatchCollection mcTags = regexPGNTag.Matches(tags);
        foreach (Match mTag in mcTags)
        {
            game.SetTag(mTag.Groups[1].Value, mTag.Groups[2].Value);
        }
        //Now let's parse the move text
        //Remove comments
        if (comments)
        {
            if (mGame.Groups[2].Value != null && mGame.Groups[2].Value.Trim().Length > 3)
            {
                game.IntroductionComment = mGame.Groups[2].Value.Trim();
                game.IntroductionComment = game.IntroductionComment.Substring(0, game.IntroductionComment.Length - 1).Substring(1).Trim();
            }
            commentBuffer = new List<string>();
            moveText = regexPGNComment.Replace(moveText, ReplaceComment);
        }
        else moveText = regexPGNComment.Replace(moveText, string.Empty);
        //Remove NAGs
        moveText = regexPGNNag.Replace(moveText, string.Empty);
        moveText = moveText.Trim();
        //Remove variations
        if (variations)
        {
            variationBuffer = new List<string>();
            moveText = regexPGNVariations.Replace(moveText, ReplaceVariation);
        }
        else moveText = regexPGNVariations.Replace(moveText, string.Empty);
        //Replace Black move numbering
        moveText = moveText[0] + regexPGNBlackMovenumber.Replace(moveText[1..], string.Empty);
        //Add missing spaces after move number
        moveText = regexPGNMovenumberWithoutSpace.Replace(moveText, InsertSpaceAfterMoveNumber);
        //Normalize movetext
        moveText = regexPGNWhitespace.Replace(moveText, " ");
        //Replace wrong castlings
        moveText = moveText.Replace("0-0-0", "O-O-O");
        moveText = moveText.Replace("0-0", "O-O");
        moveText = moveText.Replace("\r\n", "\n");
        //Now everything should be prepared to parse the moves
        string[] tokens = moveText.Split(new char[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        UciPosition beforePos = null;
        foreach (string token in tokens)
        {
            Match m;
            if (ParseMove(game.Position, token, out UciMove move))
            {
                beforePos = game.Position;
                game.Add(new UciExtendedMove(move));
            }
            else if ((m = regexPGNMovenumber.Match(token)).Success)
            {
                int movenumber = Int32.Parse(m.Groups[1].Value);
                Debug.Assert(movenumber == game.Position.MoveNumber);
            }
            else if (comments && (m = regexPGNCommentPlaceholder.Match(token)).Success)
            {
                try
                {
                    int indx = Int32.Parse(m.Groups[1].Value);
                    if (game.Moves.Count > 0)
                    {
                        game.Moves.Last().Comment = RemoveLineBreaks(commentBuffer[indx]);
                        game.Moves.Last().ParseComment();
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Debug.Fail(ex.Message);
                }
            }
            else if (variations && (m = regexPGNVariationPlaceholder.Match(token)).Success)
            {
                try
                {
                    int indx = Int32.Parse(m.Groups[1].Value);
                    AddVariation(game, beforePos, variationBuffer[indx]);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Debug.Fail(ex.Message);
                }
            }
            else if (token == "1-0")
            {
                game.Result = UciResult.WhiteWins;
            }
            else if (token == "0-1")
            {
                game.Result = UciResult.BlackWins;
            }
            else if (token == "1/2-1/2")
            {
                game.Result = UciResult.Draw;
            }
            else if (token == "*") game.Result = UciResult.Open;
            else if (token == "...") continue;
            else Debug.Fail("Could not interpret token: " + token);
        }
        commentBuffer = null;
        variationBuffer = null;
        return game;
    }

    private static void AddVariation(UciGame game, UciPosition pos, string variation)
    {
        UciGame vGame = new(pos.FEN);
        UciPosition beforePos = null;
        variation = variation.Replace("\r\n", "\n");
        string[] tokens = variation.Split(new char[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            Match m;
            if (ParseMove(vGame.Position, token, out UciMove move))
            {
                beforePos = vGame.Position;
                vGame.Add(new UciExtendedMove(move));
            }
            else if ((m = regexPGNMovenumber.Match(token)).Success)
            {
                int movenumber = Int32.Parse(m.Groups[1].Value);
                Debug.Assert(movenumber == vGame.Position.MoveNumber);
            }
            else if (commentBuffer != null && (m = regexPGNCommentPlaceholder.Match(token)).Success)
            {
                try
                {
                    int indx = Int32.Parse(m.Groups[1].Value);
                    if (vGame.Moves.Count > 0)
                    {
                        vGame.Moves.Last().Comment = RemoveLineBreaks(commentBuffer[indx]);
                        vGame.Moves.Last().ParseComment();
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Debug.Fail(ex.Message);
                }
            }
            else if ((m = regexPGNVariationPlaceholder.Match(token)).Success)
            {
                try
                {
                    int indx = Int32.Parse(m.Groups[1].Value);
                    AddVariation(vGame, beforePos, variationBuffer[indx]);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Debug.Fail(ex.Message);
                }
            }
            else if (token == "1-0")
            {
                vGame.Result = UciResult.WhiteWins;
            }
            else if (token == "0-1")
            {
                vGame.Result = UciResult.BlackWins;
            }
            else if (token == "1/2-1/2")
            {
                vGame.Result = UciResult.Draw;
            }
            else if (token == "*") vGame.Result = UciResult.Open;
            else if (token == "...") continue;
            else Debug.Fail("Could not interpret token: " + token);
        }
        if (game.Moves.Last().Variations == null) game.Moves.Last().Variations = new List<List<UciExtendedMove>>();
        game.Moves.Last().Variations.Add(vGame.Moves);
    }

    internal static string RemoveLineBreaks(string comment)
    {
        return regexReplaceLineBreakTimeSpan.Replace(comment, ":$1").Replace('\n', ' ').Replace("\r", "");
    }

    internal static bool ParseMove(UciPosition pos, string token, out UciMove move)
    {
        Match m = regexPGNMove.Match(token);
        move = UciMove.Null;
        if (!m.Success) return false;
        move = ParseMove(pos, token, m);
        return !move.Equals(UciMove.Null);
    }

    internal static UciMove ParseMove(UciPosition pos, string token, Match m)
    {
        UciMove move = UciMove.Null;
        if (token.StartsWith("O-O-O")) move = pos.CastleMove((CastleFlag)(1 << (1 + 2 * (int)pos.SideToMove)));
        else if (token.StartsWith("O-O")) move = pos.CastleMove((CastleFlag)(1 << (2 * (int)pos.SideToMove)));
        else
        {
            Square to = UciFen.ParseSquare(m.Groups[4].Value);
            char pchar = m.Groups[1].Value.Length > 0 ? m.Groups[1].Value[0] : 'P';
            if (pos.SideToMove == Side.Black) pchar = Char.ToLower(pchar);
            Piece movingPiece = UciFen.ParsePieceChar(pchar);
            PieceType promoType = PieceType.None;
            if (m.Groups[5].Value.Length > 0) promoType = UciChess.ParsePieceTypeChar(m.Groups[5].Value[0]);
            List<UciMove> moves = pos.GetMoves();
            List<UciMove> potMoves = new();
            foreach (UciMove potMove in moves)
            {
                if (potMove.To == to && potMove.PromoteTo == promoType
                    && pos.GetPiece(potMove.From) == movingPiece) potMoves.Add(potMove);
            }
            Debug.Assert(potMoves.Count > 0, $"Couldn't parse token: {token} m: {m.Value} pos: {pos.FEN}");
            if (potMoves.Count == 1) move = potMoves[0];
            else
            {
                char dchar = m.Groups[2].Value[0];
                int dIndx = (dchar >= 'a' && dchar <= 'h') ? 0 : 1;
                foreach (UciMove potMove in potMoves)
                {
                    if (dchar == UciChess.SquareToString(potMove.From)[dIndx])
                    {
                        move = potMove;
                        break;
                    }
                }
            }
        }
        Debug.Assert(!move.Equals(UciMove.Null));
        return move;
    }

    private static readonly Regex regexPGNGame = new(@"((?:^\[[^\]]+\]\s*$?){2,})(?:^\s*)(^\{[^\}]+\}\r?\n)?(^[^\[\{].*(?:\r?\n^\S.*$)*)", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex regexPGNTag = new(@"\[([^\s]*)\s+""(.*)""\]", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex regexPGNComment = new(@"\{(?>\{(?<c>)|[^{}]+|\}(?<-c>))*(?(c)(?!))\}", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex regexPGNVariations = new(@"\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex regexPGNNag = new(@"\$\d+", RegexOptions.Compiled);
    private static readonly Regex regexPGNCommentPlaceholder = new(@"@(\d+)@", RegexOptions.Compiled);
    private static readonly Regex regexPGNVariationPlaceholder = new(@"@V(\d+)@", RegexOptions.Compiled);
    private static readonly Regex regexPGNBlackMovenumber = new(@"(\d+\.)\.+", RegexOptions.Compiled);
    private static readonly Regex regexPGNWhitespace = new(@"\s{2,}", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex regexPGNMovenumberWithoutSpace = new(@"\d+\.[OQRBNKa-h]", RegexOptions.Compiled);
    private static readonly Regex regexPGNMovenumber = new(@"(\d+)\.", RegexOptions.Compiled);
    internal static readonly Regex regexPGNMove = new(@"(?:([QRBNK])?([1-8a-h])?(x?)([a-h][1-8])(?:=([QRBN]))?[\+#!\?]*)|(?:O-O-O[\+#!\?]*)|(?:O-O[\+#!\?]*)", RegexOptions.Compiled);
    private static readonly Regex regexReplaceLineBreakTimeSpan = new(@"\:\r?\n^(\d)", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex regexReplaceLineBreak = new(@"\r?\n^", RegexOptions.Multiline | RegexOptions.Compiled);

    private static List<string> commentBuffer = null;
    private static List<string> variationBuffer = null;

    private static string ReplaceComment(Match m)
    {
        try
        {
            commentBuffer.Add(m.Value[1..^1]);
            return $"@{commentBuffer.Count - 1}@";
        }
        catch (NullReferenceException)
        {
            return string.Empty;
        }
    }

    private static string ReplaceVariation(Match m)
    {
        try
        {
            string v = m.Value[1..^1];
            if (commentBuffer != null) v = regexPGNComment.Replace(v, ReplaceComment);
            v = regexPGNVariations.Replace(v, ReplaceVariation);
            variationBuffer.Add(v);
            return $"@V{variationBuffer.Count - 1}@";
        }
        catch (NullReferenceException)
        {
            return string.Empty;
        }
    }

    private static string InsertSpaceAfterMoveNumber(Match m)
    {
        int len = m.Value.Length;
        return m.Value.Substring(0, len - 1) + ' ' + m.Value[len - 1];
    }

    internal static string[] resultStrings = new string[] { "*", "1-0", "0-1", "1/2-1/2", "*" };
    /// <summary>
    /// Converts the Result to a string as used f.e. in PGN Format. It will return one of "*", "1-0", "0-1", "1/2-1/2"
    /// </summary>
    /// <param name="result">The result to be converted</param>
    /// <returns>The result as string</returns>
    public static string ResultToString(UciResult result)
    {
        return resultStrings[(int)result];
    }

}