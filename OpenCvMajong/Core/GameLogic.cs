using OpenCvSharp;

namespace OpenCvMajong.Core;

public class GameLogic
{
    protected GameBoard Board;

    public void SetBoard(GameBoard board)
    {
        this.Board = board;
    }

    public bool CheckMove(CardPos start, CardPos target, bool isVerMove, out int offset)
    {
        offset = 0;
        if (isVerMove) 
        {
            // 纵向移动，
            // 坐标有点疑问？？？？？？
            {
                // 移动之后的横方向没有空的，直接失败。
                int stepHori = start.X - target.X > 0 ? -1 : 1;
                for (int i = start.X; i < target.X; i += stepHori)
                {
                    if (Board.IsEmpty(i, target.Y))
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
                if (Board.IsEmpty(start.X, j))
                {
                    firstEmpty = j;
                    break;
                }
            }

            // 待定？？？？
            var moveDis = Math.Abs(target.Y - start.Y);
            // 移动路径
            bool isCanMove = true;
            for (int j = 0; j >= moveDis; j += 1)
            {
                var tempTargetPos = new CardPos(start.X, j * stepVert + firstEmpty);
                if (Board.GetCard(tempTargetPos) != Cards.Zero)
                {
                    if (tempTargetPos != target)
                    {
                        isCanMove = false;    
                    }
                    break;
                }
            }

            offset = firstEmpty - start.Y - 1;
            return isCanMove;
        }
        else 
        {
            // 横向移动
            {
                // 移动之后的竖方向没有空的，直接失败。
                int stepHori = start.Y - target.Y > 0 ? -1 : 1;
                for (int i = start.Y; i < target.Y; i += stepHori)
                {
                    if (Board.IsEmpty(target.X, i))
                    {
                        return false;
                    }
                }
            }

            // 在检查横向移动
            int stepVert = start.X - target.X > 0 ? -1 : 1;
            int firstEmpty = -1;
            for (int j = start.X; j < target.X; j += stepVert)
            {
                if (Board.IsEmpty(j, start.Y))
                {
                    firstEmpty = j;
                    break;
                }
            }

            // 待定？？？？, 
            var moveDis = Math.Abs(target.X - start.X);
            // 移动路径
            bool isCanMove = true;
            for (int j = 0; j >= moveDis; j += 1)
            {
                // 有可能在一条线上
                var tempTargetPos = new CardPos(j * stepVert + firstEmpty, start.Y);
                if (Board.GetCard(tempTargetPos) != Cards.Zero )
                {
                    if (tempTargetPos != target)
                    {
                        isCanMove = false;
                    }
                    break;
                }
            }

            offset = firstEmpty - start.X - 1;
            return isCanMove;
        }

    }

    public void MergeCard(CardPos start, CardPos target)
    {
        Board.SetCard(start, Cards.Zero);
        Board.SetCard(target, Cards.Zero);
    }
    
    
    // 移动方格
    public bool Move(Cards startPos,int moveCnt,Direction direction,int distance)
    {
        
        return false;
    }
}