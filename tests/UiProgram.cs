using System.Drawing;
using System.Reflection;
using System.Text.Json;
using Solitude;

internal static partial class UiProgram
{
    private static int checks;
    private static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private static object? Field(GameWindow form,string name)=>typeof(GameWindow).GetField(name,Private)!.GetValue(form);
    private static void Set(GameWindow form,string name,object? value)=>typeof(GameWindow).GetField(name,Private)!.SetValue(form,value);
    private static void Call(GameWindow form,string name,params object[] args)=>typeof(GameWindow).GetMethod(name,Private)!.Invoke(form,args);
    private static void Check(bool condition,string description){if(!condition)throw new Exception(description);checks++;}
    private static RectangleF Hit(GameWindow form,string id)
    {
        foreach(var hot in (System.Collections.IEnumerable)Field(form,"hotspots")!)
            if((string)hot.GetType().GetProperty("Id")!.GetValue(hot)! == id)return (RectangleF)hot.GetType().GetProperty("Bounds")!.GetValue(hot)!;
        throw new Exception("Missing control "+id);
    }
    private static void Click(GameWindow form,RectangleF r,bool release=true)
    {
        int x=(int)((r.X+r.Width/2)*form.Preferences.Scale/100),y=(int)((r.Y+r.Height/2)*form.Preferences.Scale/100);
        Call(form,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,x,y,0));
        if(release)Call(form,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,x,y,0));
    }
    private static void Paint(GameWindow form,bool settle=true)
    {
        if(settle)Call(form,"StopCardMotion");
        using var bitmap=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);using var g=Graphics.FromImage(bitmap);Call(form,"PaintScaled",g);
    }
    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        try
        {
            if(args.Contains("--future-only")){CheckFutureEdition();Console.WriteLine($"{checks} ORBIT checks passed without showing windows.");return 0;}
            if(args.Contains("--render-audit-only")){CheckAnimationContinuity();Console.WriteLine($"{checks} animation audit checks passed, without showing windows.");return 0;}
            if(args.Contains("--input-audit-only")){CheckImmediateInput();Console.WriteLine($"{checks} immediate-input checks passed, without showing windows.");return 0;}
            if(args.Contains("--caption-only")){CheckCaptionRendering();CheckFidelityFixes();Console.WriteLine($"{checks} caption checks passed, without showing windows.");return 0;}
            if(args.Contains("--fidelity-only")){CheckFidelityFixes();Console.WriteLine($"{checks} fidelity checks passed, without showing windows.");return 0;}
            if(args.Contains("--partial-only")){CheckPartialFrames();Console.WriteLine($"{checks} repaint checks passed, without showing windows.");return 0;}
            if(args.Contains("--period-only")){CheckPeriodPresentation();Console.WriteLine($"{checks} period UI checks passed, without showing windows.");return 0;}
            var hashes=new HashSet<string>();
            foreach(Era era in GameCatalog.HistoricalEras)
            {
                using var skin=new Skin(era);using var bitmap=new Bitmap(420,260);
                using(var g=Graphics.FromImage(bitmap)){skin.Configure(g);skin.Frame(g,new(0,0,420,260),"Solitaire");}
                using var bytes=new MemoryStream();bitmap.Save(bytes,System.Drawing.Imaging.ImageFormat.Png);
                Check(hashes.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes.ToArray()))),"Duplicate frame: "+era);
                Check(bitmap.GetPixel(0,0).A==(skin.Xp || skin.Vista?0:255),"Incorrect window silhouette: "+era);
                if(skin.Early || era is Era.Windows95 or Era.Windows98 or Era.WindowsMe)Check(skin.BitmapFontAvailable,"Historical Windows bitmap font missing: "+era);
                if(skin.BitmapFontAvailable)
                {
                    using var unicode=new Bitmap(200,30);using var ascii=new Bitmap(200,30);
                    using(var g=Graphics.FromImage(unicode)){skin.Configure(g);skin.Text(g,"Deck… — 7–11 “A”",new(0,0,200,30));}
                    using(var g=Graphics.FromImage(ascii)){skin.Configure(g);skin.Text(g,"Deck... - 7-11 \"A\"",new(0,0,200,30));}
                    bool same=true;for(int y=0;y<30;y++)for(int x=0;x<200;x++)if(unicode.GetPixel(x,y)!=ascii.GetPixel(x,y))same=false;
                    Check(same,"Unsupported raster punctuation produced unreadable glyphs: "+era);
                }
                Color caption=bitmap.GetPixel(205,skin.Border+3);
                using(var g=Graphics.FromImage(bitmap))skin.Frame(g,new(0,0,420,260),"Solitaire",active:false);
                Check(bitmap.GetPixel(205,skin.Border+3)!=caption,"Inactive caption did not change: "+era);
                using var form=new GameWindow(new Store("artifacts/ui-test-state"),era,1989,150,true);
                form.Game.Draw();string initial=JsonSerializer.Serialize(form.Game.State);
                Paint(form);Click(form,Hit(form,"settings"));Paint(form);
                var row=Hit(form,"dialog-era-"+(int)Era.WindowsXP);Click(form,row,false);
                Check(((Preferences)Field(form,"draft")!).Era==era,"Selection executed before mouse release: "+era);
                // Releasing outside a pressed control must cancel it.
                Call(form,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,1,1,0));
                Check(((Preferences)Field(form,"draft")!).Era==era,"Outside release applied selection: "+era);
                Click(form,row);Paint(form);
                Check(((Preferences)Field(form,"draft")!).Era==Era.WindowsXP,"Era list click failed: "+era);
                Click(form,Hit(form,"dialog-ok"));Paint(form);
                Check(form.Preferences.Era==Era.WindowsXP,"Settings did not apply: "+era);
                Check(form.Region!=null && !form.Region.IsVisible(0,0),"Luna window region did not update: "+era);
                Call(form,"SwitchGame",GameCatalog.Defaults(era,GameKind.Klondike));
                Check(JsonSerializer.Serialize(form.Game.State)==initial,"Era round trip lost its saved deal: "+era);
                Call(form,"SwitchGame",GameCatalog.Defaults(Era.WindowsXP,GameKind.Klondike));
                Call(form,"OpenDialog",DialogPage.Options);Paint(form);
                Check(!HasControl(form,"dialog-more"),"Modern options expansion is still present");
                // Modal caption dragging changes the dialog, never the underlying game.
                var bounds=(RectangleF)Field(form,"dialogBounds")!;float scale=form.Preferences.Scale/100f;
                int sx=(int)((bounds.X+bounds.Width/2)*scale),sy=(int)((bounds.Y+14)*scale);
                Call(form,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,sx,sy,0));
                Call(form,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,0,sx+20,sy+20,0));
                Call(form,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,sx+20,sy+20,0));
                Check((PointF)Field(form,"dialogOffset")! != PointF.Empty,"Modal dragging failed: "+era);
                Call(form,"CloseDialog");
                Call(form,"ToggleMaximize");Check((bool)Field(form,"maximized")!,"Maximize failed");
                Check(form.Region!.IsVisible(0,0),"Maximized window retained rounded cutouts");
                Call(form,"ToggleMaximize");Check(!(bool)Field(form,"maximized")!,"Restore failed");
                Console.WriteLine("PASS "+era+" frame, typography, inactive state, input, era switch, dialog drag, maximize/restore");
            }
            CheckGameInteractions();CheckDoubleClicks();CheckFeel();CheckPartialFrames();CheckPeriodPresentation();CheckCaptionRendering();CheckFidelityFixes();CheckAnimationContinuity();CheckImmediateInput();CheckFutureEdition();
            Console.WriteLine($"{checks} UI checks passed. No windows were shown and no desktop input was sent.");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
    private static void Key(GameWindow form,Keys key)
    {
        object[] args=[new Message(),key];typeof(GameWindow).GetMethod("ProcessCmdKey",Private)!.Invoke(form,args);
    }
    private static RectangleF Area(GameWindow form,Position pos)
    {
        foreach(var area in (IEnumerable<(Position Position,RectangleF Rect)>)Field(form,"cardAreas")!)
            if(area.Position.Kind==pos.Kind && area.Position.Pile==pos.Pile && (pos.Index<0 || area.Position.Index==pos.Index))return area.Rect;
        throw new Exception("Missing card area: "+pos);
    }
    private static void CheckGameInteractions()
    {
        foreach(var era in GameCatalog.HistoricalEras)foreach(var kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))
        {
            using var form=new GameWindow(new Store("artifacts/ui-variant-state"),era,1,100,true,kind);Paint(form);
            Check(form.Game.Rules.Kind==kind,"Wrong game selected");
            Check(form.Game.State.Tableau.Count==(kind==GameKind.Klondike?7:kind==GameKind.FreeCell?8:10),"Wrong board");
            Call(form,"OpenDialog",DialogPage.Settings);Paint(form);
            foreach(var choice in Enum.GetValues<GameKind>())
            {
                bool found=((System.Collections.IEnumerable)Field(form,"hotspots")!).Cast<object>().Any(h=>(string)h.GetType().GetProperty("Id")!.GetValue(h)! == "dialog-game-"+choice);
                Check(found==GameCatalog.Available(era,choice),"Settings offered an unbundled game");
            }
            Call(form,"CloseDialog");Paint(form);
            if(kind==GameKind.FreeCell)
            {
                // The exposed bottom card in deal #1, column 1 is 6S. It cannot go home automatically.
                var source=Area(form,new(PileKind.Tableau,0,6));
                var dest=(RectangleF)typeof(GameWindow).GetMethod("CellRect",Private)!.Invoke(form,[0,false])!;
                Click(form,source);Paint(form);Click(form,dest);Paint(form);
                Check(form.Game.State.FreeCells[0].Count==1 && form.Game.State.FreeCells[0][0].Rank==6,"FreeCell click-to-cell failed");
                Key(form,Keys.Control|Keys.Z);Paint(form);
                Check(form.Game.State.FreeCells.All(p=>p.Count==0),"FreeCell undo failed");
                // Production drag events back up the click-to-move path.
                int sx=(int)(source.X+source.Width/2),sy=(int)(source.Y+source.Height/2),dx=(int)(dest.X+dest.Width/2),dy=(int)(dest.Y+dest.Height/2);
                Call(form,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,sx,sy,0));
                Call(form,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,0,dx,dy,0));Paint(form);
                Call(form,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,dx,dy,0));Paint(form);
                Check(form.Game.State.FreeCells[0].Count==(era==Era.WindowsVista?1:0),"FreeCell did not follow its era's click/drag interaction");
                Key(form,Keys.F3);Paint(form);Key(form,Keys.Control|Keys.A);
                foreach(char digit in "11982")Key(form,Keys.D0+(digit-'0'));
                Paint(form);Key(form,Keys.Enter);Paint(form);
                Check(form.Game.State.Seed==11982 && form.Game.State.Moves==0,"Numbered game input failed");
            }
            else if(kind==GameKind.Spider)
            {
                Call(form,"DrawCards");Paint(form);Check(form.Game.State.Stock.Count==40 && form.Game.State.Moves==1,"Spider deal row failed");
                Key(form,Keys.Control|Keys.Z);Paint(form);Check(form.Game.State.Stock.Count==(era==Era.WindowsVista?50:40),"Spider UI undo did not respect the era's deal boundary");
                Key(form,era==Era.WindowsVista?Keys.F5:Keys.F3);Paint(form);Click(form,Hit(form,"dialog-suits-4"));Paint(form);Click(form,Hit(form,"dialog-ok"));Paint(form);
                Check(form.Game.Rules.SpiderSuits==4 && form.Game.State.Stock.Count==50,"Spider difficulty did not redeal");
                Check(form.Game.State.Tableau.SelectMany(p=>p).Concat(form.Game.State.Stock).Select(c=>c.Suit).Distinct().Count()==4,"Wrong Spider suits");
            }
            else {Click(form,(RectangleF)typeof(GameWindow).GetProperty("StockRect",Private)!.GetValue(form)!);Paint(form);Check(form.Game.State.Stock.Count==21,"Klondike draw failed");}
            string initial=JsonSerializer.Serialize(form.Game.State);
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.WindowsVista,kind));
            Call(form,"SwitchGame",GameCatalog.Defaults(era,kind));
            Check(JsonSerializer.Serialize(form.Game.State)==initial,"Session round trip lost progress");
            Console.WriteLine("PASS "+era+" "+kind+" controls and session");
        }
        string dir=Path.GetFullPath("artifacts/ui-session-save-"+Guid.NewGuid().ToString("N"));
        string freeState,spiderState;
        using(var form=new GameWindow(new Store(dir),Era.WindowsXP,1,100,kind:GameKind.FreeCell))
        {
            Paint(form);Call(form,"Changed",form.Game.ToFreeCell(new(PileKind.Tableau,0)));freeState=JsonSerializer.Serialize(form.Game.State);
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.WindowsMe,GameKind.Spider));Call(form,"DrawCards");spiderState=JsonSerializer.Serialize(form.Game.State);
        }
        using(var form=new GameWindow(new Store(dir)))
        {
            Check(form.Game.Rules.Kind==GameKind.Spider && JsonSerializer.Serialize(form.Game.State)==spiderState,"Current game did not resume");
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.WindowsXP,GameKind.FreeCell));
            Check(JsonSerializer.Serialize(form.Game.State)==freeState && form.Game.CanUndo,"Stored FreeCell did not resume with undo");
        }
        using(var form=new GameWindow(new Store("artifacts/ui-vegas"),Era.WindowsXP,1,100,true))
        {
            form.Game.State.Score=100;Call(form,"OpenDialog",DialogPage.Options);Paint(form);
            Click(form,Hit(form,"dialog-score1"));Paint(form);Click(form,Hit(form,"dialog-keep-score"));Paint(form);Click(form,Hit(form,"dialog-ok"));
            Check(form.Game.State.Score==-52,"Standard points leaked into the Vegas balance");
            Call(form,"NewGame",false,null!);Check(form.Game.State.Score==-104,"Vegas keep score did not carry the balance");
            form.Game.Draw();form.Game.Draw();Check(form.Game.Undo() && !form.Game.Undo(),"Classic Solitaire allowed more than one undo");
        }
        using(var art=new CardArt())
        {
            string BackHash(int frame,int elapsed,bool timed,Era era)
            {
                art.AnimationFrame=frame;art.GameElapsed=elapsed;art.TimedBacks=timed;
                using var b=new Bitmap(71,96);using(var g=Graphics.FromImage(b))art.Draw(g,new Card(0,false),new(0,0,71,96),era,6);
                using var bytes=new MemoryStream();b.Save(bytes,System.Drawing.Imaging.ImageFormat.Png);return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes.ToArray()));
            }
            Check(BackHash(0,1,true,Era.Windows31)!=BackHash(1,1,true,Era.Windows31),"Robot back did not animate");
            Check(BackHash(0,1,false,Era.Windows31)==BackHash(1,1,false,Era.Windows31),"Untimed card back animated");
            Check(BackHash(0,1,true,Era.WindowsXP)==BackHash(1,1,true,Era.WindowsXP),"XP photographic back used an earlier animation");
        }
    }
    private static void DoubleClick(GameWindow form,RectangleF r,bool repaint=true)
    {
        int x=(int)((r.X+r.Width/2)*form.Preferences.Scale/100),y=(int)((r.Y+r.Height/2)*form.Preferences.Scale/100);
        Call(form,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,x,y,0));
        Call(form,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,x,y,0));
        if(repaint)Paint(form,false);
        Call(form,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,2,x,y,0));
        Call(form,"OnMouseDoubleClick",new MouseEventArgs(MouseButtons.Left,2,x,y,0));
        Call(form,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,2,x,y,0));
        Paint(form);
    }
    private static void CheckDoubleClicks()
    {
        foreach(var era in GameCatalog.HistoricalEras)foreach(var kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))
        foreach(int scale in new[]{100,125,150,200})foreach(bool animate in new[]{false,true})
        {
            using var form=new GameWindow(new Store("artifacts/double-click-test"),era,1,scale,true,kind);
            form.Preferences.Animate=animate;Paint(form);
            string label=$"{era}/{kind}/{scale}/{animate}";
            Check(!form.Preferences.QuickControls,"Original layout should default to menu-only: "+label);
            if(kind!=GameKind.FreeCell)
            {
                var stock=(RectangleF)typeof(GameWindow).GetProperty("StockRect",Private)!.GetValue(form)!;
                int count=form.Game.State.Stock.Count;
                DoubleClick(form,stock,repaint:animate);
                Check(form.Game.State.Stock.Count==count-(kind==GameKind.Spider?10:3) && form.Game.State.Moves==1,"Double-click drew more than one packet: "+label);
                // A rapid second click before a repaint must also tolerate stale hit rectangles.
                DoubleClick(form,stock,false);
                Check(form.Game.State.Moves==2,"Rapid double-click repeated a draw: "+label);
            }
            if(kind==GameKind.Spider)continue;
            var s=new GameState{Kind=kind,Seed=1,Tableau=Enumerable.Range(0,kind==GameKind.FreeCell?8:7).Select(_=>new List<Card>()).ToList()};
            int chosen=kind==GameKind.FreeCell && era!=Era.WindowsVista?44:0; // 6S in classic FreeCell, AC otherwise.
            s.Tableau[0].Add(new(chosen));s.Tableau[1].Add(new(13));
            var remaining=Enumerable.Range(0,52).Where(i=>i!=chosen && i!=13).Select(i=>new Card(i,kind==GameKind.FreeCell)).ToList();
            if(kind==GameKind.FreeCell)s.Tableau[7]=remaining;else s.Stock=remaining;
            form.SetRenderState(s);Paint(form);
            DoubleClick(form,Area(form,new(PileKind.Tableau,0,0)));
            Check(form.Game.State.Moves==1 && form.Game.State.Tableau[0].Count==0,"Double-click did not move its chosen card: "+label);
            Check(form.Game.State.Tableau[1].Count==1 && form.Game.State.Tableau[1][0].Id==13,"Double-click also moved the other available ace: "+label);
            Check(kind==GameKind.FreeCell && era!=Era.WindowsVista?form.Game.State.FreeCells[0].Single().Id==chosen && form.Game.State.Foundations.All(p=>p.Count==0):form.Game.State.Foundations.Sum(p=>p.Count)==1,"Wrong double-click destination: "+label);
            Key(form,Keys.Control|Keys.Z);Paint(form);
            Check(form.Game.State.Tableau[0].Single().Id==chosen && form.Game.State.Tableau[1].Count==1,"Double-click undo changed unrelated cards: "+label);
            if(kind==GameKind.Klondike && era!=Era.WindowsVista)
            {
                s.Tableau[0][0]=s.Tableau[0][0] with{FaceUp=false};form.SetRenderState(s);Paint(form);
                DoubleClick(form,Area(form,new(PileKind.Tableau,0,0)));
                Check(form.Game.State.Tableau[0].Single().FaceUp && form.Game.State.Foundations.All(p=>p.Count==0) && form.Game.State.Moves==1,"Double-click flipped then automatically moved a hidden card: "+label);
                Call(form,"OpenDialog",DialogPage.Options);Paint(form);
                Click(form,Hit(form,"dialog-keep-score"));
                Check(!((Preferences)Field(form,"draft")!).Rules.KeepVegasScore,"Keep score was interactive outside Vegas: "+label);
            }
            else if(kind==GameKind.FreeCell && era!=Era.WindowsVista)
            {
                s.Tableau[0].Add(new(30));s.Tableau[7].RemoveAll(c=>c.Id==30);form.SetRenderState(s);Paint(form);
                DoubleClick(form,Area(form,new(PileKind.Tableau,0,0)));
                Check(form.Game.State.Moves==0 && form.Game.State.FreeCells.All(p=>p.Count==0),"Covered-card double-click moved a different bottom card: "+label);
            }
        }
        Console.WriteLine("PASS complete double-click event sequences across all 17 profiles, four sizes, with and without animation");
        foreach(var era in GameCatalog.HistoricalEras.Where(e=>e!=Era.WindowsVista))
        {
            using var form=new GameWindow(new Store("artifacts/deck-double-click"),era,1,150,true);
            Call(form,"OpenDialog",DialogPage.Deck);Paint(form);DoubleClick(form,Hit(form,"dialog-back-2"));
            Check((DialogPage)Field(form,"dialog")! == DialogPage.None && form.Preferences.CardBack==2,"Card-back double-click did not apply and close: "+era);
        }
        foreach(var era in new[]{Era.Windows95,Era.WindowsXP})
        {
            using var form=new GameWindow(new Store("artifacts/selection-inversion"),era,1,100,true,GameKind.FreeCell);Paint(form);
            var r=Area(form,new(PileKind.Tableau,0,6));
            using var before=new Bitmap(form.Width,form.Height);using(var g=Graphics.FromImage(before))Call(form,"PaintScaled",g);
            Click(form,r);Paint(form);
            using var after=new Bitmap(form.Width,form.Height);using(var g=Graphics.FromImage(after))Call(form,"PaintScaled",g);
            int inverted=0;
            for(int y=(int)r.Top+3;y<r.Bottom-3;y++)for(int x=(int)r.Left+3;x<r.Right-3;x++)
            {var a=before.GetPixel(x,y);var b=after.GetPixel(x,y);if(a.R+b.R==255 && a.G+b.G==255 && a.B+b.B==255)inverted++;}
            Check(inverted>(r.Width-6)*(r.Height-6)*.9,"Classic FreeCell selection is not a negative image: "+era);
            Directory.CreateDirectory("artifacts/selection-v050");after.Save($"artifacts/selection-v050/{era}.png");
        }
        foreach(var era in new[]{Era.WindowsMe,Era.WindowsXP,Era.WindowsVista})
        {
            using var form=new GameWindow(new Store("artifacts/difficulty-stats"),era,1,150,true,GameKind.Spider);Paint(form);Call(form,"DrawCards");
            Call(form,"OpenDialog",era==Era.WindowsVista?DialogPage.Options:DialogPage.Difficulty);Paint(form);Click(form,Hit(form,"dialog-suits-4"));Paint(form);Click(form,Hit(form,"dialog-ok"));Paint(form);
            Check(form.Statistics.Difficulties[1].Played==1 && !form.Statistics.Difficulties.ContainsKey(4),"Changing difficulty charged the wrong statistics bucket: "+era);
            Key(form,Keys.F4);Paint(form);Check((int)Field(form,"statisticsLevel")! == 4,"Statistics did not default to the current difficulty");
            Click(form,Hit(form,"dialog-stats-1"));Paint(form);Check((int)Field(form,"statisticsLevel")! == 1,"Difficulty tab did not change");
            Check(!HasControl(form,"dialog-stats-0"),"Modern Overall tab is still in Spider statistics");
        }
        string disposeDir=Path.GetFullPath("artifacts/double-dispose-"+Guid.NewGuid().ToString("N"));
        var disposed=new GameWindow(new Store(disposeDir),Era.WindowsXP,1,100,kind:GameKind.FreeCell);Paint(disposed);
        Call(disposed,"Changed",disposed.Game.ToFreeCell(new(PileKind.Tableau,0)));
        disposed.Dispose();
        // A late notification must not cause the second Dispose to flush an already disposed writer.
        Set(disposed,"saveDirty",true);disposed.Dispose();
        var recovered=new Store(disposeDir).Load();Check(recovered.Game?.FreeCells[0].Count==1,"Double disposal failed to retain the saved move");
    }
    private static void CheckFeel()
    {
        object? Flight(GameWindow f,int key)=>((System.Collections.IDictionary)Field(f,"flights")!)[key];
        RectangleF Rect(object pose)=>(RectangleF)pose.GetType().GetProperty("Rect")!.GetValue(pose)!;
        object Prop(object item,string name)=>item.GetType().GetProperty(name)!.GetValue(item)!;
        foreach(var era in new[]{Era.WindowsVista,Era.Future2126})
        {
            using var form=new GameWindow(new Store("artifacts/feel-test"),era,1,100,true);Paint(form);
            int key=form.Game.State.Stock[^1].Key;Key(form,Keys.Space);var flight=Flight(form,key);
            Check(flight!=null,"Stock draw did not animate: "+era);
            double start=(double)Prop(flight!,"Start"),duration=(double)Prop(flight!,"Duration");
            var atStart=flight!.GetType().GetMethod("Sample")!.Invoke(flight,[start])!;
            var atMid=flight.GetType().GetMethod("Sample")!.Invoke(flight,[start+duration*.5])!;
            var atEnd=flight.GetType().GetMethod("Sample")!.Invoke(flight,[start+duration])!;
            float Center(object pose)=>Rect(pose).X+Rect(pose).Width/2;
            Check(Center(atStart)<Center(atMid) && Center(atMid)<=Center(atEnd),"Card did not move continuously from stock to waste");
            Check(Rect(atMid).Width<Rect(atEnd).Width,"Stock card did not turn over");
            Check(!(bool)Prop(Prop(atStart,"Card"),"FaceUp") && (bool)Prop(Prop(atEnd,"Card"),"FaceUp"),"Flip revealed the wrong face");
            Set(form,"renderMotionTime",start+duration*.5);
            Key(form,Keys.Control|Keys.Z);Check(form.Game.State.Stock.Count==24,"Undo during motion failed");
            var reversed=Flight(form,key)!;var reversedStart=reversed.GetType().GetMethod("Sample")!.Invoke(reversed,[start+duration*.5])!;
            Check(Rect(reversedStart)==Rect(atMid),"Undo snapped the moving card to a different position or width");Set(form,"renderMotionTime",null);
            Call(form,"StopCardMotion");form.Preferences.Animate=false;Key(form,Keys.Space);
            Check(((System.Collections.IDictionary)Field(form,"flights")!).Count==0,"Reduced motion still animated");
            Key(form,Keys.F8);Paint(form);Check((DialogPage)Field(form,"dialog")! == DialogPage.None,"Removed Feel shortcut still opens a dialog");
        }
        using(var form=new GameWindow(new Store("artifacts/feel-drag"),Era.WindowsVista,1,100,true,GameKind.FreeCell))
        {
            Paint(form);var source=Area(form,new(PileKind.Tableau,0,6));string before=JsonSerializer.Serialize(form.Game.State);int key=form.Game.State.Tableau[0][6].Key;
            int sx=(int)(source.X+source.Width/2),sy=(int)(source.Y+source.Height/2);
            Call(form,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,sx,sy,0));
            Call(form,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,0,500,460,0));Paint(form,false);
            Call(form,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,500,460,0));
            Check(JsonSerializer.Serialize(form.Game.State)==before,"Invalid drag altered the game");
            var flight=Flight(form,key);Check(flight!=null,"Invalid drop did not animate home");
            Check(Rect(Prop(flight!,"From")).X>400 && Rect(Prop(flight!,"To")).X<60,"Invalid drop started at the stock instead of the pointer");
            Paint(form);source=Area(form,new(PileKind.Tableau,0,6));
            var dest=(RectangleF)typeof(GameWindow).GetMethod("CellRect",Private)!.Invoke(form,[0,false])!;
            int dx=(int)(dest.X+dest.Width/2+8),dy=(int)(dest.Y+dest.Height/2);
            Call(form,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,sx,sy,0));
            Call(form,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,0,dx,dy,0));Paint(form,false);
            Call(form,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,dx,dy,0));
            Check(form.Game.State.FreeCells[0].Count==1,"Valid drag stopped working");
            flight=Flight(form,key);Check(flight!=null && Math.Abs(Rect(Prop(flight!,"From")).X-(dest.X+8))<2,"Drop settling lost the pointer position");
        }
        Console.WriteLine("PASS motion endpoints, flips, interrupted undo, invalid-return and drop settling, reduced motion and settings");
    }
    private static void CheckPartialFrames()
    {
        byte[] Pixels(Bitmap b)
        {
            var bits=b.LockBits(new(0,0,b.Width,b.Height),System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try{var bytes=new byte[bits.Stride*bits.Height];System.Runtime.InteropServices.Marshal.Copy(bits.Scan0,bytes,0,bytes.Length);return bytes;}
            finally{b.UnlockBits(bits);}
        }
        foreach(var era in new[]{Era.Windows95,Era.WindowsXP,Era.WindowsVista})foreach(int scale in new[]{100,125,150,200})
        {
            using var form=new GameWindow(new Store("artifacts/partial-frames"),era,1,scale,true,GameKind.Klondike);
            Call(form,"StopCardMotion");Set(form,"renderMotionTime",10.0);
            using var incremental=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);
            void Render(Bitmap b,Rectangle? clip=null){using var g=Graphics.FromImage(b);if(clip.HasValue)g.SetClip(clip.Value);Call(form,"PaintScaled",g);}
            Render(incremental);var source=Area(form,new(PileKind.Tableau,0,0));float density=scale/100f;
            int sx=(int)((source.X+source.Width/2)*density),sy=(int)((source.Y+source.Height/2)*density);
            Call(form,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,sx,sy,0));Render(incremental);
            foreach(var point in new[]{new PointF(150,350),new PointF(550,400),new PointF(620,460),new PointF(610,450)})
            {
                Call(form,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,0,(int)(point.X*density),(int)(point.Y*density),0));
                Check((bool)Field(form,"dragging")!,"Partial repaint check did not exercise dragging");
                var damage=(Rectangle)typeof(GameWindow).GetMethod("FrameDamage",Private)!.Invoke(form,null)!;
                Call(form,"Animate");Render(incremental,damage);
                using var full=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);Render(full);
                bool identical=Pixels(incremental).AsSpan().SequenceEqual(Pixels(full));
                if(!identical)
                {
                    Directory.CreateDirectory("artifacts/partial-frame-failure");
                    incremental.Save($"artifacts/partial-frame-failure/{era}-{scale}-partial.png");full.Save($"artifacts/partial-frame-failure/{era}-{scale}-full.png");
                }
                Check(identical,$"Partial repaint left stale pixels: {era} {scale}% at {point}");
            }
            Call(form,"CancelDrag");
        }
        Console.WriteLine("PASS partial drag frames match full renders at 100/125/150/200% in classic, XP and Vista");
    }
}
