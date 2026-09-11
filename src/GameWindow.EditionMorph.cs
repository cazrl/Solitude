using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Solitude;

public sealed partial class GameWindow
{
    private sealed record EditionMorph(Bitmap Before,Bitmap After,Rectangle From,Rectangle To,Size Minimum,double Start,bool Reverse,Color Accent);
    private EditionMorph? editionMorph;
    private Size? editionRenderSize;
    private const double EditionMorphDuration=.9;
    private static float MorphEase(float t){t=Math.Clamp(t,0,1);return t*t*t*(10+t*(-15+6*t));}
    private Bitmap CaptureEditionSurface(Size size)
    {
        var previous=editionRenderSize;editionRenderSize=size;
        var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppPArgb);
        try{using var g=Graphics.FromImage(image);PaintScaled(g);return image;}
        catch{image.Dispose();throw;}
        finally{editionRenderSize=previous;layoutState=null;boardStamp=null;}
    }
    private void BeginEditionMorph(Bitmap before,Rectangle from,Rectangle area,bool animate,Color departureAccent)
    {
        FinishEditionMorph();
        if(!animate || orbitMotionFaulted){CenterGameWindow(area);return;}
        var (size,minimum)=EditionWindowMetrics(area);
        var destination=new Rectangle(area.Left+(area.Width-size.Width)/2,area.Top+(area.Height-size.Height)/2,size.Width,size.Height);
        var next=CaptureEditionSurface(size);
        editionMorph=new(new Bitmap(before),next,from,destination,minimum,MotionNow,!skin.Future,skin.Future?Orbit.Accent:departureAccent);
        // The source may be smaller than ORBIT's normal minimum. Only the
        // transition owns these intermediate native bounds; gameplay is paused.
        MinimumSize=new(1,1);Bounds=from;hotspots.Clear();cardAreas.Clear();
        ScheduleFrames();Invalidate();
    }
    private void UpdateEditionMorph()
    {
        if(editionMorph is not {} morph)return;
        float progress=(float)((MotionNow-morph.Start)/EditionMorphDuration);
        if(progress>=1 || !morph.Reverse && !Preferences.Animate){FinishEditionMorph();return;}
        float t=MorphEase(progress);
        int Mix(int a,int b)=>(int)Math.Round(a+(b-a)*t);
        var bounds=new Rectangle(Mix(morph.From.X,morph.To.X),Mix(morph.From.Y,morph.To.Y),Mix(morph.From.Width,morph.To.Width),Mix(morph.From.Height,morph.To.Height));
        if(Bounds!=bounds)Bounds=bounds;
    }
    private void FinishEditionMorph()
    {
        if(editionMorph is not {} morph)return;
        editionMorph=null;
        try
        {
            bool previous=fittingOrbit;fittingOrbit=true;
            try{MinimumSize=morph.Minimum;Bounds=morph.To;}
            finally{fittingOrbit=previous;}
            layoutState=null;boardStamp=null;StopCardMotion();UpdateWindowShape();
            lastTick=Environment.TickCount64;Invalidate();ScheduleFrames();
        }
        finally{morph.Before.Dispose();morph.After.Dispose();}
    }
    private void DisposeEditionMorph()
    {
        var morph=editionMorph;editionMorph=null;morph?.Before.Dispose();morph?.After.Dispose();
    }
    private void PaintEditionMorph(Graphics g)
    {
        if(editionMorph is not {} morph)return;
        float p=Math.Clamp((float)((MotionNow-morph.Start)/EditionMorphDuration),0,1);
        var saved=g.Save();
        try
        {
            g.CompositingMode=CompositingMode.SourceCopy;g.Clear(Color.FromArgb(7,17,26));g.CompositingMode=CompositingMode.SourceOver;
            g.InterpolationMode=InterpolationMode.Bilinear;g.PixelOffsetMode=PixelOffsetMode.Half;
            void Layer(Bitmap bitmap,float alpha)
            {
                if(alpha<=0)return;
                using var attributes=new ImageAttributes();attributes.SetColorMatrix(new ColorMatrix{Matrix33=Math.Clamp(alpha,0,1)});
                g.DrawImage(bitmap,ClientRectangle,0,0,bitmap.Width,bitmap.Height,GraphicsUnit.Pixel,attributes);
            }
            Layer(morph.Before,1);
            float reveal=MorphEase((p-.04f)/.92f);
            float edge=ClientSize.Width*(morph.Reverse?1-reveal:reveal);
            var revealClip=g.Save();g.SetClip(morph.Reverse?new RectangleF(edge,0,ClientSize.Width-edge,ClientSize.Height):new RectangleF(0,0,edge,ClientSize.Height),CombineMode.Intersect);
            Layer(morph.After,1);g.Restore(revealClip);
            float energy=MathF.Pow(MathF.Sin(MathF.PI*p),2);
            g.SmoothingMode=SmoothingMode.AntiAlias;
            float w=ClientSize.Width,h=ClientSize.Height,cx=w*.5f,cy=h*.5f;
            var accent=morph.Accent;
            float phase=morph.Reverse?1-p:p;
            // A light front crosses the old surface as orbital arcs resolve
            // around the new one. Every effect vanishes at both endpoints.
            float beam=Math.Max(24,w*.045f),x=edge;
            using(var sweep=new LinearGradientBrush(new RectangleF(x-beam,0,beam*2,h),Color.Transparent,Color.Transparent,0f))
            {
                sweep.InterpolationColors=new ColorBlend{Positions=[0,.5f,1],Colors=[Color.Transparent,Color.FromArgb((int)(48*energy),accent),Color.Transparent]};
                g.FillRectangle(sweep,x-beam,0,beam*2,h);
            }
            using(var front=new Pen(Color.FromArgb((int)(150*energy),accent),Math.Max(1,ScaleFactor)))g.DrawLine(front,x,0,x,h);
            for(int ring=0;ring<3;ring++)
            {
                float rw=w*(.38f+ring*.16f+phase*.12f),rh=h*(.16f+ring*.085f);
                using var pen=new Pen(Color.FromArgb((int)((64-ring*14)*energy),accent),Math.Max(1,ScaleFactor));
                g.DrawArc(pen,cx-rw/2,cy-rh/2,rw,rh,phase*170+ring*72,185);
            }
            using var light=new SolidBrush(Color.FromArgb((int)(180*energy),accent));
            for(int i=0;i<18;i++)
            {
                float angle=i*MathF.Tau/18+phase*1.5f,radius=w*(.12f+.22f*phase),r=(1+i%3)*ScaleFactor;
                g.FillEllipse(light,cx+MathF.Cos(angle)*radius-r/2,cy+MathF.Sin(angle)*radius*.42f-r/2,r,r);
            }
        }
        finally{g.Restore(saved);}
    }
}
