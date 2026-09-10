using System.Drawing;
using System.Reflection;
using System.Text.Json;
using Solitude;

internal static partial class UiProgram
{
    private static bool HasControl(GameWindow form,string id)=>((System.Collections.IEnumerable)Field(form,"hotspots")!).Cast<object>().Any(h=>(string)h.GetType().GetProperty("Id")!.GetValue(h)! == id);
    private static List<(string Label,string Key)> Menu(GameWindow form,int number)
    {
        Set(form,"menu",number);
        return ((System.Collections.IEnumerable)typeof(GameWindow).GetMethod("PeriodMenu",Private)!.Invoke(form,null)!).Cast<object>().Select(e=>((string)e.GetType().GetProperty("Label")!.GetValue(e)!,(string)e.GetType().GetProperty("Key")!.GetValue(e)!)).ToList();
    }
    private static string RenderHash(GameWindow form)
    {
        using var b=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);using(var g=Graphics.FromImage(b))Call(form,"PaintScaled",g);
        using var m=new MemoryStream();b.Save(m,System.Drawing.Imaging.ImageFormat.Png);return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(m.ToArray()));
    }
    private static int FlightCount(GameWindow f)=>((System.Collections.IDictionary)Field(f,"flights")!).Count;
    private static void CheckPeriodPresentation()
    {
        string[][] expected=
        [
            ["&Deal|F2","|","&Undo|","De&ck...|","&Options...|","|","E&xit|"],
            ["&New Game|F2","&Select Game|F3","&Restart Game|","|","S&tatistics...|F4","&Options...|F5","|","&Undo|F10","|","E&xit|"],
            ["&New Game|F2","&Restart This Game|","|","&Undo|Ctrl+Z","&Deal Next Row|D","Show An Available &Move|M","|","D&ifficulty...|F3","S&tatistics...|F4","O&ptions...|F5","|","&Save This Game|Ctrl+S","&Open Last Saved Game|Ctrl+O","|","E&xit|"]
        ];
        foreach(var era in GameCatalog.HistoricalEras)foreach(var kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))
        {
            using var form=new GameWindow(new Store("artifacts/period-ui"),era,1,100,true,kind);Paint(form);Set(form,"renderMotionTime",10.0);
            var menu=Menu(form,0);Check(menu.All(e=>!e.Label.Contains("Feel") && !e.Label.Contains("Windows") && !e.Label.Contains("Collect") && !e.Label.Contains("Controls")),"Modern controls leaked into the game menu");
            if(era!=Era.WindowsVista)Check(menu.Select(e=>e.Label+"|"+e.Key).SequenceEqual(expected[(int)kind]),"Period menu differs from the archived MENU resource: "+era+"/"+kind);
            Check(Menu(form,2).Any(e=>e.Key=="F6" && e.Label.Contains("Windows")),"Windows selector missing from the window menu");
            Set(form,"menu",-1);string before=RenderHash(form);
            form.Preferences.QuickControls=true;form.Preferences.ShowFrameRate=true;form.Preferences.HighlightTargets=true;
            Set(form,"status","That move isn't allowed.");Set(form,"statusUntil",DateTime.Now.AddHours(1));
            Check(RenderHash(form)==before,"Removed banner, toolbar or frame counter resurfaced from saved preferences: "+era+"/"+kind);
            Key(form,Keys.F8);Key(form,Keys.F12);Check((DialogPage)Field(form,"dialog")! == DialogPage.None,"Removed shortcut still opens a dialog");
            Click(form,Hit(form,"settings"));Paint(form);Check((DialogPage)Field(form,"dialog")! == DialogPage.Settings && HasControl(form,"dialog-era-0") && HasControl(form,"dialog-era-7"),"Visible Settings button failed to open Windows selection");Call(form,"CloseDialog");
            Key(form,Keys.Alt|Keys.G);Paint(form);Click(form,Hit(form,"settings"));Paint(form);Check((DialogPage)Field(form,"dialog")! == DialogPage.Settings,"Settings could not open while the Game menu was active");Call(form,"CloseDialog");
            Key(form,Keys.F6);Paint(form);Check(HasControl(form,"dialog-era-0") && HasControl(form,"dialog-era-7"),"F6 lost the Windows selector");Call(form,"CloseDialog");
            Call(form,"OpenHelp",3);Paint(form);Key(form,Keys.U);Key(form,Keys.N);Key(form,Keys.D);Key(form,Keys.O);Paint(form);
            Check(HasControl(form,"dialog-index-1") && !HasControl(form,"dialog-index-2"),"Help index did not filter the keyword");
            Key(form,Keys.Enter);Check((int)Field(form,"helpPage")! == 2,"Help index did not open the matching topic");Call(form,"CloseDialog");
            if(era!=Era.WindowsVista && kind==GameKind.Klondike)
            {
                Paint(form);Call(form,"DrawCards");Check(FlightCount(form)==0,"Classic Solitaire drew with modern card flights");
                int moves=form.Game.State.Moves;Key(form,Keys.H);Key(form,Keys.A);Check(form.Game.State.Moves==moves && Field(form,"hint")==null && !(bool)Field(form,"collecting")!,"Classic Solitaire gained an invented hint or collect key");
                Key(form,Keys.F2);Check(form.Game.State.Moves==0 && (DialogPage)Field(form,"dialog")! == DialogPage.None,"Classic Deal inserted an invented confirmation");
            }
            if(kind==GameKind.FreeCell && era!=Era.WindowsVista)
            {
                Call(form,"OpenDialog",DialogPage.Options);Paint(form);
                Check(HasControl(form,"dialog-messages") && HasControl(form,"dialog-quick-play") && HasControl(form,"dialog-double-click") && !HasControl(form,"dialog-animations"),"Classic FreeCell lost its three original options");
                Click(form,Hit(form,"dialog-double-click"));Paint(form);Click(form,Hit(form,"dialog-ok"));Paint(form);
                DoubleClick(form,Area(form,new(PileKind.Tableau,0,6)));Check(form.Game.State.Moves==0,"Disabled FreeCell double-click still moved a card");
                Set(form,"selection",null);Paint(form);Click(form,Area(form,new(PileKind.Tableau,0,6)));
                var dest=(RectangleF)typeof(GameWindow).GetMethod("CellRect",Private)!.Invoke(form,[0,true])!;Click(form,dest);Paint(form);
                Check((DialogPage)Field(form,"dialog")! == DialogPage.Notice && (string)Field(form,"notice")! == "That move is not allowed.","FreeCell illegal move did not show its optional dialog");
                if(era==Era.WindowsXP){Directory.CreateDirectory("artifacts/period-details");form.RenderTo("artifacts/period-details/freecell-illegal.png",DialogPage.Notice);}
                Call(form,"CloseDialog");form.Preferences.FreeCellMessages=false;Set(form,"selection",null);Paint(form);Click(form,Area(form,new(PileKind.Tableau,0,6)));Click(form,dest);
                Check((DialogPage)Field(form,"dialog")! == DialogPage.None && form.Game.State.Moves==0,"Disabled illegal-move messages still interrupted the game");
                Call(form,"OpenDialog",DialogPage.MoveColumn);Paint(form);Check(HasControl(form,"dialog-cancel"),"Move column is missing its Cancel button");
            }
            if(kind==GameKind.Spider && era!=Era.WindowsVista)
            {
                Key(form,Keys.F5);Paint(form);
                foreach(var id in new[]{"animate-deal","auto-save","auto-open","prompt-save","prompt-open","sound"})Check(HasControl(form,"dialog-"+id),"Missing Spider option: "+id);
                Check(!HasControl(form,"dialog-suits-4"),"Difficulty is still mixed into Spider Options");Call(form,"CloseDialog");
                Key(form,Keys.F3);Paint(form);Check(HasControl(form,"dialog-suits-4") && !HasControl(form,"dialog-auto-save"),"Difficulty did not open its own dialog");Call(form,"CloseDialog");
                form.Preferences.SpiderAnimateDeal=false;Call(form,"DrawCards");Check(FlightCount(form)==0,"Spider ignored disabled deal animation");
                form.Preferences.SpiderAnimateDeal=true;Paint(form);Call(form,"DrawCards");Check(FlightCount(form)==10,"Spider did not animate exactly the dealt row");
            }
        }
        using(var form=new GameWindow(new Store("artifacts/settings-choice"),Era.Windows31,1,100,true))
        {
            Paint(form);Click(form,Hit(form,"settings"));Paint(form);Click(form,Hit(form,"dialog-era-6"));Paint(form);
            Click(form,Hit(form,"dialog-game-FreeCell"));Paint(form);Click(form,Hit(form,"dialog-ok"));Paint(form);
            Check(form.Preferences.Era==Era.WindowsXP && form.Game.Rules.Kind==GameKind.FreeCell,"Settings clicks did not apply both Windows and game selection");
        }
        CheckFreeCellTransfers();CheckSpiderCheckpoints();CheckPresentationMigration();CheckSharedSettings();CheckFreeCellWinPrompt();CheckTextRasterization();
        Console.WriteLine("PASS original menus/options, removed overlays, period input/animation, FreeCell transfers, Spider save slot and presentation migration");
    }
    private static void CheckFreeCellTransfers()
    {
        var s=new GameState{Kind=GameKind.FreeCell,Seed=1,Tableau=Enumerable.Range(0,8).Select(_=>new List<Card>()).ToList()};
        s.Tableau[0]=[new(8),new(33)];s.Tableau[1]=[new(35)];s.Tableau[7]=Enumerable.Range(0,52).Where(i=>i is not (8 or 33 or 35)).Select(i=>new Card(i)).ToList();
        foreach(bool quick in new[]{false,true})
        {
            using var form=new GameWindow(new Store("artifacts/freecell-transfers"),Era.WindowsXP,1,100,true,GameKind.FreeCell);form.SetRenderState(s);Paint(form);Set(form,"renderMotionTime",10.0);form.Preferences.FreeCellQuickPlay=quick;
            Call(form,"Changed",form.Game.Move(new(PileKind.Tableau,0,0),new(PileKind.Tableau,1)));
            Check(form.Game.State.Tableau[0].Count==0 && form.Game.State.Tableau[1].Count==3,"FreeCell transfer fixture did not move");
            Check(FlightCount(form)==(quick?0:2),"Quick play did not skip the intermediate transfers");
            if(!quick)
            {
                object f=((System.Collections.IDictionary)Field(form,"flights")!)[33]!;var pose=f.GetType().GetMethod("Sample")!.Invoke(f,[10.03])!;
                var position=(Position)pose.GetType().GetProperty("Position")!.GetValue(pose)!;
                Check(position.Kind==PileKind.FreeCell && position.Pile==0,"Classic column animation skipped its temporary free cell");
            }
        }
    }
    private static void CheckSpiderCheckpoints()
    {
        string dir=Path.GetFullPath("artifacts/spider-slot-"+Guid.NewGuid().ToString("N"));string saved;
        using(var form=new GameWindow(new Store(dir),Era.WindowsXP,1,100,kind:GameKind.Spider))
        {
            Paint(form);Call(form,"DrawCards");Paint(form);saved=JsonSerializer.Serialize(form.Game.State);Call(form,"SaveSpiderGame",false);Call(form,"DrawCards");Paint(form);
            Key(form,Keys.Control|Keys.S);Paint(form);Check((DialogPage)Field(form,"dialog")! == DialogPage.Confirm,"Replacing Spider's save slot omitted the prompt");Click(form,Hit(form,"dialog-no"));
            Call(form,"OpenSpiderGame",false,false);Paint(form);Check(JsonSerializer.Serialize(form.Game.State)==saved,"Manual open failed to restore the saved Spider deal");
            var e=new FormClosingEventArgs(CloseReason.UserClosing,false);Call(form,"PeriodClosing",e);Paint(form);
            Check(e.Cancel && (DialogPage)Field(form,"dialog")! == DialogPage.Confirm && HasControl(form,"dialog-cancel"),"Spider closing skipped its save/cancel question");Click(form,Hit(form,"dialog-cancel"));
            Check(!(bool)Field(form,"closingDecision")!,"Cancel approved Spider closing");
            form.Preferences.SpiderAutoSave=true;Call(form,"DrawCards");Paint(form);saved=JsonSerializer.Serialize(form.Game.State);e=new(CloseReason.UserClosing,false);Call(form,"PeriodClosing",e);
            Check(!e.Cancel && (bool)Field(form,"closingDecision")!,"Spider automatic save still prompted");
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.WindowsMe,GameKind.Spider));Call(form,"OpenSpiderGame",false,false);Check((DialogPage)Field(form,"dialog")! == DialogPage.Notice,"Spider save slot leaked between Windows versions");
        }
        var store=new Store(dir);var file=store.Load();Check(store.Warning==null && file.SpiderSaves.Count==1 && JsonSerializer.Serialize(file.SpiderSaves["WindowsXP"].Game)==saved,"Spider checkpoint was not independently persisted");
        var copy=file.SpiderSaves["WindowsXP"].Clone();file.SpiderSaves["WindowsXP"].Game.Stock.Clear();Check(copy.Game.Stock.Count>0,"Spider checkpoint clone shares card lists");
        file.SpiderSaves["Windows95"]=copy;File.WriteAllText(store.FilePath,JsonSerializer.Serialize(file));store.Load();Check(store.Warning!=null,"Invalid Spider checkpoint accepted an unsupported Windows version");
    }
    private static void CheckPresentationMigration()
    {
        string dir=Path.GetFullPath("artifacts/period-migrate-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        var p=GameCatalog.Defaults(Era.WindowsXP,GameKind.Klondike);p.QuickControls=true;p.ShowFrameRate=true;p.HighlightTargets=true;p.Rules.AutoFlip=true;
        var g=new Game(p.Rules,123);g.Draw();var file=new SaveFile{Format=3,Preferences=p,Game=g.State,History=g.History,Statistics=new(){Played=7,Won=3},Sessions=new(){["Windows95/FreeCell"]=new(){Preferences=GameCatalog.Defaults(Era.Windows95,GameKind.FreeCell),Statistics=new(){Played=2,Won=1}}}};
        file.Sessions.Values.Single().Preferences.QuickControls=true;File.WriteAllText(Path.Combine(dir,"solitude.json"),JsonSerializer.Serialize(file));
        var store=new Store(dir);var migrated=store.Load();Check(store.Warning==null && migrated.Format==6 && !migrated.Preferences.QuickControls && !migrated.Preferences.ShowFrameRate && !migrated.Preferences.HighlightTargets && !migrated.Preferences.Rules.AutoFlip,"Old modern presentation survived migration");
        Check(JsonSerializer.Serialize(migrated.Game)==JsonSerializer.Serialize(g.State) && migrated.Statistics.Won==3 && !migrated.Sessions.Values.Single().Preferences.QuickControls && migrated.Sessions.Values.Single().Statistics.Won==1,"Presentation migration lost cards or records");
    }
    private static void CheckSharedSettings()
    {
        string dir=Path.GetFullPath("artifacts/shared-settings-"+Guid.NewGuid().ToString("N"));string spiderState;
        using(var form=new GameWindow(new Store(dir),Era.WindowsMe,71,150,kind:GameKind.Spider))
        {
            Paint(form);Call(form,"DrawCards");spiderState=JsonSerializer.Serialize(form.Game.State);
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.WindowsXP,GameKind.Spider,150));Call(form,"OpenDialog",DialogPage.Difficulty);Paint(form);Click(form,Hit(form,"dialog-suits-4"));Click(form,Hit(form,"dialog-ok"));Paint(form);
            Call(form,"OpenDialog",DialogPage.Options);Paint(form);Click(form,Hit(form,"dialog-animate-deal"));Click(form,Hit(form,"dialog-prompt-save"));Click(form,Hit(form,"dialog-ok"));Paint(form);
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.WindowsMe,GameKind.Spider,150));
            Check(form.Preferences.Rules.SpiderSuits==4 && !form.Preferences.SpiderAnimateDeal && !form.Preferences.SpiderPromptSave,"Compatible Spider options did not cross to an existing preset");
            Check(form.Game.Rules.SpiderSuits==1 && JsonSerializer.Serialize(form.Game.State)==spiderState,"Shared difficulty destroyed or reinterpreted an existing deal");
        }
        using(var form=new GameWindow(new Store(dir)))
        {
            Check(form.Preferences.Rules.SpiderSuits==4 && form.Game.Rules.SpiderSuits==1 && JsonSerializer.Serialize(form.Game.State)==spiderState,"Restart lost shared options or the original deal rules");
            Call(form,"OpenDialog",DialogPage.Options);Paint(form);Click(form,Hit(form,"dialog-sound"));Click(form,Hit(form,"dialog-ok"));Paint(form);
            Check(form.Game.Rules.SpiderSuits==1 && JsonSerializer.Serialize(form.Game.State)==spiderState,"Changing a presentation option applied pending difficulty and discarded the deal");
            int seed=form.Game.State.Seed;Call(form,"NewGame",true,null!);
            Check(form.Game.Rules.SpiderSuits==1 && form.Game.State.Seed==seed && form.Game.State.Moves==0,"Restart Game changed the original deal's difficulty");
            Call(form,"NewGame",false,null!);Check(form.Game.Rules.SpiderSuits==4,"New deal did not use shared difficulty");
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.WindowsXP,GameKind.FreeCell,125));Call(form,"OpenDialog",DialogPage.Options);Paint(form);
            Click(form,Hit(form,"dialog-messages"));Click(form,Hit(form,"dialog-quick-play"));Click(form,Hit(form,"dialog-double-click"));Click(form,Hit(form,"dialog-ok"));
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.Windows95,GameKind.FreeCell,125));
            Check(!form.Preferences.FreeCellMessages && form.Preferences.FreeCellQuickPlay && !form.Preferences.FreeCellDoubleClick,"Classic FreeCell choices reset on a new Windows preset");
            Check(!form.Game.Rules.FreeCellSupermoves && form.Game.Rules.UndoLimit==1,"Sharing copied Vista mechanics into classic FreeCell");
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.WindowsVista,GameKind.FreeCell,125));
            Check(form.Game.Rules.FreeCellSupermoves && form.Game.Rules.UndoLimit==0,"Sharing overwrote Vista mechanics");
            Call(form,"OpenDialog",DialogPage.Deck);Paint(form);Click(form,Hit(form,"dialog-vistadeck-3"));Click(form,Hit(form,"dialog-background-4"));Click(form,Hit(form,"dialog-ok"));
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.WindowsVista,GameKind.Klondike,125));Check(form.Preferences.VistaDeck==3 && form.Preferences.VistaBackground==4,"Vista appearance was not shared between games");
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.Windows95,GameKind.Klondike,125));Call(form,"OpenDialog",DialogPage.Options);Paint(form);
            Click(form,Hit(form,"dialog-draw1"));Click(form,Hit(form,"dialog-outline"));Click(form,Hit(form,"dialog-ok"));
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.Windows98,GameKind.Klondike,125));Check(form.Preferences.Rules.DrawCount==1 && form.Preferences.OutlineDragging && !form.Game.Rules.AutoFlip,"Solitaire options did not carry or changed the preset's mechanics");
        }
        var store=new Store(dir);var saved=store.Load();Check(store.Warning==null && saved.Format==6 && saved.Shared?.Scale==125 && saved.Shared.FreeCellQuickPlay==true && saved.Shared.VistaDeck==3,"Shared preferences did not persist to disk");
        saved.Shared!.SpiderSuits=3;File.WriteAllText(store.FilePath,JsonSerializer.Serialize(saved));store.Load();Check(store.Warning!=null,"Malformed shared settings were accepted");
    }
    private static void CheckFreeCellWinPrompt()
    {
        foreach(var era in new[]{Era.Windows95,Era.Windows98,Era.WindowsMe,Era.Windows2000,Era.WindowsXP})
        {
            using var form=new GameWindow(new Store("artifacts/freecell-win-prompt"),era,1,150,true,GameKind.FreeCell);
            string state=JsonSerializer.Serialize(form.Game.State);Call(form,"OpenDialog",DialogPage.Won);Paint(form);
            Click(form,Hit(form,"dialog-select"));Paint(form);
            Check((DialogPage)Field(form,"dialog")! == DialogPage.Won && JsonSerializer.Serialize(form.Game.State)==state,"Select game checkbox dealt before confirmation: "+era);
            Click(form,Hit(form,"dialog-ok"));Paint(form);
            Check((DialogPage)Field(form,"dialog")! == DialogPage.SelectGame,"Selected win prompt did not open the game-number dialog: "+era);
            Key(form,Keys.D7);Key(form,Keys.Enter);Paint(form);Check(form.Game.State.Seed==7 && (DialogPage)Field(form,"dialog")! == DialogPage.None,"Win prompt did not start the selected numbered deal: "+era);
            Call(form,"OpenDialog",DialogPage.Won);Paint(form);Check(!(bool)Field(form,"winSelectGame")!,"A previous win selection leaked into the next prompt: "+era);
            state=JsonSerializer.Serialize(form.Game.State);Click(form,Hit(form,"dialog-cancel"));Check((DialogPage)Field(form,"dialog")! == DialogPage.None && JsonSerializer.Serialize(form.Game.State)==state,"No replaced the completed deal: "+era);
        }
        foreach(var era in new[]{Era.Windows95,Era.WindowsXP,Era.WindowsVista})
        {
            using var form=new GameWindow(new Store("artifacts/freecell-number-input"),era,1,125,true,GameKind.FreeCell);
            Call(form,"OpenDialog",DialogPage.SelectGame);Paint(form);Key(form,Keys.D4);Key(form,Keys.D2);Key(form,Keys.Enter);
            Check(form.Game.State.Seed==42,"Quick number entry used stale paint-time text: "+era);
            Call(form,"OpenDialog",DialogPage.SelectGame);Paint(form);Key(form,Keys.D0);Key(form,Keys.Enter);
            Check(form.Game.State.Seed==42 && (DialogPage)Field(form,"dialog")! == DialogPage.SelectGame,"Quick invalid number entry accepted stale valid text: "+era);
        }
    }
    private static void CheckTextRasterization()
    {
        foreach(var era in new[]{Era.Windows95,Era.Windows2000,Era.WindowsXP})foreach(float scale in new[]{1f,1.25f,1.5f,2f})
        {
            using var skin=new Skin(era);using var bitmap=new Bitmap(500,80);
            using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.White);g.ScaleTransform(scale,scale);skin.Configure(g);skin.Text(g,"Settings  This session  Win percentage: 50%",new(4,3,240,26));}
            int black=0;bool opaque=true,clean=true;
            for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++){var c=bitmap.GetPixel(x,y);opaque&=c.A==255;clean&=c.R==c.G && c.G==c.B && c.R is 0 or 255;if(c.R==0)black++;}
            Check(opaque && clean && black>100,$"Text has faded/color-fringed or missing strokes: {era}/{scale}");
        }
    }
}
