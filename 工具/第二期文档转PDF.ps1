param([string]$InputPath,[string]$OutputPath)
$ErrorActionPreference='Stop'
$word=$null
$doc=$null
try {
    $word=New-Object -ComObject Word.Application
    $word.Visible=$false
    $word.DisplayAlerts=0
    $doc=$word.Documents.Open([IO.Path]::GetFullPath($InputPath),$false,$true)
    $doc.Repaginate()
    $pages=$doc.ComputeStatistics(2)
    $doc.ExportAsFixedFormat([IO.Path]::GetFullPath($OutputPath),17)
    [pscustomobject]@{Renderer=$word.Name;Version=$word.Version;Pages=$pages;PDF=$OutputPath}|ConvertTo-Json
} finally {
    if($doc){try {$doc.Close(0)} catch {if($_.Exception.HResult -ne -2147023174){throw}};[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($doc)}
    if($word){try {$word.Quit()} catch {if($_.Exception.HResult -ne -2147023174){throw}};[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($word)}
}
