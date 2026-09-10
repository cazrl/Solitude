using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Solitude;

public sealed partial class Skin
{
    // Colour samples from the lossless Blue Luna Calculator capture recorded in
    // docs/XP-COLOUR.md. Caption rows and glyph-free button columns are independent
    // samples; no white wash is composited over the active button faces.
    private static readonly Color[] xpCaptionRows=[C(0,88,238),C(63,151,255),C(43,144,255),C(3,114,255),C(3,101,241),C(0,92,233),C(0,88,230),C(0,84,227),C(0,84,227),C(0,84,227),C(0,85,229),C(0,85,229),C(0,85,229),C(0,85,229),C(0,85,234),C(0,85,234),C(0,88,238),C(0,91,242),C(0,90,245),C(0,96,249),C(1,100,249),C(2,106,254),C(2,106,254),C(2,106,254),C(2,106,254),C(0,101,253),C(0,96,252),C(0,77,227),C(0,67,207),C(0,67,207)];
    private static readonly (Color Left,Color Right)[] xpBlueRows=[
        (C(85,132,246),C(40,100,244)),
        (C(133,167,248),C(63,117,245)),
        (C(137,170,249),C(67,119,245)),
        (C(127,163,248),C(58,114,244)),
        (C(113,153,248),C(49,107,244)),
        (C(106,147,248),C(45,105,245)),
        (C(101,143,248),C(42,104,245)),
        (C(96,139,247),C(37,103,245)),
        (C(93,137,247),C(34,104,245)),
        (C(89,134,247),C(31,106,246)),
        (C(86,132,247),C(28,109,246)),
        (C(82,130,246),C(24,110,247)),
        (C(78,128,246),C(22,111,247)),
        (C(73,123,245),C(17,109,247)),
        (C(66,119,245),C(14,108,248)),
        (C(57,113,244),C(11,104,247)),
        (C(47,104,240),C(7,96,243)),
        (C(33,90,226),C(4,84,230)),
        (C(17,68,189),C(1,63,195))];
    private static readonly (Color Left,Color Right)[] xpRedRows=[
        (C(231,113,85),C(224,77,40)),
        (C(238,154,133),C(228,96,63)),
        (C(238,157,137),C(228,99,67)),
        (C(237,149,127),C(227,92,58)),
        (C(235,138,113),C(226,85,49)),
        (C(234,131,106),C(226,82,45)),
        (C(233,126,101),C(226,80,42)),
        (C(232,122,96),C(226,77,37)),
        (C(232,118,93),C(227,77,34)),
        (C(232,115,89),C(229,77,31)),
        (C(231,113,86),C(230,77,28)),
        (C(231,111,82),C(231,77,24)),
        (C(230,108,78),C(232,76,22)),
        (C(229,103,73),C(233,74,17)),
        (C(228,98,66),C(233,72,14)),
        (C(227,91,57),C(232,68,11)),
        (C(222,82,47),C(228,62,7)),
        (C(207,68,33),C(213,53,4)),
        (C(172,48,17),C(180,39,1))];
    private readonly Dictionary<(bool,bool,bool,Size),Bitmap> xpButtonFaces=[];
    private void PaintXpCaption(Graphics g,RectangleF bounds)
    {
        for(int y=0;y<xpCaptionRows.Length;y++)
            Fill(g,xpCaptionRows[y],new(bounds.X,bounds.Y+y,bounds.Width,1));
    }
    private static Color XpMix(Color left,Color right,float amount)
    {
        amount=Math.Clamp(amount,0,1);
        return Color.FromArgb((int)MathF.Round(left.R+(right.R-left.R)*amount),(int)MathF.Round(left.G+(right.G-left.G)*amount),(int)MathF.Round(left.B+(right.B-left.B)*amount));
    }
    private void PaintXpButtonFace(Graphics g,RectangleF bounds,bool close,bool hover,bool pressed)
    {
        using var transform=g.Transform;PointF[] corners=[bounds.Location,new(bounds.Right,bounds.Bottom)];transform.TransformPoints(corners);
        var device=Rectangle.FromLTRB((int)MathF.Round(corners[0].X),(int)MathF.Round(corners[0].Y),(int)MathF.Round(corners[1].X),(int)MathF.Round(corners[1].Y));
        if(device.Width<=0 || device.Height<=0)return;
        var key=(close,hover,pressed,device.Size);
        if(!xpButtonFaces.TryGetValue(key,out var bitmap))
        {
            if(xpButtonFaces.Count>=32){foreach(var old in xpButtonFaces.Values)old.Dispose();xpButtonFaces.Clear();}
            bitmap=new Bitmap(device.Width,device.Height,PixelFormat.Format32bppPArgb);
            var rows=close?xpRedRows:xpBlueRows;
            for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++)
            {
                int sy=Math.Clamp((int)((y+.5f)*21/bitmap.Height)-1,0,18);
                float sx=(x+.5f)*21/bitmap.Width-.5f;
                var colour=XpMix(rows[sy].Left,rows[sy].Right,(sx-4)/12);
                if(pressed)colour=XpMix(colour,Color.Black,.22f);
                else if(hover)colour=XpMix(colour,Color.White,.16f);
                bitmap.SetPixel(x,y,colour);
            }
            xpButtonFaces[key]=bitmap;
        }
        var saved=g.Save();g.ResetTransform();g.DrawImageUnscaled(bitmap,device.Location);g.Restore(saved);
    }
}
