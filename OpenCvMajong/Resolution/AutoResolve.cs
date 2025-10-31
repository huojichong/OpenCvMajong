using OpenCvMajong.Core;

namespace OpenCvMajong.Resolution;

public class AutoResolve
{
    public GameBoard Board;
    public GameLogic Logic;
    
    public void Init(Cards[,] initBoard)
    {
        Board = new GameBoard();
        Board.InitBoard(initBoard);
        
        Logic = new GameLogic();
        Logic.SetBoard(Board);
    }
    
    public void Execute()
    {
        
    }

    public void PrintResule(GameBoard board)
    {
        
    }
}