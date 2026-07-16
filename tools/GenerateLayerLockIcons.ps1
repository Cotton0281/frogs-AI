Add-Type -AssemblyName System.Drawing

$outputDirectory = Join-Path $PSScriptRoot '..\AI-Evlo-Test\img'

function New-LayerLockIcon {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [bool] $Locked
    )

    $bitmap = [System.Drawing.Bitmap]::new(64, 64)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $bodyColor = if ($Locked) {
        [System.Drawing.Color]::FromArgb(255, 194, 116, 44)
    } else {
        [System.Drawing.Color]::FromArgb(255, 78, 135, 184)
    }
    $shackleColor = if ($Locked) {
        [System.Drawing.Color]::FromArgb(255, 123, 70, 22)
    } else {
        [System.Drawing.Color]::FromArgb(255, 47, 95, 134)
    }

    $bodyBrush = [System.Drawing.SolidBrush]::new($bodyColor)
    $shacklePen = [System.Drawing.Pen]::new($shackleColor, 6)
    $shacklePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $shacklePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

    if ($Locked) {
        $graphics.DrawArc($shacklePen, 17, 6, 30, 34, 180, 180)
        $graphics.DrawLine($shacklePen, 17, 23, 17, 31)
        $graphics.DrawLine($shacklePen, 47, 23, 47, 31)
    } else {
        $graphics.DrawArc($shacklePen, 15, 6, 34, 34, 180, 150)
        $graphics.DrawLine($shacklePen, 15, 23, 15, 31)
    }

    $graphics.FillRectangle($bodyBrush, 10, 28, 44, 28)
    $graphics.FillEllipse($bodyBrush, 10, 24, 10, 10)
    $graphics.FillEllipse($bodyBrush, 44, 24, 10, 10)
    $graphics.FillEllipse($bodyBrush, 10, 50, 10, 10)
    $graphics.FillEllipse($bodyBrush, 44, 50, 10, 10)

    $keyBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $graphics.FillEllipse($keyBrush, 27, 36, 10, 10)
    $graphics.FillRectangle($keyBrush, 30, 43, 4, 7)

    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)

    $keyBrush.Dispose()
    $shacklePen.Dispose()
    $bodyBrush.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

New-LayerLockIcon -Path (Join-Path $outputDirectory 'nn-layer-locked.png') -Locked $true
New-LayerLockIcon -Path (Join-Path $outputDirectory 'nn-layer-unlocked.png') -Locked $false
