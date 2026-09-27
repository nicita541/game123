param([string]$Ffmpeg = 'E:\ffmpeg\bin\ffmpeg.exe')
$sourceRoot = $PSScriptRoot
$audioRoot = [IO.Path]::GetFullPath((Join-Path $sourceRoot '..\..\Assets\Audio'))
& $Ffmpeg -hide_banner -loglevel error -y -i (Join-Path $sourceRoot 'Satie-Gymnopedie-1-Robin-Alciatore.ogg') -af 'volume=6dB,afade=t=in:d=0.7,afade=t=out:st=180.5:d=3.08' -c:a pcm_s16le (Join-Path $audioRoot 'LibraryMusic.wav')
if ($LASTEXITCODE -ne 0) { throw 'Music conversion failed' }
# Balance the supplied recordings; no oscillators, generated notes or AI audio.
$effects = @(@('bookFlip1','Click',4),@('metalClick','Correct',10),@('bookClose','Mistake',-4),@('handleCoins2','Victory',10))
foreach ($pair in $effects) {
    $filters = 'volume=' + $pair[2] + 'dB,afade=t=in:d=0.005,alimiter=limit=0.85:level=false'
    & $Ffmpeg -hide_banner -loglevel error -y -i (Join-Path $sourceRoot ('KenneyRPG\Audio\' + $pair[0] + '.ogg')) -ar 44100 -ac 1 -af $filters -c:a pcm_s16le (Join-Path $audioRoot ($pair[1] + '.wav'))
    if ($LASTEXITCODE -ne 0) { throw ('Effect conversion failed: ' + $pair[1]) }
}
