namespace ChessAnalyzer.ChessLib.Interfaces;

public interface IChessPieceStringProvider
{
    public string Get(PieceType pt, Side side = Side.White);
    public string Get(Piece p);
}