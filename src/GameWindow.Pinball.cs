namespace Solitude;

public sealed partial class GameWindow
{
    private bool draftPinball;
    private Bitmap? pinballPreview;
    private Icon? pinballPreviewIcon;
    private void EnsurePinballPreview()
    {
        var assembly=typeof(GameWindow).Assembly;
        if(pinballPreview==null)
        {
            using var stream=assembly.GetManifestResourceStream("Solitude.Assets.Pinball.preview.bmp")!;
            using var decoded=new Bitmap(stream);pinballPreview=new Bitmap(decoded);
            using var iconStream=assembly.GetManifestResourceStream("Solitude.Assets.Pinball.pinball.ico")!;
            pinballPreviewIcon=new Icon(iconStream);
        }
    }
    private void PaintPinballPreview(Graphics g,RectangleF table)
    {
        EnsurePinballPreview();Skin.Fill(g,Color.Black,table);
        float ratio=Math.Min(table.Width/pinballPreview!.Width,table.Height/pinballPreview.Height);
        var target=new RectangleF(table.X+(table.Width-pinballPreview.Width*ratio)/2,table.Y,pinballPreview.Width*ratio,pinballPreview.Height*ratio);
        var state=g.Save();g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(pinballPreview,target);g.Restore(state);
    }
    internal static bool PinballAvailable(Era era)=>era==Era.WindowsXP;

    internal void LaunchPinball()
    {
        if(ephemeral)return;
        FinishEditionMorph();CloseDialog();CloseDialogHost();CancelDrag();StopCardMotion();
        if(!Save()){notice="Could not save your card game before opening Pinball.\n\n"+(saves.Error??store.Warning);OpenDialog(DialogPage.Notice);return;}
        string path=Path.Combine(store.DirectoryPath,"pinball");Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path,"selected"),"Space Cadet\n");
        var area=Screen.FromControl(this).WorkingArea;
        clock.Stop();animation.Stop();Hide();
        try
        {
            using var pinball=new PinballWindow(store.DirectoryPath,Preferences.Scale,area);
            pinball.ShowDialog();
            if(pinball.ReturnToSettings)
            {
                File.Delete(Path.Combine(path,"selected"));
                Show();Activate();OpenDialog(DialogPage.Settings);
                draft!.Era=Era.WindowsXP;draftPinball=true;
            }
            else {shutdown=true;Close();}
        }
        finally
        {
            if(!IsDisposed && !shutdown){lastTick=Environment.TickCount64;clock.Start();ScheduleFrames();Show();}
        }
    }
}
