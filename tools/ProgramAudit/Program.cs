using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Solitude;

internal static class Program
{
    const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static string Output=Path.GetFullPath("artifacts/program-audit");
    static object? Field(GameWindow f,string n)=>typeof(GameWindow).GetField(n,Flags)!.GetValue(f);
    static void Set(GameWindow f,string n,object? v)=>typeof(GameWindow).GetField(n,Flags)!.SetValue(f,v);
    static object? Call(GameWindow f,string n,params object?[] args)=>typeof(GameWindow).GetMethod(n,Flags)!.Invoke(f,args);
    static T Prop<T>(GameWindow f,string n)=>(T)typeof(GameWindow).GetProperty(n,Flags)!.GetValue(f)!;
    static T Value<T>(object o,string n)=>(T)o.GetType().GetProperty(n)!.GetValue(o)!;
    static GameWindow Form(Era era=Era.Future2126,GameKind kind=GameKind.Klondike,int scale=100)=>new(new Store(Output+"/ephemeral"),era,1989,scale,true,kind);
    static Bitmap Paint(GameWindow f,string? file=null)
    {
        var b=new Bitmap(f.Width,f.Height);using(var g=Graphics.FromImage(b))Call(f,"PaintScaled",g);
        if(file!=null)b.Save(Path.Combine(Output,file+".png"));return b;
    }
    static void Key(GameWindow f,Keys key){object[] args=[new Message(),key];typeof(GameWindow).GetMethod("ProcessCmdKey",Flags)!.Invoke(f,args);}
    static void Activate(GameWindow f,string id)
    {
        using var b=Paint(f);var h=((IEnumerable)Field(f,"hotspots")!).Cast<object>().Single(h=>Value<string>(h,"Id")==id);Value<Action>(h,"Action")();
    }
    static GameState WonState()
    {
        var state=new GameState{Seed=1989,Started=true};
        for(int f=0;f<4;f++)state.Foundations[f]=Enumerable.Range(f*13,13).Select(id=>new Card(id)).ToList();return state;
    }
    [STAThread]static int Main(string[] args)
    {
        var outputOption=args.FirstOrDefault(a=>a.StartsWith("--output=",StringComparison.Ordinal));
        if(outputOption!=null){Output=Path.GetFullPath(outputOption[9..]);args=args.Where(a=>a!=outputOption).ToArray();}
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Directory.CreateDirectory(Output);
        var results=new Dictionary<string,object>();int errors=0;
        void Probe(string name,Func<object> run)
        {
            if(args.Length>0 && !args.Contains(name,StringComparer.Ordinal))return;
            try{results[name]=run();}catch(Exception e){errors++;results[name]=new{error=e.ToString()};}
            Console.WriteLine(name+": "+JsonSerializer.Serialize(results[name]));
        }
        Probe("historical-active-rules",()=>
        {
            var rows=new List<object>();
            foreach(Era era in GameCatalog.HistoricalEras)
            {
                using var f=Form(era);Call(f,"DrawCards");int originalSeed=f.Game.State.Seed;
                Call(f,"SwitchGame",GameCatalog.Defaults(Era.Future2126,GameKind.Klondike,100));
                Call(f,"OpenDialog",DialogPage.Options);((Preferences)Field(f,"draft")!).Rules.DrawCount=1;Call(f,"ApplyOptions");
                Call(f,"SwitchGame",GameCatalog.Defaults(era,GameKind.Klondike,100));Call(f,"OpenDialog",DialogPage.Options);
                int active=f.Game.Rules.DrawCount,displayed=((Preferences)Field(f,"draft")!).Rules.DrawCount;
                if(era is Era.WindowsXP or Era.WindowsVista){using var b=Paint(f,"rules-"+era);}
                Activate(f,"dialog-draw1");Activate(f,"dialog-ok");Call(f,"StopCardMotion");int before=f.Game.State.Stock.Count;Call(f,"DrawCards");
                rows.Add(new{era=era.ToString(),active,displayed,afterChoosingOne=f.Game.Rules.DrawCount,cardsDrawn=before-f.Game.State.Stock.Count,retainedDeal=originalSeed==f.Game.State.Seed});
            }
            return rows;
        });
        Probe("spider-active-difficulty",()=>
        {
            var rows=new List<object>();
            foreach(var era in new[]{Era.WindowsMe,Era.WindowsXP,Era.WindowsVista})
            {
                using var f=Form(era,GameKind.Spider);Call(f,"DrawCards");
                var other=era==Era.WindowsXP?Era.WindowsMe:Era.WindowsXP;
                Call(f,"SwitchGame",GameCatalog.Defaults(other,GameKind.Spider,100));Call(f,"OpenDialog",DialogPage.Difficulty);
                ((Preferences)Field(f,"draft")!).Rules.SpiderSuits=4;Call(f,"ApplyOptions");Call(f,"SwitchGame",GameCatalog.Defaults(era,GameKind.Spider,100));
                Call(f,"OpenDialog",era==Era.WindowsVista?DialogPage.Options:DialogPage.Difficulty);
                int displayed=((Preferences)Field(f,"draft")!).Rules.SpiderSuits;Activate(f,"dialog-suits-4");Activate(f,"dialog-ok");
                rows.Add(new{era=era.ToString(),displayed,afterChoosingFour=f.Game.State.SpiderSuits});
            }
            return rows;
        });
        Probe("foundation-underlay",()=>
        {
            var rows=new List<object>();
            foreach(var era in new[]{Era.WindowsVista,Era.Future2126})foreach(var kind in era==Era.WindowsVista?new[]{GameKind.Klondike,GameKind.FreeCell}:new[]{GameKind.Klondike})foreach(int scale in new[]{100,125,150,200})
            {
                using var f=Form(era,kind,scale);f.Preferences.Sound=false;var state=new GameState{Kind=kind,Seed=1989};
                if(kind==GameKind.FreeCell)state.Tableau=Enumerable.Range(0,8).Select(_=>new List<Card>()).ToList();
                state.Foundations[0]=[new(0)];state.Tableau[0]=[new(1)];
                var others=Enumerable.Range(2,50).Select(id=>new Card(id,kind==GameKind.FreeCell)).ToList();
                if(kind==GameKind.FreeCell)state.Tableau[1]=others;else state.Stock=others;
                f.SetRenderState(state);Call(f,"StopCardMotion");using var before=Paint(f);
                var r=kind==GameKind.FreeCell?(RectangleF)Call(f,"CellRect",0,true)!:(RectangleF)Call(f,"TopCard",3)!;
                int x=(int)((r.X+r.Width*.82f)*scale/100),y=(int)((r.Y+r.Height*.25f)*scale/100);
                Set(f,"renderMotionTime",100.0);bool moved=f.Game.Move(new(PileKind.Tableau,0),new(PileKind.Foundation,0),false);Call(f,"Changed",moved);Set(f,"renderMotionTime",100.02);
                using var moving=Paint(f,$"underlay-{era}-{kind}-{scale}");
                rows.Add(new{era=era.ToString(),kind=kind.ToString(),scale,moved,before=before.GetPixel(x,y).ToArgb(),during=moving.GetPixel(x,y).ToArgb(),underlayPreserved=before.GetPixel(x,y)==moving.GetPixel(x,y)});
            }
            return rows;
        });
        Probe("victory-shortcuts",()=>
        {
            using var f=Form();f.SetRenderState(WonState());Call(f,"StopCardMotion");Set(f,"renderMotionTime",100.0);Call(f,"StartVictory");
            using(var b=Paint(f)){};Key(f,Keys.F5);bool optionsOpened=(DialogPage)Field(f,"dialog")! ==DialogPage.Options;
            if(optionsOpened){Activate(f,"dialog-animations");Activate(f,"dialog-ok");}
            Set(f,"renderMotionTime",100.2);Call(f,"Animate");using var frame=Paint(f,"motion-disabled-during-victory");
            return new{optionsOpened,motionPreference=f.Preferences.Animate,showingVictory=(bool)Field(f,"showingVictory")!,needsFrames=Prop<bool>(f,"NeedsFrames"),dialog=Field(f,"dialog")!.ToString()};
        });
        Probe("small-screen-fitting",()=>
        {
            var rows=new List<object>();
            foreach(Era era in Enum.GetValues<Era>())foreach(GameKind kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))foreach(int scale in new[]{150,200})
            {
                using var f=Form(era,kind,scale);var area=new Rectangle(0,0,1366,728);Call(f,"CenterGameWindow",area);
                rows.Add(new{era=era.ToString(),kind=kind.ToString(),scale,bounds=f.Bounds.ToString(),fits=area.Contains(f.Bounds)});
            }
            return rows;
        });
        Probe("two-open-instances",()=>
        {
            string dir=Output+"/two-instances-"+Guid.NewGuid().ToString("N");
            var storeA=new Store(dir);var storeB=new Store(dir);
            using var a=new GameWindow(storeA,Era.Future2126,1,100,false);
            using var b=new GameWindow(storeB,Era.Future2126,1,100,false);a.Preferences.Sound=b.Preferences.Sound=false;
            a.Game.Draw();a.Game.Draw();a.Preferences.FuturePalette=2;
            bool saveA=(bool)Call(a,"Save")!;int first=new Store(dir).Load().Game!.Moves;
            b.Game.Draw();bool saveB=(bool)Call(b,"Save")!;var final=new Store(dir).Load();
            return new{saveA,saveB,firstMoves=first,finalMoves=final.Game!.Moves,firstPalette=2,finalPalette=final.Preferences.FuturePalette,warningA=storeA.Warning,warningB=storeB.Warning};
        });
        Probe("accessibility-and-keyboard",()=>
        {
            var rows=new List<object>();
            foreach(Era era in Enum.GetValues<Era>())foreach(GameKind kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))
            {
                using var f=Form(era,kind);using var b=Paint(f);int boardChildren=f.AccessibilityObject.GetChildCount();
                Key(f,Keys.F6);using var dialog=Paint(f);
                rows.Add(new{era=era.ToString(),kind=kind.ToString(),boardChildren,settingsChildren=f.AccessibilityObject.GetChildCount()});
            }
            return rows;
        });
        Probe("resource-soak",()=>
        {
            using var process=Process.GetCurrentProcess();var samples=new List<object>();
            for(int pass=0;pass<8;pass++)
            {
                using(var f=Form())
                {
                    _=f.Handle;
                    foreach(var era in Enum.GetValues<Era>())
                    {
                        Call(f,"SwitchGame",GameCatalog.Defaults(era,GameKind.Klondike,100));using var b=Paint(f);
                        foreach(var page in new[]{DialogPage.Settings,DialogPage.Options,DialogPage.Deck}){Call(f,"OpenDialog",page);using var d=Paint(f);Call(f,"CloseDialog");}
                    }
                }
                GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();process.Refresh();
                samples.Add(new{pass,gdi=GetGuiResources(process.Handle,0),user=GetGuiResources(process.Handle,1),handles=process.HandleCount,privateBytes=process.PrivateMemorySize64,managedBytes=GC.GetTotalMemory(false)});
            }
            return samples;
        });
        Probe("long-session-cost",()=>
        {
            var rows=new List<object>();
            using var f=Form();f.Preferences.Sound=false;
            string dir=Output+"/long-session-"+Guid.NewGuid().ToString("N");var store=new Store(dir);
            foreach(int moves in new[]{100,1000,5000})
            {
                while(f.Game.State.Moves<moves)if(!f.Game.Draw())throw new Exception("Expected an unlimited standard stock recycle.");
                _=f.Game.OrbitHints();var costs=new List<double>();
                for(int i=0;i<9;i++){var watch=Stopwatch.StartNew();_=f.Game.OrbitHints();costs.Add(watch.Elapsed.TotalMilliseconds);}
                costs.Sort();var snapshotWatch=Stopwatch.StartNew();var snapshot=(SaveFile)Call(f,"SnapshotSave")!;double snapshotMs=snapshotWatch.Elapsed.TotalMilliseconds;
                var saveWatch=Stopwatch.StartNew();bool saved=store.Save(snapshot);double saveMs=saveWatch.Elapsed.TotalMilliseconds;
                var loadWatch=Stopwatch.StartNew();var loaded=store.Load();double loadMs=loadWatch.Elapsed.TotalMilliseconds;
                rows.Add(new{moves,history=f.Game.History.Count,hintMedianMs=costs[4],hintMaxMs=costs[^1],snapshotMs,saveMs,loadMs,saved,bytes=new FileInfo(store.FilePath).Length,loadedMoves=loaded.Game?.Moves,warning=store.Warning});
            }
            return rows;
        });
        Probe("hint-cold-vs-warm",()=>
        {
            using var form=Form();var game=form.Game;var rows=new List<object>();
            object cache=typeof(Game).GetField("orbitHistoryKeys",Flags)!.GetValue(game)!;
            foreach(int moves in new[]{100,1000,5000})
            {
                while(game.State.Moves<moves)game.Draw();
                var cold=new List<double>();var warm=new List<double>();
                for(int i=0;i<9;i++)
                {
                    cache.GetType().GetMethod("Clear")!.Invoke(cache,null);
                    var timer=Stopwatch.StartNew();var a=game.OrbitHints();cold.Add(timer.Elapsed.TotalMilliseconds);
                    timer.Restart();var b=game.OrbitHints();warm.Add(timer.Elapsed.TotalMilliseconds);
                    if(!a.SequenceEqual(b))throw new Exception("Hint cache changed choices");
                }
                cold.Sort();warm.Sort();rows.Add(new{moves,history=game.History.Count,coldMedianMs=cold[4],warmMedianMs=warm[4]});
            }
            return rows;
        });
        string resultFile=args.Length==0?"results.json":"selected-results.json";
        File.WriteAllText(Path.Combine(Output,resultFile),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
        return errors==0?0:1;
    }
    [DllImport("user32.dll")]static extern uint GetGuiResources(nint process,uint flags);
}
