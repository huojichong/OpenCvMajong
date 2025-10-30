using OpenCvMajong.Core;

namespace OpenCvMajong;

public class GameBoard
{
    public Cards[,] Boards = new Cards[8, 10];
    public List<MoveData> Moves = new List<MoveData>();
    
    public int Width => Boards.GetLength(0);
    public int Height => Boards.GetLength(1);
    
    public void InitGame(Cards[,] initialBoard)
    {
        Boards = initialBoard;
    }

    public bool IsEmpty(CardPos pos)
    {
        return Boards[pos.X,pos.Y] == Cards.Zero;
    }
    
    public bool IsEmpty(int posX,int posY)
    {
        return Boards[posX,posY] == Cards.Zero;
    }
    
    public void SetCard(CardPos pos, Cards card)
    {
        Boards[pos.X, pos.Y] = card;
    }
}