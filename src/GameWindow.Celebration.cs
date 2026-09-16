using System.Drawing.Drawing2D;

namespace Solitude;

public sealed partial class GameWindow
{
    private int celebrationCue=-1;
    private double victoryStart,lastVictoryTime,victoryAccumulator;
    private bool StartPeriodCelebration()
    {
        if(Kind==GameKind.FreeCell)
        {
            PlayVistaSound("FREECELLWIN");OpenDialog(DialogPage.Won);return true;
        }
        if(!skin.Modern && Kind!=GameKind.Spider)return false;
        if(skin.Modern && !Preferences.Animate){OpenDialog(DialogPage.Won);return true;}
        collecting=false;selection=null;showingVictory=true;victoryStart=MotionNow;celebrationCue=-1;
        victoryTrail?.Dispose();victoryTrail=new Bitmap((int)MathF.Ceiling(WorldWidth),(int)MathF.Ceiling(WorldHeight));
        boardStamp=null;ScheduleFrames();return true;
    }
    private void AnimatePeriodCelebration()
    {
        double elapsed=MotionNow-victoryStart;
        using var g=Graphics.FromImage(victoryTrail!);g.Clear(Color.Transparent);skin.Configure(g);g.SetClip(Table);
        if(Kind==GameKind.Spider)
        {
            PaintFireworks(g,elapsed);
            if(!skin.Modern)
            {
                using var font=new Font("Arial",30,FontStyle.Bold,GraphicsUnit.Pixel);
                string label="You Won!";float x=Table.Left+Table.Width/2-70,y=Table.Top+Table.Height/2-24;
                Color[] colors=[Color.Red,Color.Yellow,Color.Lime,Color.Cyan,Color.Magenta];
                for(int i=0;i<label.Length;i++)skin.Text(g,label[i].ToString(),new(x+i*20,y,28,40),colors[i%colors.Length],font);
            }
            // The captured Spider show continues until the player dismisses it.
        }
        else
        {
            PaintShatteringCards(g,elapsed);
            if(elapsed>8.2)FinishVictory();
        }
        Invalidate();
    }
    private void PaintShatteringCards(Graphics g,double elapsed)
    {
        // Vista RC1 reference: four foundations empty in parallel, cards fall
        // upright and fragment on the table's lower edge (Long Zheng, 2006).
        for(int i=51;i>=0;i--)
        {
            int f=i%4,rank=12-i/4;if(Game.State.Foundations[f].Count<=rank)continue;
            var origin=TopCard(f+3);double age=elapsed-i*.105;
            if(age<0){art.Draw(g,Game.State.Foundations[f][rank],origin,Preferences.Era,Preferences.CardBack);continue;}
            float gravity=Table.Height*1.1f;
            double impact=Math.Sqrt(2*(Table.Bottom-origin.Y-CardHeight)/gravity);
            float vx=-(45+i*17%95)*Table.Width/700;
            float x=origin.X+vx*(float)Math.Min(age,impact),y=origin.Y+gravity*.5f*(float)(age*age);
            if(age<impact)
            {
                art.Draw(g,Game.State.Foundations[f][rank],new(x,y,CardWidth,CardHeight),Preferences.Era,Preferences.CardBack);continue;
            }
            double burst=age-impact;if(burst>1.05)continue;
            for(int piece=0;piece<24;piece++)
            {
                float angle=(piece*2.39996f+i),speed=28+(piece*31+i*13)%130;
                float px=x+CardWidth/2+MathF.Cos(angle)*speed*(float)burst;
                float py=Table.Bottom-8-MathF.Abs(MathF.Sin(angle))*speed*(float)burst+130*(float)(burst*burst);
                float size=2+(piece%4);Color ink=piece%7==0?Color.Red:piece%9==0?Color.Black:Color.White;
                using var brush=new SolidBrush(Color.FromArgb((int)(255*(1-burst/1.05)),ink));
                g.FillPolygon(brush,new PointF[]{new(px,py),new(px+size,py-size),new(px+size*1.7f,py+size)});
            }
        }
        int cue=(int)(elapsed/.7);
        if(cue!=celebrationCue){celebrationCue=cue;if(elapsed>1.1 && elapsed<7.5)PlayVistaSound("SHARED_CARDSHATTER"+(cue%3+1));}
    }
    private void PaintFireworks(Graphics g,double elapsed)
    {
        g.SmoothingMode=skin.Modern?SmoothingMode.AntiAlias:SmoothingMode.None;
        int newest=(int)(elapsed/.65);
        for(int n=Math.Max(0,newest-4);n<=newest;n++)
        {
            double age=elapsed-n*.65;
            float x=Table.Left+Table.Width*(.16f+((n*37)%69)/100f),top=Table.Top+Table.Height*(.12f+((n*23)%39)/100f);
            Color color=skin.Modern?Color.FromArgb(170+(n*19)%85,200+(n*7)%55,100+(n*43)%155):new[]{Color.Yellow,Color.Cyan,Color.Magenta,Color.Lime,Color.Red}[n%5];
            if(age<.55)
            {
                float y=Table.Bottom-(Table.Bottom-top)*(float)(age/.55);
                using var pen=new Pen(color,skin.Modern?2:1);g.DrawLine(pen,x,y,x+2,y+15);continue;
            }
            age-=.55;if(age>2)continue;int alpha=(int)(255*(1-age/2));
            for(int p=0;p<50;p++)
            {
                float angle=p*MathF.PI*2/50+n*.41f,speed=Table.Width*(.07f+.045f*((p*17)%11)/10);
                float px=x+MathF.Cos(angle)*speed*(float)age,py=top+MathF.Sin(angle)*speed*(float)age+25*(float)(age*age);
                using var pen=new Pen(Color.FromArgb(alpha,color),skin.Modern?1.4f:1);
                g.DrawLine(pen,px,py,px-MathF.Cos(angle)*4,py-MathF.Sin(angle)*4);
            }
        }
        if(newest!=celebrationCue){celebrationCue=newest;if(skin.Modern)PlayVistaSound("SPIDER_FIREWORKS0"+(newest%3+1));}
    }
}
