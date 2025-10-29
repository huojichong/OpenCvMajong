namespace OpenCvMajong;

public class GameBoard
{
    public Cards[,] Boards = new Cards[8, 10];
    
    public void InitGame(Cards[,] initialBoard)
    {
        Boards = initialBoard;
    }
    
    public bool IsCanMove(bool isVer, int x, int y)
    {
        if (isVer)
        {
            // 纵向移动
            if (y == 0 || y == Boards.GetLength(1) - 1)
                return true; // 顶部或底部可以移动
            if (Boards[x, y - 1] == Cards.Zero || Boards[x, y + 1] == Cards.Zero)
                return true; // 上下有空位可以移动
            return false;
        }
        else
        {
            // 横向移动
            if (x == 0 || x == Boards.GetLength(0) - 1)
                return true; // 左侧或右侧可以移动
            if (Boards[x - 1, y] == Cards.Zero || Boards[x + 1, y] == Cards.Zero)
                return true; // 左右有空位可以移动
            return false;
        }
        return false;
    }
}