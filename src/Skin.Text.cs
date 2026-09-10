using System.Drawing.Text;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Solitude;

public sealed partial class Skin
{
    private readonly Dictionary<(string,float,FontStyle),Font> nativeFonts=[];
    private readonly Dictionary<(string,Font,Size,int,bool,bool),Bitmap> nativeText=[];
    private int textPixels;
    private readonly Dictionary<(string,Font,Size),Bitmap> captionGlows=[];
    private void PaintCaptionGlow(Graphics g,string text,RectangleF bounds)
    {
        using var transform=g.Transform;float scale=transform.Elements[0];PointF[] points=[bounds.Location,new(bounds.Right,bounds.Bottom)];transform.TransformPoints(points);
        var rect=Rectangle.FromLTRB((int)MathF.Round(points[0].X),(int)MathF.Round(points[0].Y),(int)MathF.Round(points[1].X),(int)MathF.Round(points[1].Y));if(rect.Width<=0 || rect.Height<=0)return;
        int pad=(int)MathF.Ceiling(7*scale);var key=(text,NativeFont(CaptionFont,scale),rect.Size);
        if(!captionGlows.TryGetValue(key,out var glow))
        {
            if(captionGlows.Count>=16){foreach(var old in captionGlows.Values)old.Dispose();captionGlows.Clear();}
            using var glyphs=RenderNativeText(text,key.Item2,rect.Size,Color.White,false,false);
            int w=rect.Width+2*pad,h=rect.Height+2*pad;glow=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
            using(var canvas=Graphics.FromImage(glow))canvas.DrawImageUnscaled(glyphs,pad,pad);
            var bits=glow.LockBits(new(0,0,w,h),ImageLockMode.ReadWrite,PixelFormat.Format32bppPArgb);
            try
            {
                var pixels=new byte[bits.Stride*h];Marshal.Copy(bits.Scan0,pixels,0,pixels.Length);var alpha=new int[w*h];var temp=new int[w*h];
                for(int y=0;y<h;y++)for(int x=0;x<w;x++)alpha[y*w+x]=pixels[y*bits.Stride+x*4+3];
                int radius=Math.Max(1,(int)MathF.Round(2*scale)),divisor=2*radius+1;
                for(int pass=0;pass<3;pass++)
                {
                    for(int y=0;y<h;y++)for(int x=0;x<w;x++){int sum=0;for(int k=-radius;k<=radius;k++)if(x+k>=0 && x+k<w)sum+=alpha[y*w+x+k];temp[y*w+x]=sum/divisor;}
                    for(int y=0;y<h;y++)for(int x=0;x<w;x++){int sum=0;for(int k=-radius;k<=radius;k++)if(y+k>=0 && y+k<h)sum+=temp[(y+k)*w+x];alpha[y*w+x]=sum/divisor;}
                }
                for(int y=0;y<h;y++)for(int x=0;x<w;x++){int i=y*bits.Stride+x*4;byte a=(byte)Math.Min(220,alpha[y*w+x]*4);pixels[i]=pixels[i+1]=pixels[i+2]=pixels[i+3]=a;}
                Marshal.Copy(pixels,0,bits.Scan0,pixels.Length);
            }
            finally{glow.UnlockBits(bits);}
            captionGlows[key]=glow;
        }
        var state=g.Save();g.ResetTransform();g.DrawImageUnscaled(glow,rect.X-pad,rect.Y-pad);g.Restore(state);
    }
    private Font NativeFont(Font font,float scale)
    {
        float pixels=Math.Max(1,MathF.Round(font.Size*scale));var key=(font.Name,pixels,font.Style);
        if(!nativeFonts.TryGetValue(key,out var result))nativeFonts[key]=result=new Font(font.Name,pixels,font.Style,GraphicsUnit.Pixel);
        return result;
    }
    private float MeasureNative(Graphics g,string text,Font font)
    {
        using var matrix=g.Transform;float scale=matrix.Elements[0];
        if(Vista)
        {
            using var format=VistaTextFormat(false,false);
            // Measure at output resolution with the same engine that paints the mask.
            return g.MeasureString(text,NativeFont(font,scale),int.MaxValue,format).Width/scale;
        }
        return TextRenderer.MeasureText(text,NativeFont(font,scale),new Size(int.MaxValue,int.MaxValue),TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine).Width/scale;
    }
    private static StringFormat VistaTextFormat(bool center,bool wrap)
    {
        var format=(StringFormat)StringFormat.GenericTypographic.Clone();
        format.Alignment=center?StringAlignment.Center:StringAlignment.Near;
        format.LineAlignment=wrap?StringAlignment.Near:StringAlignment.Center;
        format.Trimming=wrap?StringTrimming.Word:StringTrimming.EllipsisCharacter;
        format.FormatFlags|=StringFormatFlags.MeasureTrailingSpaces;
        if(!wrap)format.FormatFlags|=StringFormatFlags.NoWrap;
        return format;
    }
    private void DrawNative(Graphics g,string text,RectangleF bounds,Color color,Font font,bool center,bool wrap)
    {
        if(bounds.Width<=0 || bounds.Height<=0)return;
        using var matrix=g.Transform;float scale=matrix.Elements[0];PointF[] corners=[bounds.Location,new(bounds.Right,bounds.Bottom)];matrix.TransformPoints(corners);
        var rect=Rectangle.FromLTRB((int)MathF.Round(corners[0].X),(int)MathF.Round(corners[0].Y),(int)MathF.Round(corners[1].X),(int)MathF.Round(corners[1].Y));
        if(rect.Width<=0 || rect.Height<=0)return;
        var key=(text,NativeFont(font,scale),rect.Size,color.ToArgb(),center,wrap);
        if(!nativeText.TryGetValue(key,out var pixels))
        {
            if(nativeText.Count>=256 || textPixels+rect.Width*rect.Height>4_000_000){foreach(var item in nativeText.Values)item.Dispose();nativeText.Clear();textPixels=0;}
            pixels=RenderNativeText(text,key.Item2,rect.Size,color,center,wrap);nativeText[key]=pixels;textPixels+=rect.Width*rect.Height;
        }
        var state=g.Save();g.ResetTransform();g.DrawImageUnscaled(pixels,rect.Location);g.Restore(state);
    }
    private Bitmap RenderNativeText(string text,Font font,Size size,Color color,bool center,bool wrap)
    {
        // GDI draws onto an opaque mask. Compositing that mask through GDI+ keeps
        // glyphs stable under clipped repaints and avoids ClearType color fringes.
        using var mask=new Bitmap(size.Width,size.Height,PixelFormat.Format24bppRgb);
        using(var g=Graphics.FromImage(mask))
        {
            g.Clear(Color.White);g.TextRenderingHint=Vista?TextRenderingHint.AntiAliasGridFit:TextRenderingHint.SingleBitPerPixelGridFit;
            if(Vista)
            {
                // DrawString preserves the requested Segoe UI face here. The GDI
                // TextRenderer mask produced substituted serif glyphs on this host.
                using var format=VistaTextFormat(center,wrap);
                g.DrawString(text,font,Brushes.Black,new RectangleF(PointF.Empty,size),format);
            }
            else
            {
                var flags=TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix;
                flags|=wrap?TextFormatFlags.WordBreak:TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
                if(center)flags|=TextFormatFlags.HorizontalCenter;
                TextRenderer.DrawText(g,text,font,new Rectangle(Point.Empty,size),Color.Black,Color.White,flags);
            }
        }
        var result=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppPArgb);var bounds=new Rectangle(Point.Empty,size);
        var input=mask.LockBits(bounds,ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);var output=result.LockBits(bounds,ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
        try
        {
            var source=new byte[input.Stride*size.Height];Marshal.Copy(input.Scan0,source,0,source.Length);var dest=new byte[output.Stride*size.Height];
            for(int y=0;y<size.Height;y++)for(int x=0;x<size.Width;x++)
            {
                int i=y*input.Stride+x*3,o=y*output.Stride+x*4,coverage=255-(source[i]+source[i+1]+source[i+2])/3;
                int a=(Vista?coverage:coverage>=128?255:0)*color.A/255;dest[o]=(byte)(color.B*a/255);dest[o+1]=(byte)(color.G*a/255);dest[o+2]=(byte)(color.R*a/255);dest[o+3]=(byte)a;
            }
            Marshal.Copy(dest,0,output.Scan0,dest.Length);
        }
        finally{mask.UnlockBits(input);result.UnlockBits(output);}
        return result;
    }
}
