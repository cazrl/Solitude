namespace Solitude;

// Only immutable snapshots cross this boundary. At most one write and one newer snapshot exist.
public sealed class SaveCoordinator(Store store) : IDisposable
{
    private readonly object gate=new();
    private (long Revision,SaveFile Snapshot)? pending;
    private Task? worker;
    private long requested,completed;
    private bool disposed,lastSuccess=true;
    private string? error;
    public string? Error {get{lock(gate)return error;}}
    public bool Busy {get{lock(gate)return completed<requested;}}
    public long Queue(SaveFile snapshot)
    {
        lock(gate)
        {
            ObjectDisposedException.ThrowIf(disposed,this);
            long revision=++requested;pending=(revision,snapshot);
            if(worker==null)worker=Task.Run(WriteLoop);
            return revision;
        }
    }
    private void WriteLoop()
    {
        while(true)
        {
            (long Revision,SaveFile Snapshot) item;
            lock(gate){if(pending==null){worker=null;Monitor.PulseAll(gate);return;}item=pending.Value;pending=null;}
            bool success;string? message;
            try{success=store.Save(item.Snapshot);message=store.Warning;}
            catch(Exception ex) when(ex is not OutOfMemoryException)
            {success=false;message="Your game could not be saved: "+ex.Message;}
            lock(gate){completed=item.Revision;lastSuccess=success;error=success?null:message;Monitor.PulseAll(gate);}
        }
    }
    public bool Flush(SaveFile snapshot)
    {
        long revision=Queue(snapshot);
        lock(gate){while(completed<revision)Monitor.Wait(gate);return lastSuccess;}
    }
    public void Dispose()
    {
        Task? task;lock(gate){if(disposed)return;disposed=true;task=worker;}task?.GetAwaiter().GetResult();
    }
}
