using System.Drawing;
using System.Reflection;
using System.Text.Json;
using Solitude;

internal static partial class UiProgram
{
    private static void CheckOrbitAuditFixes()
    {
        Directory.CreateDirectory("artifacts/orbit-fixes");
        using var form=new GameWindow(new Store("artifacts/orbit-fix-state"),Era.Future2126,1989,100,true);
        T Property<T>(string name)=>(T)typeof(GameWindow).GetProperty(name,Private)!.GetValue(form)!;
        // A long valid run must retain readable strips, remain navigable and
        // stay fully visible without changing cards or rules.
        var state=new GameState{Seed=1};
        var run=Enumerable.Range(1,13).Reverse().Select(rank=>new Card((rank%2==0?26:0)+rank-1)).ToList();
        var rest=Enumerable.Range(0,52).Where(id=>run.All(c=>c.Id!=id)).ToList();
        state.Tableau[0]=rest.Take(6).Select(id=>new Card(id,false)).Concat(run).ToList();state.Stock=rest.Skip(6).Select(id=>new Card(id,false)).ToList();
        form.SetRenderState(state);Call(form,"StopCardMotion");form.ClientSize=new(800,540);Paint(form);
        float step=(float)typeof(GameWindow).GetMethod("StackStep",Private)!.Invoke(form,[0])!;
        Check(step>=Property<float>("CardWidth")*.27f-.01,"Long-column rank/suit strip is too short");
        string before=JsonSerializer.Serialize(form.Game.State);
        var root=form.AccessibilityObject;
        AccessibleObject Find(string text)=>Enumerable.Range(0,root.GetChildCount()).Select(i=>root.GetChild(i)!).First(c=>c.Name!.Contains(text,StringComparison.OrdinalIgnoreCase));
        Check(root.GetChildCount()>25,"ORBIT does not expose cards and controls");
        var ace=Find("Ace of Clubs, column 1");Check(!ace.State.HasFlag(AccessibleStates.Offscreen),"Full fitted run has an offscreen card");
        ace.DoDefaultAction();Paint(form);
        Check(!ace.Bounds.IsEmpty && !ace.State.HasFlag(AccessibleStates.Offscreen),"Accessible activation did not expose the fitted card");
        Check(((Position?)Field(form,"selection"))?.Index==18,"Accessible activation selected the wrong card");
        Check(JsonSerializer.Serialize(form.Game.State)==before,"Scrolling changed the deal");
        using(var image=new Bitmap(form.Width,form.Height)){DrawFrame(form,image);image.Save("artifacts/orbit-fixes/long-column-bottom.png");}
        Check(!HasControl(form,"future-scroll-up-0"),"Fitted column still has scroll buttons");Paint(form);
        var aceBounds=ace.Bounds;
        Call(form,"OnMouseWheel",new MouseEventArgs(MouseButtons.None,0,100,350,-120));Paint(form);
        Check(ace.Bounds==aceBounds && JsonSerializer.Serialize(form.Game.State)==before,"Wheel input scrolled a fitted column or changed the deal");
        using(var image=new Bitmap(form.Width,form.Height)){DrawFrame(form,image);image.Save("artifacts/orbit-fixes/long-column-top.png");}

        // Cross-preset preferences describe the suspended game's actual rules;
        // unchanged Apply must retain the shared next-deal rules and current deal.
        Call(form,"SwitchGame",GameCatalog.Defaults(Era.WindowsXP,GameKind.Klondike,100));
        Call(form,"OpenDialog",DialogPage.Options);((Preferences)Field(form,"draft")!).Rules.DrawCount=1;Call(form,"ApplyOptions");
        Call(form,"SwitchGame",GameCatalog.Defaults(Era.Future2126,GameKind.Klondike,100));
        before=JsonSerializer.Serialize(form.Game.State);Call(form,"OpenDialog",DialogPage.Options);Paint(form);
        Check(((Preferences)Field(form,"draft")!).Rules.DrawCount==form.Game.Rules.DrawCount && form.Game.Rules.DrawCount==3,"Experience does not describe the active rules");
        Check(HasControl(form,"dialog-shared-rules"),"Shared next-deal rules are not discoverable");
        Click(form,Hit(form,"dialog-ok"));Check(form.Preferences.Rules.DrawCount==1 && JsonSerializer.Serialize(form.Game.State)==before,"Unchanged Apply replaced the deal or shared next-deal rules");
        Call(form,"OpenDialog",DialogPage.Options);Paint(form);Click(form,Hit(form,"dialog-shared-rules"));Click(form,Hit(form,"dialog-ok"));
        Check(form.Game.Rules.DrawCount==1 && form.Game.State.Moves==0,"Explicitly adopting shared rules did not start the requested deal");

        var stalled=new GameState{Seed=1,Started=true,WasteFan=3};
        int[][] columns=[[12,24,10,22],[51,37,49,35],[25,11,23,9],[38,50,36,48],[8,20,6,18],[47,33,45,31],[21,7,19,5]];
        stalled.Tableau=columns.Select(p=>p.Select(id=>new Card(id)).ToList()).ToList();stalled.Waste=Enumerable.Range(0,52).Where(id=>columns.All(p=>!p.Contains(id))).Select(id=>new Card(id)).ToList();
        form.SetRenderState(stalled);Call(form,"StopCardMotion");Paint(form);Check(!Property<bool>("CanFinish") && !HasControl(form,"future-finish"),"Completion still promises a no-op");
        var ready=new GameState{Seed=1,Started=true};for(int s=0;s<4;s++){ready.Foundations[s]=Enumerable.Range(s*13,12).Select(id=>new Card(id)).ToList();ready.Tableau[s].Add(new(s*13+12));}
        form.SetRenderState(ready);Paint(form);Check(Property<bool>("CanFinish"),"A collectable win no longer offers completion");
        before=JsonSerializer.Serialize(form.Game.State);Check(form.Game.CanCollectToWin() && JsonSerializer.Serialize(form.Game.State)==before,"Completion simulation mutated the live state");
        var aceState=new GameState{Seed=1};aceState.Tableau[0].Add(new(0));aceState.Stock=Enumerable.Range(1,51).Select(id=>new Card(id,false)).ToList();
        form.SetRenderState(aceState);Call(form,"StopCardMotion");Set(form,"renderMotionTime",10.0);Call(form,"Changed",form.Game.ToFoundation(new(PileKind.Tableau,0)));Set(form,"renderMotionTime",10.1);Call(form,"UndoMove");
        Check(((System.Collections.ICollection)Field(form,"futurePulses")!).Count==0 && form.Game.State.Foundations.Sum(p=>p.Count)==0,"Undo left an obsolete docking wave");

        // Frozen snapshots remain independent after Undo/ticking/new moves, including Spider.
        for(int i=0;i<1000;i++)form.Game.Draw();
        var snapshot=(SaveFile)typeof(GameWindow).GetMethod("SnapshotSave",Private)!.Invoke(form,null)!;
        string frozen=JsonSerializer.Serialize(snapshot);form.Game.Undo();form.Game.Tick();form.Game.Draw();
        Check(JsonSerializer.Serialize(snapshot)==frozen && snapshot.History.Count==1000,"Save snapshot was mutated by later play");
        var restored=Game.Restore(snapshot.ActiveRules!,snapshot.Game!,snapshot.History);Check(restored.History.Count==1000 && restored.Undo(),"Full-session Undo was lost from the lightweight snapshot");
        var spider=new Game(GameCatalog.Defaults(Era.WindowsVista,GameKind.Spider).Rules,1989);spider.Draw();var spiderHistory=spider.History.ToList();string spiderFrozen=JsonSerializer.Serialize(spiderHistory);spider.Undo();spider.Tick();Check(JsonSerializer.Serialize(spiderHistory)==spiderFrozen,"Spider Undo mutated a history snapshot in use by a save");

        Call(form,"OpenDialog",DialogPage.Deck);Paint(form);
        var tile=Hit(form,"dialog-futurepalette2");Check(tile.Height==96,"Only a small row of the palette tile is clickable");
        Click(form,new(tile.X+5,tile.Y+5,10,10));Check(((Preferences)Field(form,"draft")!).FuturePalette==2,"Palette preview click did not select its palette");
        Paint(form);Check(Find("Nebula").Role==AccessibleRole.RadioButton && Find("Nebula").State.HasFlag(AccessibleStates.Checked),"Palette accessible selection is incorrect");
        Key(form,Keys.Escape);Call(form,"OpenDialog",DialogPage.Options);Paint(form);int volume=((Preferences)Field(form,"draft")!).FutureVolume;
        Click(form,Hit(form,"dialog-volume-down"));Check(((Preferences)Field(form,"draft")!).FutureVolume==volume-10,"Volume decrease failed");
        Key(form,Keys.Escape);Check(form.Preferences.FutureVolume==volume,"Cancelled volume draft was saved");
        Call(form,"OpenDialog",DialogPage.Options);Paint(form);Click(form,Hit(form,"dialog-volume-down"));Click(form,Hit(form,"dialog-ok"));Check(form.Preferences.FutureVolume==volume-10,"Applied volume was lost");
        var shared=new SharedSettings();shared.Capture(form.Preferences);var defaults=GameCatalog.Defaults(Era.Future2126,GameKind.Klondike);shared.Apply(defaults);Check(defaults.FutureVolume==volume-10,"Volume does not persist across presets");
        Paint(form);Click(form,Hit(form,"future-menu"));Paint(form);Check(Enumerable.Range(0,root.GetChildCount()).Any(i=>root.GetChild(i)!.Name!.Contains("manual")),"Visible Game menu omits Help");Key(form,Keys.Escape);
        var fit=typeof(GameWindow).GetMethod("FitOrbitScale",BindingFlags.Static|BindingFlags.NonPublic)!;
        float scale=(float)fit.Invoke(null,[200,new Size(1366,728)])!;Check(scale*800<=1366 && scale*540<=728.01,"Screen fitting exceeds a small work area");
        Set(form,"orbitPaintAverage",20.0);Check(Property<int>("EffectiveFrameRate")==60,"Expensive ORBIT frames do not reduce the scheduling target");
        Set(form,"orbitPaintAverage",2.0);Check(Property<int>("EffectiveFrameRate")==form.Preferences.FrameRate,"ORBIT scheduling does not recover after frame cost falls");

        for(int seed=1;seed<=40;seed++)
        {
            var game=new Game(GameCatalog.Defaults(Era.Future2126,GameKind.Klondike).Rules,seed);var seen=new HashSet<string>();
            for(int i=0;i<300 && !game.State.Won;i++)
            {
                string key=JsonSerializer.Serialize(new{game.State.Stock,game.State.Waste,game.State.Tableau,game.State.Foundations});
                Check(seen.Add(key),$"ORBIT hints repeated a board for seed {seed}");
                var hints=game.OrbitHints();if(hints.Count==0)break;var h=hints[0];
                bool moved=h.From.Kind==PileKind.Stock?game.Draw():h.From==h.To?game.Flip(h.From.Pile):game.Move(h.From,h.To);Check(moved,"ORBIT suggested an illegal move");Game.Validate(game.State);
            }
        }
        Console.WriteLine("PASS ORBIT audit regressions: fitted columns, settings, hints, completion, accessibility, snapshots, palette, volume and screen fitting");
    }
}
