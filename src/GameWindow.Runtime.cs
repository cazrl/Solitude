using System.Diagnostics;

namespace Solitude;

public sealed partial class GameWindow
{
    // Gameplay uses monotonic whole milliseconds, independently of the high-
    // resolution animation clock. Timer callbacks only sample this clock.
    private void AdvanceGameTime(long now,bool active)
    {
        long delta=Math.Max(0,now-lastTick);lastTick=now;
        if(!active)return;
        elapsedMilliseconds+=delta;
        long seconds=elapsedMilliseconds/1000;elapsedMilliseconds%=1000;
        for(long i=0;i<seconds;i++)Game.Tick();
    }
    private FramePump? framePump;
    private bool startedRendering,saveDirty;
    private double saveDue;
    private readonly SaveCoordinator saves;
    private string? reportedSaveError;
    private readonly Queue<double> frameTimes=[];
    private double lastPaintTime,paintMilliseconds;
    private double orbitPaintAverage;
    private int EffectiveFrameRate=>(skin.Future || editionMorph!=null) && Preferences.FrameRate>60 && orbitPaintAverage>1000.0/Preferences.FrameRate*.9?60:Preferences.FrameRate;
    private string? hoverId;
    private RectangleF previousDamage;
    private bool orbitHadDynamicFrame;
    private Rectangle FrameDamage()
    {
        if(skin.Future)
        {
            bool dynamic=MotionActive || dragging || collecting || showingVictory || pendingWin || futurePulses.Count>0;
            bool settling=orbitHadDynamicFrame;orbitHadDynamicFrame=dynamic;
            // One full repaint settles a completed flight or erases a fading
            // effect before the logo can resume corner-only invalidations.
            if(OrbitLogoAnimating && !dynamic && !settling && boardStamp is {Flights:0,Dragging:false} && boardStamp.State==Game.State && boardStamp.Moves==Game.State.Moves && boardStamp.Selection==selection)
                return Rectangle.Ceiling(new RectangleF(0,0,OrbitLogoButton.Width*ScaleFactor+2,OrbitLogoButton.Height*ScaleFactor+2));
            return ClientRectangle;
        }
        RectangleF current=RectangleF.Empty;
        void Include(RectangleF r){r.Inflate(8,8);current=current.IsEmpty?r:RectangleF.Union(current,r);}
        // A lifted stack also changes its source; FreeCell's king follows the pointer.
        if(boardStamp==null || boardStamp.Dragging!=dragging || boardStamp.Selection!=selection ||
            Kind==GameKind.FreeCell && boardStamp.KingRight!=(mouse.X>Table.Left+Table.Width/2))Include(Table);
        foreach(var f in flights.Values){Include(f.From.Rect);Include(f.Sample(MotionNow).Rect);Include(f.To.Rect);}
        if(dragging && selection is {} selected)
        {
            var pile=Game.Pile(selected)!;
            Include(new(mouse.X-dragOffset.X,mouse.Y-dragOffset.Y,CardWidth,CardHeight+(pile.Count-Game.Index(selected)-1)*(selected.Kind==PileKind.Tableau?StackStep(selected.Pile):18)));
            var target=FindDropTarget(selected,mouse);
            if(target.HasValue)Include(target.Value.Kind==PileKind.Tableau?TableauCard(target.Value.Pile,Math.Max(0,Game.State.Tableau[target.Value.Pile].Count-1)):Kind==GameKind.FreeCell?CellRect(target.Value.Pile,target.Value.Kind==PileKind.Foundation):TopCard(target.Value.Pile+3));
        }
        if(showingVictory)Include(Table);
        var damage=previousDamage.IsEmpty?current:current.IsEmpty?previousDamage:RectangleF.Union(current,previousDamage);previousDamage=current;
        // Align raster clipping with the scaled pixel grid, including 125% and 150%.
        int grid=skin.DeviceText?1:Preferences.Scale switch{125=>5,150=>3,200=>2,_=>1};
        return damage.IsEmpty?Rectangle.Empty:Rectangle.FromLTRB((int)Math.Floor(damage.Left*ScaleFactor/grid)*grid,(int)Math.Floor(damage.Top*ScaleFactor/grid)*grid,(int)Math.Ceiling(damage.Right*ScaleFactor/grid)*grid,(int)Math.Ceiling(damage.Bottom*ScaleFactor/grid)*grid);
    }
    private bool NeedsFrames=>editionMorph!=null || OrbitLogoAnimating || dialog==DialogPage.None && (MotionActive || dragging || collecting || VictoryNeedsFrames || pendingWin || skin.Future && futurePulses.Count>0);
    private void RenderNextFrame()
    {
        var previous=frameTime;frameTime=MotionNow;
        try
        {
            if(NeedsFrames && windowActive && WindowState!=FormWindowState.Minimized)
            {
                if(editionMorph!=null){UpdateEditionMorph();Invalidate();}
                else {Animate();var damage=FrameDamage();if(damage.IsEmpty)Invalidate();else Invalidate(damage);}
                Update();
            }
            ScheduleFrames();
        }
        finally{frameTime=previous;}
    }
    private void StartFrameClock()
    {
        startedRendering=true;
        try{framePump??=new FramePump(Handle);}catch(System.ComponentModel.Win32Exception){animation.Interval=8;}
        ScheduleFrames();
    }
    private void ScheduleFrames()
    {
        if(OrbitLogoAnimating)orbitLogoAnimationStart??=MotionNow;else orbitLogoAnimationStart=null;
        bool active=NeedsFrames && windowActive && WindowState!=FormWindowState.Minimized;
        framePump?.SetActive(active,EffectiveFrameRate);
        if(framePump==null && startedRendering){animation.Interval=Math.Max(1,(int)Math.Round(1000.0/EffectiveFrameRate));animation.Enabled=active;}
    }
    protected override void OnHandleDestroyed(EventArgs e){framePump?.Dispose();framePump=null;base.OnHandleDestroyed(e);}
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);if(startedRendering)StartFrameClock();}
    private void RequestSave(){if(ephemeral)return;saveDirty=true;saveDue=MotionNow+.35;}
    // History entries are frozen; copy the list, not thousands of immutable boards.
    // Undo clones its entry before mutation. Active state/preferences still need deep copies.
    private SaveFile SnapshotSave()
    {
        SavedSession Copy(SavedSession s)=>new(){Preferences=s.Preferences.Clone(),ActiveRules=s.ActiveRules?.Clone(),Statistics=s.Statistics.Clone(),Game=s.Preferences.SaveOnExit?s.Game?.Clone():null,History=s.Preferences.SaveOnExit?s.History.ToList():[]};
        var key=GameCatalog.Key(Preferences.Era,Kind);
        return new()
        {
            Preferences=Preferences.Clone(),Statistics=Statistics.Clone(),Game=Preferences.SaveOnExit || vistaSaveOnce?Game.State.Clone():null,
            Shared=shared.Clone(),ActiveRules=Game.Rules.Clone(),
            History=Preferences.SaveOnExit || vistaSaveOnce?Game.History.ToList():[],
            Sessions=sessions.Where(s=>s.Key!=key).ToDictionary(s=>s.Key,s=>Copy(s.Value)),
            SpiderSaves=spiderSaves.ToDictionary(p=>p.Key,p=>p.Value.Clone())
        };
    }
    private void BackgroundSaveTick()
    {
        if(saveDirty && MotionNow>=saveDue && !dragging){saveDirty=false;saves.Queue(SnapshotSave());}
        var error=saves.Error;
        if(error!=reportedSaveError){reportedSaveError=error;if(error!=null){notice=error;OpenDialog(DialogPage.Notice);}}
    }
    private void RecordFrame(long start)
    {
        double now=MotionNow;paintMilliseconds=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        if(skin.Future || editionMorph!=null)orbitPaintAverage=orbitPaintAverage==0?paintMilliseconds:orbitPaintAverage*.85+paintMilliseconds*.15;
        if(lastPaintTime>0 && now-lastPaintTime<.25){frameTimes.Enqueue(now-lastPaintTime);if(frameTimes.Count>120)frameTimes.Dequeue();}
        else frameTimes.Clear();lastPaintTime=now;
    }
    private int collectStartHome;
    private void CollectCards()
    {
        if(Kind!=GameKind.Klondike)return;
        selection=null;collectStartHome=Game.State.Foundations.Sum(p=>p.Count);collecting=true;nextCollect=MotionNow;ScheduleFrames();Invalidate();
    }
    private GameState? finishState;
    private int finishMoves=-1;
    private bool finishAvailable;
    private bool CanFinish
    {
        get
        {
            if(!skin.Future)return Kind==GameKind.Klondike && !Game.State.Won && Game.State.Stock.Count==0 && Game.State.Tableau.All(p=>p.All(c=>c.FaceUp));
            if(finishState!=Game.State || finishMoves!=Game.State.Moves){finishState=Game.State;finishMoves=Game.State.Moves;finishAvailable=Game.CanCollectToWin();}
            return finishAvailable;
        }
    }
    private void QueueSaveNow(){if(ephemeral)return;saveDirty=false;saves.Queue(SnapshotSave());}
    private Position? FindDropTarget(Position source,PointF point)
    {
        var direct=Destination(point);
        if(direct.HasValue && Game.CanMove(SelectedMove(source,direct.Value),direct.Value))return direct;
        var card=new RectangleF(point.X-dragOffset.X,point.Y-dragOffset.Y,CardWidth,CardHeight);
        return BestOverlap(card,source);
    }
    private void PaintDropTarget(Graphics g)
    {
        if(!dragging || selection is not {} source || !skin.Modern && !Preferences.OutlineDragging)return;
        var to=FindDropTarget(source,mouse);if(to==null)return;
        var r=to.Value.Kind==PileKind.Tableau?TableauCard(to.Value.Pile,Math.Max(0,Game.State.Tableau[to.Value.Pile].Count-1))
            :Kind==GameKind.FreeCell?CellRect(to.Value.Pile,to.Value.Kind==PileKind.Foundation):TopCard(to.Value.Pile+3);
        if(!skin.Modern)
        {
            var pile=Game.Pile(to.Value);
            if(pile is {Count:>0})art.Draw(g,pile[^1],r,Preferences.Era,Preferences.CardBack,invert:true);
            else {using var pen=new Pen(Color.White,2);g.DrawRectangle(pen,r.X,r.Y,r.Width-1,r.Height-1);}
        }
        else {r.Inflate(2,2);using var pen=new Pen(Color.FromArgb(190,220,239,255),2);g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);}
    }
}
