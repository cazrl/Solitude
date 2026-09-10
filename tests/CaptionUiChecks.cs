using System.Drawing;
using System.Drawing.Imaging;
using Solitude;

internal static partial class UiProgram
{
    private static Icon CaptionTestIcon()
    {
        using var bitmap=new Bitmap(16,16);using(var g=Graphics.FromImage(bitmap))g.Clear(Color.Fuchsia);
        using var png=new MemoryStream();bitmap.Save(png,ImageFormat.Png);byte[] bytes=png.ToArray();using var file=new MemoryStream();
        using(var writer=new BinaryWriter(file,System.Text.Encoding.UTF8,true))
        {writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)1);writer.Write((byte)16);writer.Write((byte)16);writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(bytes.Length);writer.Write(22);writer.Write(bytes);}
        file.Position=0;using var icon=new Icon(file);return (Icon)icon.Clone();
    }
    private static void CheckCaptionRendering()
    {
        // Segoe UI's capital I is a plain vertical stroke. The previous Vista
        // mask silently painted a serif face even while Font.Name said Segoe UI.
        foreach(float scale in new[]{1f,1.25f,1.5f,2f})
        {
            using var skin=new Skin(Era.WindowsVista);using var bitmap=new Bitmap(100,100);
            using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.White);g.ScaleTransform(scale,scale);skin.Text(g,"I",new(5,5,30,30));}
            int left=100,right=-1,top=100,bottom=-1;
            for(int y=0;y<100;y++)for(int x=0;x<100;x++)if(bitmap.GetPixel(x,y).R<160)
            {left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
            Check(right>=left && right-left+1<=MathF.Ceiling(2*scale) && bottom-top+1>=7*scale,$"Vista text substituted a serif face at {scale}");
        }
        // Independent 96-DPI measurements from original screenshots documented in
        // docs/FRAME-ALIGNMENT.md, not a snapshot of this renderer's constants.
        using(var vista=new Skin(Era.WindowsVista))
        {
            var r=vista.CaptionLayout(new(0,0,252,146));
            Check(r.Header.Height==27 && r.Icon==new RectangleF(8,7,16,16) && r.Title.X==28,"Vista caption does not match the standard DWM reference");
            Check(r.Minimize==new RectangleF(151,1,26,18) && r.Maximize==new RectangleF(177,1,25,18) && r.Close==new RectangleF(202,1,43,18),"Vista caption buttons do not match the DWM reference");
            var maximized=vista.CaptionLayout(new(0,0,720,540),maximized:true);
            Check(maximized.Header.Height==21 && maximized.Close.Right==720 && maximized.Icon.X==2,"Vista maximized caption retained restored-window margins");
        }
        using(var xp=new Skin(Era.WindowsXP))
        {
            var r=xp.CaptionLayout(new(0,0,260,260));
            Check(r.Icon==new RectangleF(5,8,16,16) && r.Title.X==25 && r.Close.X==235 && r.Maximize.X==212 && r.Minimize.X==189,"XP caption does not match the Luna reference");
        }
        using var sentinel=CaptionTestIcon();
        foreach(var era in new[]{Era.Windows95,Era.Windows98,Era.WindowsMe,Era.Windows2000,Era.WindowsXP,Era.WindowsVista})foreach(float scale in new[]{1f,1.25f,1.5f,2f})foreach(var offset in new[]{PointF.Empty,new PointF(23,17)})
        {
            using var skin=new Skin(era){GameIcon=sentinel};using var bitmap=new Bitmap(800,500);var bounds=new RectangleF(offset.X,offset.Y,320,200);
            using(var g=Graphics.FromImage(bitmap)){g.ScaleTransform(scale,scale);skin.Configure(g);skin.Frame(g,bounds,"Caption");}
            int left=bitmap.Width,top=bitmap.Height,right=-1,bottom=-1;
            for(int y=0;y<Math.Ceiling((offset.Y+35)*scale);y++)for(int x=0;x<Math.Ceiling((offset.X+45)*scale);x++)
            {var c=bitmap.GetPixel(x,y);if(c.R>230 && c.B>230 && c.G<30){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}}
            var icon=skin.CaptionLayout(bounds).Icon;var expected=Rectangle.FromLTRB((int)MathF.Round(icon.Left*scale),(int)MathF.Round(icon.Top*scale),(int)MathF.Round(icon.Right*scale),(int)MathF.Round(icon.Bottom*scale));
            Check(right>=left && Math.Abs(left-expected.Left)<=1 && Math.Abs(top-expected.Top)<=1 && Math.Abs(right+1-expected.Right)<=1 && Math.Abs(bottom+1-expected.Bottom)<=1,$"Icon escaped caption scale/translation: {era}/{scale}/{offset}");
        }
        foreach(var era in new[]{Era.WindowsXP,Era.WindowsVista})foreach(int scale in new[]{100,125,150,200})
        {
            using var form=new GameWindow(new Store("artifacts/caption-hit-check"),era,1,scale,true);Paint(form);
            foreach(var id in new[]{"minimize","maximize","close","system"})
            {
                var r=Hit(form,id);var local=new Point((int)MathF.Round((r.X+r.Width/2)*scale/100f),(int)MathF.Ceiling((r.Y+.75f)*scale/100f));var screen=form.PointToScreen(local);
                var packed=unchecked((int)((uint)(ushort)screen.X|((uint)(ushort)screen.Y<<16)));object[] args=[new Message{HWnd=form.Handle,Msg=0x84,LParam=(nint)packed}];
                typeof(GameWindow).GetMethod("WndProc",Private)!.Invoke(form,args);
                Check(((Message)args[0]).Result==1,$"Native resize edge intercepts {id}: {era}/{scale}");
            }
            var help=Hit(form,"bar-1");Click(form,help);Paint(form);
            var item=((System.Collections.IEnumerable)Field(form,"hotspots")!).Cast<object>().First(h=>((string)h.GetType().GetProperty("Id")!.GetValue(h)!).StartsWith("menu-item-"));
            var row=(RectangleF)item.GetType().GetProperty("Bounds")!.GetValue(item)!;Check(Math.Abs(row.Left-help.Left)<10,"Help popup is detached from its measured menu label: "+era);
            Set(form,"menu",-1);Set(form,"maximized",true);Paint(form);
            if(era==Era.WindowsVista)
            {
                var table=(RectangleF)typeof(GameWindow).GetProperty("Table",Private)!.GetValue(form)!;
                Check(table.Left==0 && table.Right==form.ClientSize.Width/(scale/100f) && table.Top==41,"Maximized Vista client has a false outer frame");
            }
        }
        Console.WriteLine("PASS Vista sans-serif glyphs, reference caption geometry, rendered icon scaling, native hit testing, menu anchors and maximized Vista client bounds");
    }
}
