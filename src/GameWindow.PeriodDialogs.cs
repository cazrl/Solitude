namespace Solitude;

public sealed partial class GameWindow
{
    private int helpPage;
    private string helpKeyword="";
    private bool helpSelectAll;
    private void OpenHelp(int page){OpenDialog(DialogPage.Help);helpPage=page;helpKeyword="";helpSelectAll=true;}
    private (string Text,int Page)[] HelpEntries=>[("Cards and columns",1),("Deal a new game",2),("Game commands",2),("How to play",1),("Move cards",1),("Options",2),("Rules",1),("Undo",2)];
    private bool EditHelpKeyword(Keys keys)
    {
        if(keys==(Keys.Control|Keys.A)){helpSelectAll=true;Invalidate();return true;}
        if(keys is Keys.Back or Keys.Delete){helpKeyword=helpSelectAll?"":helpKeyword.Length>0?helpKeyword[..^1]:"";helpSelectAll=false;Invalidate();return true;}
        if(keys==Keys.Enter){var matches=HelpEntries.Where(e=>e.Text.Contains(helpKeyword,StringComparison.OrdinalIgnoreCase)).ToArray();if(matches.Length>0)helpPage=matches[0].Page;Invalidate();return true;}
        var key=keys&Keys.KeyCode;
        if((keys&Keys.Control)==0 && (key is >= Keys.A and <= Keys.Z || key==Keys.Space))
        {if(helpSelectAll)helpKeyword="";helpSelectAll=false;if(helpKeyword.Length<40)helpKeyword+=key==Keys.Space?' ':char.ToLowerInvariant((char)key);Invalidate();return true;}
        return false;
    }
    private void PaintPeriodHelp(Graphics g,float x,float y,float w,float bottom)
    {
        string[] topics=["Contents","How to play","Commands","Index","Using Help"];
        int[] pages=[0,3,4];for(int i=0;i<pages.Length;i++){int page=pages[i];DialogButton(g,"help-"+page,new(x+i*105,y,99,24),topics[page],()=>{helpPage=page;Invalidate();},helpPage==page);}
        var paper=new RectangleF(x,y+32,w,bottom-y-78);Skin.Fill(g,Color.White,paper);Skin.Box(g,paper,true);
        float tx=paper.X+12,ty=paper.Y+12,tw=paper.Width-24;
        skin.Text(g,helpPage==0?GameCatalog.Name(Kind):topics[Math.Clamp(helpPage,0,4)],new(tx,ty,tw,25),font:skin.Bold);
        if(helpPage==0)
        {
            string[] links=["How to play "+GameCatalog.Name(Kind),"Using the game commands"];
            for(int i=0;i<2;i++)
            {
                int page=i+1;var link=new RectangleF(tx,ty+42+i*36,tw,27);skin.Text(g,links[i],link,Color.FromArgb(0,128,0));
                Skin.Line(g,Color.FromArgb(0,128,0),link.X,link.Bottom-4,link.X+skin.Measure(g,links[i]),link.Bottom-4);
                Add("dialog-topic-"+page,link,()=>{helpPage=page;Invalidate();});hotspots[^1]=hotspots[^1] with{Label=links[i],Role=AccessibleRole.Link};dialogControl++;
            }
        }
        else if(helpPage==3)
        {
            skin.Text(g,"Type a keyword:",new(tx,ty+30,tw,22));
            var field=new RectangleF(tx,ty+56,tw,25);Skin.Fill(g,Color.White,field);Skin.Box(g,field,true);
            skin.Text(g,helpKeyword+"|",new(field.X+5,field.Y,field.Width-10,field.Height));Add("dialog-help-keyword",field,()=>{helpSelectAll=true;Invalidate();});dialogControl++;
            int row=0;foreach(var entry in HelpEntries.Where(e=>e.Text.Contains(helpKeyword,StringComparison.OrdinalIgnoreCase)))
            {
                var item=entry;var r=new RectangleF(tx,ty+88+row++*19,tw,19);skin.Text(g,item.Text,r,Color.FromArgb(0,128,0));
                Add("dialog-index-"+row,r,()=>{helpPage=item.Page;Invalidate();});hotspots[^1]=hotspots[^1] with{Label=item.Text,Role=AccessibleRole.Link};dialogControl++;
            }
            if(row==0)skin.Text(g,"No matching topics.",new(tx,ty+94,tw,24));
        }
        else if(helpPage==4)
        {
            Wrapped(g,"Choose Contents to see the main topics. Select an underlined topic to read it.\n\nChoose Index to find a topic by keyword. Type a word and select a matching topic, or press Enter.\n\nPress Escape or choose Close to return to the game.",new(tx,ty+34,tw,paper.Height-56));
        }
        else
        {
            string content=helpPage==1?Kind switch
            {
                GameKind.Klondike=>"Build the four suit stacks from Ace to King. Build the rows downward, alternating red and black cards. Only a King may fill an empty row.\n\nDrag a card or a sequence to move it. Double-click an exposed card to move it to a suit stack. Click the deck to turn over cards. "+(skin.Modern?"Exposed face-down cards turn over automatically.":"Click an exposed face-down card to turn it over. Use Enter or Space to select and place cards."),
                GameKind.FreeCell=>"Build the four home cells from Ace to King in each suit. Build the columns downward in alternating colors. Each free cell holds one card. An empty column can hold any card.\n\nClick a card and then its destination. Enough empty free cells and columns must be available to move a sequence. Safe cards move to the home cells automatically.",
                _=>"Build a sequence from King down to Ace in the same suit. A completed sequence is removed from the table. Remove all eight sequences to win.\n\nA card can go on the next higher rank of any suit. A sequence moves together only when it is in the same suit. Click the stock to deal a row; there must be a card in every column."
            }:Kind switch
            {
                GameKind.Klondike=>skin.Modern?"New Game starts another deal. Restart Game returns to the current deal. Undo reverses a move; Hint shows a possible move. Change Appearance selects the cards and background. Options controls draw count, scoring, animation, sounds, tips and saved games.\n\nStandard awards points for playing cards. Vegas starts with a wager of 52; each card moved from the table to a suit stack returns 5.":"Deal starts a new game. Undo reverses the last action. Deck selects the card back. Options selects Draw One or Draw Three, the scoring system, the timer and the status bar.\n\nStandard awards points for playing cards. Vegas starts with a wager of 52; each card played to a suit stack returns 5. Keep score carries the Vegas balance between games.",
                GameKind.FreeCell=>ClassicFreeCell?"New Game: F2\nSelect Game: F3\nStatistics: F4\nOptions: F5\nUndo: F10\n\nOptions controls messages for illegal moves, Quick play and the double-click shortcut to a free cell.":"New Game: F2\nSelect Game: F3\nStatistics: F4\nOptions: F5\nChange Appearance: F7\nUndo: Ctrl+Z\nHint: H",
                _=>ClassicSpider?"New Game: F2\nDifficulty: F3\nStatistics: F4\nOptions: F5\nUndo: Ctrl+Z\nDeal Next Row: D\nShow An Available Move: M\nSave This Game: Ctrl+S\nOpen Last Saved Game: Ctrl+O":"New Game: F2\nStatistics: F4\nOptions: F5\nChange Appearance: F7\nUndo: Ctrl+Z\nHint: H"
            };
            Wrapped(g,content,new(tx,ty+34,tw,paper.Height-56));
        }
        DialogButton(g,"ok",new(x+w-80,bottom-35,80,25),"Close",CloseDialog,true);
    }
    private void PaintPeriodAbout(Graphics g,float x,float y,float w,float bottom)
    {
        if(ClassicFreeCell)art.DrawKing(g,new(x+6,y+5,35,35),false);else Skin.DrawTinyIcon(g,x+8,y+9);
        skin.Text(g,GameCatalog.Name(Kind),new(x+52,y,w-52,27),font:skin.CaptionFont);
        skin.Text(g,Skin.Names[(int)Preferences.Era],new(x+52,y+33,w-52,23));
        string credits=Kind==GameKind.Klondike && !skin.Modern?"Developed for Microsoft by Wes Cherry.\nOriginal card designs by Susan Kare.":ClassicFreeCell?"By Jim Horne.":"Microsoft Windows Games";
        Wrapped(g,credits,new(x+5,y+80,w-10,55));
        Wrapped(g,"Solitude recreation\nCreated by Cazrl",new(x+5,y+143,w-10,40));
        DialogButton(g,"donate",new(x,bottom-36,80,25),"&Donate",OpenDonationPage);
        DialogButton(g,"ok",new(x+w-80,bottom-36,80,25),"OK",CloseDialog,true);
    }
    private void PaintPeriodWin(Graphics g,float x,float y,float w,float bottom)
    {
        if(skin.Modern)
        {
            skin.Text(g,"Congratulations, you won the game!",new(x,y,w,24),center:true);
            int bonus=Game.State.TimeBonus;
            skin.Group(g,"Game Score",new(x,y+32,w,95));
            skin.Text(g,$"Score: {Game.State.Score-bonus}",new(x+9,y+52,w/2-9,20));
            skin.Text(g,$"Time: {Game.State.Elapsed} seconds",new(x+w/2,y+52,w/2-3,20));
            skin.Text(g,$"Time Bonus: {bonus}",new(x+9,y+75,w-18,20));
            skin.Text(g,$"Total Score: {Game.State.Score}",new(x+9,y+97,w-18,20));
            skin.Text(g,$"High score: {Statistics.BestScore}",new(x,y+136,w/2,20));
            if(Statistics.BestScoreDate is {} date)skin.Text(g,$"Date: {date:d/M/yyyy}",new(x+w/2,y+136,w/2,20));
            skin.Text(g,$"Games played: {Statistics.Played}",new(x,y+157,w,20));
            skin.Text(g,$"Games won: {Statistics.Won}",new(x,y+178,w/2,20));
            skin.Text(g,$"Win percentage: {(Statistics.Played==0?0:100*Statistics.Won/Statistics.Played)}%",new(x+w/2,y+178,w/2,20));
            DialogButton(g,"ok",new(x,bottom-35,116,23),"&Play again",()=>NewGame(),true);
            DialogButton(g,"cancel",new(x+w-116,bottom-35,116,23),"E&xit",Close);return;
        }

        string text=!skin.Modern && Kind==GameKind.Klondike?"Deal Again?":ClassicFreeCell?"Congratulations, you win!\nDo you want to play again?":"Congratulations, you won!\nDo you want to start another game?";
        Wrapped(g,text,new(x+4,y+7,w-8,62));
        if(skin.Modern)skin.Text(g,$"Score: {Game.State.Score}    Time: {Game.State.Elapsed}",new(x,y+72,w,23));
        DialogButton(g,"ok",new(x,bottom-39,78,25),"Yes",()=>NewGame(),true);
        DialogButton(g,"cancel",new(x+88,bottom-39,78,25),"No",()=>{victoryTrail?.Dispose();victoryTrail=null;CloseDialog();});
        if(ClassicFreeCell)DialogButton(g,"select",new(x+176,bottom-39,122,25),"Select game",()=>OpenDialog(DialogPage.SelectGame));
    }
    private void PaintDifficulty(Graphics g,float x,float y,float w,float bottom)
    {
        skin.Text(g,"Select the game difficulty level that you want:",new(x,y,w,24));
        int[] values=[1,2,4];string[] names=ClassicSpider?["Easy: One Suit","Medium: Two Suits","Difficult: Four Suits"]:["Beginner: One Suit","Intermediate: Two Suits","Advanced: Four Suits"];
        for(int i=0;i<3;i++){int level=values[i];Check(g,"suits-"+level,new(x+5,y+32+i*28,w-10,24),names[i],draft!.Rules.SpiderSuits==level,()=>{draft.Rules.SpiderSuits=level;Invalidate();},true);}
        DialogButton(g,"ok",new(x+w-170,bottom-36,80,25),"OK",ApplyOptions,true);DialogButton(g,"cancel",new(x+w-80,bottom-36,80,25),"Cancel",CloseDialog);
    }
    private void PaintVistaOptions(Graphics g,float x,float y,float w,float bottom)
    {
        float next=y;
        if(Kind==GameKind.Klondike)
        {
            skin.Group(g,"Draw",new(x,y,145,84));
            Check(g,"draw1",new(x+10,y+22,130,23),"Draw one",draft!.Rules.DrawCount==1,()=>{draft.Rules.DrawCount=1;Invalidate();},true);
            Check(g,"draw3",new(x+10,y+48,130,23),"Draw three",draft.Rules.DrawCount==3,()=>{draft.Rules.DrawCount=3;Invalidate();},true);
            skin.Group(g,"Scoring",new(x+157,y,w-157,98));
            for(int i=0;i<3;i++){int score=i;Check(g,"score"+i,new(x+168,y+19+i*24,w-179,23),((Scoring)i).ToString(),(int)draft.Rules.Scoring==i,()=>{draft.Rules.Scoring=(Scoring)score;Invalidate();},true);}
            next=y+109;
        }
        if(Kind==GameKind.Spider)
        {
            skin.Group(g,"Difficulty",new(x,y,w,113));int[] levels=[1,2,4];string[] names=["Beginner: One Suit","Intermediate: Two Suits","Advanced: Four Suits"];
            for(int i=0;i<3;i++){int level=levels[i];Check(g,"suits-"+level,new(x+12,y+20+i*27,w-24,24),names[i],draft!.Rules.SpiderSuits==level,()=>{draft.Rules.SpiderSuits=level;Invalidate();},true);}next=y+123;
        }
        Check(g,"animations",new(x,next,w,24),"Display &animations",draft!.Animate,()=>{draft.Animate=!draft.Animate;Invalidate();});
        Check(g,"sound",new(x,next+25,w,24),"Play &sounds",draft.Sound,()=>{draft.Sound=!draft.Sound;Invalidate();});
        Check(g,"tips",new(x,next+50,w,24),"Display &tips",draft.DisplayTips,()=>{draft.DisplayTips=!draft.DisplayTips;Invalidate();});
        Check(g,"save",new(x,next+75,w,24),"Always save game on e&xit",draft.SaveOnExit,()=>{draft.SaveOnExit=!draft.SaveOnExit;Invalidate();});
        Check(g,"continue",new(x,next+100,w,24),"Always &continue saved game",draft.ContinueSavedGame,()=>{draft.ContinueSavedGame=!draft.ContinueSavedGame;Invalidate();});
        DialogButton(g,"ok",new(x+w-170,bottom-36,80,25),"OK",ApplyOptions,true);DialogButton(g,"cancel",new(x+w-80,bottom-36,80,25),"Cancel",CloseDialog);
    }
}
