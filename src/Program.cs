namespace Solitude;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        string? Arg(string key){int i=Array.IndexOf(args,key);return i>=0 && i+1<args.Length?args[i+1]:null;}
        string data=Arg("--data-dir") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Solitude");
        GameWindow? activeWindow=null;
        AppDomain.CurrentDomain.UnhandledException+=(_,e)=>RuntimeDiagnostics.Write(data,e.ExceptionObject as Exception??new Exception(e.ExceptionObject?.ToString()),activeWindow?.DiagnosticContext??"Unhandled runtime error");
        Application.ThreadException+=(_,e)=>
        {
            RuntimeDiagnostics.Write(data,e.Exception,activeWindow?.DiagnosticContext??"UI callback error");
            if(activeWindow?.TryRecoverOrbitUi(e.Exception)==true)return;
            MessageBox.Show("Solitude encountered an error and must close. Error details were saved to:\n\n"+Path.Combine(data,"Solitude-error.txt"),"Solitude",MessageBoxButtons.OK,MessageBoxIcon.Error);
            Application.Exit();
        };
        try
        {
            if(Arg("--licenses") is string licenses)
            {
                Directory.CreateDirectory(licenses);
                var assembly=typeof(Program).Assembly;
                foreach(string resource in assembly.GetManifestResourceNames().Where(n=>n.StartsWith("Solitude.Notices.",StringComparison.Ordinal)))
                {
                    using var input=assembly.GetManifestResourceStream(resource)!;
                    using var output=File.Create(Path.Combine(licenses,resource["Solitude.Notices.".Length..]));
                    input.CopyTo(output);
                }
                return 0;
            }
            if(Arg("--render") is string render)
            {
                Directory.CreateDirectory(render);
                int count=0;
                var eras=Enum.TryParse<Era>(Arg("--render-era"),out var renderEra) && Enum.IsDefined(renderEra)?new[]{renderEra}:Enum.GetValues<Era>();
                foreach(var era in eras)foreach(var kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))foreach(int scale in new[]{100,125,150,200})
                {
                    using var form=new GameWindow(new Store(data),era,kind==GameKind.FreeCell?1:1989,scale,true,kind);
                    string prefix=kind==GameKind.Klondike?era.ToString():$"{era}-{kind}";
                    form.RenderTo(Path.Combine(render,$"{prefix}-{scale}.png"));
                    count++;
                    if(scale==150)
                    {
                        foreach(var page in new[]{DialogPage.Settings,DialogPage.Options,DialogPage.Help,DialogPage.About}.Concat(kind!=GameKind.Klondike || era==Era.WindowsVista?[DialogPage.Statistics]:Array.Empty<DialogPage>()).Concat(kind==GameKind.Spider && era!=Era.WindowsVista?[DialogPage.Difficulty]:Array.Empty<DialogPage>()))
                        {form.RenderTo(Path.Combine(render,$"{prefix}-{page.ToString().ToLowerInvariant()}.png"),page);count++;}
                        if(kind==GameKind.Klondike || era==Era.WindowsVista){form.RenderTo(Path.Combine(render,$"{prefix}-deck.png"),DialogPage.Deck);count++;}
                        if(kind==GameKind.FreeCell)
                        {
                            form.RenderTo(Path.Combine(render,$"{prefix}-number.png"),DialogPage.SelectGame);count++;
                            if(era!=Era.WindowsVista)foreach(var page in new[]{DialogPage.MoveColumn,DialogPage.Won})
                            {form.RenderTo(Path.Combine(render,$"{prefix}-{page.ToString().ToLowerInvariant()}.png"),page);count++;}
                        }
                        form.RenderTo(Path.Combine(render,$"{prefix}-menu.png"),openMenu:0);count++;
                        form.RenderTo(Path.Combine(render,$"{prefix}-inactive.png"),inactive:true);count++;
                        form.RenderTo(Path.Combine(render,$"{prefix}-maximized.png"),maximize:true);count++;
                        if(era is Era.WindowsXP or Era.WindowsVista or Era.Future2126)form.RenderMotionSequence(Path.Combine(render,"motion"));
                        if(era==Era.Future2126)form.RenderOrbitVictorySequence(Path.Combine(render,"victory"));
                    }
                }
                File.WriteAllText(Path.Combine(render,"complete.txt"),$"Rendered {count} application views using the production renderer.\n");
                return 0;
            }
            Era? selected=Enum.TryParse<Era>(Arg("--era"),out var parsed)?parsed:null;
            int? seed=int.TryParse(Arg("--seed"),out int deal)?deal:null;
            int? size=int.TryParse(Arg("--scale"),out int scaleArg) && scaleArg is 100 or 125 or 150 or 200?scaleArg:null;
            GameKind? kindArg=Enum.TryParse<GameKind>(Arg("--game"),out var parsedKind)&&Enum.IsDefined(parsedKind)?parsedKind:null;
            using var instance=SingleInstanceLease.TryAcquire(data);
            if(instance==null)
            {
                if(!SingleInstanceLease.ActivateExisting(data))MessageBox.Show("Solitude is already open or starting for these saved games.","Solitude",MessageBoxButtons.OK,MessageBoxIcon.Information);
                return 0;
            }
            using var main=new GameWindow(new Store(data),selected,seed,size,kind:kindArg);
            main.HandleCreated+=(_,_)=>instance.SetWindow(main.Handle);
            if(main.IsHandleCreated)instance.SetWindow(main.Handle);
            activeWindow=main;
            Application.Run(main);return 0;
        }
        catch(Exception ex)
        {
            RuntimeDiagnostics.Write(data,ex,activeWindow?.DiagnosticContext??"Startup or render error");
            string log=Path.Combine(AppContext.BaseDirectory,"Solitude-error.txt");
            try{File.WriteAllText(log,ex.ToString());}catch(IOException){}catch(UnauthorizedAccessException){}
            if(!args.Contains("--render"))MessageBox.Show("Solitude could not start.\n\n"+ex.Message,"Solitude",MessageBoxButtons.OK,MessageBoxIcon.Error);
            return 1;
        }
    }
}
