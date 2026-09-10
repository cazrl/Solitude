namespace Solitude;

public sealed partial class GameWindow
{
    private int dialogControl;
    private readonly Dictionary<char,string> dialogMnemonics=[];
    private readonly Dictionary<string,string> dialogRadios=[];
    private readonly HashSet<string> dialogButtons=[];
    private void RefreshDialogInput()
    {
        hotspots.RemoveAll(h=>h.Id.StartsWith("dialog-") || h.Id=="modal-close");
        using var probe=new Bitmap(1,1);using var g=Graphics.FromImage(probe);
        g.ScaleTransform(ScaleFactor,ScaleFactor);skin.Configure(g);PaintDialog(g);
    }
    private void RegisterDialogKey(string id,string label)
    {
        int index=label.IndexOf('&');if(index>=0 && index+1<label.Length)dialogMnemonics[char.ToUpperInvariant(label[index+1])]="dialog-"+id;
    }
    private void PaintDialog(Graphics g)
    {
        dialogControl=0;dialogMnemonics.Clear();dialogRadios.Clear();dialogButtons.Clear();
        if(skin.Future && dialog is DialogPage.Options or DialogPage.Deck or DialogPage.Won or DialogPage.Help or DialogPage.About or DialogPage.AppAbout){PaintFutureDialog(g);return;}
        string title=dialog switch {
            DialogPage.Settings=>"Settings",DialogPage.Options=>ClassicFreeCell?"FreeCell Options":ClassicSpider?"Spider Options":"Options",DialogPage.Deck=>skin.Modern?"Change Appearance":"Select Card Back",
            DialogPage.Help=>GameCatalog.Name(Kind)+" Help",DialogPage.About=>"About "+(Kind==GameKind.Spider?"Spider":GameCatalog.Name(Kind)),DialogPage.Statistics=>"Statistics",
            DialogPage.SelectGame=>"Game Number",DialogPage.MoveColumn=>"Move to Empty Column...",DialogPage.Difficulty=>"Difficulty",DialogPage.Confirm=>GameTitle,DialogPage.Records=>"Saved Records",DialogPage.AppAbout=>"About Solitude",
            DialogPage.NewGame=>"Deal",DialogPage.Restart=>"Restart",DialogPage.Lost=>"Game Over",DialogPage.Won=>skin.Modern?"Game Won":Kind==GameKind.Klondike?"Solitaire":"Game Over",_=>GameTitle};
        if(dialogHost!=null){dialogHost.Text=title;dialogHost.AccessibleName=title;}
        int width=dialog switch{DialogPage.Deck=>546,DialogPage.Help=>skin.Early?566:490,DialogPage.Settings=>548,DialogPage.Options=>skin.Early?510:430,_=>skin.Early?470:410};
        int height=dialog switch{DialogPage.Deck=>312,DialogPage.Help=>396,DialogPage.Settings=>406,DialogPage.Options=>338,DialogPage.About=>272,DialogPage.Statistics=>252,DialogPage.Won=>216,_=>182};
        if(dialog==DialogPage.Options && !skin.Modern && Kind==GameKind.Klondike){width=skin.Early?278:342;height=skin.Early?230:216+(skin.Xp?9:0);}
        if(dialog==DialogPage.SelectGame){width=360;height=228;}
        if(dialog==DialogPage.About && Kind==GameKind.Spider && !skin.Modern){width=395;height=320;}
        if(dialog==DialogPage.Deck && skin.Modern)height=408;
        if(dialog==DialogPage.Options && ClassicFreeCell){width=390;height=200;}
        if(dialog==DialogPage.Options && ClassicSpider){width=395;height=268;}
        if(dialog==DialogPage.Options && skin.Modern){width=430;height=Kind==GameKind.FreeCell?245:370;}
        if(dialog==DialogPage.Difficulty){width=355;height=236;}
        if(dialog==DialogPage.Won && skin.Modern){width=316;height=285;}
        if(dialog==DialogPage.Won && !skin.Modern){width=365;height=190;}
        if(dialog==DialogPage.Confirm){width=400;height=178;}
        if(dialog==DialogPage.AppAbout || dialog==DialogPage.Records){height=236;}
        if(dialog==DialogPage.Help){width=skin.Early?590:560;height=430;}
        if(dialog==DialogPage.Statistics && ClassicFreeCell){width=385;height=339;}
        if(dialog==DialogPage.Statistics && Kind==GameKind.Spider){width=420;height=350;}
        bool illegalFreeCell=dialog==DialogPage.Notice && ClassicFreeCell && notice=="That move is not allowed.";
        if(illegalFreeCell){title="FreeCell";width=280;height=125;}
        if(FreeCellDialogClient is {} client){width=client.Width+2*skin.Border;height=client.Height+skin.Caption+2*skin.Border;if(dialog==DialogPage.Statistics)title="FreeCell Statistics";}
        var bounds=new RectangleF(MathF.Round((WorldWidth-width)/2+dialogOffset.X),MathF.Round((WorldHeight-height)/2+dialogOffset.Y),width,height);
        dialogBounds=bounds;
        if(skin.Xp || skin.Modern)
        {
            int steps=skin.Modern?7:3;
            for(int i=steps;i>=1;i--){using var shadow=Skin.Rounded(new(bounds.X-i+2,bounds.Y-i+3,bounds.Width+2*i,bounds.Height+2*i),skin.Modern?8:5);using var brush=new SolidBrush(Color.FromArgb(skin.Modern?9:14,0,0,0));g.FillPath(brush,shadow);}
        }
        var buttons=skin.Frame(g,bounds,title,true,dialogHost!=null || windowActive,mouse,pressedHotspot!=null);
        var body=new RectangleF(bounds.X+skin.Border,bounds.Y+skin.Border+skin.Caption,bounds.Width-2*skin.Border,bounds.Height-skin.Caption-2*skin.Border);
        Skin.Fill(g,skin.Face,body);
        if(!buttons[2].IsEmpty)Add("modal-close",buttons[2],CloseDialog);
        if(FreeCellDialogClient.HasValue){PaintFreeCellDialog(g,body);return;}
        float x=body.X+14,y=body.Y+12,w=body.Width-28;
        switch(dialog)
        {
            case DialogPage.Settings:PaintSettings(g,x,y,w,body.Bottom);break;
            case DialogPage.Options:PaintOptions(g,x,y,w,body.Bottom);break;
            case DialogPage.Deck:PaintDeck(g,x,y,w,body.Bottom);break;
            case DialogPage.Help:PaintHelp(g,x,y,w,body.Bottom);break;
            case DialogPage.SelectGame:PaintSelectGame(g,x,y,w,body.Bottom);break;
            case DialogPage.Difficulty:PaintDifficulty(g,x,y,w,body.Bottom);break;
            case DialogPage.Confirm:
                Wrapped(g,confirmText,new(x,y+7,w,63));
                DialogButton(g,"yes",new(body.Right-(confirmCancel?266:178),body.Bottom-38,76,25),"Yes",()=>ConfirmChoice(true),true);
                DialogButton(g,"no",new(body.Right-(confirmCancel?180:92),body.Bottom-38,76,25),"No",()=>ConfirmChoice(false));
                if(confirmCancel)DialogButton(g,"cancel",new(body.Right-92,body.Bottom-38,76,25),"Cancel",CloseDialog);break;
            case DialogPage.Records:
                skin.Text(g,"All saved games for this Windows preset",new(x,y,w,24),font:skin.Bold);
                Wrapped(g,$"Games played: {Statistics.Played}\nGames won: {Statistics.Won}\nBest score: {Statistics.BestScore}",new(x,y+38,w,75));
                DialogButton(g,"ok",new(body.Right-94,body.Bottom-36,80,25),"OK",CloseDialog,true);break;
            case DialogPage.AppAbout:
                skin.Text(g,"Solitude 0.10.0",new(x,y,w,25),font:skin.Bold);
                Wrapped(g,"Windows card games, 1990-2007.\nORBIT, imagined for 2126.\nPress F6 to choose an edition.",new(x,y+38,w,78));
                DialogButton(g,"ok",new(body.Right-94,body.Bottom-36,80,25),"OK",CloseDialog,true);break;
            case DialogPage.MoveColumn:
                Wrapped(g,"Move the entire column, or just the single card?",new(x,y,w,45));
                DialogButton(g,"column",new(x,body.Bottom-40,110,26),"Move column",()=>{CloseDialog();Changed(Game.Move(pendingFrom,pendingTo));},true);
                DialogButton(g,"single",new(x+119,body.Bottom-40,110,26),"Single card",()=>{CloseDialog();Changed(Game.Move(pendingFrom with{Index=-1},pendingTo));});
                DialogButton(g,"cancel",new(x+238,body.Bottom-40,110,26),"Cancel",CloseDialog);break;
            case DialogPage.About:
                if(Kind==GameKind.Spider && !skin.Modern)
                {
                    art.DrawSpiderAbout(g,new(x,y,w,243*w/359));
                    DialogButton(g,"ok",new(body.Right-94,body.Bottom-36,80,25),"OK",CloseDialog,true);break;
                }
                PaintPeriodAbout(g,x,y,w,body.Bottom);break;
            case DialogPage.Statistics:
                PaintStatistics(g,x,y,w,body.Bottom);break;
            case DialogPage.NewGame:case DialogPage.Restart:
                Wrapped(g,dialog==DialogPage.Restart?"Do you want to restart this game?":"Do you want to start a new game?",new(x,y+7,w,55));
                DialogButton(g,"ok",new(body.Right-178,body.Bottom-38,76,25),"Yes",()=>NewGame(dialog==DialogPage.Restart),true);
                DialogButton(g,"cancel",new(body.Right-92,body.Bottom-38,76,25),"No",CloseDialog);break;
            case DialogPage.Won:PaintPeriodWin(g,x,y,w,body.Bottom);break;
            case DialogPage.Lost:
                Wrapped(g,"There are no more legal moves.\nDo you want to play again?",new(x,y,w,65));
                DialogButton(g,"ok",new(x,body.Bottom-38,95,25),"&Play again",()=>NewGame(),true);
                DialogButton(g,"restart",new(x+105,body.Bottom-38,95,25),"&Try again",()=>NewGame(true));
                DialogButton(g,"cancel",new(x+210,body.Bottom-38,95,25),"&Close",CloseDialog);break;
            default:
                Wrapped(g,notice,new(x,y+2,w,illegalFreeCell?30:85));
                DialogButton(g,"ok",new(illegalFreeCell?body.X+(body.Width-80)/2:body.Right-94,body.Bottom-36,80,25),"OK",CloseDialog,true);break;
        }
    }
    private void Wrapped(Graphics g,string text,RectangleF r)
    {
        skin.Wrapped(g,text,r);
    }
    private void DialogButton(Graphics g,string id,RectangleF r,string label,Action action,bool primary=false)
    {
        RegisterDialogKey(id,label);
        dialogButtons.Add("dialog-"+id);
        // The close widget is excluded from the tab sequence so indexes follow the visible form controls.
        bool focus=keyboardFocus==dialogControl++;
        skin.Button(g,r,label,r.Contains(mouse),primary,focus:focus,pressed:pressedHotspot=="dialog-"+id && r.Contains(mouse));Add("dialog-"+id,r,action);
    }
    private void Check(Graphics g,string id,RectangleF r,string label,bool selected,Action action,bool radio=false,bool enabled=true)
    {
        RegisterDialogKey(id,label);
        if(radio)dialogRadios["dialog-"+id]=new string(id.TakeWhile(c=>!char.IsDigit(c)).ToArray());
        skin.Check(g,r,label,selected,radio,enabled && r.Contains(mouse),enabled && pressedHotspot=="dialog-"+id && r.Contains(mouse),enabled);
        if(enabled && keyboardFocus==dialogControl){var rect=r;rect.X+=16;rect.Width-=16;using var pen=new Pen(skin.Future?Orbit.Accent:Color.Black){DashStyle=System.Drawing.Drawing2D.DashStyle.Dot};g.DrawRectangle(pen,rect.X,rect.Y,rect.Width,rect.Height);}
        if(enabled)dialogControl++;Add("dialog-"+id,r,action,enabled);
    }
    private void PaintSettings(Graphics g,float x,float y,float w,float bottom)
    {
        skin.Text(g,"Choose your edition",new(x,y,w,19),font:skin.Bold);
        var list=new RectangleF(x,y+29,242,194);Skin.Fill(g,Color.White,list);Skin.Box(g,list,true);
        for(int i=0;i<Skin.Names.Length;i++)
        {
            float rowHeight=190f/Skin.Names.Length;int index=i;var row=new RectangleF(list.X+2,list.Y+2+i*rowHeight,list.Width-4,rowHeight);bool selected=(int)draft!.Era==i;
            if(selected)Skin.Fill(g,skin.Selection,row);
            skin.Text(g,Skin.Names[i],new(row.X+7,row.Y,row.Width-55,row.Height),selected?Color.White:Color.Black);
            skin.Text(g,Skin.Years[i],new(row.Right-40,row.Y,37,row.Height),selected?Color.White:Color.Gray);
            if(keyboardFocus==dialogControl){using var pen=new Pen(selected?Color.White:Color.Black){DashStyle=System.Drawing.Drawing2D.DashStyle.Dot};g.DrawRectangle(pen,row.X+2,row.Y+2,row.Width-5,row.Height-5);}
            Add("dialog-era-"+i,row,()=>ChooseDraftEra((Era)index));dialogControl++;
        }
        float px=x+255,pw=w-255;
        using(var preview=new Skin(draft!.Era))
        {
            var state=g.Save();var r=new RectangleF(px,y+29,pw,95);
            preview.Frame(g,r,GameCatalog.Name(draft.Rules.Kind));
            if(!preview.Future)
            {
                preview.MenuBar(g,new(r.X+preview.Border,r.Y+preview.Border+preview.Caption,r.Width-2*preview.Border,preview.MenuHeight));
                preview.Text(g,"Game   Help",new(r.X+preview.Border+5,r.Y+preview.Border+preview.Caption,r.Width-2*preview.Border-10,preview.MenuHeight));
            }
            var table=new RectangleF(r.X+preview.Border,r.Y+(preview.Future?36:preview.Top),r.Width-preview.Border*2,r.Height-(preview.Future?36:preview.Top)-preview.Border);
            if(preview.Future)Skin.Fill(g,Color.FromArgb(9,24,37),table);else if(preview.Vista)art.DrawFelt(g,table);else Skin.Fill(g,Color.Green,table);
            PaintGamePreview(g,table,draft);g.Restore(state);
        }
        skin.Text(g,"Game",new(px,y+133,pw,19),font:skin.Bold);
        int gameRow=0;
        foreach(var kind in Enum.GetValues<GameKind>().Where(k=>GameCatalog.Available(draft!.Era,k)))
        {
            var chosen=kind;Check(g,"game-"+kind,new(px,y+157+gameRow*23,pw,23),GameCatalog.Name(kind),draft!.Rules.Kind==kind,()=>{draft.Rules.Kind=chosen;Invalidate();},true);gameRow++;
        }
        skin.Group(g,"Display size",new(x,y+232,w,49));
        int[] scales=[100,125,150,200];
        for(int i=0;i<4;i++)
        {
            int scale=scales[i];Check(g,"scale-"+scale,new(x+13+i*(w-20)/4,y+248,(w-20)/4,22),scale+"%",draft.Scale==scale,()=>{draft.Scale=scale;Invalidate();},true);
        }
        DialogButton(g,"records",new(x,bottom-36,92,25),"Records...",()=>OpenDialog(DialogPage.Records));
        DialogButton(g,"app-about",new(x+100,bottom-36,88,25),"About...",()=>OpenDialog(DialogPage.AppAbout));
        skin.Text(g,"Shared settings. Existing deals keep their current rules.",new(x,y+286,w,20));
        DialogButton(g,"ok",new(x+w-170,bottom-36,80,25),"OK",ApplySettings,true);
        DialogButton(g,"cancel",new(x+w-80,bottom-36,80,25),"Cancel",CloseDialog);
    }
    private void ApplySettings()
    {
        SwitchGame(draft!);
    }
    private void PaintOptions(Graphics g,float x,float y,float w,float bottom)
    {
        if(Kind!=GameKind.Klondike){PaintVariantOptions(g,x,y,w,bottom);return;}
        if(!skin.Modern){PaintClassicOptions(g,x,y,w,bottom);return;}
        PaintVistaOptions(g,x,y,w,bottom);
    }
    private void ApplyOptions()
    {
        bool rulesChanged=Preferences.Rules.DrawCount!=draft!.Rules.DrawCount || Preferences.Rules.Scoring!=draft.Rules.Scoring || Preferences.Rules.Timed!=draft.Rules.Timed || Preferences.Rules.AutoFlip!=draft.Rules.AutoFlip || Preferences.Rules.SpiderSuits!=draft.Rules.SpiderSuits;
        Preferences=draft;shared.Capture(Preferences);CloseDialog();if(rulesChanged)NewGame();else Save();Invalidate();
    }
    private void PaintClassicOptions(Graphics g,float x,float y,float w,float bottom)
    {
        RectangleF R(float dx,float dy,float width,float height)=>new(dialogBounds.X+dx,dialogBounds.Y+dy+(skin.Xp?9:0),width,height);
        bool early=skin.Early;
        skin.Group(g,"Draw",early?R(14,33,119,66):R(16,34,148,84));
        skin.Group(g,"Scoring",early?R(140,33,113,94):R(177,34,149,84));
        Check(g,"draw1",early?R(21,50,105,21):R(34,55,120,23),early?"Draw &One":"Draw &one",draft!.Rules.DrawCount==1,()=>{draft.Rules.DrawCount=1;Invalidate();},true);
        Check(g,"draw3",early?R(21,75,108,21):R(34,83,120,23),early?"Draw &Three":"Draw &three",draft.Rules.DrawCount==3,()=>{draft.Rules.DrawCount=3;Invalidate();},true);
        string[] scoringLabels=["St&andard","&Vegas","&None"];
        for(int i=0;i<3;i++){int score=i;Check(g,"score"+i,early?R(149,51+i*25,104,21):R(195,49+i*21,120,21),scoringLabels[i],(int)draft.Rules.Scoring==i,()=>{draft.Rules.Scoring=(Scoring)score;Invalidate();},true);}
        Check(g,"timed",early?R(21,112,145,21):R(16,124,146,21),"T&imed game",draft.Rules.Timed,()=>{draft.Rules.Timed=!draft.Rules.Timed;Invalidate();});
        Check(g,"status",early?R(21,136,125,21):R(16,146,146,21),"Status &bar",draft.ShowStatus,()=>{draft.ShowStatus=!draft.ShowStatus;Invalidate();});
        Check(g,"outline",early?R(21,160,225,21):R(178,124,154,21),"Out&line dragging",draft.OutlineDragging,()=>{draft.OutlineDragging=!draft.OutlineDragging;Invalidate();});
        Check(g,"keep-score",early?R(149,136,119,21):R(178,146,150,21),"&Keep score",draft.Rules.KeepVegasScore,()=>{draft.Rules.KeepVegasScore=!draft.Rules.KeepVegasScore;Invalidate();},enabled:draft.Rules.Scoring==Scoring.Vegas);
        DialogButton(g,"ok",early?R(79,193,70,25):R(171,178,74,23),"OK",ApplyOptions,true);
        DialogButton(g,"cancel",early?R(158,193,77,25):R(252,178,75,23),"Cancel",CloseDialog);
    }
    private void PaintDeck(Graphics g,float x,float y,float w,float bottom)
    {
        if(skin.Modern)
        {
            skin.Text(g,"Select a card deck:",new(x,y-4,w,20));
            string[] names=["Hearts","Seasons","Classic","Large Print"];
            for(int i=0;i<4;i++)
            {
                int index=i;float tileWidth=w/4-6;var tile=new RectangleF(x+i*(w/4),y+26,tileWidth,184);
                if(draft!.VistaDeck==i){Skin.Fill(g,Color.FromArgb(216,234,251),tile);using var pen=new Pen(Color.SteelBlue);g.DrawRectangle(pen,tile.X,tile.Y,tile.Width,tile.Height);}
                art.VistaDeck=i;
                art.Draw(g,new Card(0,false),new(tile.X+8,tile.Y+10,64,91),Era.WindowsVista,0);
                art.Draw(g,new Card(38),new(tile.X+39,tile.Y+39,64,91),Era.WindowsVista,0);
                skin.Text(g,names[i],new(tile.X,tile.Bottom-34,tile.Width,25),center:true);
                Add("dialog-vistadeck-"+i,tile,()=>{draft!.VistaDeck=index;Invalidate();});dialogControl++;
            }
            art.VistaDeck=Preferences.VistaDeck;
            skin.Text(g,"Background",new(x,y+218,w,20));
            string[] backgroundNames=["Green","Brown","Hearts","Nature","Red"];
            for(int i=0;i<5;i++)
            {
                int index=i;var tile=new RectangleF(x+i*w/5,y+245,w/5-6,47);art.VistaBackground=i;art.DrawFelt(g,tile);
                if(draft!.VistaBackground==i){using var pen=new Pen(Color.SteelBlue,3);g.DrawRectangle(pen,tile.X-2,tile.Y-2,tile.Width+4,tile.Height+4);}
                skin.Text(g,backgroundNames[i],new(tile.X,tile.Bottom+2,tile.Width,18),center:true);
                Add("dialog-background-"+i,tile,()=>{draft!.VistaBackground=index;Invalidate();});dialogControl++;
            }
            art.VistaBackground=Preferences.VistaBackground;
            DialogButton(g,"ok",new(x+w-170,bottom-34,80,24),"OK",()=>{Preferences.VistaDeck=draft!.VistaDeck;Preferences.VistaBackground=draft.VistaBackground;shared.Capture(Preferences);CloseDialog();Save();},true);
            DialogButton(g,"cancel",new(x+w-80,bottom-34,80,24),"Cancel",CloseDialog);
            return;
        }
        skin.Text(g,"Select a card back:",new(x,y-4,w,20));
        for(int i=0;i<12;i++)
        {
            int index=i;var r=new RectangleF(x+(i%6)*(w/6)+3,y+23+(i/6)*104,71,96);
            art.Draw(g,new Card(0,false),r,skin.Modern?Era.WindowsXP:Preferences.Era,i);
            if(draft!.CardBack==i || keyboardFocus==dialogControl){using var pen=new Pen(skin.Title,2){DashStyle=keyboardFocus==dialogControl?System.Drawing.Drawing2D.DashStyle.Dot:System.Drawing.Drawing2D.DashStyle.Solid};g.DrawRectangle(pen,r.X-3,r.Y-3,r.Width+5,r.Height+5);}
            Add("dialog-back-"+i,r,()=>{draft!.CardBack=index;Invalidate();});dialogControl++;
        }
        DialogButton(g,"ok",new(x+w-170,bottom-34,80,24),"OK",()=>{Preferences.CardBack=draft!.CardBack;shared.Capture(Preferences);CloseDialog();Save();},true);
        DialogButton(g,"cancel",new(x+w-80,bottom-34,80,24),"Cancel",CloseDialog);
    }
    private void PaintHelp(Graphics g,float x,float y,float w,float bottom)=>PaintPeriodHelp(g,x,y,w,bottom);
}
