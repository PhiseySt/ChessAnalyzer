using System.Globalization;
using System.Text;
using ChessAnalyzer.ChessLib.Interfaces;
using ChessAnalyzer.ChessLib.PieceStringProviders;

namespace ChessAnalyzer.ChessLib;

/// <summary>
/// Square of a standard chess board. ChessAnalyzer uses "Little-Endian Rank-File Mapping"
/// (see https://chessprogramming.wikispaces.com/Square+Mapping+Considerations).
/// </summary>
public enum Square
{

    A1, B1, C1, D1, E1, F1, G1, H1,
    A2, B2, C2, D2, E2, F2, G2, H2,
    A3, B3, C3, D3, E3, F3, G3, H3,
    A4, B4, C4, D4, E4, F4, G4, H4,
    A5, B5, C5, D5, E5, F5, G5, H5,
    A6, B6, C6, D6, E6, F6, G6, H6,
    A7, B7, C7, D7, E7, F7, G7, H7,
    A8, B8, C8, D8, E8, F8, G8, H8, Outside = 64
}
/// <summary>
/// Enumeration for the different Pieces
/// </summary>
public enum Piece
{
    Wqueen, Bqueen, Wrook, Brook, Wbishop, Bbishop, Wknight, Bknight, Wpawn, Bpawn, Wking, Bking, Blank
}
/// <summary>
/// The Piece Types
/// </summary>
public enum PieceType
{
    Queen, Rook, Bishop, Knight, Pawn, King, None
}
/// <summary>
/// Side or Color
/// </summary>
public enum Side { White, Black }

/// <summary>
/// Castling Options
/// </summary>
public enum CastleFlag
{
    /// <summary>
    /// No castlings, neither white nor black possible
    /// </summary>
    None = 0,
    /// <summary>
    /// White king-side
    /// </summary>
    W00 = 1,
    /// <summary>
    /// White capturingQueen-side
    /// </summary>
    W000 = 2,
    /// <summary>
    /// Black king-side
    /// </summary>
    B00 = 4,
    /// <summary>
    /// Black capturingQueen side
    /// </summary>
    B000 = 8
}

/// <summary>
/// This class contains static methods useful when dealing with chess
/// </summary>
public class UciChess
{
    /// <summary>
    /// List of supported languages 
    /// </summary>
    public static List<string> SupportedLanguages => PieceChars.Keys.Select(ci => ci.TwoLetterISOLanguageName).Distinct().ToList();

    private static readonly Dictionary<char, PieceType> PieceTypeMapping = new()
    {
        { 'Q', PieceType.Queen },
        { 'R', PieceType.Rook },
        { 'B', PieceType.Bishop },
        { 'N', PieceType.Knight },
        { 'K', PieceType.King },
        { 'P', PieceType.Pawn }
    };

    internal static readonly Dictionary<CultureInfo, IChessPieceStringProvider> PieceChars = new()
    {
        { CultureInfo.InvariantCulture, new CharPieceStringProvider("QRBNPK") },
        { new CultureInfo("en"), new CharPieceStringProvider("QRBNPK") },
        { new CultureInfo("de"), new CharPieceStringProvider("DTLSBK") },
        { new CultureInfo("fr"), new CharPieceStringProvider("DTFCPR") },
        { new CultureInfo("es"), new CharPieceStringProvider("DTACPR") },
        { new CultureInfo("it"), new CharPieceStringProvider("DTACPR") },
        { new CultureInfo("ru"), new StringArrayPieceStringProvider(new[] { "Ф", "Л", "С", "К", "П", "Кр" }) }
    };


    /// <summary>
    /// Provides the type of a given piece
    /// </summary>
    /// <param name="piece">A chess piece</param>
    /// <returns>The type of this chess piece</returns>
    public static PieceType GetPieceType(Piece piece) { return (PieceType)((int)piece >> 1); }

    /// <summary>
    /// Parses a PieceType Character ('Q', 'R', 'B', 'N', 'K' or 'P')
    /// </summary>
    /// <param name="piecetypechar">The Piece type character</param>
    /// <returns>the piece type value</returns>
    public static PieceType ParsePieceTypeChar(char piecetypechar)
    {
        return PieceTypeMapping[piecetypechar];

    }
    /// <summary>
    /// Determines the color of a piece
    /// </summary>
    /// <param name="piece">The piece</param>
    /// <returns>The color of the piece</returns>
    public static Side GetColor(Piece piece) { return (Side)((int)piece & 1); }

    /// <summary>
    /// Get's the string representation (e.g. "a1" or "d5") of a square
    /// </summary>
    /// <param name="square">The square</param>
    /// <returns>The square's string representation</returns>
    public static string SquareToString(Square square)
    {
        StringBuilder sb = new();
        sb.Append("abcdefgh"[(int)square & 7]).Append(((int)square >> 3) + 1);
        return sb.ToString();
    }

    /// <summary>
    /// Creates a Piece from <see cref="PieceType"/> and <see cref="Side"/>
    /// </summary>
    /// <param name="pt">Piece Type</param>
    /// <param name="side">Side</param>
    /// <returns>Piece</returns>
    public static Piece GetPiece(PieceType pt, Side side)
    {
        return (Piece)(2 * (int)pt + (int)side);
    }
  
}