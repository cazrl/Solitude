$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$originals=Join-Path $PSScriptRoot '..\references\Vista'
New-Item -ItemType Directory -Force -Path $originals | Out-Null
for($deck=0;$deck -lt 4;$deck++) {
    $asset=Join-Path $PSScriptRoot "..\src\Assets\Vista\deck-$deck.png"
    $original=Join-Path $originals "deck-$deck.png"
    if(-not(Test-Path -LiteralPath $original)) { Copy-Item -LiteralPath $asset -Destination $original }
    $source=[Drawing.Image]::FromFile($original)
    $sheet=New-Object Drawing.Bitmap 1443,608
    $g=[Drawing.Graphics]::FromImage($sheet)
    $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    for($suit=0;$suit -lt 4;$suit++) { for($rank=1;$rank -le 13;$rank++) {
        $index=if($deck -eq 3){ @([int]0,1,3,2)[$suit]*13 + $(if($rank -eq 1){12}else{$rank-2}) }elseif($rank -eq 1){$suit*10+9}elseif($rank -le 10){$suit*10+$rank-2}else{40+$suit*3+$rank-11}
        $cw=$source.Width/10.0;$ch=$source.Height/7.0
        $dest=New-Object Drawing.Rectangle (($rank-1)*111),($suit*152),111,152
        $g.DrawImage($source,$dest,[single](($index%10)*$cw+0.5),[single]([Math]::Floor($index/10)*$ch+0.5),[single]($cw-1),[single]($ch-1),[Drawing.GraphicsUnit]::Pixel)
    } }
    $sheet.Save($asset,[Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose();$sheet.Dispose();$source.Dispose()
}
