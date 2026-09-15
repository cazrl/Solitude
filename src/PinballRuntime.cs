using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Solitude;

internal static class PinballRuntime
{
    internal const uint StateMessage=0x800c, PauseMessage=0x800b, CaptureMessage=0x800d;
    private const string EngineResource="Solitude.Pinball.Engine.dll";
    private const string DataResource="Solitude.Assets.Pinball.content.zip";
    internal static string? WorkerExecutableForChecks;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int RunEngine(nint host);

    internal static string Prepare(string dataDirectory)
    {
        // One deterministic cache shared by launches for this save directory. Verify every file
        // before executing native code, and repair interrupted or altered extractions atomically.
        string folder=Path.Combine(dataDirectory,"pinball","runtime");
        Directory.CreateDirectory(folder);
        using var guard=new FileStream(Path.Combine(folder,"extract.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var assembly=typeof(PinballRuntime).Assembly;
        using(var input=assembly.GetManifestResourceStream(EngineResource) ?? throw new InvalidDataException("Pinball engine is missing."))
            WriteVerified(input,Path.Combine(folder,"Solitude.Pinball.dll"));
        using var source=assembly.GetManifestResourceStream(DataResource) ?? throw new InvalidDataException("Pinball table data is missing.");
        using var archive=new ZipArchive(source,ZipArchiveMode.Read);
        foreach(var entry in archive.Entries)
        {
            if(entry.Name.Length==0 || entry.FullName!=entry.Name)throw new InvalidDataException("Invalid Pinball asset path.");
            using var input=entry.Open();WriteVerified(input,Path.Combine(folder,entry.Name));
        }
        return folder;
    }
    private static void WriteVerified(Stream input,string path)
    {
        using var buffer=new MemoryStream();input.CopyTo(buffer);byte[] bytes=buffer.ToArray();
        if(File.Exists(path))
        {
            using var current=File.OpenRead(path);
            if(current.Length==bytes.Length && CryptographicOperations.FixedTimeEquals(SHA256.HashData(current),SHA256.HashData(bytes)))return;
        }
        string temporary=path+".tmp";File.WriteAllBytes(temporary,bytes);File.Move(temporary,path,true);
    }
    internal static Process Start(nint host,string dataDirectory,string? evidence=null)
    {
        string runtime=Prepare(dataDirectory);
        string executable=WorkerExecutableForChecks??Path.Combine(AppContext.BaseDirectory,"Solitude.exe");
        if(!File.Exists(executable))executable=Environment.ProcessPath ?? throw new IOException("Cannot locate Solitude.exe.");
        var start=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,WorkingDirectory=runtime};
        start.ArgumentList.Add("--pinball-worker");start.ArgumentList.Add(host.ToInt64().ToString(System.Globalization.CultureInfo.InvariantCulture));
        start.ArgumentList.Add("--data-dir");start.ArgumentList.Add(dataDirectory);
        start.Environment["SOLITUDE_PINBALL_DATA"]=runtime;
        start.Environment["SOLITUDE_PINBALL_SETTINGS"]=Path.Combine(dataDirectory,"pinball","settings.ini");
        if(evidence!=null)start.Environment["SOLITUDE_PINBALL_EVIDENCE"]=evidence;
        else start.Environment.Remove("SOLITUDE_PINBALL_EVIDENCE");
        return Process.Start(start) ?? throw new IOException("Could not start Pinball.");
    }
    internal static int RunWorker(nint host,string dataDirectory)
    {
        if(!IsWindow(host))return 2;
        string runtime=Prepare(dataDirectory);
        nint library=NativeLibrary.Load(Path.Combine(runtime,"Solitude.Pinball.dll"));
        // The worker exits after one game window lifetime. Never reuse native global state.
        return Marshal.GetDelegateForFunctionPointer<RunEngine>(NativeLibrary.GetExport(library,"SolitudeRun"))(host);
    }
    internal static long Query(nint window,int field)
        =>SendMessageTimeout(window,StateMessage,(nint)field,0,2,500,out var value)!=0?(long)value:-1;
    [DllImport("user32.dll")] internal static extern bool IsWindow(nint window);
    [DllImport("user32.dll")] internal static extern nint GetWindow(nint window,uint command);
    [DllImport("user32.dll")] internal static extern bool PostMessage(nint window,uint message,nint wp,nint lp);
    [DllImport("user32.dll")] internal static extern nint SetFocus(nint window);
    [DllImport("user32.dll")] internal static extern bool MoveWindow(nint window,int x,int y,int width,int height,bool repaint);
    [DllImport("user32.dll")] internal static extern nint SendMessageTimeout(nint window,uint message,nint wp,nint lp,uint flags,uint timeout,out nuint result);
}
