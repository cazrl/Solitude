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
            Key(form,Keys.F6);Paint(form);Click(form,Hit(form,"dialog-era-8"));Paint(form);
            var orbitArea=Screen.FromControl(form).WorkingArea;
            Click(form,Hit(form,"dialog-ok"));Paint(form);
            Check(form.Left==orbitArea.Left+(orbitArea.Width-form.Width)/2 && form.Top==orbitArea.Top+(orbitArea.Height-form.Height)/2,"Selecting ORBIT does not center the resized window on its current monitor");
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
        CheckFuturePersistence();CheckFutureWin();CheckOrbitCardMotion();CheckFutureMotionInterruption();CheckFutureCardSeparation();CheckOrbitLogoMenu();CheckOrbitAutoPlacement();CheckOrbitSpacing();CheckOrbitStartupPlacement();CheckEditionMorph();CheckGameClock();CheckOrbitAuditFixes();CheckOrbitPolish();
        Console.WriteLine("PASS ORBIT palettes, input, frame continuity, reduced motion, saved sessions and card victory");
    }
    private static void CheckOrbitLogoMenu()
    {
        Directory.CreateDirectory("artifacts/orbit-logo");
        foreach(int scale in new[]{100,125,150,200})
        {
            using var form=new GameWindow(new Store("artifacts/orbit-logo-state"),Era.Future2126,1989,scale,true);Paint(form);
            var logo=Hit(form,"future-menu");Check(logo.Left==0 && logo.Top==0 && logo.Width==36 && logo.Height==36,"Game menu does not fit flush into the window corner");
            Check(!HasControl(form,"system"),"ORBIT has duplicate corner menu targets");
            Check((bool)typeof(GameWindow).GetMethod("CaptionControlAt",Private)!.Invoke(form,[new PointF(logo.Left+1,logo.Top+1)])!,"Logo button edge starts window dragging");
            Set(form,"renderMotionTime",100.0);Call(form,"OnMouseMove",new MouseEventArgs(MouseButtons.None,0,(int)((logo.X+15)*scale/100),(int)((logo.Y+15)*scale/100),0));
            Check((bool)typeof(GameWindow).GetProperty("NeedsFrames",Private)!.GetValue(form)!,"Hover does not schedule the orbital logo animation");
            using var first=new Bitmap(form.Width,form.Height);using var next=new Bitmap(form.Width,form.Height);
            DrawFrame(form,first);Set(form,"renderMotionTime",100.6);DrawFrame(form,next);
            Check(!FramePixels(first).AsSpan().SequenceEqual(FramePixels(next)),"Hovered logo remains static");
            if(scale==150)next.Save("artifacts/orbit-logo/hover.png");
            Click(form,logo);Paint(form);Check((int)Field(form,"menu")! == 0,"Corner logo does not open the Game menu");
            var item=Hit(form,"menu-item-0");Check(item.Left<logo.Right && item.Top<80,"Game menu is not anchored under its logo");
            Call(form,"OnMouseLeave",EventArgs.Empty);
            Check((bool)typeof(GameWindow).GetProperty("NeedsFrames",Private)!.GetValue(form)!,"Logo stops orbiting while its menu is open");
            Paint(form);if(scale==150){DrawFrame(form,next);next.Save("artifacts/orbit-logo/open-menu.png");}
            Click(form,Hit(form,"future-menu"));Paint(form);Check((int)Field(form,"menu")! == -1,"Clicking the logo again does not close the menu");
            Call(form,"OnMouseLeave",EventArgs.Empty);
            Check(!(bool)typeof(GameWindow).GetProperty("NeedsFrames",Private)!.GetValue(form)!,"Logo keeps requesting frames when idle");
            form.Preferences.Animate=false;Call(form,"OnMouseMove",new MouseEventArgs(MouseButtons.None,0,(int)((logo.X+15)*scale/100),(int)((logo.Y+15)*scale/100),0));
            DrawFrame(form,first);Set(form,"renderMotionTime",103.0);DrawFrame(form,next);SameFrame(first,next,"logo-reduced-motion-"+scale);
            Check(!(bool)typeof(GameWindow).GetProperty("NeedsFrames",Private)!.GetValue(form)!,"Reduced motion still rotates the logo");
            Key(form,Keys.Alt|Keys.G);Paint(form);Check((int)Field(form,"menu")! == 0,"Game-menu keyboard shortcut broke");Key(form,Keys.Escape);
            Key(form,Keys.Alt|Keys.Space);Paint(form);Check((int)Field(form,"menu")! == 2,"Window-menu keyboard shortcut broke");Key(form,Keys.Escape);
        }
        Console.WriteLine("PASS ORBIT logo menu: corner target, animation on hover/open, anchored menu, toggle, idle stop, keyboard and reduced motion");
    }
    private static void CheckOrbitAutoPlacement()
    {
        static void CompleteDeck(GameState state)
        {
            var used=state.Tableau.Concat(state.Foundations).SelectMany(p=>p).Concat(state.Waste).Select(c=>c.Id).ToHashSet();
            state.Stock=Enumerable.Range(0,52).Where(i=>!used.Contains(i)).Select(i=>new Card(i,false)).ToList();
        }
        using(var form=new GameWindow(new Store("artifacts/orbit-auto-place"),Era.Future2126,1989,100,true))
        foreach(int rank in Enumerable.Range(1,13))
        {
            var state=new GameState{Seed=1};state.Foundations[0]=Enumerable.Range(0,rank-1).Select(i=>new Card(i)).ToList();
            state.Tableau[0]=[new(rank-1)];state.Tableau[2]=[new(13)];
            if(rank<13)state.Tableau[1]=[new(13+rank)];CompleteDeck(state);form.SetRenderState(state);Paint(form);
            DoubleClick(form,Area(form,new(PileKind.Tableau,0,0)));
            Check(form.Game.State.Foundations[0].Count==rank && form.Game.State.Foundations[0][^1].Id==rank-1,"Double-click did not prioritize the eligible foundation for rank "+rank);
            Check(form.Game.State.Moves==1 && form.Game.State.Tableau[2].Single().Id==13,"Double-click chained another available foundation move");
        }
        foreach(int scale in new[]{100,125,150,200})foreach(bool animate in new[]{false,true})
        {
            using var form=new GameWindow(new Store("artifacts/orbit-auto-place"),Era.Future2126,1989,scale,true);form.Preferences.Animate=animate;
            var state=new GameState{Seed=1};state.Tableau[0]=[new(12,false),new(34)];state.Tableau[1]=[new(9)];state.Tableau[2]=[new(48)];state.Tableau[3]=[new(13)];CompleteDeck(state);
            form.SetRenderState(state);Paint(form);DoubleClick(form,Area(form,new(PileKind.Tableau,0,1)));
            Check(form.Game.State.Tableau[1].Select(c=>c.Id).SequenceEqual(new[]{9,34}) && form.Game.State.Tableau[2].Single().Id==48,"Double-click did not choose one deterministic legal column");
            Check(form.Game.State.Moves==1 && form.Game.State.Foundations.All(p=>p.Count==0) && form.Game.State.Tableau[0].Single().FaceUp,"Double-click did not move exactly once and reveal its source");
            Key(form,Keys.Control|Keys.Z);Paint(form);Check(form.Game.State.Tableau[0].Select(c=>c.Id).SequenceEqual(new[]{12,34}) && !form.Game.State.Tableau[0][0].FaceUp,"Auto-place Undo did not restore the source");
            state=new GameState{Seed=1};state.Tableau[0]=[new(34),new(7),new(32)];state.Tableau[1]=[new(9)];CompleteDeck(state);form.SetRenderState(state);Paint(form);
            DoubleClick(form,Area(form,new(PileKind.Tableau,0,0)));
            Check(form.Game.State.Tableau[1].Select(c=>c.Id).SequenceEqual(new[]{9,34,7,32}) && form.Game.State.Moves==1,"Double-click did not move the chosen descending alternating-color sequence");
            state=new GameState{Seed=1,Waste=[new(34)],WasteFan=1};state.Tableau[1]=[new(9)];CompleteDeck(state);form.SetRenderState(state);Paint(form);DoubleClick(form,Area(form,new(PileKind.Waste,0,0)));
            Check(form.Game.State.Waste.Count==0 && form.Game.State.Tableau[1][^1].Id==34 && form.Game.State.Moves==1,"Double-click did not auto-place from the waste");
            state=new GameState{Seed=1};state.Tableau[0]=[new(0,false),new(38)];CompleteDeck(state);form.SetRenderState(state);Paint(form);DoubleClick(form,Area(form,new(PileKind.Tableau,0,1)));
            Check(form.Game.State.Tableau[1].Single().Id==38 && form.Game.State.Tableau[0].Single().FaceUp,"Double-click did not move a King to an empty column to reveal a card");
            state=new GameState{Seed=1};state.Tableau[0]=[new(38)];CompleteDeck(state);form.SetRenderState(state);Paint(form);DoubleClick(form,Area(form,new(PileKind.Tableau,0,0)));
            Check(form.Game.State.Moves==0 && form.Game.State.Tableau[0].Single().Id==38,"Double-click pointlessly shuffled a whole King column to an empty slot");
            state=new GameState{Seed=1};state.Tableau[0]=[new(34)];state.Tableau[1]=[new(35)];CompleteDeck(state);form.SetRenderState(state);Paint(form);DoubleClick(form,Area(form,new(PileKind.Tableau,0,0)));
            Check(form.Game.State.Moves==0,"Double-click accepted a same-color tableau move");
        }
        foreach(var era in GameCatalog.HistoricalEras)
        {
            using var form=new GameWindow(new Store("artifacts/historical-auto-place"),era,1989,100,true);
            var state=new GameState{Seed=1};state.Tableau[0]=[new(34)];state.Tableau[1]=[new(9)];CompleteDeck(state);form.SetRenderState(state);Paint(form);DoubleClick(form,Area(form,new(PileKind.Tableau,0,0)));
            Check(form.Game.State.Moves==0,"ORBIT tableau auto-placement leaked into historical Solitaire");
        }
        Console.WriteLine("PASS ORBIT double-click: Ace-to-King foundation priority, single tableau/waste/sequence move, legal targets, Undo and historical isolation");
    }
    private static void CheckEditionMorph()
    {
        Directory.CreateDirectory("artifacts/orbit-morph");
        foreach(int scale in new[]{100,150,200})
        foreach(var (era,maximized) in new[]{(Era.Windows30,false),(Era.WindowsVista,false),(Era.WindowsVista,true)})
        {
            using var form=new GameWindow(new Store("artifacts/orbit-morph-state"),era,1989,scale,true);
            Call(form,"OnLoad",EventArgs.Empty);if(maximized)Call(form,"ToggleMaximize");Paint(form);
            using var before=new Bitmap(form.Width,form.Height);DrawFrame(form,before);var from=form.Bounds;
            var area=Screen.FromControl(form).WorkingArea;
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.Future2126,GameKind.Klondike,scale));var destination=form.Bounds;
            string state=JsonSerializer.Serialize(form.Game.State);
            Set(form,"renderMotionTime",100.0);Call(form,"BeginEditionMorph",before,from,area,form.Preferences.Animate,Color.Cyan);
            Check(form.Bounds==from,"ORBIT morph jumped to its destination before animating");
            Key(form,Keys.Space);Call(form,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,100,200,0));
            Check(JsonSerializer.Serialize(form.Game.State)==state,"ORBIT morph accepted gameplay input");
            Check(form.AccessibilityObject.GetChildCount()==0,"ORBIT exposes stale card actions during its morph");
            int previousWidth=form.Width;
            for(int frame=0;frame<=30;frame++)
            {
                Set(form,"renderMotionTime",100.0+frame*.9/30);Call(form,"UpdateEditionMorph");
                bool within=from.Width<=destination.Width?form.Width>=previousWidth && form.Width<=destination.Width:form.Width<=previousWidth && form.Width>=destination.Width;
                Check(within && area.Contains(form.Bounds),"ORBIT morph overshot or left the working area");previousWidth=form.Width;
                using var image=new Bitmap(form.Width,form.Height);using var repeated=new Bitmap(form.Width,form.Height);
                DrawFrame(form,image);DrawFrame(form,repeated);SameFrame(image,repeated,$"edition-morph-{era}-{scale}-{frame}");
                if(era==Era.WindowsVista && scale==100 && !maximized)image.Save($"artifacts/orbit-morph/frame-{frame:000}.png");
            }
            Set(form,"renderMotionTime",101.0);Call(form,"UpdateEditionMorph");
            Check(Field(form,"editionMorph")==null && form.Bounds==destination,"ORBIT morph did not finish at its centered destination");
            Check(JsonSerializer.Serialize(form.Game.State)==state,"ORBIT morph altered the deal");
            Check(!(bool)typeof(GameWindow).GetProperty("NeedsFrames",Private)!.GetValue(form)!,"ORBIT morph leaves idle animation running");
            using(var actual=new Bitmap(form.Width,form.Height))using(var expected=new Bitmap(form.Width,form.Height))
            {DrawFrame(form,actual);Set(form,"boardStamp",null);DrawFrame(form,expected);SameFrame(actual,expected,$"edition-morph-settled-{era}-{scale}");}
            Set(form,"renderMotionTime",110.0);Call(form,"BeginEditionMorph",before,from,area,form.Preferences.Animate,Color.Cyan);Key(form,Keys.Escape);
            Check(Field(form,"editionMorph")==null && form.Bounds==destination,"Escape did not settle the ORBIT morph");
            Call(form,"BeginEditionMorph",before,from,area,form.Preferences.Animate,Color.Cyan);Call(form,"OnDeactivate",EventArgs.Empty);
            Check(Field(form,"editionMorph")==null && form.Bounds==destination,"Deactivation stranded a partially sized window");
            form.Preferences.Animate=false;Call(form,"BeginEditionMorph",before,from,area,form.Preferences.Animate,Color.Cyan);
            Check(Field(form,"editionMorph")==null && form.Bounds==destination,"Reduced motion still animates the edition change");
        }
        Console.WriteLine("PASS ORBIT morph: bounded window trajectory, repeatable frames, input isolation, final handoff, Escape, focus loss and reduced motion");
        CheckReverseEditionMorph();
    }
    private static void CheckReverseEditionMorph()
    {
        foreach(int scale in new[]{100,125,150,200})
        foreach(var era in GameCatalog.HistoricalEras)
        foreach(var kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))
        {
            using var form=new GameWindow(new Store("artifacts/reverse-morph-state"),era,1989,scale,true,kind);
            Call(form,"OnLoad",EventArgs.Empty);var area=Screen.FromControl(form).WorkingArea;var destination=form.Bounds;
            string original=JsonSerializer.Serialize(form.Game.State);
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.Future2126,GameKind.Klondike,scale));Paint(form);
            var from=form.Bounds;using var before=new Bitmap(form.Width,form.Height);DrawFrame(form,before);
            Call(form,"SwitchGame",GameCatalog.Defaults(era,kind,scale));
            Check(JsonSerializer.Serialize(form.Game.State)==original,"Returning from ORBIT changed the suspended historical deal");
            // Historical deal-animation settings must not cancel the departing
            // ORBIT experience's enabled edition transition.
            form.Preferences.Animate=false;Set(form,"renderMotionTime",200.0);
            Call(form,"BeginEditionMorph",before,from,area,true,Color.MediumPurple);
            var morph=Field(form,"editionMorph")!;
            Check(morph!=null && (bool)morph.GetType().GetProperty("Reverse")!.GetValue(morph)! && (Color)morph.GetType().GetProperty("Accent")!.GetValue(morph)! == Color.MediumPurple,"Reverse morph omitted direction or departing palette");
            foreach(double t in new[]{.3,.6})
            {
                Set(form,"renderMotionTime",200.0+t);Call(form,"UpdateEditionMorph");
                Check(Field(form,"editionMorph")!=null && area.Contains(form.Bounds),"Reverse morph snapped early or escaped the monitor");
                Key(form,Keys.Space);using var frame=new Bitmap(form.Width,form.Height);DrawFrame(form,frame);
                if(era==Era.WindowsVista && kind==GameKind.Klondike && scale==100)frame.Save($"artifacts/orbit-morph/reverse-{t:F1}.png");
            }
            Set(form,"renderMotionTime",201.0);Call(form,"UpdateEditionMorph");
            Check(Field(form,"editionMorph")==null && form.Bounds==destination,"Reverse morph missed the centered historical bounds");
            Check(JsonSerializer.Serialize(form.Game.State)==original,"Reverse morph accepted input or changed the saved deal");
            using(var frame=new Bitmap(form.Width,form.Height))using(var expected=new Bitmap(form.Width,form.Height))
            {DrawFrame(form,frame);Set(form,"boardStamp",null);DrawFrame(form,expected);SameFrame(frame,expected,$"reverse-morph-final-{era}-{kind}-{scale}");}
            Set(form,"renderMotionTime",210.0);Call(form,"BeginEditionMorph",before,from,area,true,Color.MediumPurple);Key(form,Keys.Escape);
            Check(Field(form,"editionMorph")==null && form.Bounds==destination,"Escape stranded the reverse morph");
            Call(form,"BeginEditionMorph",before,from,area,false,Color.MediumPurple);
            Check(Field(form,"editionMorph")==null && form.Bounds==destination,"Disabled ORBIT motion still animated the return");
        }
        Console.WriteLine("PASS reverse ORBIT morph to every historical game at four scales, with palette, suspended deals, final handoff and reduced motion preserved");
    }
    private static void CheckOrbitStartupPlacement()
    {
        foreach(int scale in new[]{100,125,150,200})
        foreach(var area in new[]{new Rectangle(0,0,2560,1392),new Rectangle(0,0,1366,728),new Rectangle(-1920,40,1920,1040),new Rectangle(2560,-1080,1920,1040)})
        {
            using var form=new GameWindow(new Store("artifacts/orbit-startup-state"),Era.Future2126,1989,scale,true);
            // Reproduce the old native handle's default-size placement, then
            // exercise Load rather than bypassing startup through SwitchGame.
            form.Location=new(area.Left+(area.Width-300)/2,area.Top+(area.Height-300)/2);
            Set(form,"startupWorkingArea",(Rectangle?)area);Call(form,"OnLoad",EventArgs.Empty);
            Check(area.Contains(form.Bounds),"Loaded ORBIT window extends outside its target working area");
            Check(Math.Abs((form.Left-area.Left)-(area.Right-form.Right))<=1 && Math.Abs((form.Top-area.Top)-(area.Bottom-form.Bottom))<=1,"Loaded ORBIT centers the default window size instead of the final size");
            Check(Field(form,"startupWorkingArea")==null,"ORBIT retains its startup monitor after loading");
        }
        Console.WriteLine("PASS ORBIT startup placement: four scales, taskbar work areas, small screen and signed monitor origins");
        foreach(int scale in new[]{100,125,150,200})
        foreach(var era in GameCatalog.HistoricalEras)
        foreach(var kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))
        {
            using var form=new GameWindow(new Store("artifacts/edition-placement-state"),era,1989,scale,true,kind);
            var area=Screen.FromControl(form).WorkingArea;
            Set(form,"startupWorkingArea",(Rectangle?)area);Call(form,"OnLoad",EventArgs.Empty);
            void Centered(string action)
            {
                Check(Math.Abs((form.Left-area.Left)-(area.Right-form.Right))<=1 && Math.Abs((form.Top-area.Top)-(area.Bottom-form.Bottom))<=1,$"{action} is off-center: {era} / {kind} / {scale}");
            }
            Centered("Historical startup");
            Call(form,"SwitchGame",GameCatalog.Defaults(Era.Future2126,GameKind.Klondike,scale));Centered("Switch to ORBIT");
            Call(form,"SwitchGame",GameCatalog.Defaults(era,kind,scale));Centered("Switch back from ORBIT");
        }
        Console.WriteLine("PASS all historical games center on startup and on both legs of an ORBIT round trip at four scales");
    }
    private static void CheckGameClock()
    {
        foreach(var era in new[]{Era.WindowsXP,Era.WindowsVista,Era.Future2126})
        foreach(int interval in new[]{1,8,16,250,1700})
        {
            using var form=new GameWindow(new Store("artifacts/clock-check-state"),era,1989,100,true);
            form.Game.Draw();Set(form,"lastTick",0L);Set(form,"elapsedMilliseconds",0L);
            for(long now=interval;now<10000;now+=interval)Call(form,"AdvanceGameTime",now,true);
            Call(form,"AdvanceGameTime",10000L,true);
            Check(form.Game.State.Elapsed==10,"Game clock rate depends on callback frequency: "+era+" / "+interval);
            Call(form,"AdvanceGameTime",10000L,true);
            Check(form.Game.State.Elapsed==10,"Repeated timer notification counted the same second twice");
            Call(form,"AdvanceGameTime",10500L,true);Call(form,"AdvanceGameTime",30500L,false);Call(form,"AdvanceGameTime",31000L,true);
            Check(form.Game.State.Elapsed==11,"Game clock counted paused time or lost a partial second");
            Set(form,"lastTick",Environment.TickCount64-20000);Call(form,"OnActivated",EventArgs.Empty);
            Call(form,"AdvanceGameTime",Environment.TickCount64,true);
            Check(form.Game.State.Elapsed==11,"Reactivation added time spent away from the game");
        }
        using var live=new GameWindow(new Store("artifacts/clock-real-state"),Era.Future2126,1989,100,true);
        live.Game.Draw();var wallStart=DateTime.UtcNow;var animationClock=System.Diagnostics.Stopwatch.StartNew();
        long clockStart=Environment.TickCount64;Set(live,"lastTick",clockStart);
        while((DateTime.UtcNow-wallStart).TotalSeconds<10){Thread.Sleep(8);Call(live,"AdvanceGameTime",Environment.TickCount64,true);}
        double wallSeconds=(DateTime.UtcNow-wallStart).TotalSeconds;long clockMs=Environment.TickCount64-clockStart;
        Check(Math.Abs(clockMs/1000.0-wallSeconds)<.15,"Monotonic game clock disagrees with wall-clock time");
        Check(Math.Abs(live.Game.State.Elapsed-wallSeconds)<1.1,"Ten real seconds do not produce ten game seconds");
        Console.WriteLine($"PASS real-time clock: wall={wallSeconds:F3}s, gameplay source={clockMs/1000.0:F3}s, display={live.Game.State.Elapsed}s, animation source={animationClock.Elapsed.TotalSeconds:F3}s");
    }
    private static void CheckOrbitSpacing()
    {
        Directory.CreateDirectory("artifacts/orbit-spacing");
        foreach(int scale in new[]{100,125,150,200})
        foreach(var size in new[]{new Size(800,540),new Size(1120,720),new Size(1920,720)})
        {
            using var form=new GameWindow(new Store("artifacts/orbit-spacing-state"),Era.Future2126,1989,scale,true);
            form.ClientSize=new(size.Width*scale/100,size.Height*scale/100);form.Game.Draw();Paint(form);
            RectangleF Slot(int col)=>(RectangleF)typeof(GameWindow).GetMethod("TopCard",Private)!.Invoke(form,[col])!;
            var first=Slot(0);var last=Slot(6);
            Check(first.Width>=56 && Math.Abs(first.Left-(size.Width-last.Right))<.01f,"ORBIT compact grid is not centered or its cards became too small");
            for(int col=0;col<7;col++)
            {
                var card=Area(form,new(PileKind.Tableau,col,form.Game.State.Tableau[col].Count-1));
                var viewport=(RectangleF)typeof(GameWindow).GetMethod("OrbitColumnViewport",Private)!.Invoke(form,[col])!;
                Check(Math.Abs(card.X-Slot(col).X)<.01f && viewport.Contains(card),"ORBIT compact layout clips a dealt card or misaligns its column");
                if(col==6)continue;
                float gap=Slot(col+1).Left-Slot(col).Right;
                Check(gap>=8 && gap<=first.Width*.20f,"ORBIT column gaps are outside their fitted range");
                var next=(RectangleF)typeof(GameWindow).GetMethod("OrbitColumnViewport",Private)!.Invoke(form,[col+1])!;
                Check(!viewport.IntersectsWith(next),"ORBIT adjacent column scroll targets overlap");
            }
            var waste=Area(form,new(PileKind.Waste,0,form.Game.State.Waste.Count-1));
            Check(waste.Right+9<Slot(3).Left,"ORBIT draw-three fan crowds the first foundation");
            using var frame=new Bitmap(form.Width,form.Height);DrawFrame(form,frame);
            if(scale==100 || scale==150 && size.Width==1120)frame.Save($"artifacts/orbit-spacing/{size.Width}x{size.Height}-{scale}.png");
        }
    }
    private static void CheckFutureCardSeparation()
    {
        foreach(int scale in new[]{100,125,150,200})
        {
            using var form=new GameWindow(new Store("artifacts/orbit-separation-state"),Era.Future2126,1989,scale,true);
            form.Game.Draw();Paint(form);
            var top=Area(form,new(PileKind.Waste,0,form.Game.State.Waste.Count-1));
            float width=(float)typeof(GameWindow).GetProperty("CardWidth",Private)!.GetValue(form)!;
            float step=(float)typeof(GameWindow).GetProperty("WasteStep",Private)!.GetValue(form)!;
            Check(step>=width*31/96,"Draw-three overlap hides a two-digit rank or its suit");
            using var frame=new Bitmap(form.Width,form.Height);DrawFrame(form,frame);
            int y=(int)((top.Y+top.Height*.55f)*scale/100);
            int x=(int)MathF.Round(top.X*scale/100);
            int darkest=Enumerable.Range(Math.Max(0,x-1),4).Select(xx=>frame.GetPixel(xx,y).R).Min();
            int face=frame.GetPixel(x+(int)(8*scale/100f),y).R;
            Check(face-darkest>65,"Overlapping ORBIT cards lack a visible separating edge");
            var guidance=(RectangleF)typeof(GameWindow).GetMethod("FuturePosition",Private)!.Invoke(form,[new Position(PileKind.Waste)])!;
            Check(guidance==top,"ORBIT waste guidance differs from its clickable card");
            if(scale==150)frame.Save("artifacts/orbit-checks/separated-waste.png");
        }
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
        Check((bool)Field(form,"showingVictory")!,"ORBIT omitted the card victory");
        using var frame=new Bitmap(form.Width,form.Height);Set(form,"renderMotionTime",12.8);Call(form,"Animate");DrawFrame(form,frame);frame.Save("artifacts/orbit-checks/card-victory.png");
        Set(form,"renderMotionTime",17.7);Call(form,"Animate");DrawFrame(form,frame);frame.Save("artifacts/orbit-checks/results.png");Check((DialogPage)Field(form,"dialog")! ==DialogPage.Won,"ORBIT victory did not reach results");
        Key(form,Keys.Escape);form.Preferences.Animate=false;Call(form,"StartVictory");Check((DialogPage)Field(form,"dialog")! ==DialogPage.Won && !(bool)Field(form,"showingVictory")!,"Reduced-motion victory still animated");
    }
}
