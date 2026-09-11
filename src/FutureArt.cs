using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace Solitude;

// Original, resolution-independent artwork. No downloaded art or platform font symbols.
public sealed class FutureArt : IDisposable
{
    private readonly Dictionary<(int,int,int,int),Bitmap> cards=[];
    private readonly Dictionary<(float,FontStyle),Font> fonts=[];
    private Bitmap? scene;
    private (int,int,int,bool) sceneKey;
    private static readonly Dictionary<int,Bitmap> glowSprites=[];
    public int Palette { get; set; }
    public Color Accent=>Palette switch{1=>Color.FromArgb(239,191,126),2=>Color.FromArgb(175,179,255),_=>Color.FromArgb(119,232,218)};
    public static readonly Color Ink=Color.FromArgb(229,239,243),Muted=Color.FromArgb(146,168,183);
    private Font Font(float size,FontStyle style=FontStyle.Regular)
    {var key=(size,style);if(!fonts.TryGetValue(key,out var f))fonts[key]=f=new Font("Segoe UI",size,style,GraphicsUnit.Pixel);return f;}
    public void Text(Graphics g,string text,RectangleF r,float size=12,Color? color=null,bool center=false,FontStyle style=FontStyle.Regular)
    {
        using var brush=new SolidBrush(color??Ink);using var format=new StringFormat(StringFormat.GenericTypographic){Alignment=center?StringAlignment.Center:StringAlignment.Near,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap,Trimming=StringTrimming.EllipsisCharacter};
        g.DrawString(text,Font(size,style),brush,r,format);
    }
    public static void Panel(Graphics g,RectangleF r,Color fill,Color edge,float radius=12)
    {using var path=Skin.Rounded(r,radius);using var brush=new SolidBrush(fill);using var pen=new Pen(edge);g.FillPath(brush,path);g.DrawPath(pen,path);}
    public static void Glow(Graphics g,PointF center,float radius,Color color)
    {
        // Reuse a small radial texture instead of tessellating gradients for
        // every particle in every frame. This cache has a fixed memory bound.
        lock(glowSprites)
        {
            int key=color.ToArgb();
            if(!glowSprites.TryGetValue(key,out var sprite))
            {
                if(glowSprites.Count>=48){int oldest=glowSprites.Keys.First();glowSprites[oldest].Dispose();glowSprites.Remove(oldest);}
                sprite=new Bitmap(128,128,PixelFormat.Format32bppPArgb);
                using var target=Graphics.FromImage(sprite);using var path=new GraphicsPath();path.AddEllipse(0,0,128,128);
                using var brush=new PathGradientBrush(path){CenterColor=color,SurroundColors=[Color.FromArgb(0,color)],CenterPoint=new(64,64)};target.FillPath(brush,path);
                glowSprites[key]=sprite;
            }
            g.DrawImage(sprite,new RectangleF(center.X-radius,center.Y-radius,radius*2,radius*2));
        }
    }
    public static void OrbitMark(Graphics g,RectangleF r,Color color)
    {
        using var p=new Pen(color,Math.Max(1,r.Width/22));var s=g.Save();g.TranslateTransform(r.X+r.Width/2,r.Y+r.Height/2);g.RotateTransform(-32);
        g.DrawEllipse(p,-r.Width*.44f,-r.Height*.21f,r.Width*.88f,r.Height*.42f);g.DrawEllipse(p,-r.Width*.21f,-r.Height*.44f,r.Width*.42f,r.Height*.88f);
        using var b=new SolidBrush(color);g.FillEllipse(b,-r.Width*.065f,-r.Width*.065f,r.Width*.13f,r.Width*.13f);g.Restore(s);
    }
    public void DrawScene(Graphics g,RectangleF r,float scale,bool maximized=false)
    {
        int w=Math.Max(1,(int)MathF.Ceiling(r.Width*scale)),h=Math.Max(1,(int)MathF.Ceiling(r.Height*scale));
        if(scene==null || sceneKey!=(w,h,Palette,maximized))
        {
            scene?.Dispose();scene=new Bitmap(w,h,PixelFormat.Format32bppPArgb);sceneKey=(w,h,Palette,maximized);
            using var canvas=Graphics.FromImage(scene);canvas.ScaleTransform(scale,scale);canvas.SmoothingMode=SmoothingMode.AntiAlias;
            // Rasterize the bottom window corners once. A complex clip around
            // every moving card otherwise makes large display sizes expensive.
            using var shape=Skin.Rounded(new(-r.X,-r.Y,r.Width+2*r.X,r.Bottom+1),maximized?0:12);
            canvas.SetClip(shape);
            PaintScene(canvas,new(0,0,r.Width,r.Height));
        }
        var saved=g.Save();using var transform=g.Transform;PointF[] origin=[r.Location];transform.TransformPoints(origin);g.ResetTransform();g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(scene,CardArt.DevicePixel(origin[0].X),CardArt.DevicePixel(origin[0].Y));g.Restore(saved);
    }
    private void PaintScene(Graphics g,RectangleF r)
    {
        using(var b=new LinearGradientBrush(r,Color.FromArgb(8,17,29),Color.FromArgb(4,10,18),90))g.FillRectangle(b,r);
        float w=r.Width,h=r.Height;Glow(g,new(w*.81f,h*.38f),w*.57f,Color.FromArgb(27,Accent));
        Glow(g,new(w*.17f,h*.9f),w*.35f,Color.FromArgb(14,Palette==1?Color.Coral:Color.SteelBlue));
        var orbit=g.Save();g.TranslateTransform(w*.54f,h*.61f);g.RotateTransform(-19);
        for(int i=0;i<4;i++){using var pen=new Pen(Color.FromArgb(12+i*2,Accent),i==0?1.2f:.6f);g.DrawEllipse(pen,-w*.6f-i*20,-h*.22f-i*13,w*1.2f+i*40,h*.44f+i*26);}g.Restore(orbit);
        // A quiet planetary limb, deliberately below the playing cards' contrast.
        using(var limb=new Pen(Color.FromArgb(27,Accent),1.1f))g.DrawArc(limb,w*.52f,-h*.28f,w*.85f,w*.85f,118,107);
        var random=new Random(2126);for(int i=0;i<170;i++)
        {
            float x=(float)random.NextDouble()*w,y=40+(float)random.NextDouble()*(h-80),size=i%17==0?1.5f:.7f;
            using var b=new SolidBrush(Color.FromArgb(22+random.Next(65),200,220,232));g.FillEllipse(b,x,y,size,size);
            if(i%41==0){using var p=new Pen(Color.FromArgb(45,Accent),.65f);g.DrawLine(p,x-3,y+.5f,x+4,y+.5f);g.DrawLine(p,x+.5f,y-3,x+.5f,y+4);}
        }
    }
    public void DrawCard(Graphics g,Card card,RectangleF r,float density,float faceWidth=0,float bank=0)
    {
        float fullWidth=faceWidth>0?faceWidth:r.Width;
        var bitmap=CardBitmap(card,fullWidth,r.Height,density);
        var target=new RectangleF(r.X,r.Y,bitmap.Width/density*r.Width/fullWidth,bitmap.Height/density);
        if(Math.Abs(bank)>.001f)
        {
            var s=g.Save();g.TranslateTransform(r.X+r.Width/2,r.Y+r.Height/2);g.RotateTransform(bank);g.TranslateTransform(-r.X-r.Width/2,-r.Y-r.Height/2);
            g.InterpolationMode=InterpolationMode.Bilinear;g.DrawImage(bitmap,target);g.Restore(s);return;
        }
        using var transform=g.Transform;PointF[] points=[target.Location,new(target.Right,target.Bottom)];transform.TransformPoints(points);
        var device=Rectangle.FromLTRB(CardArt.DevicePixel(points[0].X),CardArt.DevicePixel(points[0].Y),CardArt.DevicePixel(points[1].X),CardArt.DevicePixel(points[1].Y));
        var state=g.Save();g.ResetTransform();g.InterpolationMode=InterpolationMode.HighQualityBilinear;
        if(device.Size==bitmap.Size)g.DrawImageUnscaled(bitmap,device.Location);else g.DrawImage(bitmap,device,0,0,bitmap.Width,bitmap.Height,GraphicsUnit.Pixel);g.Restore(state);
    }
    private Bitmap CardBitmap(Card card,float width,float height,float density)
    {
        int w=Math.Max(1,(int)MathF.Ceiling(width*density)),h=Math.Max(1,(int)MathF.Ceiling(height*density));
        var key=(card.FaceUp?card.Id:-1,Palette,w,h);
        if(!cards.TryGetValue(key,out var bitmap))
        {
            if(cards.Count>=192){var oldest=cards.Keys.First();cards[oldest].Dispose();cards.Remove(oldest);}
            int pad=(int)MathF.Ceiling(8*density);bitmap=new Bitmap(w+pad,h+pad,PixelFormat.Format32bppPArgb);
            using(var c=Graphics.FromImage(bitmap)){c.ScaleTransform(w/96f,h/136f);c.SmoothingMode=SmoothingMode.AntiAlias;c.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;PaintCard(c,card);}
            cards[key]=bitmap;
        }
        return bitmap;
    }
    public void DrawOrbitingCard(Graphics g,Card card,PointF center,float width,float height,float roll,float yaw,float density,float referenceWidth,float referenceHeight)
    {
        float facing=MathF.Cos(yaw),squash=Math.Max(.025f,Math.Abs(facing));
        var bitmap=CardBitmap(card with{FaceUp=facing>=0},referenceWidth,referenceHeight,density);
        float c=MathF.Cos(roll),s=MathF.Sin(roll),shear=MathF.Sin(yaw)*width*.08f;
        PointF Project(float x,float y)
        {
            float px=(x-.5f)*width*squash+(y-.5f)*shear,py=(y-.5f)*height;
            return new(center.X+px*c-py*s,center.Y+px*s+py*c);
        }
        // Use one stable-resolution sprite per card. Perspective/flip changes
        // its destination only, avoiding a new bitmap for every animation step.
        float right=bitmap.Width/(referenceWidth*density),bottom=bitmap.Height/(referenceHeight*density);
        PointF[] corners=[Project(0,0),Project(right,0),Project(0,bottom)];
        var saved=g.Save();g.InterpolationMode=InterpolationMode.Bilinear;
        g.DrawImage(bitmap,corners,new RectangleF(0,0,bitmap.Width,bitmap.Height),GraphicsUnit.Pixel);g.Restore(saved);
    }
    private void PaintCard(Graphics g,Card card)
    {
        for(int i=5;i>=1;i--)Panel(g,new(2+i*.35f,3+i*.7f,95,135),Color.FromArgb(8,0,0,0),Color.Transparent,7);
        var r=new RectangleF(.5f,.5f,95,135);using var path=Skin.Rounded(r,6);
        using(var fill=new LinearGradientBrush(r,card.FaceUp?Color.FromArgb(251,252,248):Color.FromArgb(24,49,65),card.FaceUp?Color.FromArgb(224,235,236):Color.FromArgb(10,24,39),68))g.FillPath(fill,path);
        using(var edge=new Pen(card.FaceUp?Color.FromArgb(76,102,116):Color.FromArgb(140,Accent),card.FaceUp?1.15f:.8f))g.DrawPath(edge,path);
        if(!card.FaceUp)
        {
            using var inset=Skin.Rounded(new(5,5,86,126),4);using var p=new Pen(Color.FromArgb(80,Accent),.6f);g.DrawPath(p,inset);
            var s=g.Save();g.SetClip(path,CombineMode.Intersect);
            for(int i=0;i<5;i++)g.DrawEllipse(p,10-i*8,35-i*12,76+i*16,66+i*24);
            Glow(g,new(48,66),45,Color.FromArgb(55,Accent));OrbitMark(g,new(25,42,46,46),Accent);
            Text(g,"O R B I T",new(7,112,82,13),7,Accent,true,FontStyle.Bold);g.Restore(s);return;
        }
        Color ink=card.Red?Color.FromArgb(179,49,65):Color.FromArgb(22,45,59);
        string rank=card.Rank switch{1=>"A",11=>"J",12=>"Q",13=>"K",_=>card.Rank.ToString()};
        void Corner(){Text(g,rank,new(6,3,25,23),19,ink,false,FontStyle.Bold);Suit(g,card.Suit,new(32,8,12,12),ink);}
        Corner();var rotated=g.Save();g.TranslateTransform(96,136);g.RotateTransform(180);Corner();g.Restore(rotated);
        if(card.Rank==1)
        {
            using var halo=new Pen(Color.FromArgb(35,ink),.65f);g.DrawEllipse(halo,19,37,58,58);g.DrawEllipse(halo,23,41,50,50);
            Suit(g,card.Suit,new(32,48,32,34),ink);Text(g,"01",new(34,91,28,13),7,Color.FromArgb(130,ink),true);
        }
        else if(card.Rank>10)
        {
            var s=g.Save();g.TranslateTransform(48,68);
            using var thin=new Pen(Color.FromArgb(120,ink),.7f);using var strong=new Pen(Color.FromArgb(180,ink),1.3f);
            for(int ring=0;ring<3;ring++)
            {
                int sides=card.Rank==11?6:card.Rank==12?8:5;float radius=28-ring*6;
                PointF[] vertices=Enumerable.Range(0,sides).Select(i=>new PointF(MathF.Cos(i*MathF.Tau/sides-MathF.PI/2)*radius,MathF.Sin(i*MathF.Tau/sides-MathF.PI/2)*radius*1.2f)).ToArray();g.DrawPolygon(ring==0?strong:thin,vertices);
            }
            g.DrawLine(thin,0,-39,0,39);g.DrawLine(thin,-31,0,31,0);g.Restore(s);
            Suit(g,card.Suit,new(40,60,16,17),ink);
            Text(g,card.Rank==11?"NAVIGATOR":card.Rank==12?"SOVEREIGN":"ARCHITECT",new(12,106,72,13),7.5f,ink,true,FontStyle.Bold);
        }
        else
        {
            // Keep recognizable pip counts and red/black suits for immediate reading.
            float[] rows=card.Rank switch{2=>[43,93],3=>[40,68,96],_=>[40,66,94]};
            var places=new List<PointF>();
            if(card.Rank<=3)places.AddRange(rows.Select(y=>new PointF(48,y)));
            else
            {
                int pairs=card.Rank/2;float[] ys=pairs==2?[40,95]:pairs==3?[39,67,95]:pairs==4?[37,57,77,97]:[36,52,68,84,100];
                foreach(float y in ys){places.Add(new(35,y));places.Add(new(62,y));}
                if(card.Rank%2!=0)places.Add(new(48,67));
            }
            foreach(var p in places)Suit(g,card.Suit,new(p.X-7,p.Y-7,14,15),ink);
        }
    }
    public static void Suit(Graphics g,Suit suit,RectangleF r,Color color)
    {
        using var p=new GraphicsPath();var s=g.Save();g.TranslateTransform(r.X,r.Y);g.ScaleTransform(r.Width/20,r.Height/22);
        if(suit==Solitude.Suit.Diamonds)p.AddPolygon([new(10,0),new(20,11),new(10,22),new(0,11)]);
        else if(suit==Solitude.Suit.Hearts)
        {p.AddBezier(10,21,-9,7,3,-6,10,4);p.AddBezier(10,4,17,-6,29,7,10,21);}
        else if(suit==Solitude.Suit.Spades)
        {p.AddBezier(10,0,-9,14,3,22,9,15);p.AddLine(9,15,5,22);p.AddLine(5,22,15,22);p.AddLine(15,22,11,15);p.AddBezier(11,15,17,22,29,14,10,0);}
        else
        {p.AddEllipse(5,0,10,11);p.AddEllipse(0,8,11,11);p.AddEllipse(9,8,11,11);p.AddPolygon([new(9,12),new(11,12),new(15,22),new(5,22)]);p.FillMode=FillMode.Winding;}
        p.CloseFigure();using var b=new SolidBrush(color);g.FillPath(b,p);g.Restore(s);
    }
    public void Dispose(){scene?.Dispose();foreach(var b in cards.Values)b.Dispose();foreach(var f in fonts.Values)f.Dispose();}
}
