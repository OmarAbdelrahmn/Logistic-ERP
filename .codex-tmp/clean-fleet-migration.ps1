$ErrorActionPreference = 'Stop'

function Replace-Checked {
    param(
        [string] $Path,
        [string] $Pattern,
        [string] $Replacement,
        [int] $ExpectedCount
    )

    $content = [System.IO.File]::ReadAllText($Path)
    $matches = [regex]::Matches($content, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($matches.Count -ne $ExpectedCount) {
        throw "Expected $ExpectedCount match(es) in $Path but found $($matches.Count)."
    }

    $updated = [regex]::Replace(
        $content,
        $Pattern,
        $Replacement,
        [System.Text.RegularExpressions.RegexOptions]::Singleline)
    [System.IO.File]::WriteAllText($Path, $updated, [System.Text.UTF8Encoding]::new($false))
}

$migration = 'src/LogisticsERP.Infrastructure/Persistence/Migrations/Application/20260822164817_AddFleetOperations.cs'
$modelFiles = @(
    '.codex-tmp/FleetCompile/LogisticsERP/Infrastructure/Persistence/Migrations/Application/ApplicationDbContextModelSnapshot.cs'
)

foreach ($modelFile in $modelFiles) {
    Replace-Checked $modelFile '\s*b\.Property<string>\("RotationReason"\).*?\.HasColumnType\("nvarchar\(1000\)"\);' '' 1
    Replace-Checked $modelFile '\s*b\.Property<int>\("PreviousLeaveStatus"\).*?\.HasColumnType\("int"\);' '' 1
    foreach ($number in 75..81) {
        $suffix = $number.ToString('000')
        Replace-Checked $modelFile "\s*,?\s*new\s*\{\s*Id = new Guid\(`"019c18d5-62e1-7000-a000-000000000$suffix`"\),.*?\s*\}" '' 1
    }
}
