using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text.Json;
using Solitude;

internal static partial class UiProgram
{
    private static GameWindow FixForm(Era era,GameKind kind=GameKind.Klondike,int scale=100)=>new(new Store("artifacts/program-fixes/ephemeral"),era,1989,scale,true,kind);
    private static T FixProperty<T>(GameWindow f,string name)=>(T)typeof(GameWindow).GetProperty(name,Private)!.GetValue(f)!;
    private static void FixControl(GameWindow f,string id)
    {
        Paint(f);
        float scale=FixProperty<float>(f,"ScaleFactor");var r=Hit(f,id);
        var e=new MouseEventArgs(MouseButtons.Left,1,(int)((r.X+r.Width/2)*scale),(int)((r.Y+r.Height/2)*scale),0);
        Call(f,"OnMouseDown",e);Call(f,"OnMouseUp",e);
    }
    private static void CheckProgramFixes()
    {
        Directory.CreateDirectory("artifacts/program-fixes");
        CheckSaveConflicts();CheckInstanceLease();CheckHistoricalActiveOptions();CheckSettledFoundations();CheckEveryWindowFits();CheckEveryAccessibleEdition();CheckVictoryInputPolicy();CheckHintCache();CheckOrbitCornerDamage();
        Console.WriteLine("PASS program audit fixes: save conflicts, instance lease, active rules, foundation underlays, monitor fitting, all-edition accessibility and victory input");
    }
    private static void CheckSaveConflicts()
    {
        string dir=Path.GetFullPath("artifacts/program-fixes/conflict-"+Guid.NewGuid().ToString("N"));
        var sa=new Store(dir);var sb=new Store(dir);
        using var a=new GameWindow(sa,Era.Future2126,1,100,false);
        using var b=new GameWindow(sb,Era.Future2126,1,100,false);
        a.Game.Draw();a.Game.Draw();a.Preferences.FuturePalette=2;
        var save=typeof(GameWindow).GetMethod("Save",Private)!;
        Check((bool)save.Invoke(a,null)!,"First writer could not save");
        string first=File.ReadAllText(sa.FilePath);b.Game.Draw();
        Check(!(bool)save.Invoke(b,null)! && sb.Warning!=null,"Stale second instance silently saved");
        Check(File.ReadAllText(sa.FilePath)==first,"Rejected stale save changed the newer data");
        Check(!(bool)save.Invoke(b,null)!,"Repeated stale save unexpectedly succeeded");
        var reloaded=new Store(dir).Load();Check(reloaded.Game!.Moves==2 && reloaded.Preferences.FuturePalette==2,"Conflict lost moves or settings");
        a.Game.Draw();Check((bool)save.Invoke(a,null)!,"Original writer could not continue after conflict");
        Check(new Store(dir).Load().Game!.Moves==3,"Original writer's next save disappeared");
        var loaded=sb.Load();Check(sb.Save(loaded),"Explicit reload did not reset the conflict baseline");
        Check(!Directory.GetFiles(dir,"*.unreadable-*").Any(),"A valid conflict was misclassified as corruption");
    }
    private static void CheckInstanceLease()
    {
        string dir=Path.GetFullPath("artifacts/program-fixes/lease-"+Guid.NewGuid().ToString("N"));
        int Child()
        {
            var info=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
            if(string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath),"dotnet",StringComparison.OrdinalIgnoreCase))info.ArgumentList.Add(typeof(UiProgram).Assembly.Location);
            info.ArgumentList.Add("--instance-probe");info.ArgumentList.Add(dir);
            using var child=Process.Start(info)!;
            if(!child.WaitForExit(15000)){child.Kill();throw new Exception("Isolated lease child did not exit");}
            return child.ExitCode;
        }
        using(var owner=SingleInstanceLease.TryAcquire(dir))
        {
            Check(owner!=null,"Initial instance did not acquire its lease");
            using var hidden=FixForm(Era.WindowsXP);owner!.SetWindow(hidden.Handle);
            Check(SingleInstanceLease.ActivateExisting(dir) && !hidden.Visible,"Existing-instance activation failed or displayed the hidden test window");
            using var duplicate=SingleInstanceLease.TryAcquire(Path.Combine(dir,"."));Check(duplicate==null,"Directory alias bypassed the lease");
            using var independent=SingleInstanceLease.TryAcquire(dir+"-other");Check(independent!=null,"Independent data directories were blocked");
            Check(Child()==11,"A separate process acquired an existing lease");
        }
        Check(Child()==10,"Lease was not released when its owner exited");
        using var again=SingleInstanceLease.TryAcquire(dir);Check(again!=null,"Closed child left a stale lease");
    }
    private static void CheckHistoricalActiveOptions()
    {
        foreach(var era in GameCatalog.HistoricalEras)
        {
            using var f=FixForm(era);Call(f,"DrawCards");
            Call(f,"SwitchGame",GameCatalog.Defaults(Era.Future2126,GameKind.Klondike,100));
            Call(f,"OpenDialog",DialogPage.Options);FixControl(f,"dialog-draw1");FixControl(f,"dialog-ok");
            Call(f,"SwitchGame",GameCatalog.Defaults(era,GameKind.Klondike,100));string before=JsonSerializer.Serialize(f.Game.State);
            Call(f,"OpenDialog",DialogPage.Options);
            Check(((Preferences)Field(f,"draft")!).Rules.DrawCount==3,"Options show shared instead of active rules: "+era);
            FixControl(f,"dialog-ok");Check(JsonSerializer.Serialize(f.Game.State)==before && f.Preferences.Rules.DrawCount==1,"Unchanged Options changed deal/defaults: "+era);
            Call(f,"OpenDialog",DialogPage.Options);FixControl(f,"dialog-draw1");FixControl(f,"dialog-ok");
            Check(f.Game.Rules.DrawCount==1,"Explicit draw-one selection was ignored: "+era);
            Call(f,"StopCardMotion");int stock=f.Game.State.Stock.Count;Call(f,"DrawCards");Check(stock-f.Game.State.Stock.Count==1,"Draw-one actually draws three: "+era);
            Call(f,"OpenDialog",DialogPage.Options);FixControl(f,"dialog-draw3");FixControl(f,"dialog-cancel");Check(f.Game.Rules.DrawCount==1,"Cancel changed active rules: "+era);
        }
        foreach(var era in new[]{Era.WindowsMe,Era.WindowsXP,Era.WindowsVista})
        {
            using var f=FixForm(era,GameKind.Spider);Call(f,"DrawCards");
            Call(f,"SwitchGame",GameCatalog.Defaults(era==Era.WindowsXP?Era.WindowsMe:Era.WindowsXP,GameKind.Spider,100));
            Call(f,"OpenDialog",DialogPage.Difficulty);FixControl(f,"dialog-suits-4");FixControl(f,"dialog-ok");
            Call(f,"SwitchGame",GameCatalog.Defaults(era,GameKind.Spider,100));string before=JsonSerializer.Serialize(f.Game.State);
            var page=era==Era.WindowsVista?DialogPage.Options:DialogPage.Difficulty;Call(f,"OpenDialog",page);
            Check(((Preferences)Field(f,"draft")!).Rules.SpiderSuits==1,"Difficulty shows shared instead of active suit count: "+era);
            FixControl(f,"dialog-ok");Check(JsonSerializer.Serialize(f.Game.State)==before && f.Preferences.Rules.SpiderSuits==4,"Unchanged difficulty replaced deal/defaults: "+era);
            Call(f,"OpenDialog",page);FixControl(f,"dialog-suits-4");FixControl(f,"dialog-ok");
            Check(f.Game.State.SpiderSuits==4 && f.Game.Rules.SpiderSuits==4,"Explicit difficulty was ignored: "+era);
        }
    }
    private static void CheckSettledFoundations()
    {
        foreach(var era in new[]{Era.WindowsVista,Era.Future2126})
        foreach(var kind in era==Era.WindowsVista?new[]{GameKind.Klondike,GameKind.FreeCell}:new[]{GameKind.Klondike})
        foreach(int scale in new[]{100,125,150,200})foreach(int rank in new[]{2,5})
        {
            using var f=FixForm(era,kind,scale);f.Preferences.Sound=false;
            var state=new GameState{Kind=kind,Seed=1989};
            if(kind==GameKind.FreeCell)state.Tableau=Enumerable.Range(0,8).Select(_=>new List<Card>()).ToList();
            state.Foundations[0]=Enumerable.Range(0,rank-1).Select(id=>new Card(id)).ToList();state.Tableau[0]=[new(rank-1)];
            var rest=Enumerable.Range(rank,52-rank).Select(id=>new Card(id,kind==GameKind.FreeCell)).ToList();
            if(kind==GameKind.FreeCell)state.Tableau[1]=rest;else state.Stock=rest;
            f.SetRenderState(state);Call(f,"StopCardMotion");
            using var before=new Bitmap(f.Width,f.Height);DrawFrame(f,before);
            var r=kind==GameKind.FreeCell?(RectangleF)typeof(GameWindow).GetMethod("CellRect",Private)!.Invoke(f,[0,true])!:(RectangleF)typeof(GameWindow).GetMethod("TopCard",Private)!.Invoke(f,[3])!;
            int x=(int)((r.X+r.Width*.82f)*scale/100),y=(int)((r.Y+r.Height*.25f)*scale/100);
            Set(f,"renderMotionTime",100.0);Check(f.Game.Move(new(PileKind.Tableau,0),new(PileKind.Foundation,0),false),"Fixture move failed");Call(f,"Changed",true);Set(f,"renderMotionTime",100.02);
            using var during=new Bitmap(f.Width,f.Height);DrawFrame(f,during);
            Check(before.GetPixel(x,y)==during.GetPixel(x,y),$"Settled foundation disappeared: {era} {kind} {scale} rank {rank}");
            Key(f,Keys.Control|Keys.Z);DrawFrame(f,during);Game.Validate(f.Game.State);
            Check(f.Game.State.Foundations[0].Count==rank-1 && f.Game.State.Tableau[0][^1].Rank==rank,"Undo during foundation flight lost cards");
            Check(before.GetPixel(x,y)==during.GetPixel(x,y),"Undo flight hid settled foundation");
        }
    }
    private static void CheckEveryWindowFits()
    {
        foreach(var era in Enum.GetValues<Era>())foreach(var kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))
        foreach(int scale in new[]{100,125,150,200})
        {
            using var f=FixForm(era,kind,scale);
            foreach(var work in new[]{new Rectangle(0,0,1366,728),new Rectangle(-1920,40,1920,1040),new Rectangle(0,0,800,540)})
            {
                Call(f,"CenterGameWindow",work);Paint(f);
                Check(work.Contains(f.Bounds),$"Window leaves work area: {era} {kind} {scale} {work}");
                Check(Math.Abs(f.Left+f.Width/2.0-(work.Left+work.Width/2.0))<=.5 && Math.Abs(f.Top+f.Height/2.0-(work.Top+work.Height/2.0))<=.5,"Fitted window is not centered");
                Check(f.Preferences.Scale==scale,"Fitting discarded preferred scale");
                Key(f,Keys.F6);Paint(f);FixControl(f,"dialog-cancel");
                Check((DialogPage)Field(f,"dialog")! ==DialogPage.None,"Fitted Settings cannot be clicked");
            }
            Call(f,"CenterGameWindow",new Rectangle(0,0,3840,2160));Check(Math.Abs(FixProperty<float>(f,"ScaleFactor")-scale/100f)<.001,"Preferred scale did not recover on a larger screen");
        }
    }
    private static void CheckEveryAccessibleEdition()
    {
        foreach(var era in Enum.GetValues<Era>())foreach(var kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))
        {
            using var f=FixForm(era,kind);Paint(f);var root=f.AccessibilityObject;
            List<AccessibleObject> Items()=>Enumerable.Range(0,root.GetChildCount()).Select(i=>root.GetChild(i)!).ToList();
            Check(root.GetChildCount()>15,"Edition has no accessible board: "+era+" "+kind);
            var top=f.Game.State.Tableau[0][^1];var card=Items().First(i=>i.Name!.StartsWith(top.Name+", column 1"));
            card.DoDefaultAction();Paint(f);Check(((Position?)Field(f,"selection"))?.Pile==0,"Accessible card action selected wrong pile");
            Check(!card.Bounds.IsEmpty && card.Name!.Contains(top.Name),"Accessible card disappeared after selection");
            var visibleNames=f.Game.State.Tableau.SelectMany((pile,column)=>pile.Where(c=>c.FaceUp).Select(c=>$"{c.Name}, column {column+1}")).Order().ToArray();
            var announced=Items().Where(i=>i.Role==AccessibleRole.ListItem).Select(i=>string.Join(',',i.Name!.Split(',').Take(2))).Order().ToArray();
            Check(announced.SequenceEqual(visibleNames),"Accessible cards do not match the visible cards: "+era+" "+kind);
            if(kind==GameKind.FreeCell)
            {
                var cell=Items().Single(i=>i.Name=="Free cell 1, empty.");cell.DoDefaultAction();Paint(f);
                Check(f.Game.State.FreeCells[0].Count==1,"Accessible FreeCell destination did not move a card");
            }
            Key(f,Keys.F6);Paint(f);Check(Items().Any(i=>i.Name=="Windows XP"),"Edition selector lacks accessible names");
            Key(f,Keys.Shift|Keys.Tab);Paint(f);Check(root.GetFocused()?.Name=="Cancel","Initial reverse Tab does not focus last dialog control");
            Items().Single(i=>i.Name=="Cancel").DoDefaultAction();Paint(f);Check((DialogPage)Field(f,"dialog")! ==DialogPage.None,"Accessible Cancel failed");
            Call(f,"OpenHelp",1);Paint(f);Check(Items().Any(i=>i.Role==AccessibleRole.StaticText && i.Name!.Length>100),"Help text is absent from accessibility tree: "+era+" "+kind);
            Key(f,Keys.Escape);Paint(f);
            if(kind==GameKind.FreeCell)
            {
                Key(f,Keys.F3);Paint(f);var number=Items().Single(i=>i.Role==AccessibleRole.Text && i.Name=="Game number");number.Value="42";Check(number.Value=="42","Accessible game-number field cannot be edited");
                Items().Single(i=>i.Name=="OK").DoDefaultAction();Paint(f);Check(f.Game.State.Seed==42,"Accessible numbered deal was not applied");
            }
        }
    }
    private static void CheckVictoryInputPolicy()
    {
        using var f=FixForm(Era.Future2126);f.Preferences.Sound=false;f.SetRenderState(CardVictoryState());Call(f,"StopCardMotion");Set(f,"renderMotionTime",100.0);Call(f,"StartVictory");Paint(f,false);
        foreach(var key in new[]{Keys.F5,Keys.F6,Keys.F2,Keys.Control|Keys.Z})
        {Key(f,key);Check((DialogPage)Field(f,"dialog")! ==DialogPage.None && (bool)Field(f,"showingVictory")!,"Shortcut bypassed the victory input state");}
        f.Preferences.Animate=false;Set(f,"renderMotionTime",100.1);Call(f,"Animate");
        Check(!(bool)Field(f,"showingVictory")! && !FixProperty<bool>(f,"NeedsFrames") && (DialogPage)Field(f,"dialog")! ==DialogPage.Won,"Reduced motion did not finish active victory");
        Check(f.Game.State.Won && f.Game.State.Foundations.Sum(p=>p.Count)==52,"Motion preference changed the won game");
        Call(f,"CloseDialog");f.Preferences.Animate=true;Set(f,"renderMotionTime",200.0);Call(f,"StartVictory");
        Set(f,"renderMotionTime",206.4);Paint(f,false);Check(!FixProperty<bool>(f,"NeedsFrames") && (bool)Field(f,"showingVictory")!,"Final still card display keeps the frame pump running");
        Set(f,"renderMotionTime",207.61);Call(f,"CheckFutureVictoryEnd");Check((DialogPage)Field(f,"dialog")! ==DialogPage.Won,"Timer cannot end the still victory hold");
    }
    private static void CheckHintCache()
    {
        var game=new Game(GameCatalog.Defaults(Era.Future2126,GameKind.Klondike).Rules,1989);
        for(int i=0;i<200;i++)game.Draw();
        for(int i=0;i<12;i++)
        {
            var cached=game.OrbitHints();var fresh=Game.Restore(game.Rules.Clone(),game.State,game.History);
            Check(cached.SequenceEqual(fresh.OrbitHints()),"Cached history changed hint choices after draw/undo");
            Check(game.History.Count==fresh.History.Count,"Hint caching trimmed undo history");
            if(i%3==0)game.Undo();else game.Draw();
        }
    }
    private static void CheckOrbitCornerDamage()
    {
        using var f=FixForm(Era.Future2126);f.Preferences.Animate=true;
        Set(f,"windowActive",true);Set(f,"mouse",new PointF(16,16));Set(f,"renderMotionTime",100.0);Paint(f);
        Rectangle Damage()=>(Rectangle)typeof(GameWindow).GetMethod("FrameDamage",Private)!.Invoke(f,null)!;
        Check(Damage().Width<f.Width/4,"Logo requests a full-width idle repaint");
        Call(f,"BeginCardMotion",true);Check(Damage()==f.ClientRectangle,"Dealing did not repaint the table");
        Set(f,"renderMotionTime",104.0);Call(f,"Animate");Check(Damage()==f.ClientRectangle,"Final flight was not given a settling repaint");
        Paint(f,false);Check(Damage().Width<f.Width/4,"Logo did not resume corner-only repaint after settling");
    }
}
