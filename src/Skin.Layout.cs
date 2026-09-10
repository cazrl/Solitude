namespace Solitude;

public readonly record struct CaptionLayout(RectangleF Header,RectangleF Icon,RectangleF Title,RectangleF Minimize,RectangleF Maximize,RectangleF Close);

public sealed partial class Skin
{
    public CaptionLayout CaptionLayout(RectangleF bounds,bool modal=false,bool maximized=false)
    {
        var cap=new RectangleF(bounds.X+Border,bounds.Y+Border,bounds.Width-2*Border,Caption);
        if(Early)
        {
            var min=modal?RectangleF.Empty:new RectangleF(cap.Right-36,cap.Y,18,Caption-1);
            var max=modal?RectangleF.Empty:new RectangleF(cap.Right-18,cap.Y,18,Caption-1);
            return new(cap,new(cap.X,cap.Y,18,Caption-1),new(cap.X+19,cap.Y,cap.Width-(modal?20:56),cap.Height-1),min,max,RectangleF.Empty);
        }
        if(Vista)
        {
            // Measured against Microsoft's standard Vista DWM frame illustration.
            float height=maximized?21:27,top=bounds.Y+(maximized?0:1);
            var close=new RectangleF(bounds.Right-(maximized?0:7)-43,top,43,18);
            var max=modal?RectangleF.Empty:new RectangleF(close.X-25,top,25,18);
            var min=modal?RectangleF.Empty:new RectangleF(max.X-26,top,26,18);
            float textLeft=bounds.X+(modal?8:maximized?22:28),right=modal?close.Left:min.Left;
            return new(new(bounds.X,bounds.Y,bounds.Width,height),new(bounds.X+(maximized?2:8),bounds.Y+(maximized?2:7),16,16),new(textLeft,bounds.Y,Math.Max(0,right-textLeft-5),height),min,max,close);
        }
        int bw=Xp?21:16,bh=Xp?21:14;float by=cap.Y+(Caption-bh)/2;
        var c=new RectangleF(bounds.Right-Border-bw-1,by,bw,bh);
        var ma=modal?RectangleF.Empty:new RectangleF(c.X-bw-2,by,bw,bh);
        var mi=modal?RectangleF.Empty:new RectangleF(ma.X-bw-(Xp?2:0),by,bw,bh);
        var title=new RectangleF(cap.X+(modal?3:22),cap.Y-1,Math.Max(0,(modal?c.X:mi.X)-cap.X-(modal?8:27)),cap.Height);
        return new(new(bounds.X,bounds.Y,bounds.Width,Border+Caption),new(cap.X+2,MathF.Floor(cap.Y+(Caption-16)/2),16,16),title,mi,ma,c);
    }
    private void PaintCaptionIcon(Graphics g,RectangleF rectangle)
    {
        if(gameIconBitmap==null){DrawTinyIcon(g,rectangle.X,rectangle.Y);return;}
        // DrawIcon uses a native HDC and bypasses the Graphics scale/clip. A bitmap
        // obeys the same transform and clipping as the caption and its hit areas.
        var state=g.Save();
        if(Xp || Vista)g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(gameIconBitmap,rectangle,new RectangleF(0,0,gameIconBitmap.Width,gameIconBitmap.Height),GraphicsUnit.Pixel);g.Restore(state);
    }
}
