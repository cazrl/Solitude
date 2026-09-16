using System.Drawing;
using System.Security.Cryptography;
using System.Text.Json;
using Solitude;

internal static partial class UiProgram
{
    private static string VictoryTrailHash(GameWindow form)
    {
        using var stream=new MemoryStream();((Bitmap)Field(form,"victoryTrail")!).Save(stream,System.Drawing.Imaging.ImageFormat.Png);
        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
    private static void CheckClassicVictory(bool captureOnly=false)
    {
        string folder="artifacts/classic-victory";Directory.CreateDirectory(folder);
        foreach(var era in captureOnly?new[]{Era.Windows98}:GameCatalog.HistoricalEras.Where(e=>e!=Era.WindowsVista))
        foreach(int scale in captureOnly?new[]{150}:new[]{100,125,150,200})
        {
            string? reference=null;
            foreach(int fps in new[]{60,120,144})
            {
                using var form=FixForm(era,scale:scale);form.Preferences.Sound=false;
                form.SetRenderState(CardVictoryState());Paint(form);Set(form,"renderMotionTime",100.0);Call(form,"StartVictory");
                string game=JsonSerializer.Serialize(form.Game.State);
                int maximum=0;
                for(int i=1;i<=fps*6;i++)
                {
                    Set(form,"renderMotionTime",100.0+i/(double)fps);Call(form,"Animate");
                    maximum=Math.Max(maximum,((System.Collections.ICollection)Field(form,"flying")!).Count);
                }
                string hash=VictoryTrailHash(form);
                if(captureOnly || era==Era.Windows98 && fps==144)
                {
                    using var frame=new Bitmap(form.Width,form.Height);using(var g=Graphics.FromImage(frame))Call(form,"PaintScaled",g);
                    frame.Save(Path.Combine(folder,$"{(captureOnly?"before":"after")}-{era}-{scale}-{fps}.png"));
                }
                Console.WriteLine($"{era} {scale}% {fps}fps: peak active cards={maximum}, trail={hash}");
                if(!captureOnly)
                {
                    Check(maximum==1,"Classic victory cards overpaint each other");
                    Check(reference==null || hash==reference,"Classic victory trails depend on display frame rate");
                    Check(JsonSerializer.Serialize(form.Game.State)==game,"Classic victory changes the won game");
                    string before=VictoryTrailHash(form);Call(form,"Animate");
                    Check(VictoryTrailHash(form)==before,"A repeated timestamp stamps another trail");
                    Key(form,Keys.Escape);Check((DialogPage)Field(form,"dialog")! ==DialogPage.Won,"Classic victory cannot be skipped");
                }
                reference=hash;
            }
        }
        if(captureOnly)return;
        using(var form=FixForm(Era.Windows98,scale:150))
        {
            form.Preferences.Sound=false;form.SetRenderState(CardVictoryState());Paint(form);
            string saved=JsonSerializer.Serialize(form.Game.State);
            Set(form,"renderMotionTime",100.0);Call(form,"StartVictory");
            int stamps=(int)Field(form,"victoryFrame")!;
            // Returning from a stalled/minimized window must not stamp a whole
            // animation's backlog in one callback.
            Set(form,"renderMotionTime",200.0);Call(form,"Animate");
            Check((int)Field(form,"victoryFrame")!-stamps<=8,"Stalled victory creates an unbounded catch-up burst");
            Check((bool)Field(form,"showingVictory")!,"Stalled victory ends before cards have left");
            var seen=new HashSet<int>();int step=0;
            while((bool)Field(form,"showingVictory")! && step<40000)
            {
                foreach(var card in (System.Collections.IEnumerable)Field(form,"flying")!)
                    seen.Add(((Card)card.GetType().GetField("Card")!.GetValue(card)!).Id);
                Set(form,"renderMotionTime",200.0+(++step)/40.0);Call(form,"Animate");
            }
            Check(step<40000 && seen.Count==52 && (int)Field(form,"victoryDealt")! ==52,"Classic celebration does not show all 52 cards and finish");
            Check((DialogPage)Field(form,"dialog")! ==DialogPage.Won,"Completed classic celebration omits results");
            Check(JsonSerializer.Serialize(form.Game.State)==saved,"Completed celebration changes cards, score or time");
            using var frame=new Bitmap(form.Width,form.Height);using(var g=Graphics.FromImage(frame))Call(form,"PaintScaled",g);
            frame.Save(Path.Combine(folder,"after-Windows98-complete.png"));
            Console.WriteLine($"PASS complete 52-card procession after {step} fixed steps; preserved won state and bounded catch-up");
        }
    }
}
