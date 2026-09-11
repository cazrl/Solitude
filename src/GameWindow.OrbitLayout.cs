namespace Solitude;

public sealed partial class GameWindow
{
    private bool fittingOrbit;
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // CenterScreen can position the initial 300x300 native handle before
        // sizing finishes. Place the final window before first display.
        CenterGameWindow(startupWorkingArea??Screen.FromControl(this).WorkingArea);
        startupWorkingArea=null;
    }
    private void CenterGameWindow(Rectangle area)
    {
        bool wasFitting=fittingOrbit;fittingOrbit=true;
        try
        {
            ApplySize(area);
            Location=new(area.Left+(area.Width-Width)/2,area.Top+(area.Height-Height)/2);
            // Moving to a monitor with a different DPI can resize the native
            // window synchronously; restore the requested fitted size there.
            ApplySize(area);
            Location=new(area.Left+(area.Width-Width)/2,area.Top+(area.Height-Height)/2);
        }
        finally{fittingOrbit=wasFitting;}
    }
    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);FitOrbitToMonitor();
    }
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);FitOrbitToMonitor();
    }
    private void FitOrbitToMonitor()
    {
        if(fittingOrbit || editionMorph!=null || skin==null || !IsHandleCreated)return;
        var area=Screen.FromControl(this).WorkingArea;float fit=FittedScale(area.Size);
        if(Math.Abs(fit-windowFitScale)<.001)return;
        fittingOrbit=true;
        try
        {
            float old=ScaleFactor;windowFitScale=fit;MinimumSize=new((int)(LogicalMinimum.Width*fit),(int)(LogicalMinimum.Height*fit));
            ClientSize=new(Math.Min(area.Width,(int)(ClientSize.Width*fit/old)),Math.Min(area.Height,(int)(ClientSize.Height*fit/old)));
            Location=new(Math.Clamp(Left,area.Left,Math.Max(area.Left,area.Right-Width)),Math.Clamp(Top,area.Top,Math.Max(area.Top,area.Bottom-Height)));
            boardStamp=null;layoutState=null;StopCardMotion();UpdateWindowShape();Invalidate();
        }
        finally{fittingOrbit=false;}
    }
    private float OrbitRoom=>Math.Clamp((WorldHeight-540)/110,0,1);
    private float OrbitMenuHeight=>44+30*OrbitRoom;
    private float OrbitStatusHeight=>56+20*OrbitRoom;
    private float OrbitTableauGap=>40+14*OrbitRoom;
    private float OrbitHiddenStep=>2+2*OrbitRoom;
    // Reserve space for the longest legal Klondike column: six hidden cards
    // and a King-to-Ace run. Size depends only on the window, never the deal,
    // so moving a card cannot make every other card resize or change position.
    private float OrbitCardWidth=>Math.Max(28,Math.Min(Math.Clamp(Table.Width*.081f,58,122),
        (TableauBottom-Table.Top-OrbitTableauGap-6*OrbitHiddenStep)/(2*152f/111+12*.27f)));
    private void PrepareOrbitMoveLayout()
    {
        EnsureLayout();
        boardStamp=null;
    }
    private RectangleF OrbitColumnViewport(int column)=>new(BoardLeft+ColumnGap*column-4,TableauY,CardWidth+8,Math.Max(1,TableauBottom-TableauY+6));
}
