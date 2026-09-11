using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Solitude;

public sealed partial class GameWindow
{
    private sealed record BoardStamp(GameState State,int Moves,RectangleF Bounds,float Scale,Era Era,int Deck,int Back,int Background,int BackFrame,
        Position? Selection,bool Dragging,(Position From,Position To)? Hint,int Flights,int Revision,bool KingRight,bool Modal,int Score);
    private BoardStamp? boardStamp;
    private Bitmap? boardBitmap;
    private readonly List<(Position Position,RectangleF Rect)> boardCardAreas=[];
    private readonly List<Hotspot> boardHotspots=[];
    private void PaintCachedTable(Graphics g)
    {
        int frame=art.TimedBacks && !skin.Xp && !skin.Modern && Kind==GameKind.Klondike && Preferences.CardBack is 6 or 9 or 10 or 11?art.AnimationFrame:0;
        if(skin.Future)frame=showingVictory?1:0;
        var stamp=new BoardStamp(Game.State,Game.State.Moves,Table,art.RenderScale,Preferences.Era,skin.Future?Preferences.FuturePalette:Preferences.VistaDeck,Preferences.CardBack,Preferences.VistaBackground,frame,
            selection,dragging,hint,flights.Count,motionRevision,Kind==GameKind.FreeCell && mouse.X>Table.Left+Table.Width/2,dialog!=DialogPage.None,Kind==GameKind.Spider?Game.State.Score:0);
        if(boardBitmap==null || boardStamp!=stamp)
        {
            int width=(int)Math.Ceiling(Table.Width*art.RenderScale),height=(int)Math.Ceiling(Table.Height*art.RenderScale);
            if(boardBitmap==null || boardBitmap.Width!=width || boardBitmap.Height!=height){boardBitmap?.Dispose();boardBitmap=new Bitmap(width,height,PixelFormat.Format32bppPArgb);}
            int firstHotspot=hotspots.Count;
            using(var target=Graphics.FromImage(boardBitmap))
            {
                target.Clear(Color.Transparent);
                skin.Configure(target);target.ScaleTransform(art.RenderScale,art.RenderScale);
                target.TranslateTransform(-CardArt.DevicePixel(Table.X*art.RenderScale)/art.RenderScale,-CardArt.DevicePixel(Table.Y*art.RenderScale)/art.RenderScale);
                if(skin.Future){Orbit.DrawScene(target,new(1,WindowHeader,WorldWidth-2,WorldHeight-WindowHeader-1),ScaleFactor,maximized);PaintFutureTable(target);}
                else PaintTable(target);
            }
            boardCardAreas.Clear();boardCardAreas.AddRange(cardAreas);boardHotspots.Clear();boardHotspots.AddRange(hotspots.Skip(firstHotspot));boardStamp=stamp;
        }
        else{cardAreas.AddRange(boardCardAreas);hotspots.AddRange(boardHotspots);}
        using var transform=g.Transform;PointF[] origin=[Table.Location];transform.TransformPoints(origin);
        var state=g.Save();g.ResetTransform();g.CompositingMode=CompositingMode.SourceCopy;
        g.DrawImageUnscaled(boardBitmap,CardArt.DevicePixel(origin[0].X),CardArt.DevicePixel(origin[0].Y));g.Restore(state);
    }
}
