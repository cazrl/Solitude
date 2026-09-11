namespace Solitude;

internal static class RuntimeDiagnostics
{
    internal static void Write(string directory,Exception error,string context)
    {
        try
        {
            Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,"Solitude-error.txt");
            if(File.Exists(path) && new FileInfo(path).Length>1_000_000)File.Move(path,path+".previous",true);
            File.AppendAllText(path,$"{DateTimeOffset.Now:O} Solitude {typeof(Game).Assembly.GetName().Version}\n{context}\n{error}\n\n");
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException){}
    }
}

public sealed partial class GameWindow
{
    private bool orbitMotionFaulted;
    private bool OrbitMotionEnabled=>Preferences.Animate && !orbitMotionFaulted;
    internal string DiagnosticContext=>$"Edition={Preferences.Era}; Game={Kind}; Seed={Game.State.Seed}; Moves={Game.State.Moves}; Foundations={Game.State.Foundations.Sum(p=>p.Count)}; Flights={flights.Count}; Collecting={collecting}; Won={Game.State.Won}";
    internal bool TryRecoverOrbitUi(Exception error)
    {
        if(!skin.Future || editionMorph!=null || orbitMotionFaulted || error is not (ArgumentException or InvalidOperationException or System.Runtime.InteropServices.ExternalException or IndexOutOfRangeException or ArithmeticException))return false;
        // Never resume a partially mutated or invalid deal after an exception.
        try{Game.Validate(Game.State);}catch(InvalidDataException){return false;}
        orbitMotionFaulted=true;collecting=false;pendingWin=false;showingVictory=false;
        selection=null;dragging=false;pressedCard=false;pressedHotspot=null;Capture=false;
        flights.Clear();dragPoses.Clear();restingCards=[];futurePulses.Clear();orbitLogoAnimationStart=null;
        boardStamp=null;layoutState=null;hoverId=null;hint=null;
        notice="An interface error occurred. Your deal is intact. Animation is paused for this session; you can continue playing. Error details have been saved.";
        OpenDialog(DialogPage.Notice);ScheduleFrames();Invalidate();return true;
    }
}
