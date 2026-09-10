$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$sprite = [Drawing.Image]::FromFile((Join-Path $PWD 'references\old-sprite.png'))
$faces = New-Object Drawing.Bitmap 923,384
$fg = [Drawing.Graphics]::FromImage($faces)
$columns = @(1,2,0,3)
for ($s=0;$s -lt 4;$s++) { for ($rank=0;$rank -lt 13;$rank++) {
    $dest = New-Object Drawing.Rectangle ($rank*71),($s*96),71,96
    $fg.DrawImage($sprite,$dest,($columns[$s]*71),($rank*96),71,96,[Drawing.GraphicsUnit]::Pixel)
} }
$faces.Save((Join-Path $PWD 'src\Assets\cards.png'),[Drawing.Imaging.ImageFormat]::Png)
$fg.Dispose();$faces.Dispose();$sprite.Dispose()
$sheet = New-Object Drawing.Bitmap 984,128
$g = [Drawing.Graphics]::FromImage($sheet)
$g.Clear([Drawing.Color]::FromArgb(0,128,0))
for ($i=0; $i -lt 12; $i++) {
    $img = [Drawing.Image]::FromFile((Join-Path $PWD "src\Assets\Classic\$($i+54).bmp"))
    $g.DrawImageUnscaled($img, ($i*82+5), 4)
    $g.DrawString("$i", [Drawing.SystemFonts]::DefaultFont, [Drawing.Brushes]::White, ($i*82+5), 105)
    $img.Dispose()
}
$sheet.Save((Join-Path $PWD 'references\backs.png'), [Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $sheet.Dispose()
$icon = New-Object Drawing.Bitmap 64,64
$g = [Drawing.Graphics]::FromImage($icon)
$g.Clear([Drawing.Color]::Transparent)
$g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.FillRectangle([Drawing.Brushes]::White, 6,8,35,46)
$g.FillRectangle([Drawing.Brushes]::RoyalBlue, 9,11,29,40)
$g.FillRectangle([Drawing.Brushes]::White, 22,14,35,46)
$g.DrawRectangle([Drawing.Pens]::DimGray,22,14,35,46)
$font = New-Object Drawing.Font 'Segoe UI Symbol',26,([Drawing.FontStyle]::Regular),([Drawing.GraphicsUnit]::Pixel)
$g.DrawString([char]0x2660,$font,[Drawing.Brushes]::Black,24,21)
$small = New-Object Drawing.Font 'Arial',10,([Drawing.FontStyle]::Bold),([Drawing.GraphicsUnit]::Pixel)
$g.DrawString('A',$small,[Drawing.Brushes]::Black,24,14)
$stream = New-Object IO.MemoryStream
$icon.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
$png = $stream.ToArray()
$output = [IO.File]::Create((Join-Path $PWD 'src\Assets\solitude.ico'))
$bw = New-Object IO.BinaryWriter $output
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]1)
$bw.Write([byte]64); $bw.Write([byte]64); $bw.Write([byte]0); $bw.Write([byte]0)
$bw.Write([uint16]1); $bw.Write([uint16]32); $bw.Write([uint32]$png.Length); $bw.Write([uint32]22); $bw.Write($png)
$bw.Dispose(); $stream.Dispose(); $font.Dispose(); $small.Dispose(); $g.Dispose(); $icon.Dispose()
