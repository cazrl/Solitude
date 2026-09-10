namespace Solitude;

public enum GameKind { Klondike, FreeCell, Spider }

public static class GameCatalog
{
    public static readonly Era[] HistoricalEras=Enum.GetValues<Era>().Where(e=>e!=Era.Future2126).ToArray();
    public static bool Modern(Era era)=>era is Era.WindowsVista or Era.Future2126;
    public static string Name(GameKind kind) => kind switch { GameKind.FreeCell => "FreeCell", GameKind.Spider => "Spider Solitaire", _ => "Solitaire" };
    public static bool Available(Era era, GameKind kind) => kind switch
    {
        GameKind.Klondike => true,
        GameKind.FreeCell => era is not (Era.Windows30 or Era.Windows31 or Era.Future2126),
        GameKind.Spider => era is Era.WindowsMe or Era.WindowsXP or Era.WindowsVista,
        _ => false
    };
    public static int MaxDeal(Era era) => era is Era.WindowsXP or Era.WindowsVista ? 1_000_000 : 32_000;
    public static Preferences Defaults(Era era, GameKind kind, int scale = 150) => new()
    {
        Era = era, Scale = scale, VistaDeck = 1, CardBack = 6,
        Sound = Modern(era), SaveOnExit = era != Era.WindowsVista, ContinueSavedGame=era==Era.Future2126,
        ShowStatus = kind == GameKind.Klondike || era == Era.WindowsVista,
        Rules = new Rules
        {
            Kind = kind, Timed = kind != GameKind.FreeCell || era == Era.WindowsVista,
            Scoring = kind == GameKind.FreeCell ? Scoring.None : Scoring.Standard,
            AutoFlip = Modern(era) || kind == GameKind.Spider,
            UndoLimit = Modern(era) || kind == GameKind.Spider ? 0 : 1,
            FreeCellSupermoves = era == Era.WindowsVista, FreeCellEasterEggs=kind==GameKind.FreeCell && era!=Era.WindowsVista,
            SpiderFullUndo = era == Era.WindowsVista,
            DealMaximum = MaxDeal(era)
        }
    };
    public static string Key(Era era, GameKind kind) => $"{era}/{kind}";
    public static void ApplyPeriodPresentation(Preferences p)
    {
        p.QuickControls=false;p.HighlightTargets=false;p.ShowFrameRate=false;p.MotionDuration=210;p.FrameRate=120;
        p.Rules.FreeCellEasterEggs=p.Rules.Kind==GameKind.FreeCell && p.Era!=Era.WindowsVista;
        p.Rules.AutoFlip=Modern(p.Era) || p.Rules.Kind==GameKind.Spider;
        if(p.Era==Era.Future2126){p.MotionDuration=320;p.OutlineDragging=false;p.ShowStatus=true;p.DisplayTips=false;}
        if(p.Rules.Kind!=GameKind.Klondike)p.ShowStatus=p.Era==Era.WindowsVista;
    }
}
