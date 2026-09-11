using System.Runtime.InteropServices;

namespace Solitude;

// The open file is the lease: process termination releases it automatically,
// and filesystem aliases still contend on the same file. No stale PID lock.
public sealed class SingleInstanceLease : IDisposable
{
    private readonly FileStream file;
    public static int ActivationMessage { get; }=(int)RegisterWindowMessage("Solitude.ActivateExistingWindow.1");
    private SingleInstanceLease(FileStream file){this.file=file;SetWindow(0);}
    public static SingleInstanceLease? TryAcquire(string directory)
    {
        Directory.CreateDirectory(directory);
        try{return new(new FileStream(Path.Combine(directory,"solitude.instance"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.Read));}
        catch(IOException ex) when((ex.HResult&0xffff) is 32 or 33){return null;}
    }
    public void SetWindow(nint handle)
    {
        Span<byte> data=stackalloc byte[16];
        data.Clear();
        BitConverter.TryWriteBytes(data,Environment.ProcessId);BitConverter.TryWriteBytes(data[8..],(long)handle);
        file.Position=0;file.Write(data);file.SetLength(data.Length);file.Flush();
    }
    public static bool ActivateExisting(string directory)
    {
        Span<byte> data=stackalloc byte[16];
        // A second launch can arrive while the first is still loading assets.
        for(int attempt=0;attempt<40;attempt++)
        {
            try
            {
                using var owner=new FileStream(Path.Combine(directory,"solitude.instance"),FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
                owner.ReadExactly(data);
                int pid=BitConverter.ToInt32(data);nint handle=(nint)BitConverter.ToInt64(data[8..]);
                if(handle!=0 && GetWindowThreadProcessId(handle,out uint actualPid)!=0 && actualPid==pid)
                {
                    AllowSetForegroundWindow((uint)pid);
                    return SendMessageTimeout(handle,ActivationMessage,0,0,2,1000,out _)!=0;
                }
            }
            catch(IOException){ }
            Thread.Sleep(50);
        }
        return false;
    }
    public void Dispose()=>file.Dispose();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern uint RegisterWindowMessage(string message);
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint window,out uint processId);
    [DllImport("user32.dll")]private static extern bool AllowSetForegroundWindow(uint processId);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern nint SendMessageTimeout(nint window,int message,nint wParam,nint lParam,uint flags,uint timeout,out nint result);
}
