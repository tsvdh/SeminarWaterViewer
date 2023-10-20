public class Cell
{
    public int X, Y;
    public float H, Qx, Qy;

    public Cell(int x, int y, float h, float qx, float qy)
    {
        X = x;
        Y = y;
        H = h;
        Qx = qx;
        Qy = qy;
    }
}