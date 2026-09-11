using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;

namespace Solitude;

public enum Era { Windows30, Windows31, Windows95, Windows98, WindowsMe, Windows2000, WindowsXP, WindowsVista, Future2126 }
public sealed class Preferences
{
    public int FrameRate { get; set; } = 120;
    public int MotionDuration { get; set; } = 210;
    public bool HighlightTargets { get; set; }
    public bool ShowFrameRate { get; set; }
    public bool QuickControls { get; set; }
    public Era Era { get; set; } = Era.WindowsXP;
    public int Scale { get; set; } = 150;
    public int CardBack { get; set; } = 6;
    public int VistaDeck { get; set; } = 1;
    public int VistaBackground { get; set; }
    public int FutureVolume { get; set; } = 65;
    public int FuturePalette { get; set; }
    public bool FutureAtmosphere { get; set; } = true;
    public bool ShowStatus { get; set; } = true;
    public bool OutlineDragging { get; set; }
    public bool Animate { get; set; } = true;
    public bool Sound { get; set; }
    public bool SaveOnExit { get; set; } = true;
    public bool ContinueSavedGame { get; set; }
    public bool DisplayTips { get; set; } = true;
    public bool FreeCellMessages { get; set; } = true;
    public bool FreeCellQuickPlay { get; set; }
    public bool FreeCellDoubleClick { get; set; } = true;
    public bool SpiderAnimateDeal { get; set; } = true;
    public bool SpiderAutoSave { get; set; }
    public bool SpiderAutoOpen { get; set; }
    public bool SpiderPromptSave { get; set; } = true;
    public bool SpiderPromptOpen { get; set; } = true;
    public Rules Rules { get; set; } = new();
    public Preferences Clone() {var copy=(Preferences)MemberwiseClone();copy.Rules=Rules.Clone();return copy;}
}
public sealed class Statistics
{
    public Statistics Clone() {var copy=(Statistics)MemberwiseClone();copy.Difficulties=Difficulties.ToDictionary(p=>p.Key,p=>p.Value.Clone());return copy;}
    public int Played { get; set; }
    public int Won { get; set; }
    public int BestScore { get; set; }
    public DateTime? BestScoreDate { get; set; }
    public int? BestTime { get; set; }
    public int Streak { get; set; }
    public int BestWinStreak { get; set; }
    public int BestLossStreak { get; set; }
    public Dictionary<int,Statistics> Difficulties { get; set; } = [];
    public void Record(GameState state,bool won,bool timed)
    {
        RecordResult(state,won,timed);
        if(state.Kind!=GameKind.Spider)return;
        if(!Difficulties.TryGetValue(state.SpiderSuits,out var level))Difficulties[state.SpiderSuits]=level=new();
        level.RecordResult(state,won,timed);
    }
    private void RecordResult(GameState state,bool won,bool timed)
    {
        Played++;Streak=won?Math.Max(0,Streak)+1:Math.Min(0,Streak)-1;
        BestWinStreak=Math.Max(BestWinStreak,Streak);BestLossStreak=Math.Max(BestLossStreak,-Streak);
        if(!won)return;
        Won++;if(Won==1 || state.Score>BestScore || state.Score==BestScore && BestScoreDate==null){BestScore=state.Score;BestScoreDate=DateTime.Today;}
        if(timed && (BestTime==null || state.Elapsed<BestTime))BestTime=state.Elapsed;
    }
    public void Validate(bool child=false)
    {
        if(Played<0 || Won<0 || Won>Played || BestTime<0 || BestWinStreak<0 || BestLossStreak<0 || (long)Math.Abs((long)Streak)>Played || Difficulties==null || Difficulties.Count>3 || child && Difficulties.Count>0)
            throw new InvalidDataException("Invalid statistics.");
        foreach(var (suits,level) in Difficulties){if(suits is not (1 or 2 or 4) || level==null)throw new InvalidDataException("Invalid difficulty statistics.");level.Validate(true);}
    }
}
public sealed class SaveFile
{
    public int Format { get; set; } = 6;
    public SharedSettings? Shared { get; set; }
    public Rules? ActiveRules { get; set; }
    public Preferences Preferences { get; set; } = new();
    public Statistics Statistics { get; set; } = new();
    public GameState? Game { get; set; }
    public List<GameState> History { get; set; } = [];
    public Dictionary<string, SavedSession> Sessions { get; set; } = [];
    public Dictionary<string, SpiderCheckpoint> SpiderSaves { get; set; } = [];
}
public sealed class SpiderCheckpoint
{
    public GameState Game { get; set; } = new();
    public List<GameState> History { get; set; } = [];
    public SpiderCheckpoint Clone()=>new(){Game=Game.Clone(),History=History.Select(s=>s.Clone()).ToList()};
}
public sealed class SavedSession
{
    public Rules? ActiveRules { get; set; }
    public Preferences Preferences { get; set; } = new();
    public Statistics Statistics { get; set; } = new();
    public GameState? Game { get; set; }
    public List<GameState> History { get; set; } = [];
}
public sealed class Store
{
    public string DirectoryPath { get; }
    public string FilePath => Path.Combine(DirectoryPath, "solitude.json");
    public string? Warning { get; private set; }
    private bool preserveUnreadable;
    private string? expectedRevision;
    private bool revisionKnown;
    public Store(string directory)
    {
        DirectoryPath=Path.GetFullPath(directory);
        try{expectedRevision=Revision();revisionKnown=true;}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) { }
    }
    private string? Revision()
    {
        if(!File.Exists(FilePath))return null;
        using var file=new FileStream(FilePath,FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete);
        return Convert.ToHexString(SHA256.HashData(file));
    }
    private FileStream LockFile()
    {
        Directory.CreateDirectory(DirectoryPath);
        // A file lock also works when two paths alias the same directory.
        for(int attempt=0;;attempt++)
        {
            try{return new FileStream(FilePath+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
            catch(IOException ex) when((ex.HResult&0xffff) is 32 or 33 && attempt<100){Thread.Sleep(10);}
        }
    }
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };
    public SaveFile Load()
    {
        try
        {
            using var fileLock=LockFile();
            expectedRevision=Revision();revisionKnown=true;Warning=null;preserveUnreadable=false;
            if(expectedRevision==null)return new();
            return Read(FilePath);
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            preserveUnreadable = true;
            Warning = "The saved game could not be read. The original file will be kept for recovery.";
            try { var backup = Read(FilePath + ".bak"); Warning = "Your previous backup was recovered. The unreadable save will be preserved."; return backup; }
            catch (Exception backupError) when (backupError is IOException or JsonException or InvalidDataException or UnauthorizedAccessException or ArgumentException) { return new(); }
        }
    }
    private static SaveFile Read(string path)
    {
        if (new FileInfo(path).Length > 128 * 1024 * 1024) throw new InvalidDataException("Save file too large.");
        var saved = JsonSerializer.Deserialize<SaveFile>(File.ReadAllText(path), Options) ?? throw new InvalidDataException("Empty save.");
        if (saved.Format is not (1 or 2 or 3 or 4 or 5 or 6) || saved.Preferences?.Rules == null || saved.Statistics == null || saved.History == null || saved.Sessions == null || saved.Sessions.Count > 18 || saved.SpiderSaves==null || saved.SpiderSaves.Count>3)
            throw new InvalidDataException("Unsupported save format.");
        ValidateSession(saved.Preferences, saved.Game, saved.History,saved.ActiveRules);
        saved.Shared?.Validate();
        saved.Statistics.Validate();
        foreach (var (key, session) in saved.Sessions)
        {
            if (session?.Preferences?.Rules == null || session.Statistics == null || key != GameCatalog.Key(session.Preferences.Era, session.Preferences.Rules.Kind)) throw new InvalidDataException("Invalid saved session.");
            ValidateSession(session.Preferences, session.Game, session.History,session.ActiveRules);
            session.Statistics.Validate();
        }
        foreach(var (key,checkpoint) in saved.SpiderSaves)
        {
            if(checkpoint?.Game==null || checkpoint.History==null || !Enum.TryParse<Era>(key,out var era) || !GameCatalog.Available(era,GameKind.Spider))throw new InvalidDataException("Invalid saved Spider game.");
            var rules=GameCatalog.Defaults(era,GameKind.Spider).Rules;rules.SpiderSuits=checkpoint.Game.SpiderSuits;_ = Game.Restore(rules,checkpoint.Game,checkpoint.History);
        }
        if (saved.Format == 1)
        {
            var profile = GameCatalog.Defaults(saved.Preferences.Era, GameKind.Klondike);
            saved.Preferences.Rules.UndoLimit = profile.Rules.UndoLimit;
            saved.Preferences.Rules.AutoFlip = profile.Rules.AutoFlip;
            saved.History = saved.History.TakeLast(profile.Rules.UndoLimit==0?int.MaxValue:profile.Rules.UndoLimit).ToList();
        }
        if(saved.Format<3)
        {
            MigrateProfile(saved.Preferences,saved.Game,saved.History);
            foreach(var session in saved.Sessions.Values)MigrateProfile(session.Preferences,session.Game,session.History);
            saved.Format=3;
        }
        if(saved.Format<4)
        {
            GameCatalog.ApplyPeriodPresentation(saved.Preferences);
            foreach(var session in saved.Sessions.Values)GameCatalog.ApplyPeriodPresentation(session.Preferences);
            saved.Format=4;
        }
        saved.Shared??=SharedSettings.FromSave(saved);
        if(saved.Format<6)
        {
            void UndoPolicy(Preferences p,Rules? active)
            {
                int limit=GameCatalog.Defaults(p.Era,p.Rules.Kind).Rules.UndoLimit;
                p.Rules.UndoLimit=limit;if(active!=null)active.UndoLimit=limit;
            }
            UndoPolicy(saved.Preferences,saved.ActiveRules);
            foreach(var session in saved.Sessions.Values)UndoPolicy(session.Preferences,session.ActiveRules);
        }
        saved.Format=6;
        return saved;
    }
    private static void MigrateProfile(Preferences preferences,GameState? state,List<GameState> history)
    {
        preferences.QuickControls=false;
        preferences.Rules.SpiderFullUndo=preferences.Era==Era.WindowsVista;
        if(state?.Kind!=GameKind.Spider)return;
        foreach(var item in history.Append(state))item.Score=500+100*item.Foundations.Count(p=>p.Count==13)-item.Moves;
        var restored=Game.Restore(preferences.Rules,state,history);history.Clear();history.AddRange(restored.History);
    }
    private static void ValidateSession(Preferences p, GameState? game, List<GameState> history,Rules? activeRules=null)
    {
        if (p?.Rules == null || history == null || !Enum.IsDefined(p.Era) || !GameCatalog.Available(p.Era, p.Rules.Kind) || !Enum.IsDefined(p.Rules.Scoring) || p.Rules.DrawCount is not (1 or 3) || p.Scale is not (100 or 125 or 150 or 200) || p.CardBack is < 0 or > 11 || p.VistaDeck is < 0 or > 3 || p.VistaBackground is < 0 or > 4)
            throw new InvalidDataException("Invalid saved preferences.");
        if(p.FutureVolume is <0 or >100)throw new InvalidDataException("Invalid ORBIT volume.");
        if(p.FuturePalette is <0 or >2)throw new InvalidDataException("Invalid ORBIT palette.");
        _ = new Game(p.Rules, 1);
        if(p.FrameRate is not (60 or 120 or 144) || p.MotionDuration is not (140 or 210 or 320))throw new InvalidDataException("Invalid animation preferences.");
        if(activeRules!=null && activeRules.Kind!=p.Rules.Kind)throw new InvalidDataException("Active game type does not match its preset.");
        if (game != null) _ = Game.Restore(activeRules??p.Rules, game, history);
        else if (history.Count != 0) throw new InvalidDataException("History has no game.");
    }
    public bool Save(SaveFile saved)
    {
        try
        {
            string json=JsonSerializer.Serialize(saved, Options);
            if(System.Text.Encoding.UTF8.GetByteCount(json)>128*1024*1024)throw new IOException("The saved game is too large. Your previous save has been preserved.");
            using var fileLock=LockFile();
            if(!revisionKnown || Revision()!=expectedRevision)
                throw new IOException("Another instance changed the saved games. Its progress has been preserved. Close this window without saving, then reopen Solitude to load the latest games.");
            if (preserveUnreadable && File.Exists(FilePath))
            {
                File.Copy(FilePath, FilePath + ".unreadable-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"), false);
                preserveUnreadable = false;
            }
            string temporary = FilePath + ".tmp";
            File.WriteAllText(temporary,json);
            if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak");
            else File.Move(temporary, FilePath);
            expectedRevision=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
            Warning = null; return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { Warning = "Your game could not be saved. "+ex.Message; return false; }
    }
}
