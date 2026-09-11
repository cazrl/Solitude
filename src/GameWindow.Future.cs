using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Solitude;

public sealed partial class GameWindow
{
    private sealed record FuturePulse(int Foundation,double Start);
    private readonly List<FuturePulse> futurePulses=[];
    private string? futureMessage;
    private double futureMessageUntil;
    private FutureArt Orbit=>art.Future;
    private RectangleF OrbitLogoButton=>new(0,0,36,36);
    private double? orbitLogoAnimationStart;
    private bool OrbitLogoAnimating=>skin!=null && skin.Future && OrbitMotionEnabled && windowActive && editionMorph==null && dialog==DialogPage.None && !showingVictory && (menu>=0 || OrbitLogoButton.Contains(mouse));
    private void PaintOrbitLogoButton(Graphics g)
    {
        var r=OrbitLogoButton;bool usable=dialog==DialogPage.None && !showingVictory;
        bool active=usable && (r.Contains(mouse) || menu>=0),pressed=usable && (pressedHotspot=="future-menu" || menu>=0);
        var buttonState=g.Save();using var window=skin.WindowShape(new(0,0,WorldWidth,WorldHeight),maximized);g.SetClip(window,CombineMode.Intersect);
        Skin.Fill(g,pressed?Color.FromArgb(22,65,74):active?Color.FromArgb(28,55,70):Color.FromArgb(20,37,51),r);
        Color edge=active?Orbit.Accent:Color.FromArgb(83,126,144);
        Skin.Line(g,edge,r.Right-.5f,r.Top,r.Right-.5f,r.Bottom-.5f);Skin.Line(g,edge,r.Left,r.Bottom-.5f,r.Right-.5f,r.Bottom-.5f);
        g.Restore(buttonState);
        var saved=g.Save();g.TranslateTransform(r.X+r.Width/2,r.Y+r.Height/2);
        float angle=orbitLogoAnimationStart is {} start?(float)((MotionNow-start)*55%360):0;
        g.RotateTransform(angle);FutureArt.OrbitMark(g,new(-10,-10,20,20),usable?Orbit.Accent:FutureArt.Muted);
        if(active)
        {
            using var dot=new SolidBrush(Orbit.Accent);g.FillEllipse(dot,8,-2,3,3);
        }
        g.Restore(saved);
        if(dialog==DialogPage.None)hotspots.Add(new("future-menu",r,()=>{menu=menu>=0?-1:0;menuFocus=-1;ScheduleFrames();Invalidate();},usable,"Game menu",AccessibleRole.ButtonMenu,menu>=0));
    }
    private void PaintFutureGame(Graphics g)
    {
        hotspots.Clear();cardAreas.Clear();skin.Configure(g);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
        // Clear the exposed surface before alpha edges are drawn; repeated
        // paints must not accumulate the antialiased window outline.
        g.CompositingMode=CompositingMode.SourceCopy;Skin.Fill(g,Color.Transparent,new(0,0,WorldWidth,WorldHeight));g.CompositingMode=CompositingMode.SourceOver;
        art.RenderScale=ScaleFactor;Orbit.Palette=Preferences.FuturePalette;skin.FutureAccent=Orbit.Accent;EnsureLayout();
        var buttons=skin.Frame(g,new(0,0,WorldWidth,WorldHeight),"SOLITUDE     /     2126",active:windowActive,pointer:dialog==DialogPage.None?mouse:null,down:pressedHotspot!=null,maximized:maximized);
        Orbit.DrawScene(g,new(1,WindowHeader,WorldWidth-2,WorldHeight-WindowHeader-1),ScaleFactor,maximized);
        if(dialog==DialogPage.None)
        {
            Add("minimize",buttons[0],()=>WindowState=FormWindowState.Minimized);Add("maximize",buttons[1],ToggleMaximize);Add("close",buttons[2],Close);
        }
        PaintOrbitLogoButton(g);
        float left=Table.Left+TableMargin;
        Orbit.Text(g,"O R B I T",new(left,WindowHeader+4+8*OrbitRoom,245,34+6*OrbitRoom),25+4*OrbitRoom,FutureArt.Ink);
        float right=WorldWidth-TableMargin;
        float controlsY=WindowHeader+6+16*OrbitRoom;
        FutureControl(g,"settings",new(right-104,controlsY,104,32),"Settings",()=>OpenDialog(DialogPage.Settings));
        FutureControl(g,"future-experience",new(right-226,controlsY,110,32),"Experience",()=>OpenDialog(DialogPage.Options));
        Orbit.Text(g,"SCORE",new(right-332,controlsY-5,86,15),9,FutureArt.Muted,true);
        Orbit.Text(g,Game.Rules.Scoring==Scoring.None?"—":Game.State.Score.ToString(),new(right-332,controlsY+9,86,28),22,FutureArt.Ink,true);
        Skin.Line(g,Color.FromArgb(36,64,82),left,Table.Top-2,right,Table.Top-2);
        PaintCachedTable(g);
        PaintFutureGuidance(g);
        PaintMovingCards(g);
        if(dragging && selection.HasValue)PaintDrag(g);
        PaintFutureEffects(g);
        PaintFutureDock(g);
        PaintOrbitAccessibleFocus(g);
        if(showingVictory)PaintFutureVictory(g);
        if(menu>=0)PaintFutureMenu(g);
        if(dialog!=DialogPage.None)
        {
            Skin.Fill(g,Color.FromArgb(115,2,7,14),new(1,WindowHeader,WorldWidth-2,WorldHeight-WindowHeader-1));
            if(dialogHost==null)PaintDialog(g);else dialogHost.RefreshSurface();
        }
        // Paint the rim last: scene/table caches use SourceCopy and would
        // otherwise erase the inside half of the window's curved outline.
        skin.FutureWindowOutline(g,new(0,0,WorldWidth,WorldHeight),ScaleFactor,maximized);
    }
    private void FutureControl(Graphics g,string id,RectangleF r,string label,Action action,bool primary=false,bool enabled=true)
    {
        bool usable=enabled && dialog==DialogPage.None && !showingVictory,hover=usable && r.Contains(mouse);
        if(primary)
        {
            FutureArt.Panel(g,r,usable?(hover?ControlPaint.Light(Orbit.Accent):Orbit.Accent):Color.FromArgb(27,45,56),Color.Transparent,8);
            skin.FutureButtonLabel(g,label,r,usable?Color.FromArgb(7,33,39):FutureArt.Muted,skin.Bold);
        }
        else skin.Button(g,r,label,hover,enabled:usable,pressed:pressedHotspot==id && hover);
        if(dialog==DialogPage.None){Add(id,r,action,usable);hotspots[^1]=hotspots[^1] with{Label=label};}
    }
    private void FutureBay(Graphics g,RectangleF r,int foundation=-1)
    {
        FutureArt.Panel(g,r,Color.FromArgb(55,16,35,48),Color.FromArgb(75,Orbit.Accent),7);
        if(foundation>=0)
        {
            float side=Math.Min(25,r.Width*.3f);FutureArt.OrbitMark(g,new(r.X+(r.Width-side)/2,r.Y+(r.Height-side)/2,side,side),Color.FromArgb(105,Orbit.Accent));
            using var p=new Pen(Color.FromArgb(30,Orbit.Accent),.7f);g.DrawEllipse(p,r.X+r.Width*.2f,r.Y+r.Height*.3f,r.Width*.6f,r.Height*.4f);
        }
        else
        {
            using var p=new Pen(Color.FromArgb(60,Orbit.Accent),.8f);float x=r.X+r.Width/2,y=r.Y+r.Height/2;g.DrawLine(p,x-6,y,x+6,y);g.DrawLine(p,x,y-6,x,y+6);
        }
    }
    private void PaintFutureTable(Graphics g)
    {
        if(showingVictory)return;
        var saved=g.Save();g.SetClip(Table,CombineMode.Intersect);
        var stock=StockRect;Orbit.Text(g,Game.State.Stock.Count>0?"DRAW  /  "+Game.State.Stock.Count:Game.CanRecycle?"RECYCLE":"STOCK EMPTY",new(stock.X,stock.Y-24,CardWidth+80,19),9,FutureArt.Muted);
        Orbit.Text(g,"FOUNDATIONS",new(TopCard(3).X,stock.Y-24,300,19),9,FutureArt.Muted);
        if(Game.State.Stock.Count>0)
        {
            var behind=stock;behind.Offset(3,4);Orbit.DrawCard(g,new(0,false),behind,ScaleFactor);Orbit.DrawCard(g,new(0,false),stock,ScaleFactor);
        }
        else
        {
            FutureBay(g,stock);if(Game.CanRecycle){using var p=new Pen(Orbit.Accent,1.5f);g.DrawArc(p,stock.X+stock.Width/2-13,stock.Y+stock.Height/2-13,26,26,30,290);Orbit.Text(g,"↻",stock,27,Orbit.Accent,true);}
        }
        int count=Math.Max(1,Math.Min(Game.State.WasteFan,Game.State.Waste.Count));
        for(int i=0;i<count && Game.State.Waste.Count>0;i++)
        {
            int index=Game.State.Waste.Count-count+i;var r=TopCard(1);r.X+=i*WasteStep;
            var pos=new Position(PileKind.Waste,0,index);if(!(dragging && IsSelected(pos)))DrawGameCard(g,Game.State.Waste[index],r);
            if(i==count-1)cardAreas.Add((pos,r));
        }
        for(int col=0;col<4;col++)
        {
            var r=TopCard(col+3);var pile=Game.State.Foundations[col];var pos=new Position(PileKind.Foundation,col);FutureBay(g,r,col);
            if(!showingVictory && pile.Count>0)
            {
                int index=SettledTop(pile,pos);
                if(index>=0)DrawGameCard(g,pile[index],r);cardAreas.Add((pos,r));
            }
            float progress=pile.Count/13f;Skin.Line(g,Color.FromArgb(43,63,77),r.X,r.Bottom+12,r.Right,r.Bottom+12);
            if(progress>0)Skin.Line(g,Orbit.Accent,r.X,r.Bottom+12,r.X+r.Width*progress,r.Bottom+12);
        }
        for(int col=0;col<7;col++)
        {
            var columnClip=g.Save();g.SetClip(OrbitColumnViewport(col),CombineMode.Intersect);
            var pile=Game.State.Tableau[col];if(pile.Count==0 && !showingVictory)FutureBay(g,TableauCard(col,0));
            for(int i=0;i<pile.Count;i++)
            {
                var pos=new Position(PileKind.Tableau,col,i);var r=TableauCard(col,i);
                if(dragging && selection is {} selected && selected.Kind==PileKind.Tableau && selected.Pile==col && i>=Game.Index(selected))break;
                DrawGameCard(g,pile[i],r);var hit=r;if(i<pile.Count-1)hit.Height=TableauCard(col,i+1).Y-r.Y;
                hit=RectangleF.Intersect(hit,OrbitColumnViewport(col));if(hit.Width>0 && hit.Height>0)cardAreas.Add((pos,hit));
            }
            g.Restore(columnClip);
        }
        g.Restore(saved);
    }
    private RectangleF FuturePosition(Position p)=>p.Kind switch
    {
        PileKind.Stock=>StockRect,PileKind.Waste=>new(TopCard(1).X+Math.Max(0,Math.Min(Game.State.WasteFan,Game.State.Waste.Count)-1)*WasteStep,TopCard(1).Y,CardWidth,CardHeight),
        PileKind.Foundation=>TopCard(p.Pile+3),_=>TableauCard(p.Pile,Math.Max(0,p.Index<0?Game.State.Tableau[p.Pile].Count-1:p.Index))
    };
    private void FutureOutline(Graphics g,RectangleF r,Color color,float strength=1)
    {
        r.Inflate(3,3);using var path=Skin.Rounded(r,9);
        for(int i=3;i>=1;i--){using var p=new Pen(Color.FromArgb((int)(12*strength),color),i*3);g.DrawPath(p,path);}
        using var edge=new Pen(Color.FromArgb((int)(220*strength),color),1.3f);g.DrawPath(edge,path);
    }
    private void PaintFutureGuidance(Graphics g)
    {
        if(dialog!=DialogPage.None || showingVictory)return;var saved=g.Save();g.SetClip(Table,CombineMode.Intersect);
        if(selection is {} selected && !dragging)FutureOutline(g,FuturePosition(selected),Orbit.Accent);
        if(dragging && selection is {} from)
        {var to=FindDropTarget(from,mouse);if(to.HasValue)FutureOutline(g,FuturePosition(to.Value),Orbit.Accent);}
        else if(!MotionActive && HitCard(mouse) is {} hover && Game.CanPick(hover.Position))FutureOutline(g,FuturePosition(hover.Position),Orbit.Accent,.55f);
        if(hint is {} suggestion)
        {
            var a=FuturePosition(suggestion.From);var b=FuturePosition(suggestion.To);FutureOutline(g,a,Color.FromArgb(249,211,143));FutureOutline(g,b,Orbit.Accent);
            if(suggestion.From!=suggestion.To)
            {
                var start=new PointF(a.X+a.Width/2,a.Y+a.Height/2);var end=new PointF(b.X+b.Width/2,b.Y+b.Height/2);
                using var p=new Pen(Color.FromArgb(210,Orbit.Accent),1.6f){DashStyle=DashStyle.Dash,EndCap=LineCap.ArrowAnchor};g.DrawBezier(p,start,new(start.X,start.Y-48),new(end.X,end.Y-48),end);
            }
        }
        g.Restore(saved);
    }
    private void PaintFutureDock(Graphics g)
    {
        float y=WorldHeight-61,left=TableMargin,right=WorldWidth-TableMargin;
        FutureArt.Panel(g,new(WorldWidth/2-189,y-5,378,47),Color.FromArgb(220,15,29,43),Color.FromArgb(47,77,95),13);
        FutureControl(g,"future-undo",new(WorldWidth/2-181,y+1,96,34),"Undo",UndoMove,enabled:Game.CanUndo);
        FutureControl(g,"future-hint",new(WorldWidth/2-77,y+1,91,34),"Hint",ShowHint,enabled:!Game.State.Won);
        if(CanFinish && !collecting && !showingVictory)
            FutureControl(g,"future-finish",new(WorldWidth/2+22,y+1,159,34),"Complete the orbit",CollectCards,true);
        else FutureControl(g,"future-new",new(WorldWidth/2+22,y+1,159,34),"New deal",()=>RequestNew(),true);
        int home=Game.State.Foundations.Sum(p=>p.Count);Orbit.Text(g,$"{home:00} / 52",new(left,y,136,24),18,Orbit.Accent);
        Orbit.Text(g,"CARDS ALIGNED",new(left,y+24,150,17),10,FutureArt.Muted);
        Orbit.Text(g,$"{Game.State.Elapsed/60:00}:{Game.State.Elapsed%60:00}",new(right-100,y,100,24),18,FutureArt.Ink,true);
        Orbit.Text(g,$"{Game.State.Moves} MOVES",new(right-100,y+24,100,17),10,FutureArt.Muted,true);
        string? message=collecting?"Aligning the remaining cards…":futureMessage!=null && MotionNow<futureMessageUntil?futureMessage:hint.HasValue?futureHintText:null;
        if(message!=null)Orbit.Text(g,message,new(left,WorldHeight-22,WorldWidth-2*TableMargin,20),11,FutureArt.Ink,true);
    }
    private void FutureMoveFeedback()
    {
        futureMessage=null;if(futureActionSound=="SHARED_UNDO" || !OrbitMotionEnabled || !Preferences.FutureAtmosphere)return;
        var before=Game.History.LastOrDefault();if(before==null)return;
        for(int i=0;i<4;i++)if(Game.State.Foundations[i].Count>before.Foundations[i].Count)futurePulses.Add(new(i,MotionNow+.30));
        if(futurePulses.Count>12)futurePulses.RemoveRange(0,futurePulses.Count-12);
    }
    private void UpdateFutureEffects()=>futurePulses.RemoveAll(p=>MotionNow-p.Start>1.05 || !Preferences.Animate || !Preferences.FutureAtmosphere);
    private void PaintFutureEffects(Graphics g)
    {
        if(!OrbitMotionEnabled || !Preferences.FutureAtmosphere)return;
        foreach(var pulse in futurePulses)
        {
            if(MotionNow<pulse.Start)continue;
            float t=(float)Math.Clamp((MotionNow-pulse.Start)/1.05,0,1);var r=TopCard(pulse.Foundation+3);
            float radius=CardWidth*(.2f+t*.8f),x=r.X+r.Width/2,y=r.Y+r.Height/2;
            using var pen=new Pen(Color.FromArgb((int)((1-t)*125),Orbit.Accent),1.3f);g.DrawEllipse(pen,x-radius,y-radius,radius*2,radius*2);
            for(int i=0;i<8;i++){float a=i*MathF.Tau/8+t*.4f;using var b=new SolidBrush(Color.FromArgb((int)((1-t)*170),Orbit.Accent));g.FillEllipse(b,x+MathF.Cos(a)*radius-1,y+MathF.Sin(a)*radius-1,2,2);}
        }
    }
    private void PaintFutureMenu(Graphics g)
    {
        var entries=PeriodMenu();float width=288,x=OrbitLogoButton.Left,y=WindowHeader+4;
        FutureArt.Panel(g,new(x,y,width,entries.Sum(e=>e.Action==null?10:31)+12),Color.FromArgb(19,33,47),Color.FromArgb(65,91,110),9);float rowY=y+6;int index=0;
        foreach(var entry in entries)
        {
            if(entry.Action==null){Skin.Line(g,Color.FromArgb(53,75,92),x+12,rowY+5,x+width-12,rowY+5);rowY+=10;continue;}
            var r=new RectangleF(x+6,rowY,width-12,30);bool focused=entry.Enabled && (r.Contains(mouse) || menuFocus==index);
            if(focused)FutureArt.Panel(g,r,Color.FromArgb(37,62,79),Color.Transparent,5);
            skin.Mnemonic(g,entry.Label,new(r.X+10,r.Y,r.Width-68,r.Height),entry.Enabled?FutureArt.Ink:FutureArt.Muted);
            Orbit.Text(g,entry.Key,new(r.Right-63,r.Y,57,r.Height),9,FutureArt.Muted,true);
            Add("menu-item-"+index,r,entry.Action,entry.Enabled);if(entry.Enabled)index++;rowY+=31;
            hotspots[^1]=hotspots[^1] with{Label=entry.Label.Replace("&",""),Role=AccessibleRole.MenuItem};
        }
    }
}
