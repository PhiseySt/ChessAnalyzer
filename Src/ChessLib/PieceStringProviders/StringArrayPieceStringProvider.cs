using ChessAnalyzer.ChessLib.Interfaces;

namespace ChessAnalyzer.ChessLib.PieceStringProviders;

internal class StringArrayPieceStringProvider : IChessPieceStringProvider
{
    readonly string[] _piecestrings;
    public StringArrayPieceStringProvider(string[] piecestrings)
    {
        _piecestrings = piecestrings;
    }

    public string Get(PieceType pt, Side side = Side.White)
    {
        return side == Side.White ? _piecestrings[(int)pt] : Get(UciChess.GetPiece(pt, side));
    }

    public string Get(Piece p)
    {
        return ((int)p & 1) == 0 ? _piecestrings[(int)p / 2] : _piecestrings[(int)p / 2].ToLower();
    }
}