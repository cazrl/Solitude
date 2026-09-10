using System.Drawing;
using System.Text.Json;
using Solitude;

internal static partial class UiProgram
{
    private static void CheckFutureEdition()
    {
        Directory.CreateDirectory("artifacts/orbit-checks");
        Directory.CreateDirectory("artifacts/orbit-demo");
        Check(GameCatalog.Available(Era.Future2126,GameKind.Klondike) && !GameCatalog.Available(Era.Future2126,GameKind.FreeCell) && !GameCatalog.Available(Era.Future2126,GameKind.Spider),"ORBIT availability is inconsistent");
        var defaults=GameCatalog.Defaults(Era.Future2126,GameKind.Klondike);
        Check(defaults.Rules.AutoFlip && defaults.Rules.UndoLimit==0 && defaults.SaveOnExit && defaults.ContinueSavedGame,"ORBIT defaults omit a modern play/persistence contract");
        foreach(int scale in new[]{100,125,150,200})
        {
            using var form=new GameWindow(new Store("artifacts/orbit-check-state"),Era.Future2126,1989,scale,true);
            using var frame=new Bitmap(form.Width,form.Height);using var expected=new Bitmap(form.Width,form.Height);
            for(int palette=0;palette<3;palette++)
            {
                form.Preferences.FuturePalette=palette;Set(form,"renderMotionTime",10.0);Call(form,"BeginCardMotion",true);Set(form,"renderMotionTime",10.2);DrawFrame(form,frame);
                Set(form,"renderMotionTime",12.0);DrawFrame(form,frame);Call(form,"StopCardMotion");DrawFrame(form,expected);SameFrame(frame,expected,$"orbit-handoff-{scale}-{palette}");
                Check(frame.GetPixel(0,0).A==0 && frame.GetPixel(frame.Width-1,frame.Height-1).A==0,"ORBIT frame paints outside its rounded silhouette");
                Set(form,"renderMotionTime",20.0);Call(form,"BeginCardMotion",true);
                for(int i=0;i<=35;i++)
                {Set(form,"renderMotionTime",20+i/30.0);Call(form,"Animate");DrawFrame(form,frame);Set(form,"boardStamp",null);DrawFrame(form,expected);SameFrame(frame,expected,$"orbit-repeat-{scale}-{palette}-{i}");if(scale==100 && palette==0)frame.Save($"artifacts/orbit-demo/deal-{i:000}.png");}
                if(scale==100)frame.Save($"artifacts/orbit-checks/palette-{palette}.png");
            }
            Check(HasControl(form,"settings") && HasControl(form,"future-undo") && HasControl(form,"future-hint") && HasControl(form,"future-new"),"ORBIT primary controls are missing");
            foreach(string id in new[]{"settings","future-experience","future-undo","future-hint","future-new"})
            {var bounds=Hit(form,id);Check(bounds.Width>60 && bounds.Height>=30,"ORBIT target is too small: "+id);}
            var stock=(RectangleF)typeof(GameWindow).GetProperty("StockRect",Private)!.GetValue(form)!;DoubleClick(form,stock,false);
            Check(form.Game.State.Stock.Count==21 && form.Game.State.Moves==1,"ORBIT stock double-click drew twice");
            var s=new GameState{Kind=GameKind.Klondike,Seed=1,Tableau=Enumerable.Range(0,7).Select(_=>new List<Card>()).ToList(),Stock=Enumerable.Range(0,52).Where(i=>i!=0 && i!=13).Select(i=>new Card(i,false)).ToList()};s.Tableau[0].Add(new(0));s.Tableau[1].Add(new(13));
            form.SetRenderState(s);Paint(form);DoubleClick(form,Area(form,new(PileKind.Tableau,0,0)));
            Check(form.Game.State.Foundations.Sum(p=>p.Count)==1 && form.Game.State.Tableau[1].Single().Id==13,"ORBIT double-click moved another available ace");
            Key(form,Keys.Control|Keys.Z);Paint(form);Check(form.Game.State.Tableau[0].Single().Id==0,"ORBIT Undo lost its card");
            Call(form,"OpenDialog",DialogPage.Options);Key(form,Keys.Alt|Keys.M);Key(form,Keys.Enter);
            Check(!form.Preferences.Animate && (DialogPage)Field(form,"dialog")! ==DialogPage.None,"ORBIT reduced motion did not apply");
            Call(form,"BeginCardMotion",true);Check(((System.Collections.IDictionary)Field(form,"flights")!).Count==0,"ORBIT still flies cards with motion disabled");
            Check(!(bool)typeof(GameWindow).GetProperty("NeedsFrames",Private)!.GetValue(form)!,"ORBIT requests idle frames after reduced motion");
            Call(form,"OpenDialog",DialogPage.Deck);Paint(form);Click(form,Hit(form,"dialog-futurepalette1"));Paint(form);Click(form,Hit(form,"dialog-ok"));Paint(form);
            Check(form.Preferences.FuturePalette==1,"ORBIT palette control did not save its choice");
            // A nested appearance page must preserve uncommitted options and
            // Cancel must roll back the whole draft, including its palette.
            Call(form,"OpenDialog",DialogPage.Options);Key(form,Keys.Alt|Keys.M);Paint(form);Click(form,Hit(form,"dialog-appearance"));Paint(form);
            Click(form,Hit(form,"dialog-futurepalette2"));Key(form,Keys.Escape);Paint(form);
            Check((DialogPage)Field(form,"dialog")! ==DialogPage.Options && ((Preferences)Field(form,"draft")!).Animate,"Atmosphere Cancel lost the unfinished motion choice");
            Check(((Preferences)Field(form,"draft")!).FuturePalette==1,"Atmosphere Cancel changed its parent palette");
            Click(form,Hit(form,"dialog-appearance"));Paint(form);Click(form,Hit(form,"dialog-futurepalette2"));Key(form,Keys.Enter);Paint(form);
            Check((DialogPage)Field(form,"dialog")! ==DialogPage.Options && ((Preferences)Field(form,"draft")!).FuturePalette==2 && form.Preferences.FuturePalette==1,"Nested palette Apply committed prematurely");
            Key(form,Keys.Escape);Check(!form.Preferences.Animate && form.Preferences.FuturePalette==1,"Experience Cancel committed its draft");
            Call(form,"OpenDialog",DialogPage.Options);Paint(form);Click(form,Hit(form,"dialog-appearance"));Paint(form);Click(form,Hit(form,"dialog-futurepalette2"));Key(form,Keys.Enter);Key(form,Keys.Enter);
            Check(form.Preferences.FuturePalette==2 && !form.Preferences.Animate,"Experience Apply omitted its nested palette");
            // Restore the fixture's expected palette through the standalone path.
            Key(form,Keys.F7);Paint(form);Click(form,Hit(form,"dialog-futurepalette1"));Key(form,Keys.Enter);Paint(form);
            string state=JsonSerializer.Serialize(form.Game.State);Call(form,"SwitchGame",GameCatalog.Defaults(Era.WindowsXP,GameKind.Klondike,scale));
            Key(form,Keys.F6);Paint(form);Click(form,Hit(form,"dialog-era-8"));Paint(form);Click(form,Hit(form,"dialog-ok"));Paint(form);
            Check(form.Preferences.Era==Era.Future2126 && form.Preferences.FuturePalette==1 && !form.Preferences.Animate,"ORBIT choices were lost across editions");
            Check(JsonSerializer.Serialize(form.Game.State)==state,"ORBIT edition round trip replaced the deal");
            form.ClientSize=new(800*scale/100,540*scale/100);Paint(form);
            foreach(var a in new[]{"settings","future-experience","future-undo","future-hint","future-new"})
            {
                var r=Hit(form,a);Check(r.Left>=0 && r.Right<=800 && r.Top>=0 && r.Bottom<=540,"ORBIT control leaves its smallest window: "+a);
                foreach(var b in new[]{"settings","future-experience","future-undo","future-hint","future-new"}.Where(b=>b!=a))Check(!r.IntersectsWith(Hit(form,b)),"ORBIT controls overlap in its smallest window");
            }
            foreach(var page in new[]{DialogPage.Options,DialogPage.Deck,DialogPage.Settings,DialogPage.Help,DialogPage.About,DialogPage.Statistics,DialogPage.Confirm})
            {
                Call(form,"OpenDialog",page);Paint(form);var bounds=(RectangleF)Field(form,"dialogBounds")!;
                Check(bounds.Top>=0 && bounds.Bottom<=form.Height/(scale/100f),"ORBIT dialog does not fit at "+scale+": "+page);Key(form,Keys.Escape);
            }
        }
        CheckFuturePersistence();CheckFutureWin();CheckFutureMotionInterruption();
        Console.WriteLine("PASS ORBIT palettes, input, frame continuity, reduced motion, saved sessions and constellation victory");
    }
    private static void CheckFutureMotionInterruption()
    {
        using var form=new GameWindow(new Store("artifacts/orbit-interruption"),Era.Future2126,1,100,true);Paint(form);
        Set(form,"renderMotionTime",10.0);Call(form,"DrawCards");int key=form.Game.State.Waste[^1].Key;
        var flights=(System.Collections.IDictionary)Field(form,"flights")!;var flight=flights[key]!;
        float Bank(object f,double t)=>(float)f.GetType().GetMethod("Bank")!.Invoke(f,[t])!;
        var sample=flight.GetType().GetMethod("Sample")!;
        Set(form,"renderMotionTime",10.16);var pose=sample.Invoke(flight,[10.16])!;
        Key(form,Keys.Control|Keys.Z);var reverse=flights[key]!;
        Check(form.Game.State.Stock.Count==24,"ORBIT interrupted draw lost cards");
        Check(Bank(reverse,10.16)==Bank(flight,10.16),"ORBIT interrupted Undo snapped the card's bank angle");
        Check(pose.GetType().GetProperty("Rect")!.GetValue(pose)!.Equals(sample.Invoke(reverse,[10.16])!.GetType().GetProperty("Rect")!.GetValue(sample.Invoke(reverse,[10.16]))),"ORBIT interrupted Undo snapped the card's position/flip width");
        Check(Bank(reverse,12.0)==0,"ORBIT interrupted card did not settle flat");
    }
    private static void CheckFuturePersistence()
    {
        string directory=Path.GetFullPath("artifacts/orbit-persistence-"+Guid.NewGuid().ToString("N"));var store=new Store(directory);string before;
        using(var form=new GameWindow(store,Era.Future2126,42,100))
        {
            form.Preferences.FuturePalette=2;form.Preferences.Sound=false;form.Preferences.Animate=false;
            ((SharedSettings)Field(form,"shared")!).Capture(form.Preferences);form.Game.Draw();Call(form,"Changed",true);before=JsonSerializer.Serialize(form.Game.State);Call(form,"Save");
        }
        using(var restored=new GameWindow(new Store(directory)))
        {Check(restored.Preferences.Era==Era.Future2126 && restored.Preferences.FuturePalette==2 && !restored.Preferences.Sound && !restored.Preferences.Animate,"ORBIT disk settings did not reload");Check(JsonSerializer.Serialize(restored.Game.State)==before && restored.Game.CanUndo,"ORBIT disk restore lost state/history");}
        var shared=new SharedSettings();var future=GameCatalog.Defaults(Era.Future2126,GameKind.Klondike);future.Rules.DrawCount=1;future.Rules.Scoring=Scoring.Vegas;future.FuturePalette=2;future.Animate=false;shared.Capture(future);
        var xp=GameCatalog.Defaults(Era.WindowsXP,GameKind.Klondike);shared.Apply(xp);Check(xp.Rules.DrawCount==1 && xp.Rules.Scoring==Scoring.Vegas && xp.CardBack==6,"ORBIT did not share compatible rules cleanly");
        var vista=GameCatalog.Defaults(Era.WindowsVista,GameKind.Klondike);shared.Apply(vista);Check(vista.Animate && vista.VistaDeck==1,"ORBIT overwrote Vista-specific appearance/motion");
    }
    private static void CheckFutureWin()
    {
        using var form=new GameWindow(new Store("artifacts/orbit-win-state"),Era.Future2126,1,100,true);
        var state=new GameState{Kind=GameKind.Klondike,Seed=1,Started=true,Elapsed=185,Score=4254,TimeBonus=3780,Tableau=Enumerable.Range(0,7).Select(_=>new List<Card>()).ToList()};
        for(int i=0;i<4;i++)state.Foundations[i]=Enumerable.Range(i*13,13).Select(id=>new Card(id)).ToList();
        form.SetRenderState(state);Set(form,"renderMotionTime",10.0);Call(form,"StartVictory");
        Check((bool)Field(form,"showingVictory")!,"ORBIT omitted the constellation");
        using var frame=new Bitmap(form.Width,form.Height);Set(form,"renderMotionTime",12.8);Call(form,"Animate");DrawFrame(form,frame);frame.Save("artifacts/orbit-checks/constellation.png");
        for(int i=0;i<53;i++){Set(form,"renderMotionTime",10+i/10.0);DrawFrame(form,frame);frame.Save($"artifacts/orbit-demo/win-{i:000}.png");}
        Set(form,"renderMotionTime",15.3);Call(form,"Animate");DrawFrame(form,frame);frame.Save("artifacts/orbit-checks/results.png");Check((DialogPage)Field(form,"dialog")! ==DialogPage.Won,"ORBIT victory did not reach results");
        Key(form,Keys.Escape);form.Preferences.Animate=false;Call(form,"StartVictory");Check((DialogPage)Field(form,"dialog")! ==DialogPage.Won && !(bool)Field(form,"showingVictory")!,"Reduced-motion victory still animated");
    }
}
