using System.Drawing.Drawing2D;

namespace Solitude;

public sealed partial class GameWindow
{
    private string dealText = "1";
    private bool dealSelectAll;
    private Position pendingFrom, pendingTo;

    private void StashSession()
    {
        sessions[GameCatalog.Key(Preferences.Era, Kind)] = new()
        {
            Preferences = Preferences.Clone(), Statistics = Statistics,
            ActiveRules=Game.Rules.Clone(),
            Game = Game.State.Clone(),
            History = Game.History.ToList()
        };
    }
    private void SwitchGame(Preferences chosen)
    {
        FinishEditionMorph();
        var area = Screen.FromControl(this).WorkingArea;
        CancelDrag();StopCardMotion();
        bool leavingOrbit=skin.Future && chosen.Era!=Era.Future2126;
        bool orbitMotion=Preferences.Animate;
        var departureAccent=skin.Future?Orbit.Accent:Color.Empty;
        bool morphRequested=Visible && (leavingOrbit || !skin.Future && chosen.Era==Era.Future2126);
        if(morphRequested)CloseDialog();
        using var previousSurface=morphRequested?CaptureEditionSurface(ClientSize):null;
        var previousBounds=Bounds;
        bool orbitTransition=skin.Future || chosen.Era==Era.Future2126;
        futurePulses.Clear();futureMessage=null;
        Rules? storedRules=Game.Rules.Clone();GameState? storedGame=Game.State;List<GameState> storedHistory=Game.History;
        bool different = Preferences.Era != chosen.Era || Kind != chosen.Rules.Kind;
        if (different)
        {
            StashSession();
            if (sessions.TryGetValue(GameCatalog.Key(chosen.Era, chosen.Rules.Kind), out var saved))
            {
                Preferences = saved.Preferences.Clone(); Preferences.Scale = chosen.Scale; Statistics = saved.Statistics;
                storedRules=saved.ActiveRules?.Clone()??Preferences.Rules.Clone();storedGame=saved.Game;storedHistory=saved.History;
            }
            else
            {
                Preferences = GameCatalog.Defaults(chosen.Era, chosen.Rules.Kind, chosen.Scale);
                Statistics = new();storedRules=null;storedGame=null;storedHistory=[];
            }
        }
        else Preferences.Scale = chosen.Scale;
        shared.Scale=chosen.Scale;shared.Apply(Preferences);GameCatalog.ApplyPeriodPresentation(Preferences);
        Game=storedGame==null?new(Preferences.Rules.Clone()):Game.Restore(storedRules??Preferences.Rules.Clone(),storedGame,storedHistory);
        selection = null; hint = null; collecting = false; showingVictory = false;pendingWin=false; keyboardPile = 0;
        victoryTrail?.Dispose(); victoryTrail = null; status = ""; elapsedMilliseconds = 0; lastSavedSecond = Game.State.Elapsed;
        skin.Dispose(); skin = new(Preferences.Era); Text = GameTitle;
        ConfigurePeriodIcon();closingDecision=false;
        vistaSaveOnce=false;offerVistaResume=false;oneMoveWarning=false;peekCard=null;vistaTipTitle=null;
        CloseDialog(); maximized = false;
        if(previousSurface!=null)BeginEditionMorph(previousSurface,previousBounds,area,leavingOrbit?orbitMotion:Preferences.Animate,departureAccent);
        else CenterGameWindow(area);
        CheckFreeCellEnd();if(orbitTransition)QueueSaveNow();else Save(); Invalidate();
    }
    private void ChooseDraftEra(Era era)
    {
        draft!.Era = era;
        if (!GameCatalog.Available(era, draft.Rules.Kind)) draft.Rules.Kind = GameKind.Klondike;
        Invalidate();
    }
    private RectangleF CellRect(int index, bool home)
    {
        float gap = skin.Modern ? CardWidth + Table.Width * .013f : CardWidth;
        float x = home ? Table.Right - TableMargin - CardWidth - (3-index)*gap : Table.Left + TableMargin + index*gap;
        return new(x, Table.Top + (skin.Modern ? 22 : 1), CardWidth, CardHeight);
    }
    private IEnumerable<(Position Position, RectangleF Rect)> DestinationAreas()
    {
        if (Kind == GameKind.FreeCell)
            for (int i=0; i<4; i++) { yield return (new(PileKind.FreeCell,i),CellRect(i,false)); yield return (new(PileKind.Foundation,i),CellRect(i,true)); }
        else if (Kind == GameKind.Klondike)
            for (int i=0; i<4; i++) yield return (new(PileKind.Foundation,i),TopCard(i+3));
    }
    private List<Position> KeyboardPiles()
    {
        var result = new List<Position>();
        if (Kind != GameKind.FreeCell) result.Add(new(PileKind.Stock));
        if (Kind == GameKind.Klondike) result.Add(new(PileKind.Waste));
        result.AddRange(DestinationAreas().Select(p => p.Position));
        for (int i=0; i<Game.State.Tableau.Count; i++) result.Add(new(PileKind.Tableau,i));
        return result;
    }
    private bool StockHit(PointF p)
    {
        var r=StockRect;
        if(Kind==GameKind.Spider){r.X-=Math.Max(0,Game.State.Stock.Count/10-1)*12;r.Width+=Math.Max(0,Game.State.Stock.Count/10-1)*12;}
        return r.Contains(p);
    }
    private void DrawCards()
    {
        if (Kind == GameKind.Spider && Game.State.Stock.Count > 0 && !Game.CanDealSpider)
        { notice="You cannot deal a new row while any columns are empty. Move a card into each empty column first."; OpenDialog(DialogPage.Notice); return; }
        futureActionSound="SHARED_CARDDEAL";bool dealt=Game.Draw();Changed(dealt);futureActionSound=null;if(dealt && !skin.Future)PlayVistaSound("SHARED_CARDDEAL");
    }
    private Position FreeCellSelection(int column)
    {
        var pile=Game.State.Tableau[column];
        for(int i=0;i<pile.Count;i++)if(Game.CanPick(new(PileKind.Tableau,column,i)))return new(PileKind.Tableau,column,i);
        return new(PileKind.Tableau,column);
    }
    private Position SelectedMove(Position origin,Position to)
    {
        if (Kind!=GameKind.FreeCell || skin.Modern || origin.Kind!=PileKind.Tableau) return origin;
        var pile=Game.Pile(origin)!;
        for(int i=Game.Index(origin);i<pile.Count;i++)if(Game.CanMove(origin with{Index=i},to))return origin with{Index=i};
        return origin;
    }
    private bool RequestFreeCellColumnMove(Position from,Position to)
    {
        if(Kind!=GameKind.FreeCell || skin.Modern || to.Kind!=PileKind.Tableau || Game.Pile(to)!.Count!=0 || Game.Pile(from)!.Count-Game.Index(from)<2)return false;
        pendingFrom=from;pendingTo=to;OpenDialog(DialogPage.MoveColumn);return true;
    }
    private bool EditDealNumber(Keys key)
    {
        string? digit = key is >= Keys.D0 and <= Keys.D9 ? ((int)key-(int)Keys.D0).ToString() : key is >= Keys.NumPad0 and <= Keys.NumPad9 ? ((int)key-(int)Keys.NumPad0).ToString() : null;
        if(key==(Keys.Control|Keys.A)){dealSelectAll=true;Invalidate();return true;}
        if(digit!=null){if(dealSelectAll)dealText="";dealSelectAll=false;if(dealText.Length<7)dealText+=digit;Invalidate();return true;}
        if(ClassicFreeCell && key is Keys.OemMinus or Keys.Subtract){if(dealSelectAll)dealText="";dealSelectAll=false;if(dealText.Length==0)dealText="-";Invalidate();return true;}
        if(key is Keys.Back or Keys.Delete){dealText=dealSelectAll?"":dealText.Length>0?dealText[..^1]:"";dealSelectAll=false;Invalidate();return true;}
        if(key==(Keys.Control|Keys.V))
        {
            try{string pasted=Clipboard.GetText().Trim();if(pasted.Length<=7 && (pasted.All(char.IsAsciiDigit) || ClassicFreeCell && pasted is "-1" or "-2")){dealText=pasted;dealSelectAll=false;}}catch(System.Runtime.InteropServices.ExternalException){}
            Invalidate();return true;
        }
        return false;
    }
    private void ConfirmDealNumber()
    {
        // Read the input at activation; the next paint may not have run between
        // the final digit and Enter.
        if(int.TryParse(dealText,out int number) && (number>=1 && number<=GameCatalog.MaxDeal(Preferences.Era) || ClassicFreeCell && number is -1 or -2))NewGame(seed:number);
    }
    private void PaintSelectGame(Graphics g,float x,float y,float w,float bottom)
    {
        skin.Text(g,"Enter a game number:",new(x,y,w,21));
        var field=new RectangleF(x,y+31,w,27);Skin.Fill(g,Color.White,field);Skin.Box(g,field,true);
        var text=new RectangleF(field.X+5,field.Y+3,field.Width-10,field.Height-6);
        if(dealSelectAll){Skin.Fill(g,skin.Selection,text);skin.Text(g,dealText,text,Color.White);}else skin.Text(g,dealText+"|",text);
        Add("dialog-number",field,()=>{dealSelectAll=true;Invalidate();});dialogControl++;
        skin.Text(g,$"1 to {GameCatalog.MaxDeal(Preferences.Era):N0}",new(x,y+63,w,20));
        bool valid=int.TryParse(dealText,out int number) && (number>=1 && number<=GameCatalog.MaxDeal(Preferences.Era) || ClassicFreeCell && number is -1 or -2);
        if(!valid)skin.Text(g,"Enter a number in this range.",new(x,y+88,w,20),Color.Firebrick);
        DialogButton(g,"ok",new(x+w-170,bottom-36,80,25),"OK",ConfirmDealNumber,true);
        DialogButton(g,"cancel",new(x+w-80,bottom-36,80,25),"Cancel",CloseDialog);
    }
    private void PaintVariantTable(Graphics g)
    {
        if(skin.Modern)art.DrawFelt(g,Table);
        else if(Kind==GameKind.Spider)art.DrawSpiderFelt(g,Table);
        else Skin.Fill(g,Color.Green,Table);
        var clip=g.Save();g.SetClip(Table,CombineMode.Intersect);
        if(Kind==GameKind.FreeCell)
        {
            foreach(var cell in DestinationAreas())
            {
                var pile=Game.Pile(cell.Position)!;int index=SettledTop(pile,cell.Position);
                if(index<0)
                {
                    if(skin.Modern)Empty(g,cell.Rect,true);
                    else {var r=cell.Rect;Skin.Line(g,Color.Black,r.X,r.Y,r.Right-1,r.Y);Skin.Line(g,Color.Black,r.X,r.Y,r.X,r.Bottom-1);Skin.Line(g,Color.LimeGreen,r.Right-1,r.Y,r.Right-1,r.Bottom-1);Skin.Line(g,Color.LimeGreen,r.X,r.Bottom-1,r.Right-1,r.Bottom-1);}
                }
                else {DrawGameCard(g,pile[index],cell.Rect);cardAreas.Add((cell.Position,cell.Rect));}
                Highlight(g,cell.Position,cell.Rect);
            }
            if(!skin.Modern)
            {
                var r=new RectangleF(Table.Left+Table.Width/2-17,Table.Top+19,35,35);
                art.DrawKing(g,r,mouse.X>Table.Left+Table.Width/2);
            }
        }
        else
        {
            for(int i=0;i<Game.State.Foundations.Count;i++)if(Game.State.Foundations[i].Count>0)
            {
                var r=new RectangleF(Table.Left+16+i*(skin.Modern?CardWidth*.42f:24),Table.Bottom-CardHeight-12,CardWidth,CardHeight);
                DrawGameCard(g,Game.State.Foundations[i][0],r);
            }
            for(int i=0;i<Game.State.Stock.Count/10;i++)
            {
                var r=StockRect;r.X-=i*12;art.Draw(g,new Card(0,false),r,Preferences.Era,Preferences.CardBack);
            }
            if(Game.State.Stock.Count>0)Highlight(g,new(PileKind.Stock),StockRect);
            if(!skin.Modern)
            {
                var panel=new RectangleF(Table.Left+Table.Width/2-88,Table.Bottom-83,176,73);
                Skin.Fill(g,Color.Green,panel);Skin.Box(g,panel,true);
                skin.Text(g,$"Score:  {Game.State.Score}",new(panel.X+8,panel.Y+16,panel.Width-16,20),Color.White,skin.Bold,true);
                skin.Text(g,$"Moves:  {Game.State.Moves}",new(panel.X+8,panel.Y+37,panel.Width-16,20),Color.White,skin.Bold,true);
                if(dialog==DialogPage.None)Add("spider-score",panel,ShowHint);
            }
        }
        PaintVariantColumns(g);
        g.Restore(clip);
    }
    private void PaintVariantColumns(Graphics g)
    {
        for(int col=0;col<Game.State.Tableau.Count;col++)
        {
            var pile=Game.State.Tableau[col];
            if(pile.Count==0){if(skin.Modern)Empty(g,TableauCard(col,0),false);Highlight(g,new(PileKind.Tableau,col),TableauCard(col,0));}
            for(int i=0;i<pile.Count;i++)
            {
                var pos=new Position(PileKind.Tableau,col,i);var r=TableauCard(col,i);
                if(dragging && selection is {} selected && selected.Kind==PileKind.Tableau && selected.Pile==col && i>=Game.Index(selected))break;
                DrawGameCard(g,pile[i],r);
                var hit=r;if(i<pile.Count-1)hit.Height=TableauCard(col,i+1).Y-r.Y;
                cardAreas.Add((pos,hit));
                if(IsSelected(pos)){var outline=r;outline.Height=TableauCard(col,pile.Count-1).Bottom-r.Y;Highlight(g,pos,outline);}
                else if(i==pile.Count-1)Highlight(g,pos,r);
            }
        }
    }
    private void PaintVariantOptions(Graphics g,float x,float y,float w,float bottom)
    {
        if(skin.Modern){PaintVistaOptions(g,x,y,w,bottom);return;}
        if(ClassicFreeCell)
        {
            Check(g,"messages",new(x,y,w,23),"Display messages on illegal moves",draft!.FreeCellMessages,()=>{draft.FreeCellMessages=!draft.FreeCellMessages;Invalidate();});
            Check(g,"quick-play",new(x,y+29,w,23),"Quick play (no animation)",draft.FreeCellQuickPlay,()=>{draft.FreeCellQuickPlay=!draft.FreeCellQuickPlay;Invalidate();});
            Check(g,"double-click",new(x,y+58,w,23),"Double click moves card to free cell",draft.FreeCellDoubleClick,()=>{draft.FreeCellDoubleClick=!draft.FreeCellDoubleClick;Invalidate();});
        }
        else
        {
            Check(g,"animate-deal",new(x,y,w,23),"&Animate when dealing cards",draft!.SpiderAnimateDeal,()=>{draft.SpiderAnimateDeal=!draft.SpiderAnimateDeal;Invalidate();});
            Check(g,"auto-save",new(x,y+27,w,23),"Automatically &save game on exit",draft.SpiderAutoSave,()=>{draft.SpiderAutoSave=!draft.SpiderAutoSave;Invalidate();});
            Check(g,"auto-open",new(x,y+54,w,23),"Automatically &open previous game at startup",draft.SpiderAutoOpen,()=>{draft.SpiderAutoOpen=!draft.SpiderAutoOpen;Invalidate();});
            Check(g,"prompt-save",new(x,y+81,w,23),"&Prompt before saving a game",draft.SpiderPromptSave,()=>{draft.SpiderPromptSave=!draft.SpiderPromptSave;Invalidate();});
            Check(g,"prompt-open",new(x,y+108,w,23),"Prompt &before opening a saved game",draft.SpiderPromptOpen,()=>{draft.SpiderPromptOpen=!draft.SpiderPromptOpen;Invalidate();});
            Check(g,"sound",new(x,y+135,w,23),"Use sound &effects",draft.Sound,()=>{draft.Sound=!draft.Sound;Invalidate();});
        }
        DialogButton(g,"ok",new(x+w-170,bottom-36,80,25),"OK",ApplyOptions,true);
        DialogButton(g,"cancel",new(x+w-80,bottom-36,80,25),"Cancel",CloseDialog);
    }
    private void PaintGamePreview(Graphics g,RectangleF table,Preferences preview)
    {
        var state=g.Save();g.SetClip(table,CombineMode.Intersect);art.Kind=preview.Rules.Kind;
        int columns=preview.Rules.Kind==GameKind.Spider?10:preview.Rules.Kind==GameKind.FreeCell?8:7;
        float width=Math.Min(15,(table.Width-16)/columns-3),height=width*96/71;
        if(preview.Rules.Kind==GameKind.Spider && preview.Era!=Era.WindowsVista)art.DrawSpiderFelt(g,table);
        for(int i=0;i<columns;i++)
        {
            float cx=table.X+8+i*(table.Width-16-width)/(columns-1),cy=table.Y+5;
            if(preview.Rules.Kind==GameKind.FreeCell)
            {
                g.DrawRectangle(Pens.LimeGreen,cx,cy,width,9);
                cy+=13;
            }
            else if(preview.Rules.Kind==GameKind.Klondike)cy+=12;
            if(preview.Rules.Kind!=GameKind.FreeCell)for(int j=0;j<(preview.Rules.Kind==GameKind.Spider?4:i);j++)
            {art.Draw(g,new Card(0,false),new(cx,cy,width,height),preview.Era,preview.CardBack);cy+=2;}
            art.Draw(g,new Card(i*5%52),new(cx,cy,width,height),preview.Era,preview.CardBack);
        }
        art.Kind=Kind;g.Restore(state);
    }
}
