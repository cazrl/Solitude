namespace Solitude;

public sealed partial class GameWindow
{
    private readonly HashSet<string> vistaTipsSeen=[];
    private string? vistaTipTitle,vistaTipBody;
    private DateTime vistaTipUntil;
    private void ShowVistaTip(string title,string text,bool once=false)
    {
        if(!skin.Vista || !Preferences.DisplayTips || once && !vistaTipsSeen.Add(title))return;
        vistaTipTitle=title;vistaTipBody=text;vistaTipUntil=DateTime.Now.AddSeconds(8);Invalidate();
    }
    private void PaintVistaTip(Graphics g)
    {
        if(!skin.Vista || !Preferences.DisplayTips || vistaTipTitle==null || dialog!=DialogPage.None || showingVictory)return;
        if(DateTime.Now>=vistaTipUntil){vistaTipTitle=null;return;}
        var box=new RectangleF(Table.Left+9,Table.Bottom-99,235,83);
        using var path=Skin.Rounded(box,4);using var brush=new SolidBrush(Color.FromArgb(255,255,225));
        g.FillPath(brush,path);g.DrawPath(Pens.Gray,path);
        skin.Text(g,vistaTipTitle,new(box.X+9,box.Y+5,box.Width-18,21),font:skin.Bold);
        skin.Wrapped(g,vistaTipBody??"",new(box.X+9,box.Y+28,box.Width-18,box.Height-32));
        Add("vista-tip",box,()=>{vistaTipTitle=null;Invalidate();});
    }
}
