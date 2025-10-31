namespace OpenCvMajong.Core;

public struct CardPos
{
    public int X { get; set; }
    public int Y { get; set; }

    public CardPos(int x, int y)
    {
        X = x;
        Y = y;
    }

    public CardPos()
    {
    }

    public static bool operator ==(CardPos p1, CardPos p2)
    {
        return p1.X == p2.X && p1.Y == p2.Y;
    }

    public static bool operator !=(CardPos p1, CardPos p2)
    {
        return p1.X != p2.X || p1.Y != p2.Y;;
    }
}