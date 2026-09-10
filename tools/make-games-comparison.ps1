$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$canvas=[Drawing.Bitmap]::new(1656,904)
$g=[Drawing.Graphics]::FromImage($canvas)
$g.Clear([Drawing.Color]::FromArgb(235,237,239))
$font=[Drawing.Font]::new('Segoe UI',16,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$small=[Drawing.Font]::new('Segoe UI',12,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$g.DrawString('Solitude 0.3 - Solitaire, FreeCell and Spider',$font,[Drawing.Brushes]::Black,18,10)
$g.DrawString('Rendered by the packaged EXE. Each game has its own rules, layout, artwork and saved session.',$small,[Drawing.Brushes]::DimGray,18,38)
$games=@('','-FreeCell','-Spider')
$labels=@('Solitaire','FreeCell','Spider')
$eras=@('WindowsXP','WindowsVista')
for($row=0;$row -lt 2;$row++){for($col=0;$col -lt 3;$col++){
    $left=18+$col*546;$top=70+$row*414
    $g.DrawString("$($eras[$row]) / $($labels[$col])",$font,[Drawing.Brushes]::Black,$left,$top)
    $source=[Drawing.Bitmap]::new((Join-Path $taskRoot "artifacts/release-v030-renders/$($eras[$row])$($games[$col])-100.png"))
    $factor=[Math]::Min(528/$source.Width,380/$source.Height)
    $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($source,[Drawing.RectangleF]::new($left,$top+27,$source.Width*$factor,$source.Height*$factor))
    $source.Dispose()
}}
$canvas.Save((Join-Path $taskRoot 'artifacts/games-comparison.png'),[Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$font.Dispose();$small.Dispose();$canvas.Dispose()
