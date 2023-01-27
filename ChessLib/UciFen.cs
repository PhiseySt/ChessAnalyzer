using System.Text;
using System.Text.RegularExpressions;

namespace ChessAnalyzer.ChessLib;

/// <summary>
/// This class provides constants and static functions to manage FEN strings (see https://chessprogramming.wikispaces.com/Forsyth-Edwards+Notation)
/// </summary>
public class UciFen
{
    internal static Dictionary<char, CastleFlag> CastleFlagMapping = new() { { 'K', CastleFlag.W00 }, { 'Q', CastleFlag.W000 }, { 'k', CastleFlag.B00 }, { 'q', CastleFlag.B000 }, { '-', CastleFlag.None } };
    internal static Dictionary<char, Piece> PieceMapping = new()
    {
        { 'Q', Piece.Wqueen },
        { 'R', Piece.Wrook },
        { 'B', Piece.Wbishop },
        { 'N', Piece.Wknight },
        { 'P', Piece.Wpawn },
        { 'K', Piece.Wking },
        { 'q', Piece.Bqueen },
        { 'r', Piece.Brook },
        { 'b', Piece.Bbishop },
        { 'n', Piece.Bknight },
        { 'p', Piece.Bpawn },
        { 'k', Piece.Bking }
    };

    private static readonly Regex RegexFen = new(@"^(?:[pnbrqkPNBRQK1-8]+/){7}[pnbrqkPNBRQK1-8]+\s(?:b|w)\s(?:-|K?Q?k?q)\s(?:-|[a-h][3-6])(?:\s+(?:\d+)){0,2}$", RegexOptions.Compiled);



    /// <summary>
    /// FEN Representation of the starting position in standard chess
    /// </summary>
    public const string InitialPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    /// <summary>
    /// Character representing the white king in <see href="https://chessprogramming.wikispaces.com/Forsyth-Edwards+Notation">Forsyth-Edwards-Notation (FEN)</see> 
    /// </summary>
    public const char PieceCharWking = 'K';
    /// <summary>
    /// Character representing the white king in <see href="https://chessprogramming.wikispaces.com/Forsyth-Edwards+Notation">Forsyth-Edwards-Notation (FEN)</see> 
    /// </summary>
    public const char PieceCharBking = 'k';
    /// <summary>
    /// Character representing the white king in <see href="https://chessprogramming.wikispaces.com/Forsyth-Edwards+Notation">Forsyth-Edwards-Notation (FEN)</see> 
    /// </summary>
    public const char PieceCharWqueen = 'Q';
    /// <summary>
    /// Character representing the black king in <see href="https://chessprogramming.wikispaces.com/Forsyth-Edwards+Notation">Forsyth-Edwards-Notation (FEN)</see> 
    /// </summary>
    public const char PieceCharBqueen = 'q';
    /// <summary>
    /// Character representing the white queen in <see href="https://chessprogramming.wikispaces.com/Forsyth-Edwards+Notation">Forsyth-Edwards-Notation (FEN)</see> 
    /// </summary>
    public const char PieceCharWrook = 'R';
    /// <summary>
    /// Character representing the black queen in <see href="https://chessprogramming.wikispaces.com/Forsyth-Edwards+Notation">Forsyth-Edwards-Notation (FEN)</see> 
    /// </summary>
    public const char PieceCharBrook = 'r';
    /// <summary>
    /// Character representing the white bishop in <see href="https://chessprogramming.wikispaces.com/Forsyth-Edwards+Notation">Forsyth-Edwards-Notation (FEN)</see> 
    /// </summary>
    public const char PieceCharWbishop = 'B';
    /// <summary>
    /// Character representing the black bishop in <see href="https://chessprogramming.wikispaces.com/Forsyth-Edwards+Notation">Forsyth-Edwards-Notation (FEN)</see> 
    /// </summary>
    public const char PieceCharBbishop = 'b';
    /// <summary>
    /// Character representing the white knight in <see href="https://chessprogramming.wikispaces.com/Forsyth-Edwards+Notation">Forsyth-Edwards-Notation (FEN)</see> 
    /// </summary>
    public const char PieceCharWknight = 'N';
    /// <summary>
    /// Character representing the black knight in <see href="https://chessprogramming.wikispaces.com/Forsyth-Edwards+Notation">Forsyth-Edwards-Notation (FEN)</see> 
    /// </summary>
    public const char PieceCharBknight = 'n';
    /// <summary>
    /// Character representing the white pawn in <see href="https://chessprogramming.wikispaces.com/Forsyth-Edwards+Notation">Forsyth-Edwards-Notation (FEN)</see> 
    /// </summary>
    public const char PieceCharWpawn = 'P';
    /// <summary>
    /// Character representing the black pawn in <see href="https://chessprogramming.wikispaces.com/Forsyth-Edwards+Notation">Forsyth-Edwards-Notation (FEN)</see> 
    /// </summary>
    public const char PieceCharBpawn = 'p';
    /// <summary>
    /// Character representing no piece (empty square)
    /// </summary>
    public const char PieceCharNone = ' ';

    /// <summary>
    /// Get's the char used for the piece (Example 'q' for black queen or 'P' for White pawn)
    /// </summary>
    /// <param name="piece"></param>
    /// <returns></returns>
    public static char PieceChar(Piece piece) => "QqRrBbNnPpKk "[(int)piece];

    /// <summary>
    /// Get's the piece for a piece characteristic (Example Black Queen for 'q' and White Pawn for 'p'
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static Piece ParsePieceChar(char c) => (Piece)"QqRrBbNnPpKk ".IndexOf(c);

    /// <summary>
    /// Calulates an Array of Piece Characters represented by the provided string in FEN Notation. The array representation used
    /// is A1 = 0, H1 = 7, H8 = 63
    /// </summary>
    /// <param name="fen">The position in FEN representation</param>
    /// <returns></returns>
    public static char[] GetPieceArray(string fen)
    {
        var board = new char[64];
        for (var i = 0; i < 64; ++i) board[i] = PieceCharNone;
        var index = fen.IndexOf(' ');
        var position = index > 0 ? fen[..index] : fen;
        var rows = position.Split(new[] { '/' });
        for (var row = 7; row >= 0; --row)
        {
            var col = 0;
            foreach (var c in rows[7 - row])
            {
                if (c is >= '1' and <= '8') col += c - '0';
                else
                {
                    board[8 * row + col] = c;
                    col++;
                }
            }
        }
        return board;
    }
    /// <summary>
    /// Creates the first part of a FEN string from a board array
    /// </summary>
    /// <param name="board"></param>
    /// <returns></returns>
    public static string FenPartFromBoard(char[] board)
    {
        StringBuilder sb = new();
        for (var row = 7; row >= 0; --row)
        {
            var countSpace = 0;
            for (var file = 0; file <= 7; ++file)
            {
                var s = 8 * row + file;
                if (board[s] == PieceCharNone) ++countSpace;
                else
                {
                    if (countSpace > 0)
                    {
                        sb.Append(countSpace);
                        countSpace = 0;
                    }
                    sb.Append(board[s]);
                }
                if (file == 7 && countSpace > 0) sb.Append(countSpace);
                if (file == 7 && row > 0) sb.Append('/');
            }
        }
        return sb.ToString();
    }
    /// <summary>
    /// Checks if a given string is a valid Fen string. This means that the fen can be parsed and 
    /// a position can be determined, but not that the position is legal. Fens without 50 move counter and
    /// movenumber (as used in EPD) are accepted
    /// </summary>
    /// <param name="fen">Fen to be checked</param>
    /// <returns>treu, if valid</returns>
    public static bool CheckValid(string fen)
    {
        return RegexFen.IsMatch(fen);
    }

    /// <summary>
    /// Parses a square provided by it's field name (such as 'a1', 'e5' or 'h8') 
    /// </summary>
    /// <param name="squareString">The square as string</param>
    /// <returns>The square defined by <paramref name="squareString"/></returns>
    /// <remarks>This method doesn't make any error checking. If invalid input is provided
    /// this might result in a crash</remarks>
    public static Square ParseSquare(string squareString)
    {
        var file = squareString[0] - (int)'a';
        var rank = int.Parse(squareString.Substring(1, 1)) - 1;
        return (Square)(8 * rank + file);
    }

    /// <summary>
    /// Creates a fen string representing an equivalent position with colors reversed
    /// </summary>
    /// <param name="fen">the positions which shall be swapped</param>
    /// <returns>fen string of the swapped position</returns>
    public static string Swap(string fen)
    {
        var token = fen.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var rows = token[0].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder sb = new();
        for (var i = 7; i >= 0; --i)
        {
            foreach (var c in rows[i])
            {
                if (char.IsLetter(c)) sb.Append(char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c));
                else sb.Append(c);
            }
            if (i > 0) sb.Append('/');
        }
        sb.Append(' ');
        sb.Append(token[1][0] == 'w' ? 'b' : 'w');
        sb.Append(' ');
        if (token[2][0] == '-') sb.Append("- ");
        else
        {
            var s1 = "kqKQ";
            var s2 = "KQkq";
            for (var i = 0; i < 4; ++i)
            {
                if (token[2].Contains(s1[i])) sb.Append(s2[i]);
            }
            sb.Append(' ');
        }
        if (token[3][0] == '-') sb.Append('-');
        else
        {
            if (token[1][0] == 'w')
                sb.Append(token[3].Replace('6', '3'));
            else
                sb.Append(token[3].Replace('3', '6'));
        }
        for (var i = 4; i < token.Length; ++i) sb.Append(' ').Append(token[i]);
        return sb.ToString();
    }

    
}