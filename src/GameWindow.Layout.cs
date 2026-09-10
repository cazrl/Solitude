namespace Solitude;

public sealed partial class GameWindow
{
    private GameState? layoutState;
    private int layoutMoves=-1;
    private RectangleF layoutTable;
    private float[][] cardY=[];
    private float[] stackSteps=[];
    private void EnsureLayout()
    {
        if(Game==null || skin==null)return;
        var table=Table;
        if(ReferenceEquals(layoutState,Game.State) && layoutMoves==Game.State.Moves && layoutTable==table)return;
        layoutState=Game.State;layoutMoves=Game.State.Moves;layoutTable=table;
        int count=Game.State.Tableau.Count;
        if(cardY.Length!=count){cardY=new float[count][];stackSteps=new float[count];}
        for(int col=0;col<count;col++)
        {
            var pile=Game.State.Tableau[col];int down=0;
            while(down<pile.Count && !pile[down].FaceUp)down++;
            float available=TableauBottom-TableauY-CardHeight;
            stackSteps[col]=Math.Clamp((available-down*7)/Math.Max(1,pile.Count-down-1),1,skin.Vista?25:Kind==GameKind.Spider?27:18);
            if(cardY[col]==null || cardY[col].Length<pile.Count+1)cardY[col]=new float[pile.Count+8];
            float y=TableauY;
            for(int i=0;i<=pile.Count;i++){cardY[col][i]=y;if(i<pile.Count)y+=pile[i].FaceUp?stackSteps[col]:7;}
        }
    }
}
