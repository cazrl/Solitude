using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text.Json;
using Solitude;

internal static class Program
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static string Folder=Path.GetFullPath("artifacts/orbit-audit-20260911");
    static object? Field(GameWindow f,string n)=>typeof(GameWindow).GetField(n,Flags)!.GetValue(f);
    static void Set(GameWindow f,string n,object? v)=>typeof(GameWindow).GetField(n,Flags)!.SetValue(f,v);
    static object? Call(GameWindow f,string n,params object?[] args)=>typeof(GameWindow).GetMethod(n,Flags)!.Invoke(f,args);
    static T Prop<T>(GameWindow f,string n)=>(T)typeof(GameWindow).GetProperty(n,Flags)!.GetValue(f)!;
    static GameWindow Form()=>new(new Store(Path.Combine(Folder,"ephemeral")),Era.Future2126,1989,100,true);
    static void Key(GameWindow f,Keys k){object?[] args=[new Message(),k];typeof(GameWindow).GetMethod("ProcessCmdKey",Flags)!.Invoke(f,args);}
    static Bitmap Paint(GameWindow f,string? name=null)
    {var b=new Bitmap(f.Width,f.Height);using var g=Graphics.FromImage(b);Call(f,"PaintScaled",g);if(name!=null)b.Save(Path.Combine(Folder,name+".png"));return b;}
    static void Activate(GameWindow f,string id)
    {using var b=Paint(f);var h=((IEnumerable)Field(f,"hotspots")!).Cast<object>().Single(h=>(string)h.GetType().GetProperty("Id")!.GetValue(h)! == id);((Action)h.GetType().GetProperty("Action")!.GetValue(h)!)();}
    [STAThread]static void Main(string[] args)
    {
        int output=Array.IndexOf(args,"--output");if(output>=0 && output+1<args.Length)Folder=Path.GetFullPath(args[output+1]);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Directory.CreateDirectory(Folder);
        if(args.Contains("--performance")){Performance();return;}
        var results=new Dictionary<string,object>();int failures=0;
        void Probe(string name,Func<object> run){try{results[name]=run();}catch(Exception e){failures++;results[name]=new{error=e.ToString()};}Console.WriteLine(name+": "+JsonSerializer.Serialize(results[name]));}
        Probe("long-column",()=>
        {
            using var f=Form();var s=new GameState{Seed=1};
            var run=Enumerable.Range(1,13).Reverse().Select(rank=>new Card((rank%2==0?26:0)+rank-1)).ToList();
            var rest=Enumerable.Range(0,52).Where(id=>run.All(c=>c.Id!=id)).ToList();
            s.Tableau[0]=rest.Take(6).Select(id=>new Card(id,false)).Concat(run).ToList();s.Stock=rest.Skip(6).Select(id=>new Card(id,false)).ToList();
            f.SetRenderState(s);Call(f,"StopCardMotion");var sizes=new List<object>();
            foreach(var size in new[]{new Size(800,540),new Size(1120,720)})
            {f.ClientSize=size;using var b=Paint(f,"long-column-"+size.Width);float step=(float)Call(f,"StackStep",0)!;sizes.Add(new{size.Width,size.Height,faceUpStripPixels=step,rankFontPixels=19*Prop<float>(f,"CardWidth")/96});}
            return sizes;
        });
        Probe("shared-options-active-rule-mismatch",()=>
        {
            using var f=Form();Call(f,"DrawCards");Call(f,"StopCardMotion");int seed=f.Game.State.Seed;
            Call(f,"SwitchGame",GameCatalog.Defaults(Era.WindowsXP,GameKind.Klondike,100));Call(f,"OpenDialog",DialogPage.Options);
            ((Preferences)Field(f,"draft")!).Rules.DrawCount=1;Call(f,"ApplyOptions");
            Call(f,"SwitchGame",GameCatalog.Defaults(Era.Future2126,GameKind.Klondike,100));
            var returned=new{preference=f.Preferences.Rules.DrawCount,active=f.Game.Rules.DrawCount,f.Game.State.Seed};
            Call(f,"OpenDialog",DialogPage.Options);int displayedDraw=((Preferences)Field(f,"draft")!).Rules.DrawCount;using var image=Paint(f,"shared-options-mismatch");Activate(f,"dialog-ok");
            var applied=new{preference=f.Preferences.Rules.DrawCount,active=f.Game.Rules.DrawCount,sameDeal=f.Game.State.Seed==seed};
            Call(f,"OpenDialog",DialogPage.Options);((Preferences)Field(f,"draft")!).Rules.DrawCount=3;Activate(f,"dialog-ok");
            return new{returned,displayedDraw,applied,reselectActualRuleResetsDeal=f.Game.State.Seed!=seed,movesAfterReselect=f.Game.State.Moves};
        });
        Probe("undo-arrival-wave",()=>
        {
            using var f=Form();var s=new GameState{Seed=1};s.Tableau[0].Add(new(0));s.Stock=Enumerable.Range(1,51).Select(id=>new Card(id,false)).ToList();f.SetRenderState(s);Call(f,"StopCardMotion");Set(f,"renderMotionTime",10.0);
            Call(f,"Changed",f.Game.ToFoundation(new(PileKind.Tableau,0)));Set(f,"renderMotionTime",10.1);Call(f,"UndoMove");Set(f,"renderMotionTime",10.5);Call(f,"Animate");using var b=Paint(f,"wave-after-undo");
            return new{foundationCards=f.Game.State.Foundations.Sum(p=>p.Count),waves=((ICollection)Field(f,"futurePulses")!).Count};
        });
        Probe("complete-button-can-stall",()=>
        {
            using var f=Form();var s=new GameState{Seed=1,Started=true,WasteFan=3};
            int[][] columns=[[12,24,10,22],[51,37,49,35],[25,11,23,9],[38,50,36,48],[8,20,6,18],[47,33,45,31],[21,7,19,5]];
            s.Tableau=columns.Select(p=>p.Select(id=>new Card(id)).ToList()).ToList();s.Waste=Enumerable.Range(0,52).Where(id=>columns.All(p=>!p.Contains(id))).Select(id=>new Card(id)).ToList();
            f.SetRenderState(s);Call(f,"StopCardMotion");using var before=Paint(f,"complete-stalled");bool offered=Prop<bool>(f,"CanFinish");Call(f,"CollectCards");Call(f,"Animate");
            return new{valid52CardFixture=true,offered,f.Game.State.Moves,collecting=(bool)Field(f,"collecting")!,f.Game.State.Won};
        });
        Probe("hint-cycles",()=>
        {
            var cycles=new List<object>();int wins=0,noMove=0;
            for(int seed=1;seed<=40;seed++)
            {
                var game=new Game(GameCatalog.Defaults(Era.Future2126,GameKind.Klondike).Rules,seed);var seen=new Dictionary<string,int>();var actions=new List<string>();
                for(int step=0;step<300;step++)
                {
                    if(game.State.Won){wins++;break;}
                    string state=JsonSerializer.Serialize(new{game.State.Stock,game.State.Waste,game.State.Foundations,game.State.Tableau,game.State.WasteFan});
                    if(seen.TryGetValue(state,out int at)){cycles.Add(new{seed,at,step,cycle=actions.Skip(at).ToArray()});break;}seen[state]=step;
                    var hints=game.OrbitHints();if(hints.Count==0){noMove++;break;}var h=hints[0];actions.Add(h.Text);
                    bool ok=h.From.Kind==PileKind.Stock?game.Draw():h.From==h.To?game.Flip(h.From.Pile):game.Move(h.From,h.To);if(!ok)throw new Exception("Illegal hint");Game.Validate(game.State);
                }
            }
            return new{seeds=40,wins,noMove,cycleCount=cycles.Count,examples=cycles.Take(3).ToArray()};
        });
        Probe("accessibility-tree",()=>{using var f=Form();using var b=Paint(f);return new{nativeControls=f.Controls.Count,accessibleChildren=f.AccessibilityObject.GetChildCount(),f.AccessibleName,drawnHotspots=((ICollection)Field(f,"hotspots")!).Count};});
        Probe("palette-hit-target",()=>
        {using var f=Form();Call(f,"OpenDialog",DialogPage.Deck);using var b=Paint(f);var hits=((IEnumerable)Field(f,"hotspots")!).Cast<object>().Where(h=>((string)h.GetType().GetProperty("Id")!.GetValue(h)!).StartsWith("dialog-futurepalette")).Select(h=>h.GetType().GetProperty("Bounds")!.GetValue(h)).ToArray();return hits;});
        Probe("minimum-window-and-screen",()=>{using var f=Form();return new{logicalMinimum=new Size(800,540),at200Percent=new Size(1600,1080),currentScreen=Screen.FromControl(f).WorkingArea};});
        Probe("keyboard-stock-selection",()=>
        {using var f=Form();Set(f,"keyboardPile",0);Key(f,Keys.Enter);Key(f,Keys.Right);Key(f,Keys.Enter);bool chosen=Field(f,"selection")!=null;Key(f,Keys.Left);Key(f,Keys.Enter);return new{chosen,selectionSurvivesStockDraw=Field(f,"selection")!=null};});
        Probe("long-history-snapshot",()=>
        {
            using var f=Form();for(int i=0;i<5000;i++)f.Game.Draw();long bytes=GC.GetAllocatedBytesForCurrentThread();var watch=Stopwatch.StartNew();var saved=(SaveFile)Call(f,"SnapshotSave")!;watch.Stop();bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
            return new{history=f.Game.History.Count,uiThreadCloneMs=watch.Elapsed.TotalMilliseconds,cloneAllocatedBytes=bytes,jsonBytes=JsonSerializer.SerializeToUtf8Bytes(saved).Length};
        });
        File.WriteAllText(Path.Combine(Folder,"findings.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
        Environment.ExitCode=failures==0?0:1;
    }
    static void Performance()
    {
        var results=new List<object>();
        foreach(int scale in new[]{100,150,200})
        {
            using var f=new GameWindow(new Store(Path.Combine(Folder,"perf-state")),Era.Future2126,1989,scale,true);
            using var b=new Bitmap(f.Width,f.Height);using var g=Graphics.FromImage(b);
            double Frame(){var w=Stopwatch.StartNew();Call(f,"PaintScaled",g);w.Stop();return w.Elapsed.TotalMilliseconds;}
            double cold=Frame();var samples=new List<double>();
            f.Preferences.FuturePalette=1;double palette=Frame();
            Set(f,"renderMotionTime",10.0);Call(f,"DrawCards");
            for(int i=0;i<60;i++){Set(f,"renderMotionTime",10+i/180.0);Call(f,"Animate");samples.Add(Frame());}
            samples.Sort();double moveMedian=samples[30],moveP95=samples[57];
            var won=new GameState{Seed=1989,Started=true};for(int suit=0;suit<4;suit++)won.Foundations[suit]=Enumerable.Range(suit*13,13).Select(id=>new Card(id)).ToList();
            f.SetRenderState(won);Call(f,"StopCardMotion");Set(f,"renderMotionTime",20.0);Call(f,"StartVictory");samples.Clear();
            for(int i=0;i<120;i++){Set(f,"renderMotionTime",20+i/30.0);Call(f,"Animate");samples.Add(Frame());}samples.Sort();
            results.Add(new{scale,coldPaintMs=cold,palettePaintMs=palette,stockMoveMedianMs=moveMedian,stockMoveP95Ms=moveP95,winMedianMs=samples[60],winP95Ms=samples[114]});
        }
        File.WriteAllText(Path.Combine(Folder,"interaction-performance.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(results));
    }
}
