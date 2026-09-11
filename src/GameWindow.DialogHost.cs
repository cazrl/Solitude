using System.Drawing.Drawing2D;

namespace Solitude;

public sealed partial class GameWindow
{
    private PeriodDialogHost? dialogHost;
    private bool dialogHostQueued;
    private void QueueDialogHost()
    {
        if(ephemeral || !Visible || dialog==DialogPage.None || dialogHost!=null || dialogHostQueued)return;
        dialogHostQueued=true;
        BeginInvoke(new Action(()=>
        {
            dialogHostQueued=false;if(IsDisposed || dialog==DialogPage.None || dialogHost!=null)return;
            dialogHost=new PeriodDialogHost(this);dialogHost.RefreshSurface();
            Enabled=false;dialogHost.Show(this);Invalidate();
        }));
    }
    private void CloseDialogHost()
    {
        if(dialogHost==null)return;
        var host=dialogHost;dialogHost=null;Enabled=true;host.Dispose();
        if(Visible)Activate();
    }
    protected override void OnInvalidated(InvalidateEventArgs e)
    {
        base.OnInvalidated(e);
        if(dialog==DialogPage.None)CloseDialogHost();else dialogHost?.Invalidate();
    }
    // A real owned window supplies activation, modal input and unbounded caption
    // movement. The era renderer still supplies its frame and controls.
    private sealed class PeriodDialogHost : Form
    {
        private readonly GameWindow game;
        private Bitmap? surface;
        private bool positioned;
        protected override AccessibleObject CreateAccessibilityInstance()=>new OrbitAccessibleRoot(this,game,true);
        public PeriodDialogHost(GameWindow game)
        {
            this.game=game;Owner=game;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;
            StartPosition=FormStartPosition.Manual;AutoScaleMode=AutoScaleMode.None;
            DoubleBuffered=true;KeyPreview=true;
            SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);
        }
        public void RefreshSurface()
        {
            if(game.dialog==DialogPage.None)return;
            game.hotspots.RemoveAll(h=>h.Id.StartsWith("dialog-") || h.Id=="modal-close");
            using(var probe=new Bitmap(1,1))using(var g=Graphics.FromImage(probe))game.PaintDialog(g);
            var bounds=game.dialogBounds;float scale=game.ScaleFactor;
            var size=new Size((int)MathF.Ceiling(bounds.Width*scale),(int)MathF.Ceiling(bounds.Height*scale));
            bool resized=surface==null || surface.Size!=size;
            if(resized){surface?.Dispose();surface=new Bitmap(size.Width,size.Height);ClientSize=size;}
            if(!positioned){Location=game.PointToScreen(new((int)(bounds.X*scale),(int)(bounds.Y*scale)));positioned=true;}
            game.hotspots.RemoveAll(h=>h.Id.StartsWith("dialog-") || h.Id=="modal-close");
            using(var g=Graphics.FromImage(surface!))
            {
                g.Clear(game.skin.Face);g.TranslateTransform(-bounds.X*scale,-bounds.Y*scale);g.ScaleTransform(scale,scale);
                game.skin.Configure(g);game.PaintDialog(g);
            }
            if(resized)
            {
                // Replacing a visible window region invalidates the window. Do
                // it only on resize, rather than generating another paint here.
                using var shape=game.skin.WindowShape(new(0,0,bounds.Width,bounds.Height));
                using var transform=new Matrix();transform.Scale(scale,scale);shape.Transform(transform);
                var old=Region;Region=new Region(shape);old?.Dispose();
            }
        }
        protected override void OnPaint(PaintEventArgs e){RefreshSurface();if(surface!=null)e.Graphics.DrawImageUnscaled(surface,0,0);}
        private PointF World(Point point){var bounds=game.dialogBounds;return new(point.X/game.ScaleFactor+bounds.X,point.Y/game.ScaleFactor+bounds.Y);}
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Left)return;game.mouse=World(e.Location);RefreshSurface();
            var hit=game.hotspots.LastOrDefault(h=>h.Enabled && h.Bounds.Contains(game.mouse));
            if(hit!=null){game.pressedHotspot=hit.Id;Capture=true;Invalidate();return;}
            if(e.Y<(game.skin.Border+game.skin.Caption)*game.ScaleFactor)
            {ReleaseCapture();SendMessage(Handle,0xA1,2,0);}
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            var point=World(e.Location);game.OnMouseDoubleClick(new MouseEventArgs(e.Button,e.Clicks,(int)(point.X*game.ScaleFactor),(int)(point.Y*game.ScaleFactor),e.Delta));
        }
        protected override void OnMouseMove(MouseEventArgs e){game.mouse=World(e.Location);Invalidate();}
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Left)return;game.mouse=World(e.Location);
            var hit=game.hotspots.LastOrDefault(h=>h.Enabled && h.Id==game.pressedHotspot && h.Bounds.Contains(game.mouse));
            game.pressedHotspot=null;Capture=false;hit?.Action();if(!IsDisposed)Invalidate();game.Invalidate();
        }
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture){game.pressedHotspot=null;Invalidate();}}
        protected override void OnMouseWheel(MouseEventArgs e){game.OnMouseWheel(e);Invalidate();}
        protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
        {
            if(keyData==(Keys.Alt|Keys.F4)){game.CloseDialog();return true;}
            RefreshSurface();bool handled=game.ProcessCmdKey(ref msg,keyData);if(!IsDisposed)Invalidate();return handled;
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if(game.dialogHost==this){e.Cancel=true;game.CloseDialog();}base.OnFormClosing(e);
        }
        protected override void Dispose(bool disposing){if(disposing){surface?.Dispose();surface=null;}base.Dispose(disposing);}
    }
}
