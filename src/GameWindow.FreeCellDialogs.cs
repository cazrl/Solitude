namespace Solitude;

public sealed partial class GameWindow
{
    private bool winSelectGame;
    private Size? FreeCellDialogClient=>!ClassicFreeCell?null:dialog switch
    {
        DialogPage.Statistics=>new(225,220),DialogPage.Options=>new(305,102),DialogPage.SelectGame=>new(180,122),
        DialogPage.MoveColumn=>new(225,130),DialogPage.Won=>new(203,130),DialogPage.Lost=>new(270,140),_=>null
    };
    private void PaintFreeCellDialog(Graphics g,RectangleF body)
    {
        // Coordinates are taken from the archived 8-point MS Shell Dlg templates.
        RectangleF D(float x,float y,float w,float h)=>new(body.X+x*1.5f,body.Y+y*1.625f,w*1.5f,h*1.625f);
        void Text(string text,float x,float y,float w,float h,bool center=false)=>skin.Text(g,text,D(x,y,w,h),center:center);
        void Button(string id,string label,float x,float y,float w,Action action,bool primary=false)=>DialogButton(g,id,D(x,y,w,14),label,action,primary);
        switch(dialog)
        {
            case DialogPage.Statistics:
                void Counts(string label,Statistics s,int yy)
                {
                    Text(label,11,yy,87,9);Text($"{(s.Played==0?0:100*s.Won/s.Played)}%",110,yy,31,9);
                    Text("won:",27,yy+10,55,9);Text(s.Won.ToString(),110,yy+10,31,9);
                    Text("lost:",27,yy+20,55,9);Text((s.Played-s.Won).ToString(),110,yy+20,31,9);
                }
                Counts("This session",sessionStatistics,10);Counts("Total",Statistics,42);
                Text("Streaks",11,74,120,9);Text("wins:",27,84,55,9);Text(Statistics.BestWinStreak.ToString(),110,84,31,9);
                Text("losses:",27,94,55,9);Text(Statistics.BestLossStreak.ToString(),110,94,31,9);
                Text("current:",27,104,55,9);Text($"{Math.Abs(Statistics.Streak)} {(Statistics.Streak>=0?"wins":"losses")}",85,104,57,9);
                Button("ok","OK",20,115,40,CloseDialog,true);Button("clear","Clear",90,115,40,ResetStatistics);break;
            case DialogPage.Options:
                Check(g,"messages",D(6,9,136,10),"Display &messages on illegal moves",draft!.FreeCellMessages,()=>{draft.FreeCellMessages=!draft.FreeCellMessages;Invalidate();});
                Check(g,"quick-play",D(6,26,136,10),"&Quick play (no animation)",draft.FreeCellQuickPlay,()=>{draft.FreeCellQuickPlay=!draft.FreeCellQuickPlay;Invalidate();});
                Check(g,"double-click",D(6,43,136,10),"&Double click moves card to free cell",draft.FreeCellDoubleClick,()=>{draft.FreeCellDoubleClick=!draft.FreeCellDoubleClick;Invalidate();});
                Button("ok","OK",148,9,50,ApplyOptions,true);Button("cancel","Cancel",148,26,50,CloseDialog);break;
            case DialogPage.SelectGame:
                Text("Select a game number",0,7,120,8,true);Text($"from 1 to {GameCatalog.MaxDeal(Preferences.Era)}",0,17,120,8,true);
                var field=D(45,32,40,12);Skin.Fill(g,Color.White,field);Skin.Box(g,field,true);var inner=field;inner.Inflate(-3,-2);
                if(dealSelectAll)Skin.Fill(g,skin.Selection,inner);skin.Text(g,dealText+(dealSelectAll?"":"|"),inner,dealSelectAll?Color.White:Color.Black);
                Add("dialog-number",field,()=>{dealSelectAll=true;Invalidate();});dialogControl++;
                Button("ok","OK",40,54,40,ConfirmDealNumber,true);break;
            case DialogPage.MoveColumn:
                Button("column","Move column",30,15,90,()=>{CloseDialog();Changed(Game.Move(pendingFrom,pendingTo));},true);
                Button("single","Move single card",30,35,90,()=>{CloseDialog();Changed(Game.Move(pendingFrom with{Index=-1},pendingTo));});
                Button("cancel","Cancel",55,57,40,CloseDialog);break;
            case DialogPage.Won:
                Text("Congratulations, you win!",15,8,105,10,true);Text("Do you want to play again?",15,28,105,10,true);
                Check(g,"select",D(15,43,80,12),"Select game",winSelectGame,()=>{winSelectGame=!winSelectGame;Invalidate();});
                Button("ok","Yes",15,58,40,()=>{if(winSelectGame)OpenDialog(DialogPage.SelectGame);else NewGame();},true);
                Button("cancel","No",80,58,40,CloseDialog);break;
            case DialogPage.Lost:
                Text("Sorry, you lose. There are no more legal moves.",8,8,164,22,true);
                Text("Do you want to play again?",12,34,155,10,true);
                Check(g,"same-game",D(40,49,100,12),"&Same game",lossSameGame,()=>{lossSameGame=!lossSameGame;Invalidate();});
                Button("ok","&Yes",30,68,45,()=>NewGame(lossSameGame),true);Button("cancel","&No",105,68,45,CloseDialog);break;
        }
    }
}
