namespace Solitude;

// Nullable entries distinguish a choice from a preset's factory default.
public sealed class SharedSettings
{
    public int Scale { get; set; }=150;
    public int? ClassicBack { get; set; }
    public int? XpBack { get; set; }
    public int? VistaDeck { get; set; }
    public int? VistaBackground { get; set; }
    public bool? VistaAnimate { get; set; }
    public bool? VistaSave { get; set; }
    public bool? VistaContinue { get; set; }
    public bool? VistaSound { get; set; }
    public bool? VistaTips { get; set; }
    public int? DrawCount { get; set; }
    public Scoring? Scoring { get; set; }
    public bool? Timed { get; set; }
    public bool? Status { get; set; }
    public bool? Outline { get; set; }
    public bool? KeepScore { get; set; }
    public bool? FreeCellMessages { get; set; }
    public bool? FreeCellQuickPlay { get; set; }
    public bool? FreeCellDoubleClick { get; set; }
    public int? SpiderSuits { get; set; }
    public bool? SpiderAnimate { get; set; }
    public bool? SpiderAutoSave { get; set; }
    public bool? SpiderAutoOpen { get; set; }
    public bool? SpiderPromptSave { get; set; }
    public bool? SpiderPromptOpen { get; set; }
    public bool? SpiderSound { get; set; }
    public SharedSettings Clone()=>(SharedSettings)MemberwiseClone();
    public void Capture(Preferences p)
    {
        Scale=p.Scale;
        if(p.Era==Era.WindowsVista){VistaDeck=p.VistaDeck;VistaBackground=p.VistaBackground;VistaAnimate=p.Animate;VistaSave=p.SaveOnExit;VistaContinue=p.ContinueSavedGame;VistaSound=p.Sound;VistaTips=p.DisplayTips;}
        if(p.Rules.Kind==GameKind.Klondike)
        {
            DrawCount=p.Rules.DrawCount;Scoring=p.Rules.Scoring;
            if(p.Era!=Era.WindowsVista){Timed=p.Rules.Timed;Status=p.ShowStatus;Outline=p.OutlineDragging;KeepScore=p.Rules.KeepVegasScore;if(p.Era==Era.WindowsXP)XpBack=p.CardBack;else ClassicBack=p.CardBack;}
        }
        if(p.Rules.Kind==GameKind.FreeCell && p.Era!=Era.WindowsVista){FreeCellMessages=p.FreeCellMessages;FreeCellQuickPlay=p.FreeCellQuickPlay;FreeCellDoubleClick=p.FreeCellDoubleClick;}
        if(p.Rules.Kind==GameKind.Spider)
        {
            SpiderSuits=p.Rules.SpiderSuits;
            if(p.Era!=Era.WindowsVista){SpiderAnimate=p.SpiderAnimateDeal;SpiderAutoSave=p.SpiderAutoSave;SpiderAutoOpen=p.SpiderAutoOpen;SpiderPromptSave=p.SpiderPromptSave;SpiderPromptOpen=p.SpiderPromptOpen;SpiderSound=p.Sound;}
        }
    }
    public void Apply(Preferences p)
    {
        p.Scale=Scale;
        if(p.Era==Era.WindowsVista){p.VistaDeck=VistaDeck??p.VistaDeck;p.VistaBackground=VistaBackground??p.VistaBackground;p.Animate=VistaAnimate??p.Animate;p.SaveOnExit=VistaSave??p.SaveOnExit;p.ContinueSavedGame=VistaContinue??p.ContinueSavedGame;p.Sound=VistaSound??p.Sound;p.DisplayTips=VistaTips??p.DisplayTips;}
        if(p.Rules.Kind==GameKind.Klondike)
        {
            p.Rules.DrawCount=DrawCount??p.Rules.DrawCount;p.Rules.Scoring=Scoring??p.Rules.Scoring;
            if(p.Era!=Era.WindowsVista){p.Rules.Timed=Timed??p.Rules.Timed;p.ShowStatus=Status??p.ShowStatus;p.OutlineDragging=Outline??p.OutlineDragging;p.Rules.KeepVegasScore=KeepScore??p.Rules.KeepVegasScore;p.CardBack=(p.Era==Era.WindowsXP?XpBack:ClassicBack)??p.CardBack;}
        }
        if(p.Rules.Kind==GameKind.FreeCell && p.Era!=Era.WindowsVista){p.FreeCellMessages=FreeCellMessages??p.FreeCellMessages;p.FreeCellQuickPlay=FreeCellQuickPlay??p.FreeCellQuickPlay;p.FreeCellDoubleClick=FreeCellDoubleClick??p.FreeCellDoubleClick;}
        if(p.Rules.Kind==GameKind.Spider)
        {
            p.Rules.SpiderSuits=SpiderSuits??p.Rules.SpiderSuits;
            if(p.Era!=Era.WindowsVista){p.SpiderAnimateDeal=SpiderAnimate??p.SpiderAnimateDeal;p.SpiderAutoSave=SpiderAutoSave??p.SpiderAutoSave;p.SpiderAutoOpen=SpiderAutoOpen??p.SpiderAutoOpen;p.SpiderPromptSave=SpiderPromptSave??p.SpiderPromptSave;p.SpiderPromptOpen=SpiderPromptOpen??p.SpiderPromptOpen;p.Sound=SpiderSound??p.Sound;}
        }
    }
    public void Validate()
    {
        if(Scale is not (100 or 125 or 150 or 200) || ClassicBack is <0 or >11 || XpBack is <0 or >11 || VistaDeck is <0 or >3 || VistaBackground is <0 or >4 || DrawCount.HasValue && DrawCount is not (1 or 3) || SpiderSuits.HasValue && SpiderSuits is not (1 or 2 or 4) || Scoring.HasValue && !Enum.IsDefined(Scoring.Value))throw new InvalidDataException("Invalid shared settings.");
    }
    public static SharedSettings FromSave(SaveFile file)
    {
        var shared=new SharedSettings();foreach(var session in file.Sessions.Values)shared.Capture(session.Preferences);shared.Capture(file.Preferences);return shared;
    }
}
