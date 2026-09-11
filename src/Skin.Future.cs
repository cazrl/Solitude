using System.Drawing.Drawing2D;

namespace Solitude;

public sealed partial class Skin
{
    public Color FutureAccent { get; set; } = Color.FromArgb(119,232,218);
    public void FutureWindowOutline(Graphics g,RectangleF bounds,float scale,bool maximized=false)
    {
        var state=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;
        float pixel=1/scale;bounds.Inflate(-pixel/2,-pixel/2);
        using var rim=Rounded(bounds,maximized?0:Math.Max(0,12-pixel/2));
        using var pen=new Pen(Color.FromArgb(54,83,103),pixel);g.DrawPath(pen,rim);g.Restore(state);
    }
    private RectangleF[] FutureFrame(Graphics g,RectangleF bounds,string title,bool modal,bool active,PointF? pointer,bool down,bool maximized)
    {
        var s=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;using var shape=WindowShape(bounds,maximized);g.SetClip(shape,CombineMode.Intersect);
        if(modal)Fill(g,Color.FromArgb(8,17,29),bounds);using var outline=new Pen(Color.FromArgb(54,83,103));g.DrawPath(outline,shape);
        var layout=CaptionLayout(bounds,modal,maximized);Fill(g,Color.FromArgb(13,24,36),layout.Header);
        Line(g,Color.FromArgb(35,57,74),bounds.Left+1,layout.Header.Bottom,bounds.Right-1,layout.Header.Bottom);
        if(!modal)FutureArt.OrbitMark(g,layout.Icon,FutureAccent);
        Text(g,title,layout.Title,active?FutureArt.Ink:FutureArt.Muted,font:CaptionFont);
        var rects=new[]{layout.Minimize,layout.Maximize,layout.Close};
        for(int i=0;i<3;i++)
        {
            var r=rects[i];if(r.IsEmpty)continue;bool hover=pointer.HasValue && r.Contains(pointer.Value);
            if(hover)FutureArt.Panel(g,r,i==2?Color.FromArgb(139,57,72):Color.FromArgb(37,57,73),Color.Transparent,6);
            float x=r.X+r.Width/2+(hover && down?1:0),y=r.Y+r.Height/2;
            using var pen=new Pen(active?FutureArt.Ink:FutureArt.Muted,1.2f);
            if(i==0)g.DrawLine(pen,x-4,y+2,x+4,y+2);
            else if(i==1)g.DrawRectangle(pen,x-4,y-4,8,8);
            else {g.DrawLine(pen,x-3,y-3,x+3,y+3);g.DrawLine(pen,x+3,y-3,x-3,y+3);}
        }
        g.Restore(s);return rects;
    }
    private void FutureButton(Graphics g,RectangleF r,string label,bool hover,bool primary,bool enabled,bool focus,bool pressed)
    {
        var s=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;
        Color fill=primary?FutureAccent:hover?Color.FromArgb(40,64,81):Color.FromArgb(25,42,57);
        if(pressed)fill=primary?FutureAccent:Color.FromArgb(17,33,47);
        if(!enabled)fill=Color.FromArgb(19,31,43);
        FutureArt.Panel(g,r,fill,focus?Color.White:primary?FutureAccent:Color.FromArgb(63,91,109),6);
        FutureButtonLabel(g,label,r,enabled?(primary?Color.FromArgb(9,38,42):FutureArt.Ink):Color.FromArgb(117,138,154));g.Restore(s);
    }
}
