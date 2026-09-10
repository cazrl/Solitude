using System.Drawing;
using System.Reflection;
using System.Text.Json;
using Solitude;

internal static partial class UiProgram
{
    private static void CheckFidelityFixes()
    {
        foreach(var era in GameCatalog.HistoricalEras)
        {
            foreach(var scoring in new[]{Scoring.Standard,Scoring.Vegas})
            {
                var rules=GameCatalog.Defaults(era,GameKind.Klondike).Rules;rules.Scoring=scoring;
                var game=new Game(rules,4);
                int col=game.State.Tableau.FindIndex(p=>p[^1].Rank==1);
                Check(col>=0 && game.Move(new(PileKind.Tableau,col),new(PileKind.Foundation,0)),"Scoring fixture Ace not exposed");
                int score=game.State.Score;
                for(int i=0;i<100;i++)Check(game.Move(new(PileKind.Foundation,i%2),new(PileKind.Foundation,1-i%2)),"Foundation reorder rejected");
                Check(game.State.Score==score,"Foundation reordering manufactured points: "+era+"/"+scoring);
            }
            using var form=new GameWindow(new Store("artifacts/fidelity-isolated"),era,4,100,true);
            Paint(form);Key(form,Keys.Space);Check(form.Game.State.Stock.Count==21,"Space did not draw: "+era);
            Call(form,"OpenDialog",DialogPage.Options);Paint(form);
            if(era!=Era.WindowsVista)
            {
                var bounds=(RectangleF)Field(form,"dialogBounds")!;
                Check(bounds.Size==new SizeF(era<=Era.Windows31?278:342,era<=Era.Windows31?230:era==Era.WindowsXP?225:216),"Measured Options geometry changed");
                Key(form,Keys.Alt|Keys.O);Paint(form);Check(((Preferences)Field(form,"draft")!).Rules.DrawCount==1,"Draw One mnemonic failed");
                Key(form,Keys.Right);Paint(form);Check(((Preferences)Field(form,"draft")!).Rules.DrawCount==3,"Radio arrow failed");
                Key(form,Keys.Alt|Keys.V);Paint(form);Check(((Preferences)Field(form,"draft")!).Rules.Scoring==Scoring.Vegas,"Vegas mnemonic failed");
            }
            Call(form,"CloseDialog");
            if(era<=Era.Windows31){Key(form,Keys.F1);Check((DialogPage)Field(form,"dialog")! ==DialogPage.None,"Early F1 gained later Help behavior");Check(Menu(form,1)[0].Label=="&Index","Early Help lost Index");Set(form,"menu",-1);}
            foreach(var kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))
            {
                Call(form,"SwitchGame",GameCatalog.Defaults(era,kind));form.Preferences.SaveOnExit=false;
                if(kind==GameKind.FreeCell)Call(form,"Changed",form.Game.ToFreeCell(new(PileKind.Tableau,0)));else Call(form,"DrawCards");
                string state=JsonSerializer.Serialize(form.Game.State),history=JsonSerializer.Serialize(form.Game.History);
                var snapshot=(SaveFile)typeof(GameWindow).GetMethod("SnapshotSave",Private)!.Invoke(form,null)!;
                Check(snapshot.Game==null && snapshot.History.Count==0,"Save disabled still persisted active cards");
                var other=era==Era.Windows30?Era.WindowsXP:Era.Windows30;
                Call(form,"SwitchGame",GameCatalog.Defaults(other,GameKind.Klondike));
                snapshot=(SaveFile)typeof(GameWindow).GetMethod("SnapshotSave",Private)!.Invoke(form,null)!;
                Check(snapshot.Sessions[GameCatalog.Key(era,kind)].Game==null,"Save disabled persisted suspended cards");
                Call(form,"SwitchGame",GameCatalog.Defaults(era,kind));
                Check(JsonSerializer.Serialize(form.Game.State)==state && JsonSerializer.Serialize(form.Game.History)==history,"Preset switch discarded unsaved runtime session: "+era+"/"+kind);
            }
        }
        CheckFreeCellAuditFixes();CheckVistaAuditFixes();CheckOwnedDialogHost();CheckLongUndo();CheckCelebrations();CheckNegativeDeals();CheckMnemonicGlyphs();
        Console.WriteLine("PASS fidelity regressions: foundation scoring, unsaved sessions, original keys/dialog geometry, FreeCell feedback/peek, Vista options/audio/save flow and owned dialog input");
    }
    private static void CheckFreeCellAuditFixes()
    {
        var blocked=JsonSerializer.Deserialize<GameState>(File.ReadAllText("tests/Fixtures/freecell-blocked.json"))!;
        foreach(var era in new[]{Era.Windows95,Era.Windows98,Era.WindowsMe,Era.Windows2000,Era.WindowsXP})
        {
            using var form=new GameWindow(new Store("artifacts/fc-fidelity-isolated"),era,1,100,true,GameKind.FreeCell);
            Paint(form);var r=Area(form,new(PileKind.Tableau,0,0));int x=(int)r.X+30,y=(int)r.Y+6;
            Call(form,"OnMouseMove",new MouseEventArgs(MouseButtons.None,0,x,y,0));string before=RenderHash(form);
            Call(form,"OnMouseDown",new MouseEventArgs(MouseButtons.Right,1,x,y,0));Check(RenderHash(form)!=before,"Right button did not reveal buried card: "+era);
            Call(form,"OnMouseUp",new MouseEventArgs(MouseButtons.Right,1,x,y,0));Check(RenderHash(form)==before,"Right reveal remained after release");
            Call(form,"OpenDialog",DialogPage.Options);Paint(form);bool quick=((Preferences)Field(form,"draft")!).FreeCellQuickPlay;
            Key(form,Keys.Alt|Keys.Q);Check(((Preferences)Field(form,"draft")!).FreeCellQuickPlay!=quick,"Alt+Q did not toggle Quick play");Call(form,"CloseDialog");
            var oneMove=JsonSerializer.Deserialize<GameState>(File.ReadAllText("tests/Fixtures/freecell-one-move.json"))!;form.SetRenderState(oneMove);
            Check(form.Game.FreeCellAvailableMoves()==1,"One-move fixture differs");Call(form,"CheckFreeCellEnd");Check((bool)Field(form,"oneMoveWarning")! && !form.Game.State.Lost,"One legal move did not flash the caption");
            form.SetRenderState(blocked);Check(form.Game.FreeCellAvailableMoves()==0,"Blocked fixture has a legal move");
            Call(form,"CheckFreeCellEnd");Paint(form);Check(form.Game.State.Lost && (DialogPage)Field(form,"dialog")! ==DialogPage.Lost,"Exhausted game did not end");
            Check(HasControl(form,"dialog-same-game") && HasControl(form,"dialog-ok") && HasControl(form,"dialog-cancel"),"Original loss choices missing");
            int played=form.Statistics.Played;Call(form,"CheckFreeCellEnd");Call(form,"NewGame",true,null!);
            Check(form.Statistics.Played==played && played==1 && !form.Game.State.Lost,"Loss was counted twice or leaked into restart");
        }
        // Two cards, no free cells: this requires a visible stop in a spare column.
        var s=new GameState{Kind=GameKind.FreeCell,Seed=1,Tableau=Enumerable.Range(0,8).Select(_=>new List<Card>()).ToList()};
        s.Tableau[0]=[new(8),new(33)];s.Tableau[1]=[new(35)];int[] occupied=[11,24,37,50];
        for(int i=0;i<4;i++)s.FreeCells[i]=[new(occupied[i])];
        s.Tableau[7]=Enumerable.Range(0,52).Where(i=>i is not (8 or 33 or 35) && !occupied.Contains(i)).Select(i=>new Card(i)).ToList();
        using var transfer=new GameWindow(new Store("artifacts/fc-fidelity-isolated"),Era.WindowsXP,1,100,true,GameKind.FreeCell);
        transfer.SetRenderState(s);Paint(transfer);Set(transfer,"renderMotionTime",10.0);transfer.Preferences.FreeCellQuickPlay=false;
        Call(transfer,"Changed",transfer.Game.Move(new(PileKind.Tableau,0,0),new(PileKind.Tableau,1),false));
        Check(FlightCount(transfer)==2,"Spare-column move skipped animation");
        object flight=((System.Collections.IDictionary)Field(transfer,"flights")!)[33]!;
        var pose=flight.GetType().GetMethod("Sample")!.Invoke(flight,[10.03])!;
        var position=(Position)pose.GetType().GetProperty("Position")!.GetValue(pose)!;
        Check(position.Kind==PileKind.Tableau && position.Pile==2,"Transfer did not visit the empty column");
    }
    private static void CheckVistaAuditFixes()
    {
        foreach(var kind in Enum.GetValues<GameKind>())
        {
            using var form=new GameWindow(new Store("artifacts/vista-fidelity-isolated"),Era.WindowsVista,1,100,true,kind);Paint(form);
            Call(form,"OpenDialog",DialogPage.Options);Paint(form);
            foreach(string id in new[]{"sound","tips","save","continue","animations"})Check(HasControl(form,"dialog-"+id),"Vista option missing: "+id);
            Key(form,Keys.Alt|Keys.S);Check(!((Preferences)Field(form,"draft")!).Sound,"Vista sound mnemonic failed");Call(form,"CloseDialog");
            Check(Menu(form,0).Any(e=>e.Label=="&Restart Game"),"Vista restart menu missing");Set(form,"menu",-1);
            form.Preferences.DisplayTips=false;Call(form,"ShowVistaTip","Test","Hidden tip",false);Check(Field(form,"vistaTipTitle")==null,"Disabled Vista tips still appeared");
            form.Preferences.DisplayTips=true;Call(form,"ShowVistaTip","Test","Visible tip",false);Paint(form);Check(HasControl(form,"vista-tip") && (DialogPage)Field(form,"dialog")! ==DialogPage.None,"Vista tip interrupted play with a modal dialog");
            form.Preferences.Sound=true;Call(form,"PlayVistaSound","SHARED_ILLEGALMOVE");Check((string?)Field(form,"lastPeriodSound")=="SHARED_ILLEGALMOVE","Vista sound dispatch missing");
            form.Preferences.Sound=false;Call(form,"PlayVistaSound","SHARED_HINTSHOWN");Check((string?)Field(form,"lastPeriodSound")=="SHARED_ILLEGALMOVE","Sound-disabled option ignored");
            if(kind==GameKind.FreeCell)Call(form,"Changed",form.Game.ToFreeCell(new(PileKind.Tableau,0)));else Call(form,"DrawCards");
            form.Preferences.SaveOnExit=false;var closing=new FormClosingEventArgs(CloseReason.UserClosing,false);Call(form,"PeriodClosing",closing);Paint(form);
            Check(closing.Cancel && HasControl(form,"dialog-yes") && HasControl(form,"dialog-no") && HasControl(form,"dialog-cancel"),"Vista close omitted save choices");Call(form,"CloseDialog");
            Set(form,"vistaSaveOnce",true);var snapshot=(SaveFile)typeof(GameWindow).GetMethod("SnapshotSave",Private)!.Invoke(form,null)!;
            Check(snapshot.Game!=null && !snapshot.Preferences.SaveOnExit,"One-time save changed always-save preference");
            Set(form,"offerVistaResume",true);Call(form,"OfferVistaResume");Check((DialogPage)Field(form,"dialog")! ==DialogPage.Confirm,"Vista startup resume question missing");
        }
        var assembly=typeof(GameWindow).Assembly;var sounds=assembly.GetManifestResourceNames().Where(n=>n.Contains(".Sounds.Vista.")).ToArray();
        Check(sounds.Length==20,"Missing embedded Vista sounds");
        foreach(string resource in sounds){using var stream=assembly.GetManifestResourceStream(resource)!;var header=new byte[12];stream.ReadExactly(header);Check(System.Text.Encoding.ASCII.GetString(header,0,4)=="RIFF" && System.Text.Encoding.ASCII.GetString(header,8,4)=="WAVE","Invalid sound resource");}
    }
    private static void CheckNegativeDeals()
    {
        foreach(var era in new[]{Era.Windows95,Era.Windows98,Era.WindowsMe,Era.Windows2000,Era.WindowsXP})foreach(int seed in new[]{-1,-2})
        {
            using var form=new GameWindow(new Store("artifacts/negative-isolated"),era,1,100,true,GameKind.FreeCell);Paint(form);Key(form,Keys.F3);Paint(form);Key(form,Keys.Control|Keys.A);Key(form,Keys.OemMinus);Key(form,seed==-1?Keys.D1:Keys.D2);Key(form,Keys.Enter);Paint(form);
            Check(form.Game.State.Seed==seed,"Classic negative game number rejected");Game.Validate(form.Game.State);
            int[] first=seed==-1?[0,2,4,6,8,10,12]:[39,51,50,49,48,47,46];
            int[] fifth=seed==-1?[11,9,7,5,3,1]:[45,44,43,42,41,40];
            Check(form.Game.State.Tableau[0].Select(c=>c.Id).SequenceEqual(first) && form.Game.State.Tableau[4].Select(c=>c.Id).SequenceEqual(fifth),"Negative deal differs from original XP data flow");
            if(seed==-1){for(int col=0;col<4;col++)Call(form,"Changed",form.Game.ToFreeCell(new(PileKind.Tableau,col)));Check(form.Game.State.Lost,"Original -1 did not lose after filling four cells");}
        }
    }
    private static void CheckCelebrations()
    {
        Directory.CreateDirectory("artifacts/fidelity-celebrations");
        foreach(var era in new[]{Era.WindowsMe,Era.WindowsXP,Era.WindowsVista})foreach(var kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))
        {
            using var form=new GameWindow(new Store("artifacts/celebration-isolated"),era,1,100,true,kind);
            var state=new GameState{Kind=kind,Seed=1,Started=true,Elapsed=185,Score=kind==GameKind.Klondike?4254:1200,TimeBonus=kind==GameKind.Klondike?3780:0,Tableau=Enumerable.Range(0,kind==GameKind.Klondike?7:kind==GameKind.FreeCell?8:10).Select(_=>new List<Card>()).ToList()};
            state.Foundations=kind==GameKind.Spider?Enumerable.Range(0,8).Select(copy=>Enumerable.Range(0,13).Reverse().Select(rank=>new Card(39+rank,true,copy)).ToList()).ToList():Enumerable.Range(0,4).Select(suit=>Enumerable.Range(0,13).Select(rank=>new Card(suit*13+rank)).ToList()).ToList();
            form.SetRenderState(state);Paint(form);
            if(kind==GameKind.Klondike)Check((int)typeof(GameWindow).GetProperty("DisplayScore",Private)!.GetValue(form)! ==(era==Era.WindowsVista?474:4254),"Win status applied the wrong era bonus display");
            Set(form,"renderMotionTime",10.0);Call(form,"StartVictory");
            if(kind==GameKind.FreeCell){Check((DialogPage)Field(form,"dialog")! ==DialogPage.Won,"FreeCell win dialog missing");if(era==Era.WindowsVista)Check((string?)Field(form,"lastPeriodSound")=="FREECELLWIN","FreeCell win music missing");continue;}
            Check((bool)Field(form,"showingVictory")!,"Win animation missing: "+era+"/"+kind);
            string data=JsonSerializer.Serialize(form.Game.State),first=RenderHash(form);
            foreach(double elapsed in new[]{.3,1.5,3.0,6.0})
            {
                Set(form,"renderMotionTime",10+elapsed);Call(form,"Animate");Paint(form);
                using var bitmap=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);using(var g=Graphics.FromImage(bitmap))Call(form,"PaintScaled",g);
                bitmap.Save($"artifacts/fidelity-celebrations/{era}-{kind}-{elapsed:0.0}.png");
            }
            Check(RenderHash(form)!=first,"Victory frames stayed static");Check(JsonSerializer.Serialize(form.Game.State)==data,"Celebration changed score or cards");
            if(era==Era.WindowsVista && kind==GameKind.Klondike){Set(form,"renderMotionTime",19.0);Call(form,"Animate");Check((DialogPage)Field(form,"dialog")! ==DialogPage.Won,"Vista finish omitted results");}
            else{Key(form,Keys.Escape);Check(!(bool)Field(form,"showingVictory")!,"Could not dismiss victory");}
            Paint(form);form.RenderTo($"artifacts/fidelity-celebrations/{era}-{kind}-result.png",DialogPage.Won);
            if(era==Era.WindowsVista && kind==GameKind.Klondike)
            {
                var cache=(System.Collections.IDictionary)typeof(Skin).GetField("nativeText",Private)!.GetValue(Field(form,"skin"))!;
                var labels=cache.Keys.Cast<object>().Select(k=>(string)k.GetType().GetField("Item1")!.GetValue(k)!).ToArray();
                Check(labels.Contains("Time Bonus: 3780"),"Win dialog recalculated the awarded bonus");
                Check(labels.Contains("Score: 474"),"Win dialog omitted the recorded base score");
            }
        }
    }
    private static void CheckMnemonicGlyphs()
    {
        foreach(var era in GameCatalog.HistoricalEras)foreach(int scale in new[]{100,125,150,200})foreach(string label in new[]{"E&xit","&Play again"})
        {
            using var skin=new Skin(era);float s=scale/100f;
            using var plain=new Bitmap((int)(140*s),(int)(40*s));using var marked=new Bitmap(plain.Width,plain.Height);
            using(var g=Graphics.FromImage(plain)){g.ScaleTransform(s,s);skin.Configure(g);skin.Button(g,new(5,5,120,25),label.Replace("&",""));}
            using(var g=Graphics.FromImage(marked)){g.ScaleTransform(s,s);skin.Configure(g);skin.Button(g,new(5,5,120,25),label);}
            // Mnemonics may add an underline, but cannot truncate or reposition glyphs.
            int rows=(int)((5+(25+skin.Ui.Height)/2-3)*s);bool same=true,changed=false;
            for(int y=0;y<plain.Height;y++)for(int x=0;x<plain.Width;x++)if(plain.GetPixel(x,y)!=marked.GetPixel(x,y)){changed=true;if(y<rows)same=false;}
            Check(same,"Mnemonic clipped or shifted button text: "+era+"/"+scale+"/"+label);
            Check(changed,"Mnemonic underline missing: "+era+"/"+scale+"/"+label);
        }
    }
    private static void CheckLongUndo()
    {
        var p=GameCatalog.Defaults(Era.WindowsVista,GameKind.Klondike);var game=new Game(p.Rules,4);
        int col=game.State.Tableau.FindIndex(c=>c[^1].Rank==1);game.Move(new(PileKind.Tableau,col),new(PileKind.Foundation,0));
        for(int i=0;i<250;i++)game.Move(new(PileKind.Foundation,i%2),new(PileKind.Foundation,1-i%2));
        Check(game.History.Count==251,"Vista still discards moves after 200");
        string directory="artifacts/long-undo-"+Guid.NewGuid().ToString("N");var store=new Store(directory);
        Check(store.Save(new(){Preferences=p,ActiveRules=game.Rules.Clone(),Game=game.State,History=game.History}),"Long history save failed");
        var saved=store.Load();Check(store.Warning==null && saved.History.Count==251,"Long history did not reload");
        var restored=Game.Restore(saved.ActiveRules!,saved.Game!,saved.History);for(int i=0;i<251;i++)Check(restored.Undo(),"Long undo stopped prematurely");
        Check(restored.State.Moves==0,"Long undo did not restore initial deal");
    }
    private static void CheckOwnedDialogHost()
    {
        foreach(var era in GameCatalog.HistoricalEras)foreach(int scale in new[]{100,125,150,200})
        {
            using var form=new GameWindow(new Store("artifacts/dialog-fidelity-isolated"),era,1,scale,true);Call(form,"OpenDialog",DialogPage.Options);Paint(form);
            var type=typeof(GameWindow).GetNestedType("PeriodDialogHost",BindingFlags.NonPublic)!;
            using var host=(Form)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,[form],null)!;Set(form,"dialogHost",host);
            type.GetMethod("RefreshSurface")!.Invoke(host,null);host.Location=new(-500,-500);
            var originalRegion=host.Region;type.GetMethod("RefreshSurface")!.Invoke(host,null);
            Check(ReferenceEquals(originalRegion,host.Region),"Unchanged dialog paint replaced its native window region");
            Check(host.Left==-500 && host.Top==-500 && host.Owner==form && !host.ShowInTaskbar,"Dialog geometry confined to parent");
            var bounds=(RectangleF)Field(form,"dialogBounds")!;var button=Hit(form,"dialog-cancel");
            int x=(int)((button.X+button.Width/2-bounds.X)*scale/100),y=(int)((button.Y+button.Height/2-bounds.Y)*scale/100);
            type.GetMethod("OnMouseDown",Private)!.Invoke(host,[new MouseEventArgs(MouseButtons.Left,1,x,y,0)]);
            type.GetMethod("OnMouseUp",Private)!.Invoke(host,[new MouseEventArgs(MouseButtons.Left,1,x,y,0)]);
            Check((DialogPage)Field(form,"dialog")! ==DialogPage.None && Field(form,"dialogHost")==null,"Owned dialog input did not close modal: "+era+"/"+scale);
        }
    }
}
