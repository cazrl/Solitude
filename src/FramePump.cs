using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Solitude;

// One outstanding window message; overdue frames are skipped instead of queued.
internal sealed class FramePump : IDisposable
{
    public const int Message = 0x8000 + 74;
    private readonly nint window;
    private readonly AutoResetEvent changed = new(false);
    private readonly TimerHandle timer;
    private readonly Thread thread;
    private volatile bool running, disposed;
    private volatile int rate = 120;
    private int posted;
    public bool HighResolution { get; }
    public FramePump(nint window)
    {
        this.window=window;
        var handle=CreateWaitableTimerExW(0,null,2,0x1F0003);HighResolution=handle!=0;
        if(handle==0)handle=CreateWaitableTimerExW(0,null,0,0x1F0003);
        if(handle==0)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        timer=new(handle);thread=new(Loop){IsBackground=true,Name="Solitude frame clock"};thread.Start();
    }
    public void SetActive(bool active,int framesPerSecond)
    {
        if(disposed)return;
        framesPerSecond=Math.Clamp(framesPerSecond,30,144);
        if(running==active && rate==framesPerSecond)return;
        running=active;rate=framesPerSecond;changed.Set();
    }
    public void Acknowledge(){if(disposed)return;Interlocked.Exchange(ref posted,0);changed.Set();}
    private void Loop()
    {
        WaitHandle[] waits=[changed,timer];double next=0;
        while(!disposed)
        {
            if(!running){changed.WaitOne();next=0;continue;}
            // Do not request another paint until the UI has completed this one.
            if(Volatile.Read(ref posted)!=0){changed.WaitOne();continue;}
            double now=Stopwatch.GetTimestamp(),interval=(double)Stopwatch.Frequency/rate;
            if(next==0)next=now+interval;
            else if(now-next>interval)next=now;
            // An overloaded renderer leaves at least 1 ms for input, WM_PAINT and
            // WM_TIMER; continuous posted messages would otherwise starve them.
            long due=-Math.Max(10_000,(long)((next-Stopwatch.GetTimestamp())/Stopwatch.Frequency*10_000_000));
            if(!SetWaitableTimer(timer.SafeWaitHandle,ref due,0,0,0,false)){changed.WaitOne(16);next=0;}
            else if(WaitHandle.WaitAny(waits)==0)continue;
            if(running && !disposed && Interlocked.CompareExchange(ref posted,1,0)==0)
            {if(!PostMessageW(window,Message,0,0))Interlocked.Exchange(ref posted,0);next+=interval;}
        }
    }
    public void Dispose()
    {
        if(disposed)return;disposed=true;changed.Set();thread.Join();timer.Dispose();changed.Dispose();
    }
    private sealed class TimerHandle : WaitHandle {public TimerHandle(nint handle)=>SafeWaitHandle=new SafeWaitHandle(handle,true);}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern nint CreateWaitableTimerExW(nint attributes,string? name,uint flags,uint access);
    [DllImport("kernel32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool SetWaitableTimer(SafeWaitHandle timer,ref long due,int period,nint callback,nint argument,[MarshalAs(UnmanagedType.Bool)]bool resume);
    [DllImport("user32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool PostMessageW(nint window,int message,nint wParam,nint lParam);
}
