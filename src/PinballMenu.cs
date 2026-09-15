namespace Solitude;

// Keep WinForms' keyboard, mnemonic, submenu and accessibility behavior, but
// render the popup in XP's palette at the same scale as the surrounding frame.
internal sealed class PinballMenu : ContextMenuStrip
{
    private readonly Font face;
    private readonly float scale;
    internal PinballMenu(float scale)
    {
        this.scale=scale;
        face=new Font("Tahoma",11*scale,FontStyle.Regular,GraphicsUnit.Pixel);
        Font=face;
    }
    internal void Prepare()=>PrepareDropDown(this);
    private void PrepareDropDown(ToolStripDropDownMenu menu)
    {
        menu.Font=face;menu.Renderer=new XpRenderer(scale);
        menu.ShowImageMargin=false;menu.ShowCheckMargin=false;
        menu.Padding=new Padding(2);menu.AutoSize=false;
        int width=0,height=4;
        foreach(ToolStripItem item in menu.Items)
        {
            item.Font=face;item.AutoSize=false;item.Margin=Padding.Empty;
            if(item is ToolStripMenuItem entry)
            {
                int label=TextRenderer.MeasureText((entry.Text??"").Replace("&",""),face,Size.Empty,TextFormatFlags.NoPadding).Width;
                int shortcut=TextRenderer.MeasureText(entry.ShortcutKeyDisplayString,face,Size.Empty,TextFormatFlags.NoPadding).Width;
                width=Math.Max(width,label+shortcut+(int)Math.Ceiling(70*scale));
                if(entry.HasDropDownItems)PrepareDropDown((ToolStripDropDownMenu)entry.DropDown);
            }
        }
        foreach(ToolStripItem item in menu.Items)
        {
            item.Size=new(width,(int)Math.Ceiling((item is ToolStripSeparator?7:20)*scale));
            height+=item.Height;
        }
        menu.Size=new(width+4,height);
    }
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)face.Dispose();}

    private sealed class XpRenderer(float scale) : ToolStripRenderer
    {
        private static readonly Color Blue=Color.FromArgb(49,106,197);
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)=>e.Graphics.Clear(Color.White);
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {using var pen=new Pen(Color.FromArgb(128,128,128));e.Graphics.DrawRectangle(pen,0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1);}
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            using var brush=new SolidBrush(e.Item.Selected?Blue:Color.White);
            e.Graphics.FillRectangle(brush,new Rectangle(Point.Empty,e.Item.Size));
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // ToolStrip asks separately for label and shortcut. Draw both once
            // in explicit columns, instead of relying on today's menu metrics.
            if(e.Item is not ToolStripMenuItem item || e.Text!=item.Text)return;
            Color ink=!item.Enabled?Color.FromArgb(128,128,128):item.Selected?Color.White:Color.Black;
            int left=(int)(24*scale),right=(int)(18*scale);
            var bounds=new Rectangle(left,0,item.Width-left-right,item.Height);
            var flags=TextFormatFlags.NoPadding|TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter;
            TextRenderer.DrawText(e.Graphics,item.Text,item.Font,bounds,ink,flags);
            if(!string.IsNullOrEmpty(item.ShortcutKeyDisplayString))
                TextRenderer.DrawText(e.Graphics,item.ShortcutKeyDisplayString,item.Font,bounds,ink,flags|TextFormatFlags.Right|TextFormatFlags.NoPrefix);
            if(item.Checked)
            {
                using var pen=new Pen(ink,Math.Max(1,scale));
                float y=item.Height/2f;
                e.Graphics.DrawLines(pen,new PointF[]{new(6*scale,y),new(9*scale,y+3*scale),new(15*scale,y-4*scale)});
            }
        }
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){}
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            using var brush=new SolidBrush(e.Item?.Selected==true?Color.White:Color.Black);
            float x=(e.Item?.Width??0)-10*scale,y=(e.Item?.Height??0)/2f;
            e.Graphics.FillPolygon(brush,new PointF[]{new(x-2*scale,y-4*scale),new(x+2*scale,y),new(x-2*scale,y+4*scale)});
        }
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {using var pen=new Pen(Color.FromArgb(172,168,153));e.Graphics.DrawLine(pen,2,e.Item.Height/2,e.Item.Width-3,e.Item.Height/2);}
    }
}
