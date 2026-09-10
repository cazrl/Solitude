param([string]$RenderDirectory='artifacts/release-v020-renders',[string]$Output='artifacts/era-comparison.png')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskEras=@('Windows30','Windows31','Windows95','Windows98','WindowsMe','Windows2000','WindowsXP','WindowsVista')
$taskNames=@('Windows 3.0','Windows 3.1 / 3.11','Windows 95','Windows 98','Windows Me','Windows 2000','Windows XP','Windows Vista')
$taskSheet=[Drawing.Bitmap]::new(1100,1032)
$taskGraphics=[Drawing.Graphics]::FromImage($taskSheet)
$taskGraphics.Clear([Drawing.Color]::FromArgb(24,27,32))
$taskGraphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$taskGraphics.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
$taskFont=[Drawing.Font]::new('Segoe UI',15,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
for($taskIndex=0;$taskIndex -lt 8;$taskIndex++){
    $taskX=16+($taskIndex%2)*548;$taskY=12+[Math]::Floor($taskIndex/2)*256
    $taskGraphics.DrawString($taskNames[$taskIndex],$taskFont,[Drawing.Brushes]::White,$taskX,$taskY)
    $taskMain=[Drawing.Bitmap]::FromFile((Join-Path $PWD (Join-Path $RenderDirectory ($taskEras[$taskIndex]+'-100.png'))))
    # Show the frame and card table at native logical resolution, not an unreadable whole-window thumbnail.
    $taskGraphics.DrawImage($taskMain,[Drawing.Rectangle]::new($taskX,$taskY+28,520,88),[Drawing.Rectangle]::new(0,0,$taskMain.Width,108),[Drawing.GraphicsUnit]::Pixel)
    $taskMain.Dispose()
    $taskOptions=[Drawing.Bitmap]::FromFile((Join-Path $PWD (Join-Path $RenderDirectory ($taskEras[$taskIndex]+'-options.png'))))
    if($taskIndex -lt 2){$taskW=310;$taskH=252}else{if($taskIndex -eq 7){$taskW=430;$taskH=338}else{$taskW=350;$taskH=234}}
    $taskSource=[Drawing.Rectangle]::new(($taskOptions.Width-$taskW*1.5)/2,($taskOptions.Height-$taskH*1.5)/2,$taskW*1.5,162)
    $taskGraphics.DrawImage($taskOptions,[Drawing.Rectangle]::new($taskX,$taskY+124,($taskW*1.15),124),$taskSource,[Drawing.GraphicsUnit]::Pixel)
    $taskOptions.Dispose()
}
$taskSheet.Save((Join-Path $PWD $Output),[Drawing.Imaging.ImageFormat]::Png)
$taskFont.Dispose();$taskGraphics.Dispose();$taskSheet.Dispose()
