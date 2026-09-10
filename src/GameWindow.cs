using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;

namespace Solitude;

public enum DialogPage { None, Settings, Options, Deck, Help, About, Statistics, NewGame, Restart, Won, Notice, SelectGame, MoveColumn, Difficulty, Confirm, Records, AppAbout, Lost }
internal sealed record Hotspot(string Id, RectangleF Bounds, Action Action, bool Enabled = true);
public sealed partial class GameWindow : Form
{
    public Preferences Preferences { get; private set; }
    public Game Game { get; private set; }
    public Statistics Statistics { get; private set; }
    private readonly Store store;
    private readonly Dictionary<string, SavedSession> sessions;
    private readonly SharedSettings shared;
    private Skin skin;
    private readonly CardArt art = new();
    private readonly System.Windows.Forms.Timer clock = new() { Interval = 250 };
    private readonly System.Windows.Forms.Timer animation = new() { Interval = 25 };
    private readonly Stopwatch activeTime = new();
    private double lastTick;
    private double elapsedFraction;
    private int lastSavedSecond;
    private readonly List<Hotspot> hotspots = [];
    private readonly List<(Position Position, RectangleF Rect)> cardAreas = [];
    private PointF mouse;
    private Position? selection;
    private PointF mouseDown, dragOffset;
    private bool dragging, pressedCard;
    private int? doubleClickCard;
    private GameState? doubleClickState;
    private int doubleClickMoves;
    private string? pressedHotspot;
    private bool windowActive=true, movingDialog;
    private Position? peekCard;
    private bool oneMoveWarning;
    private bool lossSameGame;
    private PointF dialogOffset, dialogDragStart, dialogDragOrigin;
    private RectangleF dialogBounds;
    private Bitmap? pixelCanvas,pixelPresentation;
    private int menu = -1, menuFocus = -1, keyboardFocus = -1, keyboardPile;
    private DialogPage dialog;
    private Preferences? draft;
    private string notice = "", status = "";
    private DateTime statusUntil;
    private (Position From, Position To)? hint;
    private bool collecting;
    private Bitmap? victoryTrail;
    private readonly List<FlyingCard> flying = [];
    private int victoryFrame, victoryDealt;
    private bool showingVictory;
    private bool shutdown;
    private bool maximized;
    private Rectangle restoreBounds;
    private readonly bool ephemeral;
    private float ScaleFactor => Preferences.Scale / 100f;
    private float WorldWidth => ClientSize.Width / ScaleFactor;
    private float WorldHeight => ClientSize.Height / ScaleFactor;
    private int WindowBorder=>skin.Vista && maximized?0:skin.Border;
    private int WindowHeader=>skin.Vista && maximized?21:skin.Border+skin.Caption;
    private RectangleF Table => new(WindowBorder, WindowHeader+skin.MenuHeight, WorldWidth - WindowBorder * 2, WorldHeight - WindowHeader-skin.MenuHeight - WindowBorder - (Preferences.ShowStatus ? skin.StatusHeight : 0));
    private GameKind Kind => Game.Rules.Kind;
    private string GameTitle => Kind == GameKind.FreeCell && !skin.Vista ? $"FreeCell Game #{Game.State.Seed}" : Kind == GameKind.Spider && !skin.Vista ? "Spider" : GameCatalog.Name(Kind);
    private float CardWidth => skin.Vista ? Math.Clamp(Table.Width*(Kind==GameKind.Klondike?.085f:Kind==GameKind.FreeCell?.089f:.074f),42,160) : 71;
    private float CardHeight => CardWidth * (skin.Vista?152f/111:96f/71);
    private float TableMargin => skin.Vista?Table.Width*(Kind==GameKind.Klondike?.1f:.035f):Kind==GameKind.FreeCell?1:12;
    private float ColumnGap => (Table.Width-2*TableMargin-CardWidth)/(Game.State.Tableau.Count-1);
    private float TableauY => Kind==GameKind.Spider?Table.Top+(skin.Vista?30:12):Table.Top + CardHeight + (skin.Vista ? 52 : Kind==GameKind.FreeCell?15:24);
    private RectangleF TopCard(int slot) => new(Table.Left+TableMargin+ColumnGap*slot,Table.Top+(skin.Vista?24:12),CardWidth,CardHeight);
    private RectangleF StockRect => Kind==GameKind.Spider?new(Table.Right-CardWidth-16,Table.Bottom-CardHeight-12,CardWidth,CardHeight):TopCard(0);
    private float TableauBottom => Table.Bottom - (Kind==GameKind.Spider?CardHeight+30:16);
    private float StackStep(int col)
    {
        EnsureLayout();return stackSteps[col];
    }
    private RectangleF TableauCard(int col,int index)
    {
        EnsureLayout();return new(Table.Left+TableMargin+ColumnGap*col,cardY[col][Math.Clamp(index,0,Game.State.Tableau[col].Count)],CardWidth,CardHeight);
    }
    public GameWindow(Store store, Era? era = null, int? seed = null, int? scale = null, bool ephemeral = false, GameKind? kind = null)
    {
        this.store=store;this.ephemeral=ephemeral;saves=new(store);
        var saved=ephemeral?new SaveFile():store.Load();
        shared=ephemeral?new():saved.Shared??SharedSettings.FromSave(saved);
        Rules? storedRules=saved.ActiveRules?.Clone()??saved.Preferences.Rules.Clone();
        sessions=saved.Sessions;spiderSaves=saved.SpiderSaves;Preferences=saved.Preferences;Statistics=saved.Statistics;
        var selectedEra=era??Preferences.Era;var selectedKind=kind??Preferences.Rules.Kind;
        if(!GameCatalog.Available(selectedEra,selectedKind))selectedKind=GameKind.Klondike;
        bool newProfile=ephemeral || !File.Exists(store.FilePath) || selectedEra!=Preferences.Era || selectedKind!=Preferences.Rules.Kind;
        if(newProfile)
        {
            if(!ephemeral && saved.Game!=null)sessions[GameCatalog.Key(Preferences.Era,Preferences.Rules.Kind)]=new(){Preferences=Preferences.Clone(),ActiveRules=storedRules,Statistics=Statistics,Game=saved.Game,History=saved.History};
            if(sessions.TryGetValue(GameCatalog.Key(selectedEra,selectedKind),out var session))
            {Preferences=session.Preferences.Clone();storedRules=session.ActiveRules?.Clone()??Preferences.Rules.Clone();Statistics=session.Statistics;saved.Game=session.Game;saved.History=session.History;}
            else {Preferences=GameCatalog.Defaults(selectedEra,selectedKind,Preferences.Scale);Statistics=new();saved.Game=null;saved.History=[];storedRules=null;}
        }
        if(scale.HasValue)shared.Scale=scale.Value;
        shared.Apply(Preferences);
        GameCatalog.ApplyPeriodPresentation(Preferences);
        skin=new Skin(Preferences.Era);
        if(seed.HasValue || saved.Game==null)Game=new Game(Preferences.Rules.Clone(),seed);
        else Game=Game.Restore(storedRules??Preferences.Rules.Clone(),saved.Game,saved.History);
        offerVistaResume=skin.Vista && !seed.HasValue && saved.Game!=null && Game.State.Started && !Game.State.Won && !Game.State.Lost && !Preferences.ContinueSavedGame;
        Text=GameTitle;
        Name="Solitude";AccessibleName="Solitude Solitaire";
        AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;
        SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);
        DoubleBuffered=true;KeyPreview=true;StartPosition=FormStartPosition.CenterScreen;
        using(var stream=typeof(GameWindow).Assembly.GetManifestResourceStream("Solitude.Assets.solitude.ico")) if(stream!=null)Icon=new Icon(stream);
        ConfigurePeriodIcon();
        ApplySize();
        restingCards=CaptureLayout();
        clock.Tick+=(_,_)=>
        {
            double now=activeTime.Elapsed.TotalSeconds;int before=Game.State.Elapsed;
            bool active=Game.State.Started && !Game.State.Won && !Game.State.Lost && dialog==DialogPage.None && menu<0 && !showingVictory && WindowState!=FormWindowState.Minimized && Form.ActiveForm==this;
            if(active) { elapsedFraction+=now-lastTick;int ticks=(int)elapsedFraction;for(int i=0;i<ticks;i++)Game.Tick();elapsedFraction-=ticks; }
            lastTick=now;
            if(Game.State.Elapsed>=lastSavedSecond+15 && active){lastSavedSecond=Game.State.Elapsed;RequestSave();}
            bool expired=DateTime.Now>statusUntil && (hint.HasValue || status.Length>0);
            if(expired){hint=null;status="";}
            BackgroundSaveTick();
            bool animatedBack=Game.Rules.Timed && windowActive && dialog==DialogPage.None && Kind==GameKind.Klondike && !skin.Xp && !skin.Vista && Preferences.CardBack is 6 or 9 or 10 or 11;
            if(Game.State.Elapsed!=before || expired || animatedBack || oneMoveWarning || vistaTipTitle!=null)Invalidate();
            ScheduleFrames();
        };
        animation.Tick+=(_,_)=>RenderNextFrame();
        Shown+=(_,_)=>{activeTime.Start();clock.Start();StartFrameClock();if(!Game.State.Started)BeginCardMotion(deal:true);if(ClassicSpider && Preferences.SpiderAutoOpen && spiderSaves.ContainsKey(Preferences.Era.ToString()))OpenSpiderGame(true,true);OfferVistaResume();if(store.Warning!=null){notice=store.Warning;OpenDialog(DialogPage.Notice);}};
        FormClosing+=(_,e)=>{ if(shutdown)return; CancelDrag();if(!PeriodClosing(e))return; if(!Save() && !ephemeral) { e.Cancel=true;notice=(saves.Error??store.Warning)+"\n\nUse the window menu > Exit without saving to close anyway.";OpenDialog(DialogPage.Notice); } };
        Activated+=(_,_)=>{windowActive=true;ScheduleFrames();Invalidate();};
        Deactivate+=(_,_)=>{windowActive=false;CancelDrag();ScheduleFrames();RequestSave();lastTick=activeTime.Elapsed.TotalSeconds;};
        MouseCaptureChanged+=(_,_)=>{if(!Capture){peekCard=null;if(dragging && selection.HasValue)ReturnDrag(selection.Value);dragging=false;pressedCard=false;pressedHotspot=null;movingDialog=false;ScheduleFrames();Invalidate();}};
    }
    private void ApplySize()
    {
        MinimumSize=new((int)((Kind==GameKind.Spider?780:Kind==GameKind.FreeCell?640:600)*ScaleFactor),(int)(430*ScaleFactor));
        var area=Screen.FromControl(this).WorkingArea;
        ClientSize=new Size(Math.Min((int)((Kind==GameKind.Spider?900:Kind==GameKind.FreeCell?720:skin.Vista?720:640)*ScaleFactor),area.Width),(int)Math.Min((Kind==GameKind.Spider?600:skin.Vista?540:480)*ScaleFactor,area.Height));
        UpdateWindowShape();
    }
    private void UpdateWindowShape()
    {
        if(skin==null || ClientSize.Width<=0 || ClientSize.Height<=0)return;
        using var shape=skin.WindowShape(new(0,0,WorldWidth,WorldHeight),maximized);
        using var transform=new System.Drawing.Drawing2D.Matrix();transform.Scale(ScaleFactor,ScaleFactor);shape.Transform(transform);
        var previous=Region;Region=new Region(shape);previous?.Dispose();
    }
    protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);UpdateWindowShape();if(Game!=null && skin!=null){layoutState=null;StopCardMotion();ScheduleFrames();}}
    private bool Save()
    {
        if(ephemeral)return true;
        saveDirty=false;return saves.Flush(SnapshotSave());
    }
    private void Changed(bool success)
    {
        if(!success)return;
        BeginCardMotion();
        PlayPeriodSound(Game.State.Won?129:124);
        if(skin.Vista)
        {
            var previous=Game.History.LastOrDefault();
            PlayVistaSound(previous!=null && previous.Foundations.Sum(p=>p.Count)<Game.State.Foundations.Sum(p=>p.Count)?"SHARED_MOVETOHOME":"SHARED_MOVECARDSETDOWN");
        }
        selection=null;hint=null;status="";
        if(skin.Vista && Kind==GameKind.Klondike && Game.History.LastOrDefault() is {} before && before.Foundations.Sum(p=>p.Count)<Game.State.Foundations.Sum(p=>p.Count) && !Game.State.Won)ShowVistaTip("Double-click and right-click","Double-click a card to move it to a foundation. Right-click the table to collect available cards.",true);
        if(Game.State.Moves==1 && Game.State.Elapsed==0){lastTick=activeTime.Elapsed.TotalSeconds;elapsedFraction=0;}
        if(Game.State.Won && !Game.State.WinRecorded)
        {
            Game.State.WinRecorded=true;Statistics.Record(Game.State,true,Game.Rules.Timed);
            sessionStatistics.Record(Game.State,true,Game.Rules.Timed);
            collecting=false;pendingWin=true;if(!MotionActive)UpdateMotions();
        }
        CheckFreeCellEnd();
        RequestSave();ScheduleFrames();Invalidate();
    }
    private void NewGame(bool same = false, int? seed = null)
    {
        RecordAbandonedGame();
        oneMoveWarning=false;vistaSaveOnce=false;peekCard=null;vistaTipTitle=null;
        int balance=Game.State.Score;
        var rules=(same?Game.Rules:Preferences.Rules).Clone();
        bool keepBalance=Kind==GameKind.Klondike && Game.Rules.Scoring==Scoring.Vegas && rules.Scoring==Scoring.Vegas && Preferences.Rules.KeepVegasScore;
        Game=new Game(rules,seed??(same?Game.State.Seed:null));
        if(keepBalance)Game.State.Score+=balance;
        Text=GameTitle;
        BeginCardMotion(deal:true);
        dialog=DialogPage.None;menu=-1;selection=null;hint=null;collecting=false;showingVictory=false;pendingWin=false;victoryTrail?.Dispose();victoryTrail=null;
        status="";lastTick=activeTime.Elapsed.TotalSeconds;elapsedFraction=0;lastSavedSecond=0;RequestSave();ScheduleFrames();Invalidate();
    }
    private void RequestNew(bool same=false)
    {
        menu=-1;
        if(!Game.State.Started || Game.State.Won || Game.State.Lost || Kind==GameKind.Klondike && !skin.Vista)NewGame(same);
        else Confirm(ClassicFreeCell?"Do you want to resign this game?":same?"Do you want to restart this game?":"Do you want to start a new game?",()=>NewGame(same));
    }
    private void ShowHint()
    {
        if(!HasHints)return;
        var result=Game.Hint();
        PlayVistaSound(result.HasValue?"SHARED_HINTSHOWN":"SHARED_HINTNOMOVE");
        if(!result.HasValue)ShowVistaTip("No moves available",Kind==GameKind.Spider && Game.State.Stock.Count>0?"Fill every empty column before dealing another row.":"You can undo a move or start a new game.");
        if(result.HasValue){hint=(result.Value.From,result.Value.To);statusUntil=DateTime.Now.AddSeconds(2);Invalidate();}
    }
    private void OpenDialog(DialogPage page)
    {
        CancelDrag();selection=null;menu=-1;keyboardFocus=-1;collecting=false;dialogOffset=PointF.Empty;dialog=page;draft=Preferences.Clone();
        if(page==DialogPage.SelectGame){dealText=Game.State.Seed.ToString();dealSelectAll=true;}
        if(page==DialogPage.Statistics)statisticsLevel=Game.State.SpiderSuits;
        if(page==DialogPage.Won)winSelectGame=false;
        if(page==DialogPage.Lost)lossSameGame=false;
        QueueDialogHost();
        ScheduleFrames();Invalidate();
    }
    private void CloseDialog(){dialog=DialogPage.None;draft=null;keyboardFocus=-1;lastTick=activeTime.Elapsed.TotalSeconds;ScheduleFrames();Invalidate();}
    private void CancelDrag(){peekCard=null;if(dragging && selection.HasValue)ReturnDrag(selection.Value);dragging=false;pressedCard=false;pressedHotspot=null;movingDialog=false;Capture=false;ScheduleFrames();Invalidate();}
    private PointF Logical(Point point)=>new(point.X/ScaleFactor,point.Y/ScaleFactor);
    private (Position Position,RectangleF Rect)? HitCard(PointF p)
    {var moving=HitMovingCard(p);if(moving!=null)return moving;for(int i=cardAreas.Count-1;i>=0;i--)if(cardAreas[i].Rect.Contains(p)){var area=cardAreas[i];var pile=Game.Pile(area.Position);int index=Game.Index(area.Position);if(pile==null || index<0 || index>=pile.Count)continue;if(flights.TryGetValue(pile[index].Key,out var flight) && MotionNow<flight.End)continue;return area;}return null;}
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);Focus();mouse=Logical(e.Location);keyboardFocus=-1;collecting=false;
        if(showingVictory){FinishVictory();return;}
        if(e.Button==MouseButtons.Right)
        {
            if(dialog==DialogPage.None && ClassicFreeCell){peekCard=HitCard(mouse)?.Position;Capture=peekCard.HasValue;Invalidate();}
            else if(dialog==DialogPage.None && Kind==GameKind.Klondike)CollectCards();
            else if(dialog==DialogPage.None && HasHints)ShowHint();return;
        }
        if(e.Button!=MouseButtons.Left)return;
        if(e.Clicks==1)
        {
            var first=HitCard(mouse);doubleClickCard=first.HasValue?Game.Pile(first.Value.Position)?[Game.Index(first.Value.Position)].Key:null;
            doubleClickState=Game.State;doubleClickMoves=Game.State.Moves;
        }
        // WM_LBUTTONDBLCLK also dispatches MouseDown. It must not repeat a draw,
        // flip or placement that the first click already performed.
        if(e.Clicks>1 && Table.Contains(mouse))return;
        if(menu>=0)
        {
            var settings=hotspots.LastOrDefault(h=>h.Id=="settings" && h.Bounds.Contains(mouse));
            if(settings!=null){menu=-1;pressedHotspot=settings.Id;Capture=true;Invalidate();return;}
            var menuHit=hotspots.LastOrDefault(h=>h.Id.StartsWith("menu-") && h.Bounds.Contains(mouse));
            if(menuHit!=null){if(menuHit.Enabled)menuHit.Action();return;}
            menu=-1;Invalidate();return;
        }
        var hit=hotspots.LastOrDefault(h=>h.Bounds.Contains(mouse));
        if(hit!=null)
        {
            if(hit.Enabled)
            {
                if(hit.Id.StartsWith("bar-") || hit.Id=="system")hit.Action();
                else {pressedHotspot=hit.Id;Capture=true;}
            }
            Invalidate();return;
        }
        if(dialog!=DialogPage.None)
        {
            if(new RectangleF(dialogBounds.X+skin.Border,dialogBounds.Y+skin.Border,dialogBounds.Width-2*skin.Border,skin.Caption).Contains(mouse))
            {movingDialog=true;dialogDragStart=mouse;dialogDragOrigin=dialogOffset;Capture=true;}
            return;
        }
        if(mouse.Y<WindowHeader)
        {
            if(e.Clicks>1){ToggleMaximize();return;}
            ReleaseCapture();SendMessage(Handle,0xA1,2,0);return;
        }
        if(!Table.Contains(mouse))return;
        if(Kind!=GameKind.FreeCell && StockHit(mouse)){DrawCards();return;}
        var cardHit=HitCard(mouse);
        Position? destination=Destination(mouse);
        if(selection.HasValue && destination.HasValue)selection=SelectedMove(selection.Value,destination.Value);
        if((ClassicFreeCell || skin.Vista) && selection.HasValue && destination.HasValue && Game.CanMove(selection.Value,destination.Value))
        {if(!RequestFreeCellColumnMove(selection.Value,destination.Value))Changed(Game.Move(selection.Value,destination.Value));return;}
        if(ClassicFreeCell && selection is {} chosen && destination is {} attempted && !SamePile(chosen,attempted))
        {selection=null;IllegalMove();Invalidate();return;}
        if(cardHit.HasValue)
        {
            var pos=cardHit.Value.Position;
            if(pos.Kind==PileKind.Tableau && Game.Index(pos)==Game.State.Tableau[pos.Pile].Count-1 && !Game.State.Tableau[pos.Pile][^1].FaceUp)
            {Changed(Game.Flip(pos.Pile));return;}
            if(Kind==GameKind.FreeCell && !skin.Vista && pos.Kind==PileKind.Tableau)pos=FreeCellSelection(pos.Pile);
            if(Game.CanPick(pos))
            {
                selection=pos;mouseDown=mouse;var picked=pos.Kind==PileKind.Tableau && pos.Index!=cardHit.Value.Position.Index?TableauCard(pos.Pile,Game.Index(pos)):cardHit.Value.Rect;
                dragOffset=new(mouse.X-picked.X,mouse.Y-picked.Y);pressedCard=true;PlayVistaSound("SHARED_LIFTOFF");Capture=true;Invalidate();return;
            }
        }
        if(selection.HasValue && destination.HasValue)IllegalMove();selection=null;Invalidate();
    }
    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if(e.Button==MouseButtons.Left && dialog==DialogPage.Deck && !skin.Vista)
        {
            var back=hotspots.LastOrDefault(h=>h.Id.StartsWith("dialog-back-") && h.Bounds.Contains(Logical(e.Location)));
            if(back!=null){back.Action();Preferences.CardBack=draft!.CardBack;shared.Capture(Preferences);CloseDialog();Save();}
            return;
        }
        if(e.Button!=MouseButtons.Left || dialog!=DialogPage.None || menu>=0 || showingVictory || ClassicFreeCell && !Preferences.FreeCellDoubleClick || doubleClickState!=Game.State || doubleClickMoves!=Game.State.Moves)return;
        var pos=HitCard(Logical(e.Location));
        if(pos.HasValue && Game.Pile(pos.Value.Position)?[Game.Index(pos.Value.Position)].Key==doubleClickCard)
        {collecting=false;CancelDrag();Changed(Kind==GameKind.FreeCell && !skin.Vista?Game.ToFreeCell(pos.Value.Position):Game.ToFoundation(pos.Value.Position));}
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);mouse=Logical(e.Location);
        if(movingDialog)
        {
            var next=new PointF(dialogDragOrigin.X+mouse.X-dialogDragStart.X,dialogDragOrigin.Y+mouse.Y-dialogDragStart.Y);
            float baseX=dialogBounds.X-dialogOffset.X,baseY=dialogBounds.Y-dialogOffset.Y;
            dialogOffset=new(Math.Clamp(next.X,-baseX,Math.Max(-baseX,WorldWidth-dialogBounds.Width-baseX)),Math.Clamp(next.Y,-baseY,Math.Max(-baseY,WorldHeight-dialogBounds.Height-baseY)));
            Invalidate();return;
        }
        if(!ClassicFreeCell && pressedCard && selection.HasValue && (Math.Abs(mouse.X-mouseDown.X)>SystemInformation.DragSize.Width/(2*ScaleFactor) || Math.Abs(mouse.Y-mouseDown.Y)>SystemInformation.DragSize.Height/(2*ScaleFactor)))dragging=true;
        var hot=hotspots.LastOrDefault(h=>h.Enabled && h.Bounds.Contains(mouse));
        Cursor=dialog==DialogPage.None && menu<0 && skin.Vista && (HitCard(mouse).HasValue || Kind!=GameKind.FreeCell && StockHit(mouse))?Cursors.Hand:Cursors.Default;
        if(dragging){ScheduleFrames();return;}
        string? nextHover=hot?.Id;bool king=Kind==GameKind.FreeCell && !skin.Vista;
        if(nextHover!=hoverId || king || pressedHotspot!=null){hoverId=nextHover;Invalidate();}
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);mouse=Logical(e.Location);
        if(e.Button==MouseButtons.Right){peekCard=null;Capture=false;Invalidate();return;}
        if(e.Button!=MouseButtons.Left)return;
        if(movingDialog){movingDialog=false;Capture=false;Invalidate();return;}
        if(pressedHotspot!=null)
        {
            var target=hotspots.LastOrDefault(h=>h.Id==pressedHotspot && h.Enabled && h.Bounds.Contains(mouse));
            pressedHotspot=null;Capture=false;target?.Action();Invalidate();return;
        }
        bool dropped=dragging;var from=selection;dragging=false;pressedCard=false;Capture=false;
        if(dropped && from.HasValue)
        {
            var dest=FindDropTarget(from.Value,mouse);RememberDragPose(from.Value);
            if(dest.HasValue){bool moved=Game.Move(SelectedMove(from.Value,dest.Value),dest.Value);if(moved)Changed(true);else ReturnDrag(from.Value);}
            else ReturnDrag(from.Value);
            selection=null;
        }
        if(!dropped && !ClassicFreeCell && !skin.Vista)selection=null;
        ScheduleFrames();Invalidate();
    }
    private Position? Destination(PointF p)
    {
        foreach(var target in DestinationAreas())if(target.Rect.Contains(p))return target.Position;
        for(int i=0;i<Game.State.Tableau.Count;i++)
        {
            var rect=TableauCard(i,0);rect.Height=TableauBottom-rect.Top;
            if(rect.Contains(p))return new(PileKind.Tableau,i);
        }
        return null;
    }
    private Position? BestOverlap(RectangleF rect,Position from)
    {
        float best=0;Position? result=null;
        foreach(var entry in DestinationAreas().Concat(Enumerable.Range(0,Game.State.Tableau.Count).Select(i=>(Position:new Position(PileKind.Tableau,i),Rect:TableauCard(i,Math.Max(0,Game.State.Tableau[i].Count-1))))))
        {
            var to=entry.Position;var target=entry.Rect;
            var overlap=RectangleF.Intersect(rect,target);float area=overlap.Width*overlap.Height;
            if(Game.CanMove(SelectedMove(from,to),to) && area>best && area>CardWidth*CardHeight*.12f){best=area;result=to;}
        }
        return result;
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {
        if(keyData==(Keys.Alt|Keys.F4)){Close();return true;}
        if(keyData==Keys.Escape)
        {
            if(showingVictory)FinishVictory();else if(dialog!=DialogPage.None)CloseDialog();else {menu=-1;selection=null;CancelDrag();collecting=false;hint=null;}Invalidate();return true;
        }
        if(dialog!=DialogPage.None)
        {
            if(dialog==DialogPage.SelectGame && EditDealNumber(keyData))return true;
            if(dialog==DialogPage.Help && helpPage==3 && EditHelpKeyword(keyData))return true;
            RefreshDialogInput();
            var controls=hotspots.Where(h=>h.Enabled && h.Id.StartsWith("dialog-")).ToList();
            if((keyData&Keys.Alt)!=0 && dialogMnemonics.TryGetValue(char.ToUpperInvariant((char)(keyData&Keys.KeyCode)),out string? mnemonic))
            {
                var target=controls.FirstOrDefault(h=>h.Id==mnemonic);if(target!=null){keyboardFocus=controls.IndexOf(target);target.Action();Invalidate();}return true;
            }
            if(keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down && keyboardFocus>=0 && keyboardFocus<controls.Count && dialogRadios.TryGetValue(controls[keyboardFocus].Id,out string? group))
            {
                var radios=controls.Where(h=>dialogRadios.GetValueOrDefault(h.Id)==group).ToList();
                int index=(radios.IndexOf(controls[keyboardFocus])+(keyData is Keys.Left or Keys.Up?-1:1)+radios.Count)%radios.Count;
                keyboardFocus=controls.IndexOf(radios[index]);radios[index].Action();Invalidate();return true;
            }
            if(keyData==Keys.Tab || keyData==(Keys.Shift|Keys.Tab))
            {keyboardFocus=(keyboardFocus+(keyData==Keys.Tab?1:-1)+controls.Count)%Math.Max(1,controls.Count);Invalidate();return true;}
            if(keyData==Keys.Enter || keyData==Keys.Space)
            {
                var focused=keyboardFocus>=0 && keyboardFocus<controls.Count?controls[keyboardFocus]:null;
                var target=focused!=null && (keyData==Keys.Space || dialogButtons.Contains(focused.Id))?focused:controls.FirstOrDefault(h=>h.Id=="dialog-ok" || h.Id=="dialog-yes");target?.Action();Invalidate();return true;
            }
            if(dialog==DialogPage.Settings && keyData is Keys.Up or Keys.Down)
            {ChooseDraftEra((Era)(((int)draft!.Era+(keyData==Keys.Down?1:7))%8));Invalidate();return true;}
            return true;
        }
        if(menu>=0)
        {
            var items=PeriodMenu().Where(e=>e.Enabled && e.Action!=null).ToList();
            if(keyData is Keys.Down or Keys.Up){if(items.Count>0)menuFocus=(menuFocus+(keyData==Keys.Down?1:-1)+items.Count)%items.Count;Invalidate();return true;}
            if(keyData==Keys.Enter){if(menuFocus>=0 && menuFocus<items.Count)items[menuFocus].Action!();return true;}
            if(keyData is Keys.Left or Keys.Right){menu=menu==0?1:0;menuFocus=-1;Invalidate();return true;}
            if((keyData&Keys.KeyCode) is >= Keys.A and <= Keys.Z)
            {
                char key=(char)(keyData&Keys.KeyCode);var entry=PeriodMenu().FirstOrDefault(e=>e.Enabled && e.Action!=null && e.Label.IndexOf('&') is int i && i>=0 && i+1<e.Label.Length && char.ToUpperInvariant(e.Label[i+1])==key);
                if(entry!=null){entry.Action!();Invalidate();return true;}
            }
        }
        switch(keyData)
        {
            case Keys.F2:RequestNew();return true;
            case Keys.F3:if(Kind==GameKind.FreeCell)OpenDialog(DialogPage.SelectGame);else if(ClassicSpider)OpenDialog(DialogPage.Difficulty);return true;
            case Keys.F4:if(Kind!=GameKind.Klondike || skin.Vista)OpenDialog(DialogPage.Statistics);return true;
            case Keys.F5:if(Kind!=GameKind.Klondike || skin.Vista)OpenDialog(DialogPage.Options);return true;
            case Keys.F6:OpenDialog(DialogPage.Settings);return true;
            case Keys.F8:case Keys.F12:return true;
            case Keys.F7:if(skin.Vista)OpenDialog(DialogPage.Deck);return true;
            case Keys.F1:if(!skin.Early)OpenHelp(0);return true;
            case Keys.Control|Keys.Z:UndoMove();return true;
            case Keys.F10:if(Kind==GameKind.FreeCell){UndoMove();return true;}break;
            case Keys.Control|Keys.N:RequestNew();return true;
            case Keys.Control|Keys.R:return true;
            case Keys.Control|Keys.S:if(ClassicSpider)SaveSpiderGame();return true;
            case Keys.Control|Keys.O:if(ClassicSpider)OpenSpiderGame();return true;
            case Keys.Alt|Keys.G:menu=0;menuFocus=-1;Invalidate();return true;
            case Keys.Alt|Keys.H:menu=1;menuFocus=-1;Invalidate();return true;
            case Keys.Alt|Keys.Space:menu=2;menuFocus=-1;Invalidate();return true;
            case Keys.H:if(skin.Vista)ShowHint();return true;
            case Keys.M:if(ClassicSpider)ShowHint();return true;
            case Keys.D:if(Kind==GameKind.Spider)DrawCards();return true;
            case Keys.A:return true;
            case Keys.Left:case Keys.Right:case Keys.Tab:
                keyboardPile=(keyboardPile+(keyData==Keys.Left?KeyboardPiles().Count-1:1))%KeyboardPiles().Count;ShowKeyboardPile();return true;
            case Keys.Enter:case Keys.Space:
                var target=KeyboardPosition();
                if(target.Kind==PileKind.Stock){DrawCards();return true;}
                if(selection.HasValue)selection=SelectedMove(selection.Value,target);
                if(selection.HasValue && Game.CanMove(selection.Value,target)){Changed(Game.Move(selection.Value,target));return true;}
                if(target.Kind==PileKind.Tableau && Game.Flip(target.Pile)){Changed(true);return true;}
                if(Kind==GameKind.FreeCell && !skin.Vista && target.Kind==PileKind.Tableau)target=FreeCellSelection(target.Pile);
                if(Game.CanPick(target)){selection=target;Invalidate();}return true;
            case Keys.Up:case Keys.Down:
                if(selection.HasValue && selection.Value.Kind==PileKind.Tableau)
                {
                    var p=selection.Value;int index=Game.Index(p)+(keyData==Keys.Up?-1:1);
                    if(Game.CanPick(p with{Index=index}) && index>=0)selection=p with{Index=index};Invalidate();
                }return true;
        }
        return base.ProcessCmdKey(ref msg,keyData);
    }
    private Position KeyboardPosition()=>KeyboardPiles()[keyboardPile%KeyboardPiles().Count];
    private void ShowKeyboardPile(){var pos=KeyboardPosition();hint=(pos,pos);statusUntil=DateTime.Now.AddSeconds(60);Invalidate();}
    private void ToggleMaximize()
    {
        if(maximized){maximized=false;Bounds=restoreBounds;}else{restoreBounds=Bounds;maximized=true;Bounds=Screen.FromControl(this).WorkingArea;}UpdateWindowShape();Invalidate();
    }
    protected override void WndProc(ref Message m)
    {
        if(m.Msg==FramePump.Message){var pump=framePump;try{RenderNextFrame();}finally{pump?.Acknowledge();}return;}
        const int hitTest=0x84;
        if(m.Msg==hitTest && !maximized && dialog==DialogPage.None)
        {
            base.WndProc(ref m);
            var pt=PointToClient(new Point((short)((long)m.LParam&0xffff),(short)(((long)m.LParam>>16)&0xffff)));
            if(CaptionControlAt(Logical(pt))){m.Result=1;return;}
            int edge=Math.Max(4,(int)(skin.Border*ScaleFactor));bool l=pt.X<edge,r=pt.X>=ClientSize.Width-edge,t=pt.Y<edge,b=pt.Y>=ClientSize.Height-edge;
            if(t&&l)m.Result=13;else if(t&&r)m.Result=14;else if(b&&l)m.Result=16;else if(b&&r)m.Result=17;else if(l)m.Result=10;else if(r)m.Result=11;else if(t)m.Result=12;else if(b)m.Result=15;
            return;
        }
        base.WndProc(ref m);
    }
    private bool CaptionControlAt(PointF point)
    {
        var layout=skin.CaptionLayout(new(0,0,WorldWidth,WorldHeight),maximized:maximized);
        return layout.Minimize.Contains(point) || layout.Maximize.Contains(point) || layout.Close.Contains(point) || layout.Icon.Contains(point);
    }
    [DllImport("user32.dll")]private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]private static extern nint SendMessage(nint hWnd,int message,nint wParam,nint lParam);
    private bool resourcesDisposed;
    protected override void Dispose(bool disposing)
    {
        if(disposing && !resourcesDisposed){resourcesDisposed=true;CloseDialogHost();clock.Dispose();animation.Dispose();framePump?.Dispose();framePump=null;if(!shutdown && !ephemeral && saveDirty)Save();saves.Dispose();foreach(var sound in periodSounds.Values.Concat(vistaSounds.Values)){sound.Stream?.Dispose();sound.Dispose();}skin.Dispose();art.Dispose();victoryTrail?.Dispose();pixelCanvas?.Dispose();pixelPresentation?.Dispose();boardBitmap?.Dispose();}
        base.Dispose(disposing);
    }
    private sealed class FlyingCard(Card card,float x,float y,float vx,float vy)
    {public Card Card=card;public float X=x,Y=y,Vx=vx,Vy=vy;}
    private void StartVictory()
    {
        if(StartPeriodCelebration())return;
        collecting=false;selection=null;showingVictory=true;victoryFrame=0;victoryDealt=0;flying.Clear();
        victoryStart=lastVictoryTime=MotionNow;nextVictoryCard=MotionNow;
        victoryTrail?.Dispose();victoryTrail=new Bitmap((int)Math.Ceiling(WorldWidth),(int)Math.Ceiling(WorldHeight));
        ScheduleFrames();
    }
    private void FinishVictory(){showingVictory=false;boardStamp=null;OpenDialog(DialogPage.Won);}
    private void Animate()
    {
        UpdateMotions();
        if(collecting && !MotionActive && MotionNow>=nextCollect && dialog==DialogPage.None && !showingVictory)
        {bool moved=Game.AutoStep();nextCollect=MotionNow+.04;if(!moved)collecting=false;else Changed(true);}
        if(!showingVictory || victoryTrail==null)return;
        if(Kind!=GameKind.Klondike || skin.Vista){AnimatePeriodCelebration();return;}
        float step=(float)Math.Clamp((MotionNow-lastVictoryTime)*40,0,2);lastVictoryTime=MotionNow;
        victoryFrame++;
        if(MotionNow>=nextVictoryCard && victoryDealt<52)
        {
            nextVictoryCard=MotionNow+.175;
            int foundation=victoryDealt%4,rank=12-victoryDealt/4;var r=TopCard(foundation+3);var card=Game.State.Foundations[foundation][rank];
            float speed=(victoryDealt%2==0?-1:1)*(2+(victoryDealt*7%5));flying.Add(new(card,r.X,r.Y,speed,-2-(victoryDealt%4)));victoryDealt++;
        }
        using(var g=Graphics.FromImage(victoryTrail))
        {
            skin.Configure(g);g.SetClip(Table);
            foreach(var c in flying)
            {
                c.X+=c.Vx*step;c.Y+=c.Vy*step;c.Vy+=.55f*step;
                if(c.Y+CardHeight>=Table.Bottom){c.Y=Table.Bottom-CardHeight;c.Vy=-Math.Abs(c.Vy)*.77f;}
                art.Draw(g,c.Card,new(c.X,c.Y,CardWidth,CardHeight),Preferences.Era,Preferences.CardBack);
            }
            flying.RemoveAll(c=>c.X < -CardWidth || c.X>WorldWidth);
        }
        if(victoryDealt==52 && flying.Count==0 || MotionNow-victoryStart>40)FinishVictory();
        Invalidate();
    }
}
