namespace ChessAnalyzer.ChessLib;

internal struct UndoInfo
{
    private readonly int _data;

    public UndoInfo(int drawPlyCount, Piece capturedPiece, Square epSquare, int castles, bool isPromotion = false) : this()
    {
        _data = (drawPlyCount & 0xFF) + ((int)capturedPiece << 8) + ((int)epSquare << 16) + (castles << 24);
        if (isPromotion) _data |= 0x1000;
    }

    public int DrawPlyCount => _data & 0xFF;
    public Piece CapturedPiece => (Piece)((_data >> 8) & 0xF);
    public Square EpSquare => (Square)((_data >> 16) & 0xFF);
    public int Castles => (_data >> 24) & 0xFF;
    public bool IsPromotion => (_data & 0x1000) != 0;
}