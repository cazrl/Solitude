using System.Drawing.Drawing2D;

namespace Solitude;

/// <summary>Reads embedded Windows bitmap font strikes as data. No font installation or DLL loading.</summary>
internal sealed class RasterFont : IDisposable
{
    private readonly byte[] data;
    private readonly int origin, first, last, table, entrySize;
    private readonly Dictionary<(string, int, bool), Bitmap> cache = [];
    public int Height { get; }
    private RasterFont(byte[] bytes, int offset)
    {
        data=bytes;origin=offset;Height=U16(offset+88);first=bytes[offset+95];last=bytes[offset+96];
        bool v3=U16(offset)==0x300;table=offset+(v3?148:118);entrySize=v3?6:4;
    }
    private int U16(int p)=>BitConverter.ToUInt16(data,p);
    public static RasterFont? Embedded(string name)
    {
        using var stream=typeof(RasterFont).Assembly.GetManifestResourceStream("Solitude.Assets.Fonts."+name);
        if(stream==null)return null;using var data=new MemoryStream();stream.CopyTo(data);return new(data.ToArray(),0);
    }
    public static RasterFont? Installed(string filename, int points)
    {
        try
        {
            var bytes=File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"Fonts",filename));
            int ne=BitConverter.ToInt32(bytes,60);
            if(BitConverter.ToUInt16(bytes,ne)!=0x454e)return null;
            int resources=ne+BitConverter.ToUInt16(bytes,ne+36),shift=BitConverter.ToUInt16(bytes,resources),p=resources+2;
            while(BitConverter.ToUInt16(bytes,p)!=0)
            {
                int type=BitConverter.ToUInt16(bytes,p),count=BitConverter.ToUInt16(bytes,p+2);p+=8;
                for(int i=0;i<count;i++,p+=12)
                {
                    int offset=BitConverter.ToUInt16(bytes,p)<<shift;
                    if(type==0x8008 && BitConverter.ToUInt16(bytes,offset+68)==points)return new(bytes,offset);
                }
            }
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or ArgumentException or IndexOutOfRangeException) { }
        return null;
    }
    // These installed raster strikes do not consistently contain the CP1252
    // punctuation slots. Use the original menus' ASCII punctuation instead.
    private static string Normalize(string value)=>value.Replace("…","...").Replace('–','-').Replace('—','-').Replace('’','\'').Replace('“','"').Replace('”','"');
    private int Code(char c)=>c switch {'←'=>'<','→'=>'>','↑'=>'^','↓'=>'v',_=>c>=first && c<=last?c:'?'};
    private int Width(char c)=>U16(table+(Code(c)-first)*entrySize);
    public int Measure(string value,bool bold=false)=>Normalize(value).Sum(c=>Width(c)+(bold?1:0));
    private Bitmap Image(string value,Color color,bool bold)
    {
        var key=(value,color.ToArgb(),bold);
        if(cache.TryGetValue(key,out var ready))return ready;
        if(cache.Count>=256){foreach(var b in cache.Values)b.Dispose();cache.Clear();}
        var bitmap=new Bitmap(Math.Max(1,Measure(value,bold)),Height);
        using var g=Graphics.FromImage(bitmap);using var brush=new SolidBrush(color);int x=0;
        foreach(char c in value)
        {
            int item=table+(Code(c)-first)*entrySize,w=U16(item);
            int bits=origin+(entrySize==6?BitConverter.ToInt32(data,item+2):U16(item+2));
            for(int row=0;row<Height;row++)for(int col=0;col<w;col++)
                if((data[bits+(col/8)*Height+row]&(0x80>>(col%8)))!=0)g.FillRectangle(brush,x+col,row,bold?2:1,1);
            x+=w+(bold?1:0);
        }
        cache[key]=bitmap;return bitmap;
    }
    public void Draw(Graphics g,string value,RectangleF r,Color color,bool bold=false,bool center=false,bool wrap=false)
    {
        if(r.Width<=0 || r.Height<=0)return;
        value=Normalize(value);
        if(wrap)
        {
            float y=r.Y;
            foreach(string paragraph in value.Split('\n'))
            {
                string line="";
                foreach(string word in paragraph.Split(' '))
                {
                    string candidate=line.Length==0?word:line+" "+word;
                    if(line.Length>0 && Measure(candidate,bold)>r.Width)
                    {Draw(g,line,new(r.X,y,r.Width,Height),color,bold);y+=Height+2;line=word;}
                    else line=candidate;
                    if(y+Height>r.Bottom)return;
                }
                Draw(g,line,new(r.X,y,r.Width,Height),color,bold);y+=Height+2;
            }
            return;
        }
        if(Measure(value,bold)>r.Width)
        {
            int length=value.Length;
            while(length>0 && Measure(value[..length]+"...",bold)>r.Width)length--;
            value=value[..length]+"...";
        }
        var bitmap=Image(value,color,bold);
        float x=MathF.Round(r.X+(center?(r.Width-bitmap.Width)/2:0)),y0=MathF.Round(r.Y+(r.Height-Height)/2);
        var state=g.Save();g.SetClip(r,CombineMode.Intersect);
        g.DrawImage(bitmap,new RectangleF(x,y0,bitmap.Width,bitmap.Height),new RectangleF(0,0,bitmap.Width,bitmap.Height),GraphicsUnit.Pixel);
        g.Restore(state);
    }
    public void Dispose(){foreach(var b in cache.Values)b.Dispose();cache.Clear();}
}
