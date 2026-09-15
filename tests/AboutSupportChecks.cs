using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using Solitude;

internal static partial class UiProgram
{
    private static void CheckAboutSupport()
    {
        Directory.CreateDirectory("artifacts/about-support/previews");
        foreach (var era in Enum.GetValues<Era>())
        foreach (var kind in Enum.GetValues<GameKind>().Where(k => GameCatalog.Available(era, k)))
        foreach (int scale in new[] { 100, 125, 150, 200 })
        {
            using var form = FixForm(era, kind, scale);
            var opened = new List<string>();
            Set(form, "openExternalLink", (Action<string>)(url => opened.Add(url)));
            string state = JsonSerializer.Serialize(form.Game.State);
            Call(form, "OpenDialog", DialogPage.Settings); Paint(form);
            Check(!HasControl(form, "dialog-donate"), "Donate belongs inside About: " + era);
            FixControl(form, "dialog-app-about"); Paint(form);
            var donate = Hit(form, "dialog-donate"); var ok = Hit(form, "dialog-ok");
            var bounds = (RectangleF)Field(form, "dialogBounds")!;
            Check(bounds.Contains(donate) && !donate.IntersectsWith(ok), "Donate overlaps or escapes About: " + era);
            var text = (List<(string Text, RectangleF Bounds)>)Field(form, "dialogReadText")!;
            Check(text.Any(t => t.Text.Contains("Created by Cazrl")), "Creator credit missing: " + era);
            Check(text.All(t => !t.Bounds.IntersectsWith(donate) && !t.Bounds.IntersectsWith(ok)), "About text overlaps buttons: " + era);
            FixControl(form, "dialog-donate");
            Check(opened.SequenceEqual(new[] { "https://ko-fi.com/flightwire" }), "Wrong donation destination or duplicate launch");
            Check((DialogPage)Field(form, "dialog")! == DialogPage.AppAbout, "Donate closed About");
            Key(form, Keys.Alt | Keys.D);
            Check(opened.Count == 2, "Donate mnemonic did not launch exactly once");
            Check(JsonSerializer.Serialize(form.Game.State) == state, "Donate changed the deal");
            if (scale == 150 && kind == GameKind.Klondike)
                form.RenderTo($"artifacts/about-support/previews/{era}-about.png", DialogPage.AppAbout);
            Call(form, "OpenDialog", DialogPage.AppAbout); Paint(form);
            Key(form, Keys.Enter);
            Check((DialogPage)Field(form, "dialog")! == DialogPage.None && opened.Count == 2, "Default About action should close, not donate");
            Call(form, "OpenDialog", DialogPage.About); Paint(form);
            var gameDonate = Hit(form, "dialog-donate"); var gameOk = Hit(form, "dialog-ok");
            var gameText = (List<(string Text, RectangleF Bounds)>)Field(form, "dialogReadText")!;
            Check(gameText.Any(t => t.Text.Contains("Created by Cazrl")), "Game About omitted the recreation credit");
            Check(gameText.All(t => !t.Bounds.IntersectsWith(gameDonate) && !t.Bounds.IntersectsWith(gameOk)), "Game About text overlaps buttons");
            FixControl(form, "dialog-donate");
            Check(opened.Count == 3 && opened[^1] == "https://ko-fi.com/flightwire", "Game About Donate did not open Ko-fi");
            if (scale == 150 && kind == GameKind.Spider)
                form.RenderTo($"artifacts/about-support/previews/{era}-spider-about.png", DialogPage.About);
        }
        foreach (var era in new[] { Era.Windows30, Era.WindowsVista, Era.Future2126 })
        {
            using var form = FixForm(era);
            Set(form, "openExternalLink", (Action<string>)(_ => throw new Win32Exception("No browser")));
            Call(form, "OpenDialog", DialogPage.AppAbout); FixControl(form, "dialog-donate"); Paint(form);
            Check((DialogPage)Field(form, "dialog")! == DialogPage.Notice, "Browser failure escaped the UI");
            Check(((string)Field(form, "notice")!).Contains("https://ko-fi.com/flightwire"), "Browser failure omitted the usable URL");
        }
    }
}
