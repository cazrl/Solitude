using System.Diagnostics;
using System.Text.Json;
using Solitude;

internal static class PinballChecks
{
    private static int checks;
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
    private static void Pump(int milliseconds){var timer=Stopwatch.StartNew();while(timer.ElapsedMilliseconds<milliseconds){Application.DoEvents();Thread.Sleep(5);}}
    [STAThread] private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();
        string root=Path.GetFullPath(args.FirstOrDefault()??"artifacts/pinball/checks");Directory.CreateDirectory(root);
        if(args.Length>1)PinballRuntime.WorkerExecutableForChecks=Path.GetFullPath(args[1]);
        Process? process=null;nint engine=0;
        try
        {
            using var host=new Form{ClientSize=new(900,624),ShowInTaskbar=false};
            using var panel=new Panel{Dock=DockStyle.Fill};host.Controls.Add(panel);
            _=host.Handle;_=panel.Handle; // Keep the entire native parent chain hidden.
            process=PinballRuntime.Start(panel.Handle,root,root);
            var deadline=Stopwatch.StartNew();
            while(deadline.Elapsed.TotalSeconds<15)
            {
                Pump(50);if(process.HasExited)throw new Exception("Worker exited early: "+process.ExitCode);
                engine=PinballRuntime.GetWindow(panel.Handle,5);
                if(engine!=0 && PinballRuntime.Query(engine,0)==1)break;
            }
            Check(engine!=0 && PinballRuntime.Query(engine,0)==1,"Native table initializes as a child of the hidden host");
            Check(PinballRuntime.Query(engine,12)==1,"Automated worker has effects and music output suppressed");
            void Send(uint msg,int wp=0){PinballRuntime.PostMessage(engine,msg,wp,0);Pump(60);}
            void Cmd(int id)=>Send(0x111,id);
            void Capture(string name)
            {
                Send(PinballRuntime.CaptureMessage);File.Copy(Path.Combine(root,"table.bmp"),Path.Combine(root,name+".bmp"),true);
                File.Copy(Path.Combine(root,"state.json"),Path.Combine(root,name+".json"),true);
            }
            Send(7);Cmd(101);Pump(400);Capture("new-game");
            Check(PinballRuntime.Query(engine,3)==3,"A new game has three balls");
            long before=PinballRuntime.Query(engine,8);var real=Stopwatch.StartNew();Pump(2100);
            long after=PinballRuntime.Query(engine,8);double ratio=(after-before)/real.Elapsed.TotalMilliseconds;
            Check(ratio>.85 && ratio<1.15,$"Simulation time follows real time (ratio {ratio:F3})");
            Cmd(402);Check(PinballRuntime.Query(engine,2)==1,"Pause enters the paused state");
            long paused=PinballRuntime.Query(engine,8);Pump(500);
            Check(PinballRuntime.Query(engine,8)==paused,"Simulation remains frozen while paused");
            Cmd(402);Check(PinballRuntime.Query(engine,2)==0,"Resume leaves the paused state");
            Send(0x100,(int)Keys.Space);Pump(1200);Send(0x101,(int)Keys.Space);Pump(900);Capture("launched");
            using(var state=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"state.json"))))
                Check(state.RootElement.GetProperty("speed").GetDouble()>0,"Holding and releasing Space launches a moving ball");
            Send(0x100,(int)Keys.Z);Send(0x100,(int)Keys.OemQuestion);Pump(150);Capture("flippers");
            Check(PinballRuntime.Query(engine,9)>0 && PinballRuntime.Query(engine,10)>0,"Both flippers extend while their keys are held");
            Send(0x101,(int)Keys.Z);Send(0x101,(int)Keys.OemQuestion);
            Pump(250);Check(PinballRuntime.Query(engine,9)==0 && PinballRuntime.Query(engine,10)==0,"Both flippers retract after key release");
            Send(8);long unfocused=PinballRuntime.Query(engine,8);Pump(500);
            Check(PinballRuntime.Query(engine,8)==unfocused,"Losing focus freezes simulation and releases controls");
            Send(7);Pump(250);Check(PinballRuntime.Query(engine,8)>unfocused,"Regaining focus resumes without a catch-up jump");
            long sounds=PinballRuntime.Query(engine,5);Cmd(201);Check(PinballRuntime.Query(engine,5)==1-sounds,"Sound menu changes sound state");
            Cmd(409);Check(PinballRuntime.Query(engine,7)==2,"Two-player mode starts with two players");
            Cmd(408);Check(PinballRuntime.Query(engine,7)==1,"One-player mode can be restored");
            PinballRuntime.MoveWindow(engine,0,0,1200,832,true);Pump(200);Check(PinballRuntime.Query(engine,0)==1,"Table remains responsive after resizing");
            Send(0x7e,32);Check(PinballRuntime.Query(engine,11)==1200,"Display-change messages preserve the hosted playfield size");
            Cmd(404);Pump(2500);Capture("demo");Check(!process.HasExited,"Demo runs without terminating the worker");
            Send(0x10);Check(process.WaitForExit(3000) && process.ExitCode==0,"Native game closes cleanly");
            Check(File.Exists(Path.Combine(root,"pinball","settings.ini")),"Pinball settings are saved inside its isolated data directory");
            Check(!File.Exists(Path.Combine(root,"solitude.json")),"Pinball does not write a card-game save");
            process.Dispose();process=PinballRuntime.Start(panel.Handle,root,root);engine=0;deadline.Restart();
            while(deadline.Elapsed.TotalSeconds<10){Pump(50);engine=PinballRuntime.GetWindow(panel.Handle,5);if(engine!=0 && PinballRuntime.Query(engine,0)==1)break;}
            Check(PinballRuntime.Query(engine,5)==1-sounds,"Sound preference survives a worker restart");
            Send(0x10);Check(process.WaitForExit(3000) && process.ExitCode==0,"Restarted worker closes cleanly");
            using(var window=new PinballWindow(root,150,new Rectangle(0,0,1920,1080)))
            {
                window.StartForChecks(root);Pump(1600);
                Check(window.Engine!=0 && PinballRuntime.Query(window.Engine,0)==1,"Production Pinball window hosts the native engine");
                Check(PinballRuntime.Query(window.Engine,12)==1,"Production-window test worker also has audio suppressed");
                window.Location=new(-30000,-30000);window.Show();Pump(200);
                window.RenderTo(Path.Combine(root,"pinball-xp.png"));
                CheckMenus(window,root);
                using(var about=window.CreateAboutDialog())
                {
                    var links=new List<string>();about.OpenLink=links.Add;
                    Check(about.AccessibilityObject.GetChild(1)!.Name!.Contains("created by Cazrl"),"Pinball About includes Cazrl's creator credit");
                    var donate=about.AccessibilityObject.GetChild(2)!;
                    donate.DoDefaultAction();Check(links.SequenceEqual(new[]{"https://ko-fi.com/flightwire"}),"About Donate opens the exact Ko-fi link once");
                    Check(!donate.Bounds.IntersectsWith(about.AccessibilityObject.GetChild(0)!.Bounds),"Themed About buttons do not overlap");
                    using var image=new Bitmap(about.Width,about.Height);about.DrawToBitmap(image,about.ClientRectangle);image.Save(Path.Combine(root,"pinball-about.png"));
                }
                using(var image=new Bitmap(Path.Combine(root,"pinball-xp.png")))
                {
                    var colors=new HashSet<int>();for(int y=100;y<image.Height-10;y+=7)for(int x=10;x<image.Width-10;x+=7)colors.Add(image.GetPixel(x,y).ToArgb());
                    Check(colors.Count>100,"Captured production window contains the rendered table, not a blank surface");
                }
                PinballRuntime.PostMessage(window.Engine,0x100,(nint)Keys.F6,0);Pump(350);
                Check(window.ReturnToSettings && window.IsDisposed,"F6 from the native playfield closes Pinball and requests Settings");
            }
            Console.WriteLine($"{checks} Pinball integration checks passed using hidden/offscreen windows.");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        finally{if(process!=null){if(!process.HasExited)process.Kill();process.Dispose();}}
    }
    private static void CheckMenus(PinballWindow window,string root)
    {
        PinballMenu Open(int index)
        {
            window.ShowMenu(index);var menu=window.ActiveMenu!;
            menu.Location=new(-29000,-29000);Pump(60);
            Check(menu.Visible,"XP popup opens");return menu;
        }
        void Capture(ToolStrip menu,string name)
        {
            using var bitmap=new Bitmap(menu.Width,menu.Height);menu.DrawToBitmap(bitmap,menu.ClientRectangle);
            bitmap.Save(Path.Combine(root,name+".png"));
        }
        var game=Open(0);
        var launch=(ToolStripMenuItem)game.Items[1];
        Check(launch.ShortcutKeyDisplayString=="Space" && !launch.Text!.Contains('\t'),"Shortcut labels occupy a separate menu column");
        Capture(game,"menu-game-150");
        game.Items[0].Select();Capture(game,"menu-game-selected-150");
        PinballRuntime.PostMessage(window.Engine,0x201,1,0);Pump(150);
        Check(window.ActiveMenu==null && !game.Visible,"Clicking the foreign-process playfield dismisses the popup");
        PinballRuntime.PostMessage(window.Engine,0x202,0,0);Pump(50);
        var options=Open(1);Capture(options,"menu-options-150");
        var players=(ToolStripMenuItem)options.Items[3];players.ShowDropDown();players.DropDown.Location=new(-28000,-28000);Pump(60);
        Capture(players.DropDown,"menu-players-150");
        PinballRuntime.PostMessage(window.Engine,0x204,2,0);Pump(150);
        Check(window.ActiveMenu==null && !players.DropDown.Visible,"Outside right-click dismisses the Players submenu and its parent");
        PinballRuntime.PostMessage(window.Engine,0x205,0,0);Pump(50);
        game=Open(0);options=Open(1);
        Check(!game.Visible && options.Visible,"Switching menu headers replaces the previous popup");
        bool oldSound=PinballRuntime.Query(window.Engine,5)==1;
        ((ToolStripMenuItem)options.Items[0]).PerformClick();Pump(150);
        Check((PinballRuntime.Query(window.Engine,5)==1)!=oldSound,"The styled Sound command still reaches the native engine");
        options.Close();Pump(60);
        var help=Open(2);Capture(help,"menu-help-150");
        PinballRuntime.PostMessage(help.Handle,0x100,(nint)Keys.Escape,0);Pump(150);
        Check(window.ActiveMenu==null,"Escape dismisses the popup");
        game=Open(0);
        typeof(PinballWindow).GetMethod("OnDeactivate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(window,[EventArgs.Empty]);Pump(60);
        Check(window.ActiveMenu==null,"Deactivation dismisses the popup");
        game=Open(0);
        var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        // Empty menu-bar space used to reopen Help instead of dismissing.
        var click=new MouseEventArgs(MouseButtons.Left,1,350,55,0);
        typeof(PinballWindow).GetMethod("OnMouseDown",flags)!.Invoke(window,[click]);
        typeof(PinballWindow).GetMethod("OnMouseUp",flags)!.Invoke(window,[click]);Pump(60);
        Check(window.ActiveMenu==null,"Clicking empty menu-bar space dismisses without opening Help");
        foreach(int percent in new[]{100,125,150,200})
        {
            using var menu=new PinballMenu(percent/100f);
            menu.Items.Add(new ToolStripMenuItem("&New Game"){ShortcutKeyDisplayString="F2"});
            menu.Items.Add(new ToolStripMenuItem("&Launch Ball"){ShortcutKeyDisplayString="Space"});
            menu.Items.Add(new ToolStripSeparator());menu.Items.Add(new ToolStripMenuItem("&Music"){Checked=true});menu.Prepare();
            menu.Show(window,Point.Empty);menu.Location=new(-28000,-28000);Pump(30);
            Capture(menu,$"menu-scale-{percent}");
            Check(menu.Items[0].Height>=20*percent/100 && Math.Abs(menu.Font.Size-11*percent/100f)<.1f,$"Menu text and rows follow {percent}% display scale");
            menu.Close();
        }
    }
}
