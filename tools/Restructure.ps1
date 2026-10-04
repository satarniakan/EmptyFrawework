# انتقال فایل‌های کپی‌شده از پوشه‌های Dashboard.* (در ریشه) به پوشه‌های درست داخل src\
# و بازنویسی namespaceها. این اسکریپت idempotent است و چند بار قابل اجراست.

$ErrorActionPreference = 'Stop'
$Root = 'C:\Users\niakan-s\source\repos\EmptyFrawework'

# ۱) نگاشت: پوشهٔ مبدأ در ریشه ← پروژهٔ مقصد
$targets = @(
    @{ Project = 'src\Platform.Domain';         Source = 'Dashboard.Domain' },
    @{ Project = 'src\Platform.Application';    Source = 'Dashboard.Application' },
    @{ Project = 'src\Platform.Infrastructure'; Source = 'Dashboard.Infrastructure' },
    @{ Project = 'src\Platform.Web';            Source = 'Dashboard.Web' }
)

foreach ($t in $targets) {
    $from = Join-Path $Root $t.Source
    if (-not (Test-Path $from)) { continue }

    $projectDir = Join-Path $Root $t.Project
    New-Item -ItemType Directory -Force -Path $projectDir | Out-Null

    Get-ChildItem $from -Force | ForEach-Object {
        $dest = Join-Path $projectDir $_.Name
        if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
        Move-Item $_.FullName $dest -Force
    }
    Remove-Item $from -Recurse -Force
}

# ۲) بازنویسی namespace در همهٔ فایل‌های متنی
$replacements = [ordered]@{
    'Dashboard.Domain.Identity'              = 'Platform.Domain.Identity'
    'Dashboard.Domain.Entities'              = 'Platform.Domain.Entities'
    'Dashboard.Domain.Enums'                 = 'Platform.Domain.Enums'
    'Dashboard.Domain.Exceptions'            = 'Platform.Domain.Exceptions'
    'Dashboard.Domain.Interfaces'            = 'Platform.Domain.Interfaces'
    'Dashboard.Domain.Queries'               = 'Platform.Domain.Queries'
    'Dashboard.Domain'                       = 'Platform.Domain'
    'Dashboard.Application.Services'         = 'Platform.Application.Services'
    'Dashboard.Application.DTOs'             = 'Platform.Application.DTOs'
    'Dashboard.Application.Helpers'          = 'Platform.Application.Helpers'
    'Dashboard.Application.Exports'          = 'Platform.Application.Exports'
    'Dashboard.Application.Validators'       = 'Platform.Application.Validators'
    'Dashboard.Application'                  = 'Platform.Application'
    'Dashboard.Infrastructure.Services'       = 'Platform.Infrastructure.Services'
    'Dashboard.Infrastructure.Repositories' = 'Platform.Infrastructure.Repositories'
    'Dashboard.Infrastructure.Data'          = 'Platform.Infrastructure.Data'
    'Dashboard.Infrastructure'               = 'Platform.Infrastructure'
    'Dashboard.Web.Components.Shared'        = 'Platform.Web.Components.Shared'
    'Dashboard.Web.Components.Layout'         = 'Platform.Web.Components.Layout'
    'Dashboard.Web.Components'                = 'Platform.Web.Components'
    'Dashboard.Web.Endpoints'                 = 'Platform.Web.Endpoints'
    'Dashboard.Web.Services'                  = 'Platform.Web.Services'
    'namespace Dashboard.Web'                 = 'namespace Platform.Web'
    'Dashboard.Web.styles.css'                = 'Platform.Web.styles.css'
}

$textFiles = Get-ChildItem $Root -Recurse -File -Include *.cs, *.razor, *.css, *.js |
    Where-Object { $_.FullName -notmatch '\\wwwroot\\(lib|fonts)\\' }

$changed = 0
foreach ($file in $textFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $updated = $content
    foreach ($pair in $replacements.GetEnumerator()) {
        $updated = $updated.Replace($pair.Key, $pair.Value)
    }
    if ($updated -ne $content) {
        Set-Content $file.FullName -Value $updated -Encoding UTF8 -NoNewline
        $changed++
    }
}

Write-Host "بازچینش تمام شد. $changed فایل بازنویسی namespace شد." -ForegroundColor Green