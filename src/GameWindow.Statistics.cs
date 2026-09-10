namespace Solitude;

public sealed partial class GameWindow
{
    private int statisticsLevel;
    private void PaintStatistics(Graphics g,float x,float y,float w,float bottom)
    {
        if(ClassicFreeCell)
        {
            void Counts(string title,Statistics s,float yy)
            {
                skin.Group(g,title,new(x,yy,w,64));
                skin.Text(g,$"Won: {s.Won}       Lost: {s.Played-s.Won}",new(x+12,yy+18,w-24,22));
                skin.Text(g,$"Win percentage: {(s.Played==0?0:100*s.Won/s.Played)}%",new(x+12,yy+39,w-24,21));
            }
            Counts("This session",sessionStatistics,y);Counts("Total",Statistics,y+72);
            skin.Group(g,"Streaks",new(x,y+144,w,85));
            skin.Text(g,$"Wins: {Statistics.BestWinStreak}       Losses: {Statistics.BestLossStreak}",new(x+12,y+164,w-24,22));
            skin.Text(g,$"Current: {Math.Abs(Statistics.Streak)} {(Statistics.Streak>=0?"wins":"losses")}",new(x+12,y+190,w-24,22));
            DialogButton(g,"clear",new(x,bottom-36,80,25),"Clear",ResetStatistics);
        }
        else if(Kind==GameKind.Spider)
        {
            string[] labels=ClassicSpider?["Easy","Medium","Difficult"]:["Beginner","Intermediate","Advanced"];int[] levels=[1,2,4];
            for(int i=0;i<3;i++)
            {
                int level=levels[i];var tab=new RectangleF(x+i*w/3,y,w/3,25);bool selected=statisticsLevel==level;
                Skin.Fill(g,skin.Face,tab);Skin.Box(g,tab);if(selected)Skin.Line(g,skin.Face,tab.Left+2,tab.Bottom-1,tab.Right-2,tab.Bottom-1);
                skin.Text(g,labels[i],tab,center:true);Add("dialog-stats-"+level,tab,()=>{statisticsLevel=level;Invalidate();});dialogControl++;
            }
            var stats=Statistics.Difficulties.GetValueOrDefault(statisticsLevel)??new();
            skin.Group(g,"High Score",new(x,y+35,w,45));skin.Text(g,stats.BestScore.ToString(),new(x+12,y+51,w-24,23));
            skin.Group(g,"Percentage",new(x,y+88,w,81));
            skin.Text(g,$"Wins: {stats.Won}     Losses: {stats.Played-stats.Won}",new(x+12,y+109,w-24,23));
            skin.Text(g,$"Win Rate: {(stats.Played==0?0:100*stats.Won/stats.Played)}%",new(x+12,y+136,w-24,23));
            skin.Group(g,"Streaks",new(x,y+177,w,75));
            skin.Text(g,$"Most Wins: {stats.BestWinStreak}     Most Losses: {stats.BestLossStreak}",new(x+12,y+196,w-24,23));
            skin.Text(g,$"Current: {Math.Abs(stats.Streak)} {(stats.Streak>=0?"Wins":"Losses")}",new(x+12,y+221,w-24,23));
            DialogButton(g,"reset",new(x,bottom-36,80,25),"Reset",ResetStatistics);
        }
        else
        {
            var rows=new[]{("Games played",Statistics.Played.ToString()),("Games won",Statistics.Won.ToString()),("Win percentage",Statistics.Played==0?"0%":$"{100*Statistics.Won/Statistics.Played}%"),("Best score",Statistics.BestScore.ToString()),("Fastest win",Statistics.BestTime.HasValue?TimeSpan.FromSeconds(Statistics.BestTime.Value).ToString(@"m\:ss"):"-")};
            for(int i=0;i<rows.Length;i++){skin.Text(g,rows[i].Item1,new(x,y+i*27,w-90,24));skin.Text(g,rows[i].Item2,new(x+w-85,y+i*27,85,24));}
            DialogButton(g,"reset",new(x,bottom-36,80,25),"Reset",ResetStatistics);
        }
        DialogButton(g,"ok",new(x+w-80,bottom-36,80,25),"OK",CloseDialog,true);
    }
}
