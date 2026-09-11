using System.Drawing;
using System.Text.Json;
using Solitude;

internal static partial class UiProgram
{
    private static void CheckOrbitNativeEndgame()
    {
        using var form=new GameWindow(new Store("artifacts/orbit-native-isolated"),Era.Future2126,1,100,false);
        form.Preferences.Sound=false;
        _=form.Handle;
        var state=new GameState{Seed=1,Started=true};
        state.Foundations[0]=Enumerable.Range(0,12).Select(i=>new Card(i)).ToList();
        state.Foundations[1]=Enumerable.Range(13,11).Select(i=>new Card(i)).ToList();
        state.Foundations[2]=Enumerable.Range(26,13).Select(i=>new Card(i)).ToList();
        state.Foundations[3]=Enumerable.Range(39,13).Select(i=>new Card(i)).ToList();
        state.Tableau[0]=[new(24,false),new(12)];state.Tableau[1]=[new(25)];
        for(int iteration=0;iteration<12;iteration++)
        {
            form.SetRenderState(state);Set(form,"renderMotionTime",100.0);Paint(form);
            var target=(RectangleF)typeof(GameWindow).GetMethod("TableauCard",Private)!.Invoke(form,[2,0])!;
            OrbitDrag(form,Area(form,new(PileKind.Tableau,0,1)),target);
            for(int frame=0;frame<24;frame++)
            {
                Set(form,"renderMotionTime",100+frame/60.0);
                var message=Message.Create(form.Handle,0x8000+74,0,0);
                object[] values=[message];typeof(GameWindow).GetMethod("WndProc",Private)!.Invoke(form,values);
                Paint(form,false);Application.DoEvents();
            }
            Check(HasControl(form,"future-finish"),"Native last-reveal path did not reach ready state");
        }
        Console.WriteLine("PASS native handle, accessibility notifications and window-message endgame frames, without showing the window");
        // Exercise sustained play through the same input, repaint and UIA
        // paths that the engine-only rule stress tests cannot cover.
        int actions=0;var watch=System.Diagnostics.Stopwatch.StartNew();
        foreach(int seed in new[]{1373664058,1989,1,17,42})
        {
            Call(form,"NewGame",false,(int?)seed);Paint(form);
            for(int turn=0;turn<260 && !form.Game.State.Won;turn++)
            {
                Set(form,"renderMotionTime",1000.0+actions);
                var hints=form.Game.OrbitHints();if(hints.Count==0)break;
                var move=hints[0];
                bool changed=move.From.Kind==PileKind.Stock?form.Game.Draw():move.From==move.To?form.Game.Flip(move.From.Pile):form.Game.Move(move.From,move.To);
                Check(changed,"Native stress received an illegal hint");Call(form,"Changed",changed);
                for(int frame=0;frame<3;frame++){Set(form,"renderMotionTime",1000+actions+frame*.18);Call(form,"RenderNextFrame");Paint(form,false);Application.DoEvents();}
                Game.Validate(form.Game.State);actions++;
                if((bool)typeof(GameWindow).GetProperty("CanFinish",Private)!.GetValue(form)!)break;
            }
            if(watch.Elapsed.TotalSeconds>120)break;
        }
        Console.WriteLine($"PASS {actions} native gameplay actions with animated frames, completion probes and accessibility callbacks");
    }
    private static void OrbitDrag(GameWindow form,RectangleF from,RectangleF to)
    {
        float scale=form.Preferences.Scale/100f;
        int x=(int)((from.X+from.Width/2)*scale),y=(int)((from.Y+Math.Min(10,from.Height/2))*scale);
        int dx=(int)((to.X+to.Width/2)*scale),dy=(int)((to.Y+Math.Min(10,to.Height/2))*scale);
        Call(form,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,x,y,0));
        Call(form,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,0,dx,dy,0));Paint(form,false);
        Call(form,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,dx,dy,0));
    }
    private static void CheckOrbitPolish()
    {
        Directory.CreateDirectory("artifacts/orbit-polish");
        foreach(int scale in new[]{100,125,150,200})
        {
            using var form=new GameWindow(new Store("artifacts/orbit-polish-state"),Era.Future2126,1,scale,true);
            T Prop<T>(string name)=>(T)typeof(GameWindow).GetProperty(name,Private)!.GetValue(form)!;
            RectangleF CardAt(int c,int i)=>(RectangleF)typeof(GameWindow).GetMethod("TableauCard",Private)!.Invoke(form,[c,i])!;
            Paint(form);
            using(var chrome=new Bitmap(form.Width,form.Height))
            {
                DrawFrame(form,chrome);chrome.Save($"artifacts/orbit-polish/chrome-{scale}.png");
                foreach(string id in new[]{"future-experience","settings","future-new"})
                {
                    var r=Hit(form,id);var pixels=Rectangle.Round(new RectangleF(r.X*scale/100,r.Y*scale/100,r.Width*scale/100,r.Height*scale/100));
                    int left=pixels.Right,top=pixels.Bottom,right=-1,bottom=-1;
                    for(int y=pixels.Top+4;y<pixels.Bottom-4;y++)for(int x=pixels.Left+4;x<pixels.Right-4;x++)
                    {var p=chrome.GetPixel(x,y);if(id=="future-new"?p.R>60 || p.G>115 || p.B>115:p.R<130 || p.G<150 || p.B<150)continue;left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
                    Check(right>=left && Math.Abs((left+right+1)/2f-(r.X+r.Width/2)*scale/100)<=1,"Button glyphs are horizontally off center: "+id);
                    Check(Math.Abs((top+bottom+1)/2f-(r.Y+r.Height/2)*scale/100)<=1,$"Button glyphs are vertically off center: {id} scale={scale}, ink={top}..{bottom}, rect={pixels}");
                }
                // Inspect the native-region composite, not just a transparent
                // renderer image that can conceal mismatched corner clipping.
                using var native=new Bitmap(chrome.Width,chrome.Height);
                using(var g=Graphics.FromImage(native)){g.Clear(Color.FromArgb(32,32,32));g.SetClip(form.Region!,System.Drawing.Drawing2D.CombineMode.Replace);g.DrawImageUnscaled(chrome,0,0);}
                using var detail=new Bitmap(480,240);
                using(var g=Graphics.FromImage(detail))
                {
                    g.Clear(Color.FromArgb(32,32,32));g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    g.DrawImage(native,new Rectangle(0,0,240,240),new Rectangle(0,0,40,40),GraphicsUnit.Pixel);
                    g.DrawImage(native,new Rectangle(240,0,120,240),new Rectangle(0,native.Height-40,20,40),GraphicsUnit.Pixel);
                    g.DrawImage(native,new Rectangle(360,0,120,240),new Rectangle(native.Width-20,native.Height-40,20,40),GraphicsUnit.Pixel);
                }
                detail.Save($"artifacts/orbit-polish/corners-{scale}.png");
                chrome.Save($"artifacts/orbit-polish/chrome-{scale}.png");
            }
            var near=new GameState{Seed=1,Started=true};
            near.Foundations[0]=Enumerable.Range(0,12).Select(i=>new Card(i)).ToList();
            near.Foundations[1]=Enumerable.Range(13,11).Select(i=>new Card(i)).ToList();
            near.Foundations[2]=Enumerable.Range(26,13).Select(i=>new Card(i)).ToList();
            near.Foundations[3]=Enumerable.Range(39,13).Select(i=>new Card(i)).ToList();
            near.Tableau[0]=[new(24,false),new(12)];near.Tableau[1]=[new(25)];
            foreach(bool animate in new[]{true,false})
            {
                form.SetRenderState(near);form.Preferences.Animate=animate;Paint(form);
                Set(form,"renderMotionTime",100.0);
                OrbitDrag(form,Area(form,new(PileKind.Tableau,0,1)),CardAt(2,0));
                Check(form.Game.State.Tableau[0].Single().FaceUp && form.Game.State.Tableau[2].Single().Id==12,"Last column move failed");
                for(int i=0;i<16;i++){Set(form,"renderMotionTime",100+i*.025);Call(form,"Animate");Paint(form,false);}
                Check(Prop<bool>("CanFinish") && !form.Game.State.Won,"Last reveal did not offer completion");
                var finish=Hit(form,"future-finish");
                Check(finish.Top>Prop<float>("TableauBottom"),"Completion control overlaps the playing cards");
                var before=JsonSerializer.Serialize(form.Game.State);Paint(form,false);Check(before==JsonSerializer.Serialize(form.Game.State),"Completion probe mutated live deal");
                Call(form,"CollectCards");
                for(int i=0;i<180;i++){Set(form,"renderMotionTime",101+i*.04);Call(form,"Animate");Paint(form,false);}
                Check(form.Game.State.Won && form.Game.State.WinRecorded,"Last-column move did not finish safely");
                Call(form,"CloseDialog");Set(form,"showingVictory",false);Set(form,"pendingWin",false);
            }
            // The Ace must remain under a travelling Two throughout its flight.
            var state=new GameState{Seed=1};state.Foundations[0]=[new(0)];state.Tableau[0]=[new(1)];
            state.Stock=Enumerable.Range(2,50).Select(i=>new Card(i,false)).ToList();
            form.SetRenderState(state);form.Preferences.Animate=true;Paint(form);Set(form,"renderMotionTime",200.0);
            var foundation=(RectangleF)typeof(GameWindow).GetMethod("TopCard",Private)!.Invoke(form,[3])!;
            using var beforeFrame=new Bitmap(form.Width,form.Height);DrawFrame(form,beforeFrame);
            Call(form,"Changed",form.Game.ToFoundation(new(PileKind.Tableau,0)));
            using var frame=new Bitmap(form.Width,form.Height);DrawFrame(form,frame);
            int cx=(int)((foundation.X+foundation.Width/2)*scale/100),cy=(int)((foundation.Y+foundation.Height/2)*scale/100);
            Check(frame.GetPixel(cx,cy)==beforeFrame.GetPixel(cx,cy),"Ace disappears when Two starts travelling");
            frame.Save($"artifacts/orbit-polish/foundation-{scale}.png");
            Set(form,"renderMotionTime",200.4);Call(form,"Animate");Paint(form,false);

            var run=Enumerable.Range(1,13).Reverse().Select(rank=>new Card((rank%2==0?26:0)+rank-1)).ToList();
            var rest=Enumerable.Range(0,52).Where(id=>run.All(c=>c.Id!=id)).ToList();
            state=new GameState{Seed=1};state.Tableau[0]=rest.Take(6).Select(i=>new Card(i,false)).Concat(run).ToList();state.Stock=rest.Skip(6).Select(i=>new Card(i,false)).ToList();
            form.SetRenderState(state);
            foreach(var size in new[]{new Size(800,540),new Size(1120,720),new Size(1600,800)})
            {
                form.ClientSize=new(size.Width*scale/100,size.Height*scale/100);Paint(form);
                Check(CardAt(0,0).Top>=Prop<float>("TableauY") && CardAt(0,18).Bottom<=Prop<float>("TableauBottom")+.01,"Full long stack does not fit");
                float step=(float)typeof(GameWindow).GetMethod("StackStep",Private)!.Invoke(form,[0])!;
                Check(step>=Prop<float>("CardWidth")*.27f-.01,"Rank/suit strip clipped");
                Check(!HasControl(form,"future-scroll-up-0") && !HasControl(form,"future-scroll-down-0"),"Scroll controls still present");
                for(int i=6;i<19;i++)Check(Area(form,new(PileKind.Tableau,0,i)).Height>0,"A face-up card cannot be selected");
                if(scale==100){using var view=new Bitmap(form.Width,form.Height);DrawFrame(form,view);view.Save($"artifacts/orbit-polish/long-{size.Width}x{size.Height}.png");}
            }
        }
        using(var recovery=new GameWindow(new Store("artifacts/orbit-recovery-state"),Era.Future2126,1989,100,true))
        {
            recovery.Game.Draw();Paint(recovery);string before=JsonSerializer.Serialize(recovery.Game.State);bool preference=recovery.Preferences.Animate;
            bool recovered=(bool)typeof(GameWindow).GetMethod("TryRecoverOrbitUi",Private)!.Invoke(recovery,[new ArgumentException("Injected render failure")])!;
            Check(recovered && JsonSerializer.Serialize(recovery.Game.State)==before,"Recoverable UI error loses the active deal");
            Check(recovery.Preferences.Animate==preference && !(bool)typeof(GameWindow).GetProperty("OrbitMotionEnabled",Private)!.GetValue(recovery)!,"Recovery changes the saved motion preference or keeps failed animation active");
            Call(recovery,"CloseDialog");Call(recovery,"Changed",recovery.Game.Draw());Paint(recovery,false);
            Check(((System.Collections.IDictionary)Field(recovery,"flights")!).Count==0 && recovery.Game.CanUndo,"Recovery cannot continue safely with Undo");
            Check(!(bool)typeof(GameWindow).GetMethod("TryRecoverOrbitUi",Private)!.Invoke(recovery,[new ArgumentException("Repeated failure")])!,"Repeated UI faults loop silently");
        }
        Console.WriteLine("PASS ORBIT last-column reveal through win, foundation underlay, full-stack fitting and selectable cards");
    }
}
