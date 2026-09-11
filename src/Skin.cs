using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Solitude;

public sealed partial class Skin : IDisposable
{
    public Era Era { get; }
    public bool Early => Era is Era.Windows30 or Era.Windows31;
    public bool Win30 => Era==Era.Windows30;
    public bool Xp => Era==Era.WindowsXP;
    public bool Vista => Era==Era.WindowsVista;
    public bool Future => Era==Era.Future2126;
    public bool Modern => Vista || Future;
    public bool DeviceText => raster==null;
    public bool Warm => Era is Era.WindowsMe or Era.Windows2000;
    public bool Gradient => Era is Era.Windows98 or Era.WindowsMe or Era.Windows2000;
    public int Border => Future?1:Early?5:Vista?8:3;
    public int Caption => Future?35:Early?19:Vista?19:Xp?27:18;
    public int MenuHeight => Future?74:Early?20:Vista?20:19;
    public int StatusHeight => Future?76:Early?18:Vista?23:18;
    public int Top => Border+Caption+MenuHeight;
    public Color Face => Future?C(16,26,38):Early?Color.White:Xp?C(236,233,216):Vista?C(240,240,240):Warm?C(212,208,200):C(192,192,192);
    public Color ButtonFace => Early?C(192,192,192):Face;
    public Color Title => Warm?C(10,36,106):C(0,0,128);
    public Color Selection => Future?C(28,99,104):Xp?C(49,106,197):Vista?C(51,153,255):Title;
    public Font Ui { get; }
    public Font Bold { get; }
    public Font CaptionFont { get; }
    private Icon? gameIcon;
    private Bitmap? gameIconBitmap;
    public Icon? GameIcon
    {
        get=>gameIcon;
        set{if(ReferenceEquals(gameIcon,value))return;gameIconBitmap?.Dispose();gameIcon=value;gameIconBitmap=value?.ToBitmap();}
    }
    private readonly RasterFont? raster;
    public bool BitmapFontAvailable => raster!=null;
    public static readonly string[] Names=["Windows 3.0","Windows 3.1 / 3.11","Windows 95","Windows 98","Windows Me","Windows 2000","Windows XP","Windows Vista","ORBIT / Solitude 2126"];
    public static readonly string[] Years=["1990","1992","1995","1998","2000","2000","2001","2007","2126"];
    public static readonly string[] Characteristics=[
        "Dithered blue frame\nSystem bitmap lettering\nWhite menus and dialogs",
        "Solid blue frame\nSystem bitmap lettering\nArrow window buttons",
        "Solid navy title bar\nSilver beveled controls\nMS Sans Serif lettering",
        "Blue gradient title bar\nSilver beveled controls\nMS Sans Serif lettering",
        "Slate blue gradient\nWarm gray controls\nMS Sans Serif lettering",
        "Slate blue gradient\nWarm gray controls\nTahoma lettering",
        "Rounded Luna frame\nBlue and red title buttons\nCream dialog surfaces",
        "Aero glass-style frame\nOriginal felt artwork\nSegoe UI lettering",
        "An orbital observatory\nOriginal porcelain cards\nSpatial motion and light"];
    public Skin(Era era)
    {
        Era=era;
        string family=Modern?"Segoe UI":era is Era.Windows2000 or Era.WindowsXP?"Tahoma":"Microsoft Sans Serif";
        Ui=new(family,Future?13:Modern?12:Early?13:11,Early?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);
        Bold=new(family,Future?13:Vista?12:Early?13:11,FontStyle.Bold,GraphicsUnit.Pixel);
        CaptionFont=new(Xp?"Trebuchet MS":family,Xp?13:Modern?12:Early?13:11,Modern?FontStyle.Regular:FontStyle.Bold,GraphicsUnit.Pixel);
        if(Early)raster=RasterFont.Embedded("system-10.fnt");
        else if(era is Era.Windows95 or Era.Windows98 or Era.WindowsMe)raster=RasterFont.Embedded("ms-sans-serif-8.fnt");
    }
    private static Color C(int r,int g,int b)=>Color.FromArgb(r,g,b);
    public void Configure(Graphics g)
    {
        g.SmoothingMode=SmoothingMode.None;
        g.TextRenderingHint=DeviceText?TextRenderingHint.AntiAliasGridFit:TextRenderingHint.SingleBitPerPixelGridFit;
        g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
    }
    private bool UseRaster(Graphics g)=>raster!=null;
    public float Measure(Graphics g,string text,Font? font=null)=>UseRaster(g)?raster!.Measure(text,!Early && (font==Bold || font==CaptionFont)):MeasureNative(g,text,font??Ui);
    public void Text(Graphics g,string text,RectangleF r,Color? color=null,Font? font=null,bool center=false)
    {
        if(UseRaster(g)){raster!.Draw(g,text,r,color??Color.Black,!Early && (font==Bold || font==CaptionFont),center);return;}
        DrawNative(g,text,r,color??(Future?Color.FromArgb(231,239,244):Color.Black),font??Ui,center,false);
    }
    public void Wrapped(Graphics g,string text,RectangleF r,Color? color=null)
    {
        if(UseRaster(g)){raster!.Draw(g,text,r,color??Color.Black,wrap:true);return;}
        DrawNative(g,text,r,color??(Future?Color.FromArgb(231,239,244):Color.Black),Ui,false,true);
    }
    public void Mnemonic(Graphics g,string text,RectangleF r,Color color,bool center=false)
    {
        int marker=text.IndexOf('&');string label=text.Replace("&","");Text(g,label,r,color,center:center);
        if(marker>=0 && marker<label.Length)
        {float x=r.X+(center?(r.Width-Measure(g,label))/2:0)+Measure(g,label[..marker]),y=r.Y+(r.Height+(raster?.Height??Ui.Height))/2-1;Line(g,color,x,y,x+Measure(g,label[marker..(marker+1)])-1,y);}
    }
    public static void Fill(Graphics g,Color color,RectangleF r){using var b=new SolidBrush(color);g.FillRectangle(b,r);}
    public static void Line(Graphics g,Color color,float x1,float y1,float x2,float y2){using var p=new Pen(color);g.DrawLine(p,x1,y1,x2,y2);}
    public static void Box(Graphics g,RectangleF r,bool sunken=false)
    {
        var tl=sunken?C(128,128,128):Color.White;var br=sunken?Color.White:Color.Black;
        Line(g,tl,r.Left,r.Bottom-1,r.Left,r.Top);Line(g,tl,r.Left,r.Top,r.Right-1,r.Top);
        Line(g,br,r.Left,r.Bottom-1,r.Right-1,r.Bottom-1);Line(g,br,r.Right-1,r.Top,r.Right-1,r.Bottom-1);
        r.Inflate(-1,-1);tl=sunken?Color.Black:C(223,223,223);br=sunken?C(223,223,223):C(128,128,128);
        Line(g,tl,r.Left,r.Bottom-1,r.Left,r.Top);Line(g,tl,r.Left,r.Top,r.Right-1,r.Top);
        Line(g,br,r.Left,r.Bottom-1,r.Right-1,r.Bottom-1);Line(g,br,r.Right-1,r.Top,r.Right-1,r.Bottom-1);
    }
    public static GraphicsPath Rounded(RectangleF r,float radius,bool roundBottom=true)
    {
        var p=new GraphicsPath();float d=radius*2;
        if(radius<=0){p.AddRectangle(r);return p;}
        p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);
        if(roundBottom){p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);}
        else p.AddLine(r.Right,r.Bottom,r.Left,r.Bottom);
        p.CloseFigure();return p;
    }
    public GraphicsPath WindowShape(RectangleF r,bool maximized=false)=>Rounded(r,maximized?0:Future?12:Vista?7:Xp?8:0,Vista || Future);
    private static void GradientFill(Graphics g,RectangleF r,Color[] colors,float[] positions,float angle=90)
    {
        using var b=new LinearGradientBrush(r,colors[0],colors[^1],angle){InterpolationColors=new ColorBlend{Colors=colors,Positions=positions}};g.FillRectangle(b,r);
    }
    public void Button(Graphics g,RectangleF r,string text,bool hover=false,bool primary=false,bool enabled=true,bool focus=false,bool pressed=false)
    {
        if(Future){FutureButton(g,r,text,hover,primary,enabled,focus,pressed);return;}
        if(Early)
        {
            if(primary)g.DrawRectangle(Pens.Black,r.X-2,r.Y-2,r.Width+3,r.Height+3);
            Fill(g,ButtonFace,r);g.DrawRectangle(Pens.Black,r.X,r.Y,r.Width-1,r.Height-1);
            if(!pressed){Line(g,Color.White,r.X+1,r.Y+1,r.Right-3,r.Y+1);Line(g,Color.White,r.X+1,r.Y+1,r.X+1,r.Bottom-3);Fill(g,Color.Gray,new(r.X+2,r.Bottom-3,r.Width-3,2));Fill(g,Color.Gray,new(r.Right-3,r.Y+2,2,r.Height-3));}
        }
        else if(Xp || Vista)
        {
            var state=g.Save();using var path=Rounded(new(r.X,r.Y,r.Width-1,r.Height-1),Xp?3:2);g.SetClip(path,CombineMode.Intersect);
            Color top=Xp?Color.White:C(246,246,246),bottom=Xp?C(221,218,207):C(207,207,207);
            if(hover){top=C(239,249,255);bottom=C(166,217,243);}if(pressed){top=C(183,218,234);bottom=C(222,241,250);}
            if(Xp)GradientFill(g,r,[top,bottom],[0,1]);else GradientFill(g,r,[top,top,bottom,bottom],[0,.46f,.5f,1]);
            g.Restore(state);
            using var edge=new Pen(Xp?C(0,60,116):primary?C(60,127,177):C(112,112,112));g.DrawPath(edge,path);
            if(Xp && (primary || hover)){using var inset=new Pen(hover?C(240,178,66):C(142,191,239),2);g.DrawRectangle(inset,r.X+2,r.Y+2,r.Width-5,r.Height-5);}
            else if(primary || hover){using var inset=new Pen(C(150,207,238));g.DrawRectangle(inset,r.X+1,r.Y+1,r.Width-3,r.Height-3);}
        }
        else {Fill(g,Face,r);if(primary)g.DrawRectangle(Pens.Black,r.X-1,r.Y-1,r.Width+1,r.Height+1);Box(g,r,pressed);}
        if(pressed)r.Offset(1,1);
        if(text.Contains('&'))Mnemonic(g,text,r,enabled?Color.Black:Color.Gray,center:true);
        else Text(g,text,r,enabled?Color.Black:Color.Gray,center:true);
        if(focus){var f=r;f.Inflate(-4,-4);using var p=new Pen(Color.Black){DashStyle=DashStyle.Dot};g.DrawRectangle(p,f.X,f.Y,f.Width,f.Height);}
    }
    public void Check(Graphics g,RectangleF r,string label,bool selected,bool radio=false,bool hover=false,bool pressed=false,bool enabled=true)
    {
        if(Future)
        {
            var saved=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;var futureBox=new RectangleF(r.X,r.Y+(r.Height-14)/2,14,14);
            using var edge=new Pen(hover?FutureAccent:Color.FromArgb(103,136,151));using var fill=new SolidBrush(Color.FromArgb(21,40,55));
            if(radio){g.FillEllipse(fill,futureBox);g.DrawEllipse(edge,futureBox);if(selected){using var b=new SolidBrush(FutureAccent);g.FillEllipse(b,futureBox.X+4,futureBox.Y+4,6,6);}}
            else {FutureArt.Panel(g,futureBox,selected?FutureAccent:Color.FromArgb(21,40,55),edge.Color,3);if(selected){using var p=new Pen(Color.FromArgb(8,38,40),1.8f);g.DrawLines(p,new PointF[]{new(futureBox.X+3,futureBox.Y+7),new(futureBox.X+6,futureBox.Y+10),new(futureBox.X+11,futureBox.Y+4)});}}
            Mnemonic(g,label,new(r.X+23,r.Y,r.Width-23,r.Height),enabled?FutureArt.Ink:FutureArt.Muted);g.Restore(saved);return;
        }
        var box=new RectangleF(r.X,MathF.Round(r.Y+(r.Height-13)/2),13,13);
        if(radio)
        {
            if(Early){g.DrawEllipse(Pens.Black,box.X+1,box.Y+1,11,11);if(selected)g.FillEllipse(Brushes.Black,box.X+4,box.Y+4,5,5);}
            else
            {
                using var fill=new LinearGradientBrush(box,Xp?C(220,220,215):C(213,213,213),Color.White,45);g.FillEllipse(fill,box);
                using var edge=new Pen(Xp?C(29,82,129):Color.Gray);g.DrawEllipse(edge,box.X,box.Y,12,12);
                if(hover && (Xp || Vista)){using var h=new Pen(Xp?C(240,178,66):C(88,163,202),2);g.DrawEllipse(h,box.X+2,box.Y+2,8,8);}
                if(selected){using var dot=new SolidBrush(Xp?C(33,161,33):Vista?C(61,111,134):Color.Black);g.FillEllipse(dot,box.X+4,box.Y+4,5,5);}
            }
        }
        else
        {
            Fill(g,pressed?ButtonFace:Color.White,box);
            if(Early)g.DrawRectangle(Pens.Black,box.X,box.Y,12,12);
            else if(Xp || Vista)
            {
                using var fill=new LinearGradientBrush(box,C(222,222,216),Color.White,45);g.FillRectangle(fill,box);using var p=new Pen(Xp?C(29,82,129):Color.Gray);g.DrawRectangle(p,box.X,box.Y,12,12);
                if(hover){using var h=new Pen(Xp?C(240,178,66):C(88,163,202));g.DrawRectangle(h,box.X+1,box.Y+1,10,10);}
            }
            else Box(g,box,true);
            if(selected)
            {
                using var p=new Pen(Xp?C(33,161,33):Color.Black,Early?1:2);
                if(Early){g.DrawLine(p,box.X+2,box.Y+2,box.X+10,box.Y+10);g.DrawLine(p,box.X+10,box.Y+2,box.X+2,box.Y+10);}
                else g.DrawLines(p,new PointF[]{new(box.X+3,box.Y+6),new(box.X+5,box.Y+8),new(box.X+10,box.Y+3)});
            }
        }
        if(!enabled){using var veil=new SolidBrush(Color.FromArgb(135,Face));g.FillRectangle(veil,box);}
        Mnemonic(g,label,new(r.X+19,r.Y,r.Width-19,r.Height),enabled?Color.Black:Color.Gray);
    }
    public void Group(Graphics g,string label,RectangleF r)
    {
        if(Future){FutureArt.Panel(g,new(r.X,r.Y+7,r.Width,r.Height-7),Face,Color.FromArgb(63,86,104),7);float width=Measure(g,label);Fill(g,Face,new(r.X+10,r.Y,width+10,17));Text(g,label,new(r.X+15,r.Y-1,width+2,18),FutureArt.Muted);return;}
        using var pen=new Pen(Early?Color.Black:Xp?C(208,208,191):C(128,128,128));
        if(!Early && !Xp && !Vista)g.DrawRectangle(Pens.White,r.X+1,r.Y+7,r.Width-1,r.Height-7);
        g.DrawRectangle(pen,r.X,r.Y+6,r.Width,r.Height-6);
        float tw=Measure(g,label);Fill(g,Face,new(r.X+8,r.Y,tw+6,16));Text(g,label,new(r.X+11,r.Y-1,tw+2,r.Height>17?17:r.Height),Xp?C(0,70,213):null);
    }
    public void MenuBar(Graphics g,RectangleF r)
    {
        if(Vista)GradientFill(g,r,[Color.White,C(229,234,245),C(211,218,237),C(234,238,246)],[0,.45f,.5f,1]);else Fill(g,Face,r);
        Line(g,Early?Color.Black:Vista?C(182,188,204):C(128,128,128),r.Left,r.Bottom-1,r.Right,r.Bottom-1);
    }
    public void MenuItem(Graphics g,RectangleF r,bool selected,bool bar=false)
    {
        if(!selected)return;
        if(Vista || (Xp && bar)){Fill(g,Vista?C(213,230,246):C(193,210,238),r);using var pen=new Pen(Vista?C(152,180,208):C(49,106,197));g.DrawRectangle(pen,r.X,r.Y,r.Width-1,r.Height-1);}
        else if(bar && !Early){Fill(g,Face,r);Box(g,r,true);}else Fill(g,Selection,r);
    }
    public void Popup(Graphics g,RectangleF r)
    {
        if(Xp || Vista)Fill(g,Color.FromArgb(65,0,0,0),new(r.X+3,r.Y+3,r.Width,r.Height));
        Fill(g,Early?Color.White:Vista?C(240,240,240):Xp?Color.White:Face,r);
        if(Early){g.DrawRectangle(Pens.Black,r.X,r.Y,r.Width-1,r.Height-1);return;}
        if(Xp || Vista){using var pen=new Pen(C(128,128,128));g.DrawRectangle(pen,r.X,r.Y,r.Width-1,r.Height-1);Fill(g,Xp?Face:C(232,232,232),new(r.X+2,r.Y+2,23,r.Height-4));if(Vista){Line(g,C(215,215,215),r.X+26,r.Y+3,r.X+26,r.Bottom-3);Line(g,Color.White,r.X+27,r.Y+3,r.X+27,r.Bottom-3);}}else Box(g,r);
    }
    public static void DrawTinyIcon(Graphics g,float x,float y)
    {
        Fill(g,Color.White,new(x+1,y,9,12));g.DrawRectangle(Pens.Black,x+1,y,9,12);
        Fill(g,Color.White,new(x+5,y+3,9,12));g.DrawRectangle(Pens.Black,x+5,y+3,9,12);
        g.FillPolygon(Brushes.Red,new PointF[]{new(x+10,y+5),new(x+13,y+8),new(x+10,y+11),new(x+7,y+8)});Fill(g,Color.Black,new(x+3,y+2,2,2));
    }
    public void Dispose(){foreach(var item in xpButtonFaces.Values)item.Dispose();foreach(var item in nativeText.Values)item.Dispose();foreach(var item in vistaParts.Values)item.Dispose();foreach(var item in vistaScaled.Values)item.Dispose();foreach(var item in captionGlows.Values)item.Dispose();foreach(var font in nativeFonts.Values)font.Dispose();gameIconBitmap?.Dispose();raster?.Dispose();Ui.Dispose();Bold.Dispose();CaptionFont.Dispose();}
}
