using System.Drawing;

public class Player
{
    public float X { get; set; } = 200;
    public float Y { get; set; } = 200;

    public float Width { get; private set; } = 32;
    public float Height { get; private set; } = 48;

    public int Direction { get; set; } = 0;
    public int Frame { get; set; } = 0;
    public float Speed { get; set; } = 2f;
    public bool IsMoving { get; set; } = false;

    private int animCounter = 0;
    private const int AnimDelay = 6;

    public void UpdateAnimation()
    {
        if (IsMoving)
        {
            animCounter++;
            if (animCounter >= AnimDelay)
            {
                Frame = (Frame + 1) % 4;
                animCounter = 0;
            }
        }
        else
        {
            Frame = 0;
            animCounter = 0;
        }
    }

    public void Draw(Graphics g, Bitmap spriteSheet)
    {
        if (spriteSheet == null)
        {
            Width = 32;
            Height = 48;
            using (SolidBrush debugBrush = new SolidBrush(Color.Magenta))
            {
                g.FillRectangle(debugBrush, X, Y, Width, Height);
            }
            return;
        }

        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        int frameWidth = spriteSheet.Width / 4;
        int frameHeight = spriteSheet.Height / 4;

        Width = frameWidth * 3f;
        Height = frameHeight * 3f;

        Rectangle srcRect = new Rectangle(Frame * frameWidth, Direction * frameHeight, frameWidth, frameHeight);
        Rectangle destRect = new Rectangle((int)X, (int)Y, (int)Width, (int)Height);

        g.DrawImage(spriteSheet, destRect, srcRect, GraphicsUnit.Pixel);
    }
}