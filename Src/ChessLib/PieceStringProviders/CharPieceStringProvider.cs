using ChessAnalyzer.ChessLib.Interfaces;

namespace ChessAnalyzer.ChessLib.PieceStringProviders;

internal class CharPieceStringProvider : IChessPieceStringProvider
{
    readonly string _piecechars;

    public CharPieceStringProvider(string piecechars)
    {
        _piecechars = piecechars;
    }

    public string Get(PieceType pt, Side side = Side.White)
    {
        return side == Side.White ? _piecechars[(int)pt].ToString() : Get(UciChess.GetPiece(pt, side));
    }

    public string Get(Piece p)
    {
        return ((int)p & 1) == 0 ? _piecechars[(int)p / 2].ToString() : char.ToLower(_piecechars[(int)p / 2]).ToString();
    }
}