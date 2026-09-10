using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Solitude;

public sealed partial class Skin
{
    private readonly Dictionary<string,Bitmap> vistaParts=[];
    private readonly Dictionary<(string,Size,float,Padding),Bitmap> vistaScaled=[];
    private Bitmap VistaPart(string name)
    {
        if(vistaParts.TryGetValue(name,out var found))return found;
        using var stream=typeof(Skin).Assembly.GetManifestResourceStream($"Solitude.Assets.Frames.Vista.{name}.png")??throw new InvalidDataException("Missing Vista frame part: "+name);
        using var source=new Bitmap(stream);return vistaParts[name]=new Bitmap(source);
    }
    private void PaintVistaPart(Graphics g,string name,RectangleF bounds,Padding margins=default)
    {
        using var transform=g.Transform;float scale=transform.Elements[0];PointF[] points=[bounds.Location,new(bounds.Right,bounds.Bottom)];transform.TransformPoints(points);
        var rect=Rectangle.FromLTRB((int)MathF.Round(points[0].X),(int)MathF.Round(points[0].Y),(int)MathF.Round(points[1].X),(int)MathF.Round(points[1].Y));if(rect.Width<=0 || rect.Height<=0)return;
        var key=(name,rect.Size,scale,margins);
        if(!vistaScaled.TryGetValue(key,out var bitmap))
        {
            if(vistaScaled.Count>=64){foreach(var item in vistaScaled.Values)item.Dispose();vistaScaled.Clear();}
            bitmap=new Bitmap(rect.Width,rect.Height,PixelFormat.Format32bppPArgb);var source=VistaPart(name);
            using(var target=Graphics.FromImage(bitmap))
            {
                target.InterpolationMode=InterpolationMode.NearestNeighbor;target.PixelOffsetMode=PixelOffsetMode.Half;
                int[] sx=[0,margins.Left,source.Width-margins.Right,source.Width],sy=[0,margins.Top,source.Height-margins.Bottom,source.Height];
                int[] dx=[0,(int)MathF.Round(margins.Left*scale),rect.Width-(int)MathF.Round(margins.Right*scale),rect.Width],dy=[0,(int)MathF.Round(margins.Top*scale),rect.Height-(int)MathF.Round(margins.Bottom*scale),rect.Height];
                for(int y=0;y<3;y++)for(int x=0;x<3;x++)if(sx[x+1]>sx[x] && sy[y+1]>sy[y] && dx[x+1]>dx[x] && dy[y+1]>dy[y])
                    target.DrawImage(source,Rectangle.FromLTRB(dx[x],dy[y],dx[x+1],dy[y+1]),Rectangle.FromLTRB(sx[x],sy[y],sx[x+1],sy[y+1]),GraphicsUnit.Pixel);
            }
            vistaScaled[key]=bitmap;
        }
        var state=g.Save();g.ResetTransform();g.DrawImageUnscaled(bitmap,rect.Location);g.Restore(state);
    }
    private void PaintVistaFrame(Graphics g,RectangleF bounds,bool active,bool maximized)
    {
        if(maximized){Fill(g,Color.Black,new(bounds.X,bounds.Y,bounds.Width,21));return;}
        var clip=g.Save();g.SetClip(new RectangleF(bounds.X+8,bounds.Y+27,bounds.Width-16,bounds.Height-35),CombineMode.Exclude);
        Fill(g,active?C(174,185,194):C(195,202,207),bounds);
        using(var attributes=new ImageAttributes())
        {
            attributes.SetColorMatrix(new ColorMatrix{Matrix33=active?.13f:.07f});var reflection=VistaPart("reflection");
            g.DrawImage(reflection,Rectangle.Round(bounds),0,0,reflection.Width,reflection.Height,GraphicsUnit.Pixel,attributes);
        }
        string state=active?"focused":"unfocused";
        PaintVistaPart(g,$"frame-{state}-top_noshadow",new(bounds.X,bounds.Y,bounds.Width,27),new(8,6,8,2));
        PaintVistaPart(g,$"frame-{state}-left",new(bounds.X,bounds.Y+27,9,bounds.Height-35),new(6,0,2,0));
        PaintVistaPart(g,$"frame-{state}-right",new(bounds.Right-9,bounds.Y+27,9,bounds.Height-35),new(2,0,6,0));
        PaintVistaPart(g,$"frame-{state}-bottom_noshadow",new(bounds.X,bounds.Bottom-9,bounds.Width,9),new(8,2,8,6));
        g.Restore(clip);
    }
    private void PaintVistaButton(Graphics g,RectangleF bounds,int type,bool active,bool hover,bool pressed,bool lone)
    {
        string button=type==2?"close":type==0?"minimize":"maximize",state=pressed?"active":hover?"hover":"normal";
        PaintVistaPart(g,$"button-{(active?"focused":"unfocused")}-{(type==2 && lone?"close-single":button)}-{state}",bounds,new(3,2,3,3));
        string glyph=type==4?"restore":button;var art=VistaPart($"glyphs-{glyph}-{(active || hover?"normal":"disabled")}");
        var rect=new RectangleF(MathF.Floor(bounds.X+(bounds.Width-art.Width)/2),MathF.Floor(bounds.Y+(bounds.Height-art.Height)/2),art.Width,art.Height);
        PaintVistaPart(g,$"glyphs-{glyph}-{(active || hover?"normal":"disabled")}",rect);
    }
}
