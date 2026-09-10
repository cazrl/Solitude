namespace Solitude;

public sealed partial class GameWindow
{
    private Dictionary<string,SpiderCheckpoint> spiderSaves=[];
    private readonly Dictionary<string,Statistics> runStatistics=[];
    private Statistics sessionStatistics {get{string key=GameCatalog.Key(Preferences.Era,Kind);if(!runStatistics.TryGetValue(key,out var s))runStatistics[key]=s=new();return s;}set=>runStatistics[GameCatalog.Key(Preferences.Era,Kind)]=value;}
    private bool closingDecision;
    private bool vistaSaveOnce, offerVistaResume;
    private readonly Dictionary<string,System.Media.SoundPlayer> vistaSounds=[];
    private string? lastPeriodSound;
    private void UndoMove()
    {
        collecting=false;CancelDrag();bool changed=Game.Undo();Changed(changed);
        if(changed)PlayVistaSound("SHARED_UNDO");
    }
    private void SystemWindowCommand(int command)
    {
        menu=-1;Invalidate();if(!ephemeral)SendMessage(Handle,0x112,command,0);
    }
    private void PlayVistaSound(string name)
    {
        if(!skin.Vista || !Preferences.Sound)return;
        lastPeriodSound=name;if(ephemeral)return;
        if(!vistaSounds.TryGetValue(name,out var sound))
        {
            var stream=typeof(GameWindow).Assembly.GetManifestResourceStream($"Solitude.Assets.Sounds.Vista.{name}.wav");if(stream==null)return;
            sound=new System.Media.SoundPlayer(stream);vistaSounds[name]=sound;
        }
        try{sound.Play();}catch(Exception ex) when(ex is InvalidOperationException or IOException or TimeoutException){}
    }
    private void OfferVistaResume()
    {
        if(!offerVistaResume)return;offerVistaResume=false;
        Confirm("A saved game was found. Do you want to continue your saved game?",()=>{},()=>NewGame());
    }
    private readonly Dictionary<int,System.Media.SoundPlayer> periodSounds=[];
    private void ConfigurePeriodIcon()
    {
        string name=Kind switch{GameKind.Klondike=>"sol",GameKind.FreeCell=>"freecell",_=>"spider"};
        int pixels=Preferences.Era is Era.WindowsXP or Era.WindowsVista?(int)MathF.Round(16*ScaleFactor):16;
        if(skin.Vista)
        {
            using var image=typeof(GameWindow).Assembly.GetManifestResourceStream($"Solitude.Assets.Icons.{name}-vista.png")!;
            using var png=new MemoryStream();image.CopyTo(png);byte[] bytes=png.ToArray();
            using var container=new MemoryStream();using(var writer=new BinaryWriter(container,System.Text.Encoding.UTF8,true))
            {writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)1);writer.Write(0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(bytes.Length);writer.Write(22);writer.Write(bytes);}
            container.Position=0;var old=Icon;Icon=new Icon(container,pixels,pixels);skin.GameIcon=Icon;old?.Dispose();return;
        }
        string suffix=Preferences.Era is Era.WindowsXP or Era.WindowsVista?"":"-classic";
        using var stream=typeof(GameWindow).Assembly.GetManifestResourceStream($"Solitude.Assets.Icons.{name}{suffix}.ico")??typeof(GameWindow).Assembly.GetManifestResourceStream($"Solitude.Assets.Icons.{name}.ico");
        if(stream!=null){var previous=Icon;Icon=new Icon(stream,pixels,pixels);skin.GameIcon=Icon;previous?.Dispose();}
    }
    private void PlayPeriodSound(int id)
    {
        if(ephemeral || !Preferences.Sound || !ClassicSpider)return;
        if(!periodSounds.TryGetValue(id,out var sound))
        {
            var resource=typeof(GameWindow).Assembly.GetManifestResourceStream($"Solitude.Assets.Sounds.spider-{id}.wav");if(resource==null)return;
            sound=new System.Media.SoundPlayer(resource);periodSounds[id]=sound;
        }
        try{sound.Play();}catch(InvalidOperationException){}catch(IOException){}catch(TimeoutException){}
    }
    private bool PeriodClosing(FormClosingEventArgs e)
    {
        if(skin.Vista && !closingDecision && Game.State.Started && !Game.State.Won && !Game.State.Lost && !Preferences.SaveOnExit)
        {
            e.Cancel=true;Confirm("Do you want to save this game before closing it?",()=>{vistaSaveOnce=true;closingDecision=true;Close();},()=>{vistaSaveOnce=false;closingDecision=true;Close();},true);return false;
        }
        if(!ClassicSpider || closingDecision || !Game.State.Started || Game.State.Won)return true;
        if(Preferences.SpiderAutoSave){SaveSpiderGame(false);closingDecision=true;return true;}
        if(!Preferences.SpiderPromptSave)return true;
        e.Cancel=true;Confirm("Do you want to save this game before closing it?",()=>{SaveSpiderGame(false);closingDecision=true;Close();},()=>{closingDecision=true;Close();},true);return false;
    }
    private Action? confirmYes,confirmNo;
    private string confirmText="";
    private bool confirmCancel;
    private bool HasHints=>skin.Vista || Kind==GameKind.Spider;
    private bool ClassicFreeCell=>!skin.Vista && Kind==GameKind.FreeCell;
    private bool ClassicSpider=>!skin.Vista && Kind==GameKind.Spider;
    private sealed record MenuEntry(string Label,string Key,Action? Action,bool Enabled=true);
    private List<MenuEntry> PeriodMenu()
    {
        var entries=new List<MenuEntry>();
        void Item(string label,string key,Action action,bool enabled=true)=>entries.Add(new(label,key,action,enabled));
        void Sep()=>entries.Add(new("","",null,false));
        void Undo(){menu=-1;UndoMove();}
        if(menu==0)
        {
            if(!skin.Vista && Kind==GameKind.Klondike)
            {
                Item("&Deal","F2",()=>RequestNew());Sep();Item("&Undo","",Undo,Game.CanUndo);
                Item("De&ck...","",()=>OpenDialog(DialogPage.Deck));Item("&Options...","",()=>OpenDialog(DialogPage.Options));Sep();
            }
            else if(ClassicFreeCell)
            {
                Item("&New Game","F2",()=>RequestNew());Item("&Select Game","F3",()=>OpenDialog(DialogPage.SelectGame));
                Item("&Restart Game","",()=>RequestNew(true),Game.State.Started);Sep();
                Item("S&tatistics...","F4",()=>OpenDialog(DialogPage.Statistics));Item("&Options...","F5",()=>OpenDialog(DialogPage.Options));Sep();
                Item("&Undo","F10",Undo,Game.CanUndo);Sep();
            }
            else if(ClassicSpider)
            {
                Item("&New Game","F2",()=>RequestNew());Item("&Restart This Game","",()=>RequestNew(true),Game.State.Started);Sep();
                Item("&Undo","Ctrl+Z",Undo,Game.CanUndo);Item("&Deal Next Row","D",()=>{menu=-1;DrawCards();},Game.State.Stock.Count>0);
                Item("Show An Available &Move","M",()=>{menu=-1;ShowHint();},!Game.State.Won);Sep();
                Item("D&ifficulty...","F3",()=>OpenDialog(DialogPage.Difficulty));Item("S&tatistics...","F4",()=>OpenDialog(DialogPage.Statistics));Item("O&ptions...","F5",()=>OpenDialog(DialogPage.Options));Sep();
                Item("&Save This Game","Ctrl+S",()=>SaveSpiderGame(),Game.State.Started);Item("&Open Last Saved Game","Ctrl+O",()=>OpenSpiderGame());Sep();
            }
            else
            {
                Item("&New Game","F2",()=>RequestNew());if(Kind==GameKind.FreeCell)Item("&Select Game...","F3",()=>OpenDialog(DialogPage.SelectGame));
                Item("&Restart Game","",()=>RequestNew(true),Game.State.Started);
                Item("&Undo","Ctrl+Z",Undo,Game.CanUndo);Item("&Hint","H",()=>{menu=-1;ShowHint();},!Game.State.Won);Sep();
                Item("S&tatistics...","F4",()=>OpenDialog(DialogPage.Statistics));Item("&Options...","F5",()=>OpenDialog(DialogPage.Options));
                Item("Change &Appearance...","F7",()=>OpenDialog(DialogPage.Deck));Sep();
            }
            Item("E&xit","",Close);
        }
        else if(menu==1)
        {
            Item(skin.Early?"&Index":skin.Vista?"View &Help":"&Contents",skin.Early?"":"F1",()=>OpenHelp(0));
            if(!skin.Early && !skin.Vista && Kind!=GameKind.Spider){Item("&Search for Help on...","",()=>OpenHelp(3));Item("&How to Use Help","",()=>OpenHelp(4));}
            Sep();Item("&About "+(ClassicSpider?"Spider":GameCatalog.Name(Kind))+(!skin.Vista && Kind==GameKind.Klondike?"":"..."),"",()=>OpenDialog(DialogPage.About));
        }
        else
        {
            Item("&Restore","",()=>{menu=-1;if(maximized)ToggleMaximize();},maximized);
            Item("&Move","",()=>SystemWindowCommand(0xF010),!maximized);Item("&Size","",()=>SystemWindowCommand(0xF000),!maximized);
            Item("Mi&nimize","",()=>{menu=-1;WindowState=FormWindowState.Minimized;});Item("Ma&ximize","",()=>{menu=-1;ToggleMaximize();},!maximized);Sep();
            Item("&Close","Alt+F4",Close);Sep();Item("Windows &version...","F6",()=>OpenDialog(DialogPage.Settings));
            if(store.Warning!=null || saves.Error!=null)Item("Exit without saving","",()=>{shutdown=true;Close();});
        }
        return entries;
    }
    private void Confirm(string text,Action yes,Action? no=null,bool cancel=false)
    {
        OpenDialog(DialogPage.Confirm);confirmText=text;confirmYes=yes;confirmNo=no;confirmCancel=cancel;
    }
    private void ConfirmChoice(bool yes)
    {
        var action=yes?confirmYes:confirmNo;confirmYes=null;confirmNo=null;CloseDialog();action?.Invoke();
    }
    private void IllegalMove()
    {
        PlayVistaSound("SHARED_ILLEGALMOVE");
        if(ClassicFreeCell && Preferences.FreeCellMessages)
        {notice="That move is not allowed.";OpenDialog(DialogPage.Notice);}
    }
    private void SaveSpiderGame(bool prompt=true)
    {
        if(!ClassicSpider)return;
        void SaveCheckpoint(){spiderSaves[Preferences.Era.ToString()]=new(){Game=Game.State.Clone(),History=Game.History.Select(s=>s.Clone()).ToList()};if(!Save()){notice=saves.Error??store.Warning??"Unable to save game.";OpenDialog(DialogPage.Notice);}}
        if(prompt && Preferences.SpiderPromptSave && spiderSaves.ContainsKey(Preferences.Era.ToString()))Confirm("A saved game already exists. Do you want to replace it with your current game?",SaveCheckpoint);else{menu=-1;SaveCheckpoint();}
    }
    private void OpenSpiderGame(bool prompt=true,bool startup=false)
    {
        if(!ClassicSpider)return;
        if(!spiderSaves.TryGetValue(Preferences.Era.ToString(),out var saved)){notice="Unable to load game.";OpenDialog(DialogPage.Notice);return;}
        void LoadCheckpoint()
        {
            menu=-1;
            if(!startup)RecordAbandonedGame();var rules=Preferences.Rules.Clone();rules.SpiderSuits=saved.Game.SpiderSuits;
            Game=Game.Restore(rules,saved.Game,saved.History);selection=null;hint=null;collecting=false;pendingWin=false;StopCardMotion();RequestSave();Invalidate();
        }
        if(prompt && Preferences.SpiderPromptOpen)Confirm("Do you want to discard the current game and open the last saved game?",LoadCheckpoint);else LoadCheckpoint();
    }
    private void RecordAbandonedGame()
    {
        if(Game.State.Started && !Game.State.WinRecorded && !Game.State.Lost){Statistics.Record(Game.State,false,Game.Rules.Timed);sessionStatistics.Record(Game.State,false,Game.Rules.Timed);}
    }
    private void CheckFreeCellEnd()
    {
        oneMoveWarning=false;
        if(!ClassicFreeCell || Game.State.Won || Game.State.Lost)return;
        int moves=Game.FreeCellAvailableMoves();oneMoveWarning=ClassicFreeCell && moves==1;
        if(moves!=0)return;
        Game.State.Lost=true;Statistics.Record(Game.State,false,Game.Rules.Timed);sessionStatistics.Record(Game.State,false,Game.Rules.Timed);
        OpenDialog(DialogPage.Lost);
    }
    private void ResetStatistics()
    {
        Confirm(ClassicFreeCell?"Are you sure you want to delete all statistics?":"Are you sure you want to reset all game statistics?",()=>{Statistics=new();sessionStatistics=new();RequestSave();OpenDialog(DialogPage.Statistics);});
    }
}
