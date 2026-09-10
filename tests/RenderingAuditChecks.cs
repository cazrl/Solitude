using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Solitude;

internal static partial class UiProgram
{
    private static byte[] FramePixels(Bitmap b)
    {
        var bits=b.LockBits(new(0,0,b.Width,b.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try{var bytes=new byte[bits.Stride*bits.Height];Marshal.Copy(bits.Scan0,bytes,0,bytes.Length);return bytes;}
        finally{b.UnlockBits(bits);}
    }
    private static void DrawFrame(GameWindow form,Bitmap b,Rectangle? clip=null)
    {using var g=Graphics.FromImage(b);if(clip.HasValue)g.SetClip(clip.Value);Call(form,"PaintScaled",g);}
    private static void SameFrame(Bitmap actual,Bitmap expected,string name)
    {
        bool same=FramePixels(actual).AsSpan().SequenceEqual(FramePixels(expected));
        if(!same)
        {
            Directory.CreateDirectory("artifacts/render-audit-failures");
            actual.Save($"artifacts/render-audit-failures/{name}-actual.png");expected.Save($"artifacts/render-audit-failures/{name}-expected.png");
        }
        Check(same,"Animation frame mismatch: "+name);
    }
    private static void CheckAnimationContinuity()
    {
        foreach(var kind in Enum.GetValues<GameKind>())foreach(int scale in new[]{100,125,150,200})
        {
            using var form=new GameWindow(new Store("artifacts/render-audit-isolated"),Era.WindowsVista,1,scale,true,kind);
            using var actual=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);using var expected=new Bitmap(actual.Width,actual.Height);
            for(int deck=0;deck<4;deck++)
            {
                form.Preferences.VistaDeck=deck;Set(form,"renderMotionTime",10.0);Call(form,"BeginCardMotion",true);
                Set(form,"renderMotionTime",10.1);DrawFrame(form,actual);
                // WM_PAINT can arrive after the flight deadline but before the next
                // animation message prunes it. No card may disappear in that gap.
                Set(form,"renderMotionTime",12.0);DrawFrame(form,actual);
                Call(form,"StopCardMotion");DrawFrame(form,expected);SameFrame(actual,expected,$"handoff-{kind}-{scale}-deck{deck}");
            }
            var table=(RectangleF)typeof(GameWindow).GetProperty("Table",Private)!.GetValue(form)!;
            Check(actual.GetPixel((int)MathF.Floor(table.X*scale/100f+.5001f),(int)MathF.Floor(table.Y*scale/100f+.5001f)).A==255,"Vista felt has a transparent repaint edge");

            Set(form,"renderMotionTime",20.0);Call(form,"BeginCardMotion",true);Set(form,"boardStamp",null);DrawFrame(form,actual);
            for(int frame=1;frame<=45;frame++)
            {
                Set(form,"renderMotionTime",20+frame/60.0);
                Call(form,"Animate");var damage=(Rectangle)typeof(GameWindow).GetMethod("FrameDamage",Private)!.Invoke(form,null)!;
                DrawFrame(form,actual,damage);DrawFrame(form,expected);SameFrame(actual,expected,$"deal-{kind}-{scale}-{frame}");
            }
            // Pausing on a partially flipped card must not change any pixels on
            // successive exposure/hover repaints at that same animation time.
            Set(form,"renderMotionTime",30.0);Call(form,"BeginCardMotion",true);Set(form,"renderMotionTime",30.11);DrawFrame(form,actual);DrawFrame(form,expected);
            SameFrame(actual,expected,$"repeat-{kind}-{scale}");
            Call(form,"StopCardMotion");Set(form,"renderMotionTime",40.0);Set(form,"previousDamage",RectangleF.Empty);
            if(kind!=GameKind.FreeCell)Call(form,"DrawCards");else Call(form,"Changed",form.Game.ToFreeCell(new(PileKind.Tableau,0)));
            DrawFrame(form,actual);
            for(int frame=1;frame<=30;frame++)
            {
                Set(form,"renderMotionTime",40+frame/120.0);Call(form,"Animate");
                var damage=(Rectangle)typeof(GameWindow).GetMethod("FrameDamage",Private)!.Invoke(form,null)!;
                DrawFrame(form,actual,damage);DrawFrame(form,expected);SameFrame(actual,expected,$"move-flip-{kind}-{scale}-{frame}");
            }
        }
        Console.WriteLine("PASS animation cache handoff and clipped/full dealing frames at every Vista scale");
    }
    private static void CheckImmediateInput()
    {
        foreach(var era in GameCatalog.HistoricalEras)foreach(var kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(era,k)))
        {
            using var form=new GameWindow(new Store("artifacts/input-audit-isolated"),era,1,100,true,kind);Paint(form);
            var before=form.Game.State;
            Key(form,Keys.Alt|Keys.G);Key(form,Keys.Down);Key(form,Keys.Enter);
            Check(!ReferenceEquals(before,form.Game.State),"Menu key sequence depended on an intervening paint: "+era+"/"+kind);
            Key(form,Keys.F6);Key(form,Keys.Enter);
            Check((DialogPage)Field(form,"dialog")! ==DialogPage.None,"Settings Enter was lost before first paint");
            Call(form,"OpenDialog",DialogPage.Options);
            if(kind==GameKind.FreeCell && era!=Era.WindowsVista)
            {
                bool quick=form.Preferences.FreeCellQuickPlay;Key(form,Keys.Alt|Keys.Q);Key(form,Keys.Enter);
                Check((DialogPage)Field(form,"dialog")! ==DialogPage.None && form.Preferences.FreeCellQuickPlay!=quick,"Enter toggled the focused checkbox instead of accepting Options");
            }
            else{Key(form,Keys.Escape);Check((DialogPage)Field(form,"dialog")! ==DialogPage.None,"Immediate dialog cancellation failed");}
        }
        foreach(var era in new[]{Era.WindowsMe,Era.WindowsXP})
        {
            using var form=new GameWindow(new Store("artifacts/checkpoint-audit-isolated"),era,1,100,true,GameKind.Spider);form.Game.Draw();
            Call(form,"SaveSpiderGame",false);Set(form,"showingVictory",true);Set(form,"victoryTrail",new Bitmap(form.Width,form.Height));
            Call(form,"OpenSpiderGame",false,false);
            Check(!(bool)Field(form,"showingVictory")! && Field(form,"victoryTrail")==null,"Opening a Spider checkpoint left old fireworks active");
        }
        Console.WriteLine("PASS immediate menu/dialog keyboard input and Spider checkpoint reset");
    }
}
