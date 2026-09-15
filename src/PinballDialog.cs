using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Solitude;

internal sealed class PinballDialog : Form
{
    private readonly Skin skin=new(Era.WindowsXP);
    private readonly string body;
    private readonly bool donation;
    private readonly float scale;
    private RectangleF close;
    private PointF pointer=new(-1,-1);
    private bool pressed;
    private int focus;
    internal Action<string> OpenLink=url=>Process.Start(new ProcessStartInfo(url){UseShellExecute=true});
    private RectangleF Ok=>new(334,Height/scale-43,80,25);
    private RectangleF Donate=>new(18,Height/scale-43,80,25);
    internal PinballDialog(string title,string text,float displayScale,bool donate)
    {
        body=text;donation=donate;scale=displayScale;
        Text=title;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.CenterParent;
        ShowInTaskbar=false;AutoScaleMode=AutoScaleMode.None;DoubleBuffered=true;
        ClientSize=new((int)(432*scale),(int)((donate?264:350)*scale));
        using var shape=skin.WindowShape(new(0,0,Width/scale,Height/scale));
        using var transform=new System.Drawing.Drawing2D.Matrix();transform.Scale(scale,scale);shape.Transform(transform);Region=new Region(shape);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.ScaleTransform(scale,scale);skin.Configure(g);
        close=skin.Frame(g,new(0,0,Width/scale,Height/scale),Text,modal:true,pointer:pointer,down:pressed)[2];
        Skin.Fill(g,skin.Face,new(3,30,Width/scale-6,Height/scale-33));
        skin.Wrapped(g,body,new(18,45,396,Height/scale-101));
        skin.Button(g,Ok,"OK",Ok.Contains(pointer),primary:true,focus:focus==0,pressed:pressed && Ok.Contains(pointer));
        if(donation)skin.Button(g,Donate,"Donate",Donate.Contains(pointer),focus:focus==1,pressed:pressed && Donate.Contains(pointer));
    }
    internal void DonateNow()
    {
        try{OpenLink("https://ko-fi.com/flightwire");}
        catch(Exception ex)when(ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {MessageBox.Show(this,"Could not open the browser. Visit https://ko-fi.com/flightwire","Donate",MessageBoxButtons.OK,MessageBoxIcon.Information);}
    }
    protected override void OnMouseMove(MouseEventArgs e){pointer=new(e.X/scale,e.Y/scale);Invalidate();base.OnMouseMove(e);}
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;pointer=new(e.X/scale,e.Y/scale);pressed=true;
        if(pointer.Y<30 && !close.Contains(pointer)){ReleaseCapture();SendMessage(Handle,0xa1,2,0);pressed=false;}Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);if(e.Button!=MouseButtons.Left)return;
        bool active=pressed;pressed=false;pointer=new(e.X/scale,e.Y/scale);Invalidate();if(!active)return;
        if(close.Contains(pointer) || Ok.Contains(pointer))Close();else if(donation && Donate.Contains(pointer))DonateNow();
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {
        if(keyData==Keys.Escape){Close();return true;}
        if(keyData==Keys.Enter || keyData==Keys.Space){if(focus==1 && donation)DonateNow();else Close();return true;}
        if(donation && keyData==(Keys.Alt|Keys.D)){DonateNow();return true;}
        if(keyData is Keys.Tab or (Keys.Shift|Keys.Tab)){focus=donation?1-focus:0;Invalidate();return true;}
        return base.ProcessCmdKey(ref msg,keyData);
    }
    protected override AccessibleObject CreateAccessibilityInstance()=>new DialogAccessibility(this);
    private sealed class DialogAccessibility(PinballDialog owner):ControlAccessibleObject(owner)
    {
        public override int GetChildCount()=>owner.donation?3:2;
        public override AccessibleObject? GetChild(int index)=>index switch
        {
            0=>new Element(owner,"OK",owner.Ok,AccessibleRole.PushButton,owner.Close),
            1=>new Element(owner,owner.body,new(18,45,396,owner.Height/owner.scale-101),AccessibleRole.StaticText,null),
            2 when owner.donation=>new Element(owner,"Donate",owner.Donate,AccessibleRole.PushButton,owner.DonateNow),_=>null
        };
    }
    private sealed class Element(PinballDialog owner,string label,RectangleF rect,AccessibleRole role,Action? action):AccessibleObject
    {
        public override string? Name{get=>label;set{}}
        public override AccessibleRole Role=>role;
        public override Rectangle Bounds=>owner.RectangleToScreen(Rectangle.Round(new RectangleF(rect.X*owner.scale,rect.Y*owner.scale,rect.Width*owner.scale,rect.Height*owner.scale)));
        public override void DoDefaultAction()=>action?.Invoke();
    }
    protected override void Dispose(bool disposing){if(disposing)skin.Dispose();base.Dispose(disposing);}
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern nint SendMessage(nint hwnd,uint message,nint wp,nint lp);
}
