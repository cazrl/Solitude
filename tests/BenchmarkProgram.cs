using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text.Json;
using Solitude;

internal static class BenchmarkProgram
{
    [STAThread]private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        var results=new List<object>();
        var pacing=new List<object>();
        var paint=typeof(GameWindow).GetMethod("PaintScaled",BindingFlags.Instance|BindingFlags.NonPublic)!;
        foreach(var era in new[]{Era.Windows31,Era.Windows95,Era.WindowsXP,Era.WindowsVista})foreach(var kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))
        {
            using var form=new GameWindow(new Store("artifacts/benchmark-state"),era,1,150,true,kind);
            if(kind==GameKind.Spider)for(int i=0;i<5;i++)form.Game.Draw();
            using var bitmap=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);using var g=Graphics.FromImage(bitmap);
            for(int i=0;i<8;i++)paint.Invoke(form,[g]);
            var samples=new List<double>();long allocated=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<120;i++){long start=Stopwatch.GetTimestamp();paint.Invoke(form,[g]);samples.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);}
            long allocation=GC.GetAllocatedBytesForCurrentThread()-allocated;samples.Sort();
            results.Add(new{era=era.ToString(),game=kind.ToString(),width=bitmap.Width,height=bitmap.Height,frames=samples.Count,medianMs=samples[samples.Count/2],p95Ms=samples[(int)(samples.Count*.95)],allocatedBytesPerFrame=allocation/samples.Count});
            Console.WriteLine($"{era} {kind}: median {samples[samples.Count/2]:F2} ms, p95 {samples[(int)(samples.Count*.95)]:F2} ms");
        }
        string path=args.FirstOrDefault()??"artifacts/render-benchmark.json";
        if(args.Contains("--pacing"))foreach(bool partial in new[]{false,true})foreach(int rate in new[]{60,120,144})pacing.Add(MeasurePacing(rate,paint,partial));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path,JsonSerializer.Serialize(new{description="Warm production renderer into an offscreen bitmap; CPU frame cost, not measured monitor FPS. Spider contains 104 cards. Pacing uses the real Windows message queue and production frame clock with an offscreen dragged card.",results,pacing},new JsonSerializerOptions{WriteIndented=true}));return 0;
    }
    private static object MeasurePacing(int rate,MethodInfo paint,bool partial)
    {
        using var form=new GameWindow(new Store("artifacts/pacing-state"),Era.WindowsVista,1,150,true,GameKind.Spider);
        for(int i=0;i<5;i++)form.Game.Draw();
        using var bitmap=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);using var graphics=Graphics.FromImage(bitmap);
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        typeof(GameWindow).GetField("selection",flags)!.SetValue(form,new Position(PileKind.Tableau,0,form.Game.State.Tableau[0].Count-1));
        typeof(GameWindow).GetField("dragging",flags)!.SetValue(form,true);
        for(int i=0;i<12;i++)paint.Invoke(form,[graphics]);
        Type type=typeof(GameWindow).Assembly.GetType("Solitude.FramePump")!;
        using var window=new FrameProbe();object pump=Activator.CreateInstance(type,window.Handle)!;
        var intervals=new List<double>();var stopwatch=Stopwatch.StartNew();double previous=0;
        window.Tick=()=>
        {
            double now=stopwatch.Elapsed.TotalSeconds;
            if(previous>0)intervals.Add((now-previous)*1000);previous=now;
            typeof(GameWindow).GetField("mouse",flags)!.SetValue(form,new PointF(350+(float)Math.Sin(now*3)*160,280+(float)Math.Cos(now*2)*70));
            var saved=graphics.Save();
            if(partial)graphics.SetClip((Rectangle)typeof(GameWindow).GetMethod("FrameDamage",flags)!.Invoke(form,null)!);
            paint.Invoke(form,[graphics]);graphics.Restore(saved);
            type.GetMethod("Acknowledge")!.Invoke(pump,null);
        };
        int heartbeatTicks=0;using var heartbeat=new System.Windows.Forms.Timer{Interval=50};heartbeat.Tick+=(_,_)=>heartbeatTicks++;heartbeat.Start();
        using var context=new ApplicationContext();using var stop=new System.Windows.Forms.Timer{Interval=2200};
        stop.Tick+=(_,_)=>context.ExitThread();stop.Start();type.GetMethod("SetActive")!.Invoke(pump,[true,rate]);
        Application.Run(context);type.GetMethod("SetActive")!.Invoke(pump,[false,rate]);((IDisposable)pump).Dispose();
        if(heartbeatTicks<15 || stopwatch.Elapsed.TotalSeconds>3.5)throw new Exception("Frame requests starved the Windows message queue.");
        double fps=1000/intervals.Average();intervals.Sort();
        Console.WriteLine($"Pacing {(partial?"partial":"full")} {rate}: {fps:F1} delivered FPS, p95 interval {intervals[(int)(intervals.Count*.95)]:F2} ms");
        return new{redraw=partial?"partial":"full",target=rate,deliveredFps=fps,p95IntervalMs=intervals[(int)(intervals.Count*.95)],frames=intervals.Count,heartbeatTicks,elapsedSeconds=stopwatch.Elapsed.TotalSeconds,highResolution=(bool)type.GetProperty("HighResolution")!.GetValue(pump)!};
    }
    private sealed class FrameProbe:NativeWindow,IDisposable
    {
        public Action? Tick;
        public FrameProbe()=>CreateHandle(new CreateParams{Caption="Solitude frame probe",Parent=(nint)(-3)});
        protected override void WndProc(ref Message m){if(m.Msg==0x8000+74){Tick?.Invoke();return;}base.WndProc(ref m);}
        public void Dispose()=>DestroyHandle();
    }
}
