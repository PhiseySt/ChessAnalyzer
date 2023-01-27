using ChessAnalyzer.ChessLib.Interfaces;

namespace ChessAnalyzer.ChessLib.PieceStringProviders;

public class FigurinePieceStringProvider : IChessPieceStringProvider
{
    readonly char[] upiece = { '\u2655', '\u265b', '\u2656', '\u265c', '\u2657', '\u265d', '\u2658', '\u265e', '\u2659', '\u265f', '\u2654', '\u265a', '\u0020' };

    public string Get(PieceType pt, Side side = Side.White)
    {
        return upiece[2 * (int)pt + (int)side].ToString();
    }

    public string Get(Piece p)
    {
        return upiece[(int)p].ToString();
    }

    public static IChessPieceStringProvider Instance { get; private set; } = new FigurinePieceStringProvider();

}