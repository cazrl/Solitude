using System.Drawing.Drawing2D;

namespace Solitude;

public sealed partial class GameWindow
{
    private const double OrbitVictoryDuration=7.6;
    private const double OrbitVictoryHoldStart=6.3;
    private bool VictoryNeedsFrames=>showingVictory && (!skin.Future || MotionNow-victoryStart<OrbitVictoryHoldStart);
    private void CheckFutureVictoryEnd()
    {if(skin.Future && showingVictory && (!OrbitMotionEnabled || MotionNow-victoryStart>=OrbitVictoryDuration))FinishVictory();}
    private readonly record struct OrbitVictoryCard(Card Card,int Foundation,int Index);
    private readonly record struct OrbitVictoryPose(Card Card,int Foundation,int Index,PointF Center,float Width,float Height,float Roll,float Yaw,float Depth,bool Launched);
    private OrbitVictoryCard[] orbitVictoryDeck=[];
    private OrbitVictoryPose[] orbitVictoryPoses=[];
    private static float VictoryEase(double value)
    {float t=(float)Math.Clamp(value,0,1);return t*t*t*(10+t*(-15+6*t));}
    private static float Mix(float from,float to,float amount)=>from+(to-from)*amount;
    private static PointF Mix(PointF from,PointF to,float amount)=>new(Mix(from.X,to.X,amount),Mix(from.Y,to.Y,amount));
    private void StartFutureVictory()
    {
        collecting=false;selection=null;hint=null;futurePulses.Clear();victoryStart=MotionNow;PlayFutureSound("ORBIT_WIN");
        if(!OrbitMotionEnabled){showingVictory=false;OpenDialog(DialogPage.Won);return;}
        orbitVictoryDeck=Game.State.Foundations.SelectMany((pile,f)=>pile.Select((card,i)=>new OrbitVictoryCard(card,f,i))).ToArray();
        orbitVictoryPoses=new OrbitVictoryPose[orbitVictoryDeck.Length];
        showingVictory=true;boardStamp=null;ScheduleFrames();
    }
    private OrbitVictoryPose[] FutureVictoryPoses(double elapsed)
    {
        float cw=CardWidth,ch=CardHeight,cx=Table.Left+Table.Width/2,cy=Table.Top+Table.Height*.49f;
        float rx=Table.Width*.35f,ry=Table.Height*.26f;
        for(int i=0;i<orbitVictoryDeck.Length;i++)
        {
            var entry=orbitVictoryDeck[i];int f=entry.Foundation,index=entry.Index,order=12-index;
            var bay=TopCard(f+3);var origin=new PointF(bay.X+cw/2,bay.Y+ch/2);
            double release=.18+order*.052+f*.07;
            float launch=VictoryEase((elapsed-release)/1.12);
            float angle=f*MathF.PI/2+order*.091f+(float)elapsed*.86f-MathF.PI/2;
            float depth=(MathF.Sin(angle)+1)/2;
            var ring=new PointF(cx+MathF.Cos(angle)*rx,cy+MathF.Sin(angle)*ry);
            var lift=new PointF(origin.X,Math.Max(Table.Top+ch*.55f,origin.Y-ch*.38f));
            var approach=new PointF(Mix(origin.X,ring.X,.45f),ring.Y-ch*.30f);
            float t=launch,inv=1-t;
            var position=new PointF(inv*inv*inv*origin.X+3*inv*inv*t*lift.X+3*inv*t*t*approach.X+t*t*t*ring.X,
                inv*inv*inv*origin.Y+3*inv*inv*t*lift.Y+3*inv*t*t*approach.Y+t*t*t*ring.Y);
            float size=Mix(1,.62f+.48f*depth,launch);
            float roll=MathF.Sin(angle+MathF.PI/2)*.28f*launch;
            float flip=MathF.Tau*VictoryEase((elapsed-2.25-order*.038-f*.10)/.72);
            // Four hands form with a travelling ripple, then become completely
            // still for the final hold. Every pose remains a card from the win.
            float settle=VictoryEase((elapsed-4.15-order*.023-f*.055)/1.45);
            float fanWidth=Math.Min(cw,Table.Width/14.8f),offset=index-6;
            var fan=new PointF(Table.Left+Table.Width*(f%2==0?.27f:.73f)+offset*fanWidth*.27f,
                Table.Top+Table.Height*(f<2?.27f:.77f)+Math.Abs(offset)*fanWidth*.026f);
            position=Mix(position,fan,settle);
            float width=Mix(cw*size,fanWidth,settle),height=Mix(ch*size,fanWidth*ch/cw,settle);
            roll=Mix(roll,offset*.060f,settle);flip=Mix(flip,MathF.Tau,settle);
            float layer=elapsed<release?-10+index*.001f:Mix(depth,2+f*.1f+index*.003f,settle);
            orbitVictoryPoses[i]=new(entry.Card,f,index,position,width,height,roll,flip,layer,elapsed>=release);
        }
        return orbitVictoryPoses;
    }
    private void PaintFutureVictory(Graphics g)
    {
        double elapsed=MotionNow-victoryStart;
        var poses=FutureVictoryPoses(elapsed);
        var saved=g.Save();g.SetClip(Table,CombineMode.Intersect);
        if(Preferences.FutureAtmosphere && elapsed<5.9)
        {
            float strength=1-VictoryEase((elapsed-4.4)/1.5);
            // Local trails reuse the scene's ambient light without blending
            // another window-sized translucent texture on every frame.
            foreach(var p in poses.Where(p=>p.Launched && p.Depth<1.1f && p.Index%3==0))
            {
                using var pen=new Pen(Color.FromArgb((int)(52*strength),Orbit.Accent),1.1f);
                float tangent=(float)elapsed*.86f+p.Foundation*MathF.PI/2+(12-p.Index)*.091f;
                g.DrawLine(pen,p.Center.X-MathF.Cos(tangent)*22,p.Center.Y-MathF.Sin(tangent)*10,p.Center.X,p.Center.Y);
            }
        }
        Array.Sort(poses,static (a,b)=>a.Depth.CompareTo(b.Depth));
        foreach(var p in poses)
        {
            if(!p.Launched && poses.Any(other=>!other.Launched && other.Foundation==p.Foundation && other.Index>p.Index))continue;
            Orbit.DrawOrbitingCard(g,p.Card,p.Center,p.Width,p.Height,p.Roll,p.Yaw,ScaleFactor,CardWidth,CardHeight);
        }
        int alpha=(int)(255*VictoryEase((elapsed-5.6)/.65));
        float y=Table.Top+Table.Height*.52f;
        Orbit.Text(g,"ORBIT COMPLETE",new(Table.Left,y-21,Table.Width,38),26,Color.FromArgb(alpha,FutureArt.Ink),true);
        Orbit.Text(g,"52 cards. Beautifully played.",new(Table.Left,y+17,Table.Width,23),12,Color.FromArgb(alpha,Orbit.Accent),true);
        Orbit.Text(g,"Click or press Escape to continue",new(Table.Left,Math.Min(Table.Bottom-22,WorldHeight-90),Table.Width,20),10,FutureArt.Muted,true);
        g.Restore(saved);
    }
    public void RenderOrbitVictorySequence(string directory)
    {
        Directory.CreateDirectory(directory);
        var state=new GameState{Seed=1989,Started=true,Elapsed=185,Score=4254,TimeBonus=3780};
        for(int f=0;f<4;f++)state.Foundations[f]=Enumerable.Range(f*13,13).Select(id=>new Card(id)).ToList();
        SetRenderState(state);Preferences.Sound=false;renderMotionTime=100;StartVictory();
        try
        {
            foreach(int ms in new[]{0,450,1100,2250,2800,3300,4800,5500,6500,7590,7700})
            {
                renderMotionTime=100+ms/1000.0;Animate();
                using var bitmap=new Bitmap(ClientSize.Width,ClientSize.Height);
                using(var g=Graphics.FromImage(bitmap))PaintScaled(g);
                bitmap.Save(Path.Combine(directory,$"card-victory-{ms:0000}.png"));
            }
        }
        finally{renderMotionTime=null;showingVictory=false;StopCardMotion();}
    }
}
