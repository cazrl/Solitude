$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
function Crop-GameAsset($source,$name,$x,$y,$width,$height) {
    $tile=$source.Clone([Drawing.Rectangle]::new($x,$y,$width,$height),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $tile.Save((Join-Path $taskRoot "src/Assets/Games/$name.png"),[Drawing.Imaging.ImageFormat]::Png)
    $tile.Dispose()
}
$spider=[Drawing.Bitmap]::new((Join-Path $taskRoot 'references/Games/spider-sheet.png'))
$faces=[Drawing.Bitmap]::new(923,384)
$g=[Drawing.Graphics]::FromImage($faces)
$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
for($s=0;$s -lt 4;$s++){for($r=0;$r -lt 13;$r++){
    $g.DrawImage($spider,[Drawing.Rectangle]::new($r*71,$s*96,71,96),29+$r*129,50+$s*162,71,96,[Drawing.GraphicsUnit]::Pixel)
}}
$faces.Save((Join-Path $taskRoot 'src/Assets/Games/spider-faces.png'),[Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$faces.Dispose()
Crop-GameAsset $spider 'spider-back' 416 698 71 96
Crop-GameAsset $spider 'spider-felt' 678 714 64 64
Crop-GameAsset $spider 'spider-about' 917 706 359 243
$spider.Dispose()
$freecell=[Drawing.Bitmap]::new((Join-Path $taskRoot 'references/Games/freecell-sheet.png'))
Crop-GameAsset $freecell 'king-left' 294 428 35 35
Crop-GameAsset $freecell 'king-right' 331 428 35 35
$freecell.Dispose()
$backgrounds=@('FELTX2.JPG','BROWNFELTX2.JPG','HEARTSX2.JPG','NATUREX2.JPG','REDFELTX2.JPG')
for($i=1;$i -lt 5;$i++) {Copy-Item -LiteralPath (Join-Path $taskRoot "references/WindowsUI/Vista/$($backgrounds[$i])") -Destination (Join-Path $taskRoot "src/Assets/Vista/background-$i.jpg")}
