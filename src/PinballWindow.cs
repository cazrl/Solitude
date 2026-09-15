using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Solitude;

internal sealed class PinballWindow : Form
{
    private readonly Skin skin=new(Era.WindowsXP);
    private readonly Panel surface=new(){BackColor=Color.Black,TabStop=true};
    private readonly System.Windows.Forms.Timer watch=new(){Interval=100};
    private readonly string directory;
    private Process? worker;
    private nint engine;
    private float scale;
    private RectangleF[] caption=[];
    private PointF pointer=new(-1,-1);
    private bool pressed,closing,ready,maximized;
    private string? error;
    private Rectangle normalBounds;
    private readonly Stopwatch startup=new();
    private PinballMenu? activeMenu;
    private int activeMenuIndex=-1;
    internal PinballMenu? ActiveMenu=>activeMenu;
    internal bool ReturnToSettings {get;private set;}
    internal nint Engine=>engine;
    internal PinballWindow(string dataDirectory,int displayScale,Rectangle area)
    {
        directory=dataDirectory;
        AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;
        Text="3D Pinball for Windows - Space Cadet";
        using(var iconStream=typeof(PinballWindow).Assembly.GetManifestResourceStream("Solitude.Assets.Pinball.pinball.ico"))
            if(iconStream!=null){Icon=new Icon(iconStream);skin.GameIcon=Icon;}
        StartPosition=FormStartPosition.Manual;KeyPreview=true;
        scale=Math.Min(displayScale/100f,Math.Min(area.Width/606f,area.Height/466f));
        ClientSize=new((int)(606*scale),(int)((416+skin.Top+skin.Border)*scale));
        Location=new(area.Left+(area.Width-Width)/2,area.Top+(area.Height-Height)/2);
        MinimumSize=new(480,360);
        SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);
        Controls.Add(surface);LayoutSurface();
        surface.MouseDown+=(_,_)=>{DismissMenu();FocusEngine();};
        watch.Tick+=(_,_)=>PollEngine();
        Shown+=(_,_)=>StartEngine();
    }
    private void StartEngine()
    {
        if(worker!=null)return;
        try{worker=PinballRuntime.Start(surface.Handle,directory);startup.Restart();watch.Start();}
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or InvalidDataException){Fail(ex.Message);}
    }
    private void PollEngine()
    {
        if(closing)return;
        if(worker?.HasExited==true){Fail("The Pinball engine closed unexpectedly. Your card games are safe.\n\nExit code: "+worker.ExitCode);return;}
        if(ready)return;
        engine=PinballRuntime.GetWindow(surface.Handle,5);
        if(engine!=0 && PinballRuntime.Query(engine,0)==1){ready=true;LayoutSurface();FocusEngine();Invalidate();return;}
        if(startup.Elapsed.TotalSeconds>20)Fail("Pinball did not finish starting. You can return to Settings and try again.");
    }
    private void Fail(string message){error=message;watch.Stop();surface.Visible=false;Invalidate();}
    private void FocusEngine(){if(ready && !closing)PinballRuntime.SetFocus(engine);}
    private void LayoutSurface()
    {
        int border=(int)(skin.Border*scale),top=(int)(skin.Top*scale);
        surface.Bounds=new(border,top,Math.Max(1,ClientSize.Width-2*border),Math.Max(1,ClientSize.Height-top-border));
        if(engine!=0)PinballRuntime.MoveWindow(engine,0,0,surface.Width,surface.Height,true);
        if(scale>0)
        {
            using var shape=skin.WindowShape(new(0,0,ClientSize.Width/scale,ClientSize.Height/scale),maximized);
            using var transform=new Matrix();transform.Scale(scale,scale);shape.Transform(transform);
            var old=Region;Region=new Region(shape);old?.Dispose();
        }
    }
    protected override void OnResize(EventArgs e){base.OnResize(e);LayoutSurface();Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.ScaleTransform(scale,scale);skin.Configure(g);
        float w=ClientSize.Width/scale,h=ClientSize.Height/scale;
        caption=skin.Frame(g,new(0,0,w,h),Text,pointer:pointer,down:pressed,maximized:maximized);
        skin.MenuBar(g,new(skin.Border,skin.Border+skin.Caption,w-2*skin.Border,skin.MenuHeight));
        for(int i=0;i<3;i++)
        {
            var r=MenuRect(i);skin.MenuItem(g,r,activeMenuIndex==i || r.Contains(pointer),bar:true);
            skin.Text(g,new[]{"Game","Options","Help"}[i],new(r.X+5,r.Y,r.Width-10,r.Height));
        }
        skin.Button(g,SettingsRect,"Settings...",SettingsRect.Contains(pointer),pressed:pressed && SettingsRect.Contains(pointer));
        if(!ready || error!=null)
        {
            Skin.Fill(g,Color.Black,new(skin.Border,skin.Top,w-2*skin.Border,h-skin.Top-skin.Border));
            using var brush=new SolidBrush(Color.White);using var font=new Font("Tahoma",10);
            g.DrawString(error??"Loading Space Cadet...",font,brush,new RectangleF(24,skin.Top+25,w-48,150));
        }
    }
    private RectangleF SettingsRect=>new(ClientSize.Width/scale-85-skin.Border,skin.Border+skin.Caption,83,skin.MenuHeight);
    private RectangleF MenuRect(int index)=>new(index==0?5:index==1?50:109,skin.Border+skin.Caption,index==1?59:45,skin.MenuHeight);
    private int MenuAt(PointF p){for(int i=0;i<3;i++)if(MenuRect(i).Contains(p))return i;return -1;}
    private void DismissMenu()=>activeMenu?.Close(ToolStripDropDownCloseReason.CloseCalled);
    internal void StartForChecks(string evidence)
    {
        _=Handle;_=surface.Handle;worker=PinballRuntime.Start(surface.Handle,directory,evidence);startup.Restart();watch.Start();
    }
    internal void RenderTo(string path)
    {
        using var bitmap=new Bitmap(Width,Height);
        using(var g=Graphics.FromImage(bitmap))OnPaint(new PaintEventArgs(g,ClientRectangle));
        if(ready)
        {
            using var table=new Bitmap(surface.Width,surface.Height);
            using(var g=Graphics.FromImage(table))
            {
                nint dc=g.GetHdc();
                try{if(!PrintWindow(engine,dc,1))throw new InvalidOperationException("Pinball capture failed.");}
                finally{g.ReleaseHdc(dc);}
            }
            using var composite=Graphics.FromImage(bitmap);composite.DrawImageUnscaled(table,surface.Location);
        }
        bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);pointer=new(e.X/scale,e.Y/scale);
        int index=MenuAt(pointer);if(activeMenu!=null && index>=0 && index!=activeMenuIndex)ShowMenu(index);
        Invalidate(new Rectangle(0,0,Width,(int)(skin.Top*scale)));
    }
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);pointer=new(-1,-1);Invalidate();}
    protected override void OnDeactivate(EventArgs e){DismissMenu();base.OnDeactivate(e);}
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;
        pointer=new(e.X/scale,e.Y/scale);pressed=true;Invalidate();
        if(MenuAt(pointer)<0)DismissMenu();
        if(pointer.Y<skin.Border+skin.Caption && !caption.Any(r=>r.Contains(pointer)))
        {ReleaseCapture();SendMessage(Handle,0xa1,2,0);pressed=false;}
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);if(e.Button!=MouseButtons.Left)return;
        bool activate=pressed;pressed=false;pointer=new(e.X/scale,e.Y/scale);Invalidate();if(!activate)return;
        if(caption.Length==3)
        {
            if(caption[2].Contains(pointer)){Close();return;}
            if(caption[1].Contains(pointer)){ToggleSize();return;}
            if(caption[0].Contains(pointer)){WindowState=FormWindowState.Minimized;return;}
        }
        if(SettingsRect.Contains(pointer)){ReturnToSettings=true;Close();return;}
        int index=MenuAt(pointer);if(index>=0)ShowMenu(index);else DismissMenu();
    }
    private void ToggleSize()
    {
        if(!maximized){normalBounds=Bounds;Bounds=Screen.FromControl(this).WorkingArea;}
        else Bounds=normalBounds;
        maximized=!maximized;LayoutSurface();Invalidate();FocusEngine();
    }
    private void Command(int id){if(ready)PinballRuntime.PostMessage(engine,0x111,id,0);}
    internal void ShowMenu(int index)
    {
        DismissMenu();
        var menu=new PinballMenu(scale);activeMenu=menu;activeMenuIndex=index;
        void Item(string text,Action action,bool enabled=true,bool check=false)
        {
            var parts=text.Split('\t');
            var item=new ToolStripMenuItem(parts[0]){ShortcutKeyDisplayString=parts.Length>1?parts[1]:"",Enabled=enabled,Checked=check};
            item.Click+=(_,_)=>action();menu.Items.Add(item);
        }
        if(index==0)
        {
            Item("&New Game\tF2",()=>Command(101),ready);
            Item("&Launch Ball\tSpace",()=>Command(401),ready);
            Item("&Pause / Resume\tF3",()=>Command(402),ready,ready && PinballRuntime.Query(engine,2)==1);
            Item("&High Scores...",()=>Command(103),ready);
            Item("&Demo",()=>Command(404),ready);
            menu.Items.Add(new ToolStripSeparator());
            Item("Back to &Settings...\tF6",()=>{ReturnToSettings=true;Close();});
            Item("E&xit",Close);
        }
        else if(index==1)
        {
            Item("&Sounds",()=>Command(201),ready,ready && PinballRuntime.Query(engine,5)==1);
            Item("&Music",()=>Command(202),ready,ready && PinballRuntime.Query(engine,6)==1);
            Item("&Player Controls...",()=>Command(406),ready);
            var players=new ToolStripMenuItem("&Players");
            for(int i=1;i<=4;i++){int n=i;var item=new ToolStripMenuItem(n==1?"1 Player":$"{n} Players"){Checked=ready && PinballRuntime.Query(engine,7)==n,Enabled=ready};item.Click+=(_,_)=>Command(407+n);players.DropDownItems.Add(item);}menu.Items.Add(players);
            menu.Items.Add(new ToolStripSeparator());Item("&Maximize / Restore\tF4",ToggleSize);
        }
        else
        {
            Item("&How to Play\tF1",ShowHelp);
            Item("&About Space Cadet...",ShowAbout);
        }
        menu.Closed+=(_,e)=>
        {
            if(activeMenu==menu){activeMenu=null;activeMenuIndex=-1;Invalidate();}
            if(closing || IsDisposed){menu.Dispose();return;}
            BeginInvoke(()=>
            {
                menu.Dispose();
                // Closing a menu because another application gained focus must
                // not activate the foreign-process table again.
                if(activeMenu==null && ActiveForm==this && e.CloseReason!=ToolStripDropDownCloseReason.AppFocusChange)FocusEngine();
            });
        };
        menu.Prepare();
        menu.Show(this,new Point((int)(MenuRect(index).X*scale),(int)(skin.Top*scale)));
        Invalidate();
    }
    private void ShowHelp()
    {
        using var help=new PinballDialog("Space Cadet Help","SPACE CADET\n\nHold Space to pull the plunger; release to launch.\nZ: left flipper     /: right flipper\nX: nudge left     .: nudge right     Up: nudge bottom\nToo much nudging causes a tilt.\n\nHit the mission targets and follow the table's message panel to select and complete missions. Complete missions to increase your rank.\n\nF2: new game     F3: pause     F4: maximize\nF6: return to Solitude Settings\n\nOptions > Player Controls changes the keys. Options > Players selects one to four players.",scale,false);
        help.ShowDialog(this);FocusEngine();
    }
    private void ShowAbout()
    {
        using var about=CreateAboutDialog();
        about.ShowDialog(this);FocusEngine();
    }
    internal PinballDialog CreateAboutDialog()=>new("About Space Cadet","3D Pinball for Windows - Space Cadet\n\nSolitude integration created by Cazrl\n\nEngine: Andrey Muzychenko and contributors (MIT).\nOriginal game: Cinematronics / Maxis / Microsoft.\nOriginal artwork and sounds retain their own rights.\n\nWindows Classic engine, hosted inside Solitude.",scale,true);
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {
        switch(keyData){case Keys.F1:ShowHelp();return true;case Keys.F2:Command(101);return true;case Keys.F3:Command(402);return true;case Keys.F4:ToggleSize();return true;case Keys.F6:ReturnToSettings=true;Close();return true;}
        return base.ProcessCmdKey(ref msg,keyData);
    }
    protected override void WndProc(ref Message m)
    {
        if(m.Msg==0x8015){DismissMenu();return;}
        if(m.Msg==0x8014){switch((Keys)m.WParam){case Keys.F1:ShowHelp();break;case Keys.F4:ToggleSize();break;case Keys.F6:ReturnToSettings=true;Close();break;}return;}
        if(m.Msg==0x84 && !maximized && scale>0)
        {
            long point=m.LParam.ToInt64();var p=PointToClient(new(unchecked((short)point),unchecked((short)(point>>16))));
            int edge=Math.Max(3,(int)(3*scale));bool left=p.X<edge,right=p.X>=Width-edge,top=p.Y<edge,bottom=p.Y>=Height-edge;
            int hit=top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:0;
            if(hit!=0){m.Result=hit;return;}
        }
        base.WndProc(ref m);
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        closing=true;DismissMenu();watch.Stop();
        if(worker!=null && !worker.HasExited)
        {
            if(engine!=0)PinballRuntime.PostMessage(engine,0x10,0,0);
            if(!worker.WaitForExit(2000))worker.Kill();
        }
        base.OnFormClosing(e);
    }
    protected override void Dispose(bool disposing)
    {if(disposing){watch.Dispose();worker?.Dispose();skin.Dispose();}base.Dispose(disposing);}
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern bool PrintWindow(nint hwnd,nint dc,uint flags);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint hwnd,uint message,nint wp,nint lp);
}
