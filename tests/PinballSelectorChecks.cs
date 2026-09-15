using System.Drawing;
using System.Text.Json;
using Solitude;

internal static partial class UiProgram
{
    private static void CheckPinballSelector()
    {
        Directory.CreateDirectory("artifacts/pinball/selector");
        foreach(var era in Enum.GetValues<Era>())foreach(int scale in new[]{100,125,150,200})
        {
            using var form=FixForm(era,GameKind.Klondike,scale);
            form.Game.Draw();string state=JsonSerializer.Serialize(form.Game.State);
            Call(form,"OpenDialog",DialogPage.Settings);Paint(form);
            Check(HasControl(form,"dialog-game-pinball")== (era==Era.WindowsXP),"Pinball availability matches the XP integration scope");
            FixControl(form,"dialog-era-"+(int)Era.WindowsXP);Paint(form);
            Check(HasControl(form,"dialog-game-pinball"),"Switching to XP exposes Pinball from every edition");
            var pinball=Hit(form,"dialog-game-pinball");
            Check(!pinball.IntersectsWith(Hit(form,"dialog-game-Spider")) && !pinball.IntersectsWith(Hit(form,"dialog-scale-100")),"Pinball radio does not overlap another choice");
            FixControl(form,"dialog-game-pinball");Paint(form);
            Check((bool)Field(form,"draftPinball")!,"Pinball radio selects the Pinball launch draft");
            Check(JsonSerializer.Serialize(form.Game.State)==state,"Selecting Pinball does not mutate the current card game");
            if(era==Era.WindowsXP && scale==150)
            {using var image=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);using(var g=Graphics.FromImage(image))Call(form,"PaintScaled",g);image.Save("artifacts/pinball/selector/xp-pinball-settings.png");}
            FixControl(form,"dialog-game-FreeCell");Paint(form);
            Check(!(bool)Field(form,"draftPinball")!,"Selecting a card game clears the Pinball draft");
            FixControl(form,"dialog-game-pinball");FixControl(form,"dialog-era-"+(int)Era.WindowsVista);Paint(form);
            Check(!(bool)Field(form,"draftPinball")! && !HasControl(form,"dialog-game-pinball"),"Switching away from XP clears the Pinball draft");
            FixControl(form,"dialog-cancel");
            Check(JsonSerializer.Serialize(form.Game.State)==state,"Cancel leaves the original deal intact");
        }
    }
}
