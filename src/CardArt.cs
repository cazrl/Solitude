using System.Drawing.Drawing2D;
using System.Reflection;
using System.Drawing.Imaging;

namespace Solitude;

public sealed class CardArt : IDisposable
{
    private readonly Dictionary<(int Face,Era Era,GameKind Kind,int Back,int Deck,int Frame,int W,int H,int Density),Bitmap> cardCache=[];
    private readonly Dictionary<(int Background,int W,int H),Bitmap> feltCache=[];
    public float RenderScale { get; set; } = 1;
    private readonly Bitmap faces;
    private readonly List<Bitmap> xpBacks = [];
    private readonly List<Bitmap> oldBacks = [];
    private readonly List<Bitmap> vistaFaces = [];
    private readonly List<Bitmap> vistaBacks = [];
    private readonly Bitmap felt=Load("Vista.felt.jpg");
    private readonly Bitmap spiderFaces=Load("Games.spider-faces.png"), spiderBack=Load("Games.spider-back.png"), spiderFelt=Load("Games.spider-felt.png");
    private readonly Bitmap spiderAbout=Load("Games.spider-about.png"), kingLeft=Load("Games.king-left.png"), kingRight=Load("Games.king-right.png");
    private readonly List<Bitmap> backgrounds = [];
    private readonly Dictionary<int,Bitmap[]> animatedBacks = [];
    public int AnimationFrame { get; set; }
    public int GameElapsed { get; set; }
    public bool TimedBacks { get; set; }
    public GameKind Kind { get; set; }
    public int VistaBackground { get; set; }
    public int VistaDeck { get; set; } = 2;
    public CardArt()
    {
        faces = Load("cards.png");
        for (int i=0; i<12; i++) xpBacks.Add(Load($"Classic.{i+54}.bmp"));
        for (int i=0; i<12; i++) oldBacks.Add(Load($"Old.back-{i}.png"));
        for (int i=0; i<4; i++) { vistaFaces.Add(Load($"Vista.deck-{i}.png")); vistaBacks.Add(Load($"Vista.back-{i}.png")); }
        for (int i=1; i<5; i++) backgrounds.Add(Load($"Vista.background-{i}.jpg"));
        animatedBacks[6]=[Load("Old.robot-1.png"),Load("Old.robot-2.png")];
        animatedBacks[9]=[Load("Old.castle-1.png")];
        animatedBacks[10]=[Load("Old.palm-1.png"),Load("Old.palm-2.png")];
        animatedBacks[11]=[Load("Old.poker-1.png"),Load("Old.poker-2.png")];
    }
    private static Bitmap Load(string name, string? fallback = null)
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Solitude.Assets."+name)
            ?? (fallback == null ? throw new FileNotFoundException(name) : Assembly.GetExecutingAssembly().GetManifestResourceStream("Solitude.Assets."+fallback)!);
        using var image = new Bitmap(s); return new Bitmap(image);
    }
    public void Draw(Graphics g, Card card, RectangleF r, Era era, int back, float faceWidth=0,bool invert=false)
    {
        float density=RenderScale;
        int width=Math.Max(1,(int)Math.Ceiling((faceWidth>0?faceWidth:r.Width)*density)),height=Math.Max(1,(int)Math.Ceiling(r.Height*density));
        int frame=!card.FaceUp && TimedBacks && era is not (Era.WindowsXP or Era.WindowsVista) && Kind==GameKind.Klondike && animatedBacks.ContainsKey(back)
            && (back is 6 or 9 || GameElapsed>0 && (back==10?GameElapsed%30<2:GameElapsed%15<2)) ? AnimationFrame%(animatedBacks[back].Length+1) : 0;
        var key=(card.FaceUp?card.Id:-1,era,Kind,card.FaceUp?0:back,VistaDeck,frame,width,height,(int)(density*100));
        if(!cardCache.TryGetValue(key,out var cached))
        {
            if(cardCache.Count>=256){foreach(var bitmap in cardCache.Values)bitmap.Dispose();cardCache.Clear();}
            int padding=era==Era.WindowsVista?(int)Math.Ceiling(5*density):0;
            cached=new Bitmap(width+padding,height+padding,PixelFormat.Format32bppPArgb);
            using(var target=Graphics.FromImage(cached))
            {
                target.ScaleTransform(density,density);target.PixelOffsetMode=PixelOffsetMode.Half;
                target.InterpolationMode=era==Era.WindowsVista?InterpolationMode.HighQualityBicubic:InterpolationMode.NearestNeighbor;
                DrawSource(target,card,new(0,0,width/density,height/density),era,back);
            }
            cardCache.Add(key,cached);
        }
        float squeeze=faceWidth>0?r.Width/faceWidth:1;
        var dest=new RectangleF(r.X,r.Y,cached.Width/density*squeeze,cached.Height/density);
        using var transform=g.Transform;PointF[] corners=[dest.Location,new(dest.Right,dest.Bottom)];transform.TransformPoints(corners);
        var device=Rectangle.FromLTRB((int)MathF.Round(corners[0].X),(int)MathF.Round(corners[0].Y),(int)MathF.Round(corners[1].X),(int)MathF.Round(corners[1].Y));
        var state=g.Save();g.ResetTransform();g.InterpolationMode=faceWidth>0 && Math.Abs(faceWidth-r.Width)>.01f?InterpolationMode.Bilinear:InterpolationMode.NearestNeighbor;
        if(invert)
        {
            using var attributes=new ImageAttributes();attributes.SetColorMatrix(new ColorMatrix(new float[][]{[-1,0,0,0,0],[0,-1,0,0,0],[0,0,-1,0,0],[0,0,0,1,0],[1,1,1,0,1]}));
            g.DrawImage(cached,device,0,0,cached.Width,cached.Height,GraphicsUnit.Pixel,attributes);
        }
        else if(device.Size==cached.Size)g.DrawImageUnscaled(cached,device.Location);
        else g.DrawImage(cached,device,new RectangleF(0,0,cached.Width,cached.Height),GraphicsUnit.Pixel);
        g.Restore(state);
    }
    private void DrawSource(Graphics g, Card card, RectangleF r, Era era, int back)
    {
        bool vista = era == Era.WindowsVista;
        if (!vista && Kind == GameKind.Spider)
        {
            g.DrawImage(card.FaceUp ? spiderFaces : spiderBack, r, card.FaceUp ? new RectangleF((card.Rank-1)*71,(int)card.Suit*96,71,96) : new RectangleF(0,0,71,96), GraphicsUnit.Pixel);
            return;
        }
        if (vista)
        {
            var smooth = g.SmoothingMode; g.SmoothingMode=SmoothingMode.AntiAlias;
            using var shadow = new SolidBrush(Color.FromArgb(55,0,0,0)); g.FillRectangle(shadow,r.X+2,r.Y+3,r.Width,r.Height);
            g.SmoothingMode=smooth;
            var oldInterpolation=g.InterpolationMode;
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            if (!card.FaceUp) g.DrawImage(vistaBacks[VistaDeck],r,new RectangleF(0,0,vistaBacks[VistaDeck].Width,vistaBacks[VistaDeck].Height),GraphicsUnit.Pixel);
            else
            {
                var sheet=vistaFaces[VistaDeck];
                g.DrawImage(sheet,r,new RectangleF((card.Rank-1)*111,(int)card.Suit*152,111,152),GraphicsUnit.Pixel);
            }
            g.InterpolationMode=oldInterpolation;
            return;
        }
        if (!card.FaceUp)
        {
            var state=g.Save();
            using var outline=new GraphicsPath();outline.AddPolygon(new PointF[]{new(r.X+2,r.Y),new(r.Right-3,r.Y),new(r.Right-1,r.Y+2),new(r.Right-1,r.Bottom-3),new(r.Right-3,r.Bottom-1),new(r.X+2,r.Bottom-1),new(r.X,r.Bottom-3),new(r.X,r.Y+2)});
            g.SetClip(outline,CombineMode.Intersect);
            var bitmap=(era == Era.WindowsXP ? xpBacks : oldBacks)[back%12];
            if(era!=Era.WindowsXP && TimedBacks && Kind==GameKind.Klondike && animatedBacks.TryGetValue(back,out var frames))
            {
                bool active=back is 6 or 9 || GameElapsed>0 && (back==10?GameElapsed%30<2:GameElapsed%15<2);
                int frame=AnimationFrame%(frames.Length+1);
                if(active && frame>0)bitmap=frames[frame-1];
            }
            g.DrawImage(bitmap,r,new RectangleF(0,0,71,96),GraphicsUnit.Pixel);
            g.Restore(state);return;
        }
        g.DrawImage(faces,r,new RectangleF((card.Rank-1)*71,(int)card.Suit*96,71,96),GraphicsUnit.Pixel);
    }
    public void DrawFelt(Graphics g,RectangleF bounds)
    {
        int width=Math.Max(1,(int)Math.Ceiling(bounds.Width*RenderScale)),height=Math.Max(1,(int)Math.Ceiling(bounds.Height*RenderScale));
        var key=(VistaBackground,width,height);
        if(!feltCache.TryGetValue(key,out var cached))
        {
            if(feltCache.Count>=8){foreach(var image in feltCache.Values)image.Dispose();feltCache.Clear();}
            cached=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
            using(var target=Graphics.FromImage(cached))
            {
                target.CompositingMode=CompositingMode.SourceCopy;target.InterpolationMode=InterpolationMode.HighQualityBicubic;
                var texture=VistaBackground==0?felt:backgrounds[VistaBackground-1];
                target.DrawImage(texture,new Rectangle(0,0,width,height),new Rectangle(0,0,1600,992),GraphicsUnit.Pixel);
            }
            feltCache.Add(key,cached);
        }
        var previous=g.InterpolationMode;g.InterpolationMode=InterpolationMode.NearestNeighbor;
        g.DrawImage(cached,bounds,new RectangleF(0,0,width,height),GraphicsUnit.Pixel);g.InterpolationMode=previous;
    }
    public void DrawSpiderFelt(Graphics g,RectangleF bounds) {using var brush=new TextureBrush(spiderFelt);g.FillRectangle(brush,bounds);}
    public void DrawKing(Graphics g,RectangleF bounds,bool right) => g.DrawImage(right?kingRight:kingLeft,bounds);
    public void DrawSpiderAbout(Graphics g,RectangleF bounds) => g.DrawImage(spiderAbout,bounds);
    public void Dispose(){faces.Dispose();felt.Dispose();spiderFaces.Dispose();spiderBack.Dispose();spiderFelt.Dispose();spiderAbout.Dispose();kingLeft.Dispose();kingRight.Dispose();foreach(var b in xpBacks.Concat(oldBacks).Concat(vistaFaces).Concat(vistaBacks).Concat(backgrounds).Concat(animatedBacks.Values.SelectMany(b=>b)).Concat(cardCache.Values).Concat(feltCache.Values))b.Dispose();}
}
