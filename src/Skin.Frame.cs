using System.Drawing.Drawing2D;

namespace Solitude;

public sealed partial class Skin
{
    private void Dither(Graphics g,RectangleF r,bool active)
    {
        using var b=new HatchBrush(HatchStyle.Percent50,active?C(0,0,128):C(128,128,128),active?C(0,128,128):C(192,192,192));g.FillRectangle(b,r);
    }
    public RectangleF[] Frame(Graphics g,RectangleF bounds,string title,bool modal=false,bool active=true,PointF? pointer=null,bool down=false,bool maximized=false)
    {
        if(Future)return FutureFrame(g,bounds,title,modal,active,pointer,down,maximized);
        var saved=g.Save();using var shape=WindowShape(bounds,maximized);g.SetClip(shape,CombineMode.Intersect);
        var cap=new RectangleF(bounds.X+Border,bounds.Y+Border,bounds.Width-2*Border,Caption);
        var layout=CaptionLayout(bounds,modal,maximized);
        if(Early)
        {
            if(Win30)Dither(g,bounds,active);else Fill(g,active?Title:C(192,192,192),bounds);
            g.DrawRectangle(Pens.Black,bounds.X,bounds.Y,bounds.Width-1,bounds.Height-1);
            g.DrawRectangle(Pens.Black,cap.X-1,cap.Y-1,cap.Width+1,bounds.Height-Border*2+1);
            if(Win30)Dither(g,cap,active);else Fill(g,active?Title:Color.White,cap);
            Line(g,Color.Black,cap.X,cap.Bottom,cap.Right,cap.Bottom);
            var sys=new RectangleF(cap.X,cap.Y,18,Caption-1);EarlyTitleButton(g,sys,3,down && pointer.HasValue && sys.Contains(pointer.Value));
            RectangleF min=RectangleF.Empty,max=RectangleF.Empty;
            if(!modal)
            {
                min=new(cap.Right-36,cap.Y,18,Caption-1);max=new(cap.Right-18,cap.Y,18,Caption-1);
                EarlyTitleButton(g,min,0,down && pointer.HasValue && min.Contains(pointer.Value));EarlyTitleButton(g,max,maximized?4:1,down && pointer.HasValue && max.Contains(pointer.Value));
            }
            Text(g,title,new(cap.X+19,cap.Y,cap.Width-(modal?20:56),cap.Height-1),active?Color.White:Color.Black,CaptionFont,true);
            g.Restore(saved);return [min,max,RectangleF.Empty];
        }
        if(Xp)
        {
            Fill(g,active?C(0,85,234):C(122,151,223),bounds);
            var head=new RectangleF(bounds.X,bounds.Y,bounds.Width,Border+Caption);
            if(active)PaintXpCaption(g,head);
            else GradientFill(g,head,[C(156,180,232),C(130,159,225),C(119,148,218),C(108,137,205)],[0,.15f,.82f,1]);
            using var rim=new Pen(active?C(0,44,149):C(85,113,183));g.DrawPath(rim,shape);
            if(!active)Line(g,C(173,194,235),bounds.X+8,bounds.Y+1,bounds.Right-9,bounds.Y+1);
            Line(g,active?C(51,132,255):C(149,174,227),bounds.X+1,bounds.Y+8,bounds.X+1,bounds.Bottom-2);
            Line(g,active?C(0,45,164):C(91,121,188),bounds.Right-2,bounds.Y+8,bounds.Right-2,bounds.Bottom-2);
            Line(g,active?C(0,45,164):C(91,121,188),bounds.Left+2,bounds.Bottom-2,bounds.Right-2,bounds.Bottom-2);
        }
        else if(Vista)
        {
            PaintVistaFrame(g,bounds,active,maximized);
        }
        else
        {
            Fill(g,Face,bounds);Box(g,bounds);
            if(Gradient)GradientFill(g,cap,active?[Title,Era==Era.Windows98?C(16,132,208):C(166,202,240)]:[C(128,128,128),C(192,192,192)],[0,1],0);
            else Fill(g,active?Title:C(128,128,128),cap);
        }
        var close=layout.Close;var maxButton=layout.Maximize;var minButton=layout.Minimize;
        if(!modal)PaintCaptionIcon(g,layout.Icon);
        var titleRect=layout.Title;
        if(Vista && !maximized)PaintCaptionGlow(g,title,titleRect);
        if(Xp && active){var shadow=titleRect;shadow.Offset(1,1);Text(g,title,shadow,C(0,34,110),CaptionFont);}
        Text(g,title,titleRect,Vista?(maximized?Color.White:Color.Black):active?Color.White:C(222,228,241),CaptionFont);
        if(!modal){WindowButton(g,minButton,0,active,pointer,down);WindowButton(g,maxButton,maximized?4:1,active,pointer,down);}
        WindowButton(g,close,2,active,pointer,down,modal);
        g.Restore(saved);return [minButton,maxButton,close];
    }
    private void EarlyTitleButton(Graphics g,RectangleF r,int type,bool pressed)
    {
        Fill(g,C(192,192,192),r);g.DrawRectangle(Pens.Black,r.X,r.Y,r.Width,r.Height);
        if(!Win30){Line(g,pressed?Color.Gray:Color.White,r.X+1,r.Y+1,r.Right-1,r.Y+1);Line(g,pressed?Color.Gray:Color.White,r.X+1,r.Y+1,r.X+1,r.Bottom-1);}
        float x=r.X+(pressed?1:0),y=r.Y+(pressed?1:0);
        if(type==3){Fill(g,Color.White,new(x+3,y+7,11,3));g.DrawRectangle(Pens.Black,x+3,y+7,11,3);return;}
        if(type==0)g.FillPolygon(Brushes.Black,new PointF[]{new(x+5,y+7),new(x+13,y+7),new(x+9,y+11)});
        else if(type==1)g.FillPolygon(Brushes.Black,new PointF[]{new(x+5,y+11),new(x+13,y+11),new(x+9,y+7)});
        else {g.FillPolygon(Brushes.Black,new PointF[]{new(x+5,y+8),new(x+13,y+8),new(x+9,y+4)});g.FillPolygon(Brushes.Black,new PointF[]{new(x+5,y+10),new(x+13,y+10),new(x+9,y+14)});}
    }
    private void WindowButton(Graphics g,RectangleF r,int type,bool active,PointF? pointer,bool down,bool lone=false)
    {
        bool hover=pointer.HasValue && r.Contains(pointer.Value),pressed=hover && down;
        if(Vista){PaintVistaButton(g,r,type,active,hover,pressed,lone);return;}
        if(Xp || Vista)
        {
            var state=g.Save();using var path=Rounded(r,3);g.SetClip(path,CombineMode.Intersect);
            Color top,bottom;
            if(type==2){top=active?C(239,163,139):C(203,179,174);bottom=active?C(190,43,18):C(165,116,112);}
            else if(Vista){top=C(207,225,234);bottom=C(92,131,151);}
            else {top=active?C(122,178,255):C(157,181,231);bottom=active?C(13,70,207):C(122,147,205);}
            if(hover){top=type==2?C(255,200,149):C(173,225,255);bottom=type==2?C(237,58,21):C(15,136,229);}
            if(pressed)(top,bottom)=(bottom,top);
            if(Xp && active)PaintXpButtonFace(g,r,type==2,hover,pressed);
            else if(Xp)GradientFill(g,r,[top,bottom],[0,1]);
            else GradientFill(g,r,[top,top,bottom,bottom],[0,.42f,.48f,1]);
            g.Restore(state);
            using var edge=new Pen(Vista?C(61,85,100):Color.White);g.DrawPath(edge,path);
            if(!Xp)Line(g,Color.FromArgb(150,255,255,255),r.X+2,r.Y+1,r.Right-3,r.Y+1);
        }
        else {Fill(g,Face,r);Box(g,r,pressed);}
        var color=Xp?Color.White:Vista?C(21,39,51):Color.Black;
        float cx=MathF.Floor(r.X+r.Width/2)+(pressed?1:0),cy=MathF.Floor(r.Y+r.Height/2)+(pressed?1:0);
        if(Vista && type==2){using var shadow=new Pen(Color.FromArgb(160,255,255,255),3);g.DrawLine(shadow,cx-3,cy-3,cx+3,cy+3);g.DrawLine(shadow,cx+3,cy-3,cx-3,cy+3);}
        using var pen=new Pen(color,type==2?2:1);
        if(type==0)Fill(g,color,new(cx-4,cy+3,7,2));
        if(type==1){g.DrawRectangle(pen,cx-4,cy-4,8,7);g.DrawLine(pen,cx-4,cy-3,cx+4,cy-3);}
        if(type==2){g.DrawLine(pen,cx-3,cy-3,cx+3,cy+3);g.DrawLine(pen,cx+3,cy-3,cx-3,cy+3);}
        if(type==4){g.DrawRectangle(pen,cx-2,cy-4,6,5);Fill(g,Xp?C(44,111,222):Vista?C(162,191,207):Face,new(cx-5,cy-1,7,6));g.DrawRectangle(pen,cx-5,cy-1,6,5);g.DrawLine(pen,cx-5,cy,cx+1,cy);}
    }
}
