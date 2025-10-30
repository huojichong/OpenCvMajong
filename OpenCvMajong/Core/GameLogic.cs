using OpenCvSharp;

namespace OpenCvMajong.Core;

public class GameLogic
{
    public GameBoard Board;
    
    public GameLogic()
    {
        Board = new GameBoard();
    }
    
    public bool CheckMove(CardPos start, CardPos target, bool isVerMove)
    {
        if (isVerMove)// 纵向移动，
        {
            {
                // 移动之后的横方向没有空的，直接失败。
                int stepHori = start.X - target.X > 0 ? -1 : 1;
                for (int i = start.X; i < target.X; i += stepHori)
                {
                    if (Board.IsEmpty(target.X, i))
                    {
                        return false;
                    }
                }
            }
            
            // 在检查纵向移动
            int stepVert = start.Y - target.Y > 0 ? -1 : 1;
            int firstEmpty = -1;
            for (int j = start.Y; j < target.Y; j += stepVert)
            {
                if (Board.IsEmpty(j, target.Y))
                {
                    firstEmpty = j;
                    break;
                }
            }

            var moveDis = target.Y - start.Y;
            // 移动路径
            for (int j = firstEmpty - stepVert; j >= start.Y; j -= stepVert)
            {
                
            }


        }
        else
        {
            
        }

        return false;
    }

    public void MergeCard(CardPos start, CardPos target)
    {
        Board.SetCard(start, Cards.Zero);
        Board.SetCard(target, Cards.Zero);
    }
    
}