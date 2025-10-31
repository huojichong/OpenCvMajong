using OpenCvMajong.Core;

namespace OpenCvMajong;

public class GameBoard
{
    // 加上边界
    protected Cards[] Boards;
    protected Dictionary<Cards,List<CardPos>> CardPositions = new Dictionary<Cards, List<CardPos>>();
    
    
    protected List<MoveData> Moves = new List<MoveData>();
    
    public int Width { get; private set; }
    public int Height { get; private set; }
    public void InitBoard(Cards[,] initialBoard)
    {
        Boards = new Cards[initialBoard.GetLength(0) + 2 * initialBoard.GetLength(1) + 2];
        Clear();
        Width = initialBoard.GetLength(0) + 2;
        Height = initialBoard.GetLength(1) + 2;
        for (int x = 0; x < initialBoard.GetLength(0); x++)
        {
            for (int y = 0; y < initialBoard.GetLength(1); y++)
            {
                SetCard(x + 1,y+1, initialBoard[x,y]);

                if (initialBoard[x, y] != Cards.Zero)
                {
                    if (!CardPositions.ContainsKey(initialBoard[x, y]))
                    {
                        CardPositions[initialBoard[x, y]] = new List<CardPos>();
                    }

                    CardPositions[initialBoard[x, y]].Add(new CardPos { X = x + 1, Y = y + 1 });
                }
            }
        }
    }

    public void Clear()
    {
        Array.Fill(Boards,Cards.Guard,0,Boards.Length);
        CardPositions.Clear();
    }
    
    public bool IsEmpty(CardPos pos)
    {
        return Boards[pos.X *  + pos.Y] == Cards.Zero;
    }
    
    public bool IsEmpty(int posX,int posY)
    {
        return Boards[posX * Width + posY] == Cards.Zero;
    }
    
    public void SetCard(CardPos pos, Cards card)
    {
        Boards[pos.X * Width + pos.Y] = card;
    }
    
    public void SetCard(int posX,int posY, Cards card)
    {
        Boards[posX * Width + posY] = card;
    }

    public Cards GetCard(CardPos pos)
    {
        return Boards[pos.X * Width + pos.Y];
    }

    public Cards GetCard(int posX, int posY)
    {
        return Boards[posX * Width + posY];
    }

}