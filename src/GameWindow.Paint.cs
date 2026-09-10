using System.Drawing.Drawing2D;

namespace Solitude;

public sealed partial class GameWindow
{
    protected override void OnPaint(PaintEventArgs e)
    {
        long start=System.Diagnostics.Stopwatch.GetTimestamp();
        base.OnPaint(e);
        e.Graphics.SetClip(e.ClipRectangle,CombineMode.Intersect);
        PaintScaled(e.Graphics);
        RecordFrame(start);
    }
    private void PaintScaled(Graphics target)
    {
        if(skin.DeviceText)
        {
            var state=target.Save();target.ScaleTransform(ScaleFactor,ScaleFactor);PaintGame(target);target.Restore(state);return;
        }
        // Preserve the original raster pixels at every display size; do not substitute modern font smoothing.
        int width=(int)Math.Ceiling(WorldWidth),height=(int)Math.Ceiling(WorldHeight);
        if(pixelCanvas==null || pixelCanvas.Width!=width || pixelCanvas.Height!=height){pixelCanvas?.Dispose();pixelCanvas=new Bitmap(width,height);}
        using(var g=Graphics.FromImage(pixelCanvas))
        {
            var clip=target.ClipBounds;g.SetClip(new RectangleF(clip.X/ScaleFactor,clip.Y/ScaleFactor,clip.Width/ScaleFactor,clip.Height/ScaleFactor));
            g.CompositingMode=CompositingMode.SourceCopy;using(var clear=new SolidBrush(Color.Transparent))g.FillRectangle(clear,g.ClipBounds);
            g.CompositingMode=CompositingMode.SourceOver;PaintGame(g);
        }
        // Scale against a fixed origin. GDI+ shifts nearest-neighbour sampling at
        // fractional scales when the destination graphics has a changing clip.
        int displayWidth=(int)Math.Ceiling(width*ScaleFactor),displayHeight=(int)Math.Ceiling(height*ScaleFactor);
        if(pixelPresentation==null || pixelPresentation.Width!=displayWidth || pixelPresentation.Height!=displayHeight)
        {pixelPresentation?.Dispose();pixelPresentation=new Bitmap(displayWidth,displayHeight,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);}
        using(var display=Graphics.FromImage(pixelPresentation))
        {
            display.CompositingMode=CompositingMode.SourceCopy;display.InterpolationMode=InterpolationMode.NearestNeighbor;display.PixelOffsetMode=PixelOffsetMode.Half;
            display.DrawImage(pixelCanvas,new RectangleF(0,0,width*ScaleFactor,height*ScaleFactor),new RectangleF(0,0,width,height),GraphicsUnit.Pixel);
        }
        var mode=target.CompositingMode;target.CompositingMode=CompositingMode.SourceCopy;target.DrawImageUnscaled(pixelPresentation,0,0);target.CompositingMode=mode;
    }
    private void PaintGame(Graphics g)
    {
        hotspots.Clear();cardAreas.Clear();skin.Configure(g);art.VistaDeck=Preferences.VistaDeck;art.Kind=Kind;art.VistaBackground=Preferences.VistaBackground;
        art.RenderScale=skin.DeviceText?ScaleFactor:1;EnsureLayout();
        art.TimedBacks=Game.Rules.Timed && dialog==DialogPage.None && windowActive;art.GameElapsed=Game.State.Elapsed;art.AnimationFrame=(int)(activeTime.ElapsedMilliseconds/250);
        var buttons=skin.Frame(g,new(0,0,WorldWidth,WorldHeight),GameTitle,active:windowActive && dialog==DialogPage.None && !(oneMoveWarning && (int)(MotionNow*2)%2==1),pointer:dialog==DialogPage.None?mouse:null,down:pressedHotspot!=null,maximized:maximized);
        if(dialog==DialogPage.None)
        {
            Add("minimize",buttons[0],()=>WindowState=FormWindowState.Minimized);
            Add("maximize",buttons[1],ToggleMaximize);
            if(!buttons[2].IsEmpty)Add("close",buttons[2],Close);
            Add("system",skin.CaptionLayout(new(0,0,WorldWidth,WorldHeight),maximized:maximized).Icon,()=>{menu=2;menuFocus=-1;});
        }
        var menuRect=new RectangleF(WindowBorder,WindowHeader,WorldWidth-2*WindowBorder,skin.MenuHeight);
        skin.MenuBar(g,menuRect);
        bool spiderMenu=Kind==GameKind.Spider && !skin.Vista;
        float menuX=menuRect.X+2;
        foreach(var entry in spiderMenu?new[]{(0,"Game"),(3,"Deal!"),(1,"Help")}:new[]{(0,"Game"),(1,"Help")})
        {
            var r=new RectangleF(menuX,menuRect.Y,skin.Measure(g,entry.Item2)+12,menuRect.Height-1);menuX=r.Right;
            bool chosen=menu==entry.Item1,hover=r.Contains(mouse) && dialog==DialogPage.None && (skin.Xp || skin.Vista);
            skin.MenuItem(g,r,chosen || hover,true);
            skin.Text(g,entry.Item2,new(r.X+4,r.Y,r.Width-8,r.Height),chosen && skin.Early?Color.White:Color.Black);
            // Classic menus expose mnemonic underlines even before Alt is pressed.
            if(!skin.Xp && !skin.Vista){float tx=r.X+4;Skin.Line(g,chosen && skin.Early?Color.White:Color.Black,tx,r.Y+(skin.Early?16:14),tx+skin.Measure(g,entry.Item2[..1])-1,r.Y+(skin.Early?16:14));}
            if(dialog==DialogPage.None)Add("bar-"+entry.Item1,r,()=>{if(entry.Item1==3)DrawCards();else{menu=entry.Item1;menuFocus=-1;Invalidate();}});
        }
        float settingsWidth=Math.Max(82,skin.Measure(g,"Settings...")+18);
        var settingsRect=new RectangleF(menuRect.Right-settingsWidth-3,menuRect.Y+1,settingsWidth,menuRect.Height-2);
        skin.Button(g,settingsRect,"Settings...",dialog==DialogPage.None && settingsRect.Contains(mouse),enabled:dialog==DialogPage.None,pressed:pressedHotspot=="settings" && settingsRect.Contains(mouse));
        if(ClassicFreeCell)
        {
            string left=$"Cards Left: {52-Game.State.Foundations.Sum(p=>p.Count)}";float width=skin.Measure(g,left)+8;
            skin.Text(g,left,new(settingsRect.X-width-8,menuRect.Y,width,menuRect.Height-1));
        }
        if(dialog==DialogPage.None)Add("settings",settingsRect,()=>OpenDialog(DialogPage.Settings));
        PaintCachedTable(g);PaintDropTarget(g);PaintMovingCards(g);
        if(peekCard is {} peek && Game.Pile(peek) is {} pile && Game.Index(peek)>=0)
        {
            var state=g.Save();g.SetClip(Table,CombineMode.Intersect);
            var r=peek.Kind==PileKind.Tableau?TableauCard(peek.Pile,Game.Index(peek)):CellRect(peek.Pile,false);
            art.Draw(g,pile[Game.Index(peek)],r,Preferences.Era,Preferences.CardBack);g.Restore(state);
        }
        if(Preferences.ShowStatus)PaintStatus(g);
        if(victoryTrail!=null && (showingVictory || dialog==DialogPage.Won))g.DrawImageUnscaled(victoryTrail,0,0);
        if(dragging && selection.HasValue)PaintDrag(g);
        PaintVistaTip(g);
        if(menu>=0)PaintMenu(g);
        if(dialog!=DialogPage.None){if(dialogHost==null)PaintDialog(g);else dialogHost.RefreshSurface();}
    }
    private void Add(string id,RectangleF bounds,Action action,bool enabled=true)
    {hotspots.Add(new(id,bounds,action,enabled));}
    private void PaintTable(Graphics g)
    {
        if(Kind!=GameKind.Klondike){PaintVariantTable(g);return;}
        var table=Table;
        if(!skin.Vista)Skin.Fill(g,Color.FromArgb(0,128,0),table);
        else art.DrawFelt(g,table);
        var clip=g.Save();g.SetClip(table,CombineMode.Intersect);
        var stock=TopCard(0);
        if(Game.State.Stock.Count>0)art.Draw(g,new Card(0,false),stock,Preferences.Era,Preferences.CardBack);
        else
        {
            Empty(g,stock,false);
            if(Game.CanRecycle)
            {
                using var pen=new Pen(skin.Vista?Color.FromArgb(170,255,255,255):Color.Lime,6);
                g.DrawArc(pen,stock.X+18,stock.Y+27,34,34,40,285);
                using var brush=new SolidBrush(pen.Color);g.FillPolygon(brush,new PointF[]{new(stock.X+48,stock.Y+25),new(stock.X+59,stock.Y+37),new(stock.X+42,stock.Y+37)});
            }
            else if(Game.State.Waste.Count>0)
            {
                using var pen=new Pen(Color.Red,5);g.DrawEllipse(pen,stock.X+17,stock.Y+27,36,36);g.DrawLine(pen,stock.X+23,stock.Y+32,stock.X+48,stock.Y+58);
            }
        }
        if(Game.State.Waste.Count>0)
        {
            int count=Math.Max(1,Math.Min(Game.State.WasteFan,Game.State.Waste.Count));
            for(int i=0;i<count;i++)
            {
                int index=Game.State.Waste.Count-count+i;var r=TopCard(1);r.X+=i*(skin.Vista?19:16);
                var pos=new Position(PileKind.Waste,0,index);
                if(!(dragging && IsSelected(pos)))DrawGameCard(g,Game.State.Waste[index],r);
                if(i==count-1){cardAreas.Add((pos,r));Highlight(g,pos,r);}
            }
        }
        for(int i=0;i<4;i++)
        {
            var r=TopCard(i+3);var pile=Game.State.Foundations[i];var pos=new Position(PileKind.Foundation,i);
            if(pile.Count==0 || (skin.Vista && victoryTrail!=null && (showingVictory || dialog==DialogPage.Won)) || (dragging && IsSelected(pos) && pile.Count==1))Empty(g,r,true);
            else
            {
                int index=pile.Count-1-(dragging && IsSelected(pos)?1:0);
                DrawGameCard(g,pile[index],r);
                cardAreas.Add((pos,r));
            }
            Highlight(g,pos,r);
        }
        for(int col=0;col<7;col++)
        {
            var pile=Game.State.Tableau[col];
            for(int i=0;i<pile.Count;i++)
            {
                var pos=new Position(PileKind.Tableau,col,i);var r=TableauCard(col,i);
                if(dragging && selection.HasValue && selection.Value.Kind==PileKind.Tableau && selection.Value.Pile==col && i>=Game.Index(selection.Value))break;
                DrawGameCard(g,pile[i],r);
                var hit=r;if(i<pile.Count-1)hit.Height=TableauCard(col,i+1).Y-r.Y;
                cardAreas.Add((pos,hit));
                if(IsSelected(pos))
                {
                    var outline=r;outline.Height=TableauCard(col,pile.Count-1).Bottom-r.Y;
                    Highlight(g,pos,outline);
                }
                else if(i==pile.Count-1)Highlight(g,pos,r);
            }
            if(pile.Count==0){if(skin.Vista)Empty(g,TableauCard(col,0),false);Highlight(g,new(PileKind.Tableau,col),TableauCard(col,0));}
        }
        g.Restore(clip);
    }
    private bool IsSelected(Position pos)=>selection.HasValue && pos.Kind==selection.Value.Kind && pos.Pile==selection.Value.Pile && Game.Index(pos)==Game.Index(selection.Value);
    private void Empty(Graphics g,RectangleF r,bool foundation)
    {
        if(skin.Vista)
        {
            var smooth=g.SmoothingMode;g.SmoothingMode=SmoothingMode.AntiAlias;
            using var outline=Skin.Rounded(r,6);using var p=new Pen(Color.FromArgb(155,223,231,219),2);g.DrawPath(p,outline);g.SmoothingMode=smooth;
        }
        else
        {
            using var outline=Skin.Rounded(new(r.X,r.Y,r.Width-1,r.Height-1),3);
            using var stipple=new HatchBrush(HatchStyle.Percent25,Color.FromArgb(0,64,0),Color.Green);
            g.FillPath(stipple,outline);g.DrawPath(Pens.Black,outline);
        }
    }
    private void Highlight(Graphics g,Position pos,RectangleF r)
    {
        bool selected=skin.Vista && IsSelected(pos) && !dragging;
        bool hinted=hint.HasValue && (SamePile(hint.Value.From,pos) || SamePile(hint.Value.To,pos));
        if(!selected && !hinted)return;
        r.Inflate(2,2);using var pen=new Pen(hinted?Color.FromArgb(255,233,107):Color.White,2){DashStyle=selected?DashStyle.Dot:DashStyle.Solid};g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);
    }
    private static bool SamePile(Position a,Position b)=>a.Kind==b.Kind && a.Pile==b.Pile;
    private void PaintDrag(Graphics g)
    {
        var pos=selection!.Value;var pile=Game.Pile(pos)!;int index=Game.Index(pos);
        float x=mouse.X-dragOffset.X,y=mouse.Y-dragOffset.Y;
        var clip=g.Save();g.SetClip(Table,CombineMode.Intersect);
        if(skin.Vista && !Preferences.OutlineDragging)
        {using var shadow=new SolidBrush(Color.FromArgb(48,0,0,0));g.FillRectangle(shadow,x+4,y+5,CardWidth,CardHeight+(pile.Count-index-1)*(pos.Kind==PileKind.Tableau?StackStep(pos.Pile):18));}
        for(int i=index;i<pile.Count;i++)
        {
            var r=new RectangleF(x,y+(i-index)*(pos.Kind==PileKind.Tableau?StackStep(pos.Pile):18),CardWidth,CardHeight);
            if(Preferences.OutlineDragging){using var pen=new Pen(Color.White){DashStyle=DashStyle.Dot};g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);}
            else art.Draw(g,pile[i],r,Preferences.Era,Preferences.CardBack);
        }
        g.Restore(clip);
    }
    private int DisplayScore=>Game.State.Score-(skin.Vista?Game.State.TimeBonus:0);
    private void PaintStatus(Graphics g)
    {
        var r=new RectangleF(WindowBorder,WorldHeight-WindowBorder-skin.StatusHeight,WorldWidth-2*WindowBorder,skin.StatusHeight);
        Skin.Fill(g,skin.Vista?skin.Face:Color.White,r);Skin.Line(g,skin.Early?Color.Black:Color.White,r.Left,r.Top,r.Right,r.Top);
        bool score=Kind!=GameKind.FreeCell && Game.Rules.Scoring!=Scoring.None,time=Game.Rules.Timed;
        var scoreRect=new RectangleF(r.Right-(time?205:105),r.Top+2,103,17);
        var timeRect=new RectangleF(r.Right-100,r.Top+2,98,17);
        if(!skin.Vista)
        {
            string timeText=$"Time: {Game.State.Elapsed}",scoreText=$"Score: {DisplayScore}";
            float timeWidth=time?skin.Measure(g,timeText,skin.Bold)+6:0,scoreWidth=score?skin.Measure(g,scoreText,skin.Bold)+6:0;
            if(score)skin.Text(g,scoreText,new(r.Right-timeWidth-scoreWidth,r.Y+1,scoreWidth,r.Height-2),Game.State.Score<0?Color.Red:Color.Black,skin.Bold);
            if(time)skin.Text(g,timeText,new(r.Right-timeWidth,r.Y+1,timeWidth,r.Height-2),font:skin.Bold);
        }
        else if(skin.Vista)
        {
            if(Kind==GameKind.FreeCell)skin.Text(g,$"Game #{Game.State.Seed}    Moves: {Game.State.Moves}",new(r.X+8,r.Y,r.Width-250,r.Height));
            if(Kind==GameKind.Spider)skin.Text(g,$"Moves: {Game.State.Moves}",new(r.X+8,r.Y,r.Width-250,r.Height));
            if(time)skin.Text(g,$"Time: {Game.State.Elapsed}",new(r.Right-220,r.Y,105,r.Height));
            if(score)skin.Text(g,$"Score: {DisplayScore}",new(r.Right-105,r.Y,100,r.Height));
            for(int i=0;i<3;i++)Skin.Line(g,Color.FromArgb(170,170,170),r.Right-4-i*4,r.Bottom-3,r.Right-3,r.Bottom-4-i*4);
        }
    }
    private void PaintMenu(Graphics g)
    {
        var entries=PeriodMenu();
        float height=entries.Sum(e=>e.Action==null?7:22)+6;
        var anchor=hotspots.LastOrDefault(h=>h.Id=="bar-"+menu)?.Bounds;
        var r=new RectangleF(anchor?.X??WindowBorder+2,menu==2?WindowHeader:WindowHeader+skin.MenuHeight-1,264,height);
        float labelInset=skin.Xp || skin.Vista?28:19;
        float shortcutWidth=entries.Max(e=>skin.Measure(g,e.Key))+4;
        r.Width=Math.Max(skin.Early?178:164,entries.Max(e=>skin.Measure(g,e.Label.Replace("&","")))+labelInset+shortcutWidth+42);
        skin.Popup(g,r);
        float y=r.Top+3;int activeIndex=0;
        foreach(var entry in entries)
        {
            if(entry.Action==null){Skin.Line(g,Color.Gray,r.X+4,y+3,r.Right-4,y+3);Skin.Line(g,Color.White,r.X+4,y+4,r.Right-4,y+4);y+=7;continue;}
            var item=new RectangleF(r.X+3,y,r.Width-6,22);bool hover=item.Contains(mouse) || (entry.Enabled && menuFocus==activeIndex);
            skin.MenuItem(g,item,hover);
            var color=entry.Enabled?(hover && !skin.Vista?Color.White:Color.Black):Color.Gray;
            var labelRect=new RectangleF(item.X+labelInset,item.Y,item.Width-labelInset-shortcutWidth-28,item.Height);
            skin.Mnemonic(g,entry.Label,labelRect,color);
            skin.Text(g,entry.Key,new(item.Right-shortcutWidth-8,item.Y,shortcutWidth,item.Height),color);
            Add("menu-item-"+y,item,entry.Action,entry.Enabled);if(entry.Enabled)activeIndex++;y+=22;
        }
    }
    public void RenderTo(string path,DialogPage page=DialogPage.None,int openMenu=-1,bool inactive=false,bool maximize=false)
    {
        StopCardMotion();
        dialog=page;draft=Preferences.Clone();menu=openMenu;windowActive=!inactive;maximized=maximize;dialogOffset=PointF.Empty;
        if(page==DialogPage.Statistics)statisticsLevel=Game.State.SpiderSuits;
        using var image=new Bitmap(ClientSize.Width,ClientSize.Height);
        using(var g=Graphics.FromImage(image))PaintScaled(g);
        image.Save(path,System.Drawing.Imaging.ImageFormat.Png);
        dialog=DialogPage.None;menu=-1;windowActive=true;maximized=false;
    }
    public void SetRenderState(GameState state){Game=Game.Restore(Preferences.Rules,state);}
}
