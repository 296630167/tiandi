param([switch]$DryRun, [string]$Model = 'gpt-image-2.5-sunburst', [switch]$OnlyMissing)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$sourcePath = Join-Path $projectRoot '美术需求/序章3D资源清单.md'
$sourceText = [IO.File]::ReadAllText($sourcePath)
$sections = [regex]::Matches($sourceText, '(?ms)^### (?<id>P\d{2}|W\d{2}) .*?(?=^### |\z)')
$jobs = foreach ($section in $sections) {
    $id = $section.Groups['id'].Value
    $promptMatch = [regex]::Match($section.Value, '(?m)^> (?<prompt>.+)$')
    if (-not $promptMatch.Success) { throw "Missing prompt: $id" }
    $size = if ($id -in @('W01', 'W02', 'P10', 'P11')) { '1024x1536' } else { '1024x1024' }
    $imageMatch = [regex]::Match($section.Value, '!\[[^\]]*\]\(\.\./output/imagegen/prologue/(?<file>[^)]+)\)')
    $filename = if ($imageMatch.Success) { $imageMatch.Groups['file'].Value } else { $id + '_reference.png' }
    @{prompt=$promptMatch.Groups['prompt'].Value; use_case='stylized-concept'; quality='high'; size=$size; model=$Model; out=$filename}
}
if (@($jobs).Count -ne 20) { throw 'Expected exactly 20 asset jobs.' }
if ($OnlyMissing) {
    $jobs = @($jobs | Where-Object { -not (Test-Path -LiteralPath (Join-Path $projectRoot ('output/imagegen/prologue/' + $_.out))) })
}
if (-not $DryRun) {
    $batchDirectory = Join-Path $projectRoot 'tmp/imagegen'
    [IO.Directory]::CreateDirectory($batchDirectory) | Out-Null
    $lines = @($jobs | ForEach-Object { ConvertTo-Json $_ -Depth 4 -Compress })
    [IO.File]::WriteAllLines((Join-Path $batchDirectory 'prologue-assets.jsonl'), $lines, [Text.UTF8Encoding]::new($false))
}
Write-Output ('Prepared ' + @($jobs).Count + ' prologue asset prompts from the local art checklist.')
