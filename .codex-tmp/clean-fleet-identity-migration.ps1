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

$migration = 'src/LogisticsERP.Infrastructure/Identity/Migrations/20260822165334_AddFleetPermissions.cs'
$modelFiles = @(
    'src/LogisticsERP.Infrastructure/Identity/Migrations/20260822165334_AddFleetPermissions.Designer.cs',
    '.codex-tmp/FleetCompile/LogisticsERP/Infrastructure/Identity/Migrations/IdentityDbContextModelSnapshot.cs'
)

foreach ($number in 22..29) {
    $suffix = $number.ToString('000')
    Replace-Checked $migration "\s*\{ new Guid\(`"019c18d5-62e1-7000-b000-000000000$suffix`"\),.*?\}(,)?" '' 1
    Replace-Checked $migration "\s*migrationBuilder\.DeleteData\(\s*schema: `"identity`",\s*table: `"RolePermissions`",\s*keyColumn: `"Id`",\s*keyValue: new Guid\(`"019c18d5-62e1-7000-b000-000000000$suffix`"\)\);" '' 1

    foreach ($modelFile in $modelFiles) {
        Replace-Checked $modelFile "\s*,?\s*new\s*\{\s*Id = new Guid\(`"019c18d5-62e1-7000-b000-000000000$suffix`"\),.*?\s*\}" '' 1
    }
}
