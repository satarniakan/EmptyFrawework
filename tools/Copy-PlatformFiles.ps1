# اسکریپت کپی فایل‌های «پایه» از ریپوی Dashboard به پروژهٔ جدید EmptyFrawework.
# فقط فایل‌هایی که بدون تغییر منتقل می‌شوند؛ namespaceها بعداً با Rename-Namespace بازنویسی می‌شوند.

$ErrorActionPreference = 'Stop'
$Src = 'C:\Users\niakan-s\source\repos\Dashboard'
$Dst = 'C:\Users\niakan-s\source\repos\EmptyFrawework'

function Copy-File {
    param([string]$RelativePath)
    $s = Join-Path $Src $RelativePath
    $d = Join-Path $Dst $RelativePath
    if (-not (Test-Path $s)) { Write-Warning "یافت نشد: $RelativePath"; return }
    New-Item -ItemType Directory -Force -Path (Split-Path $d) | Out-Null
    Copy-Item $s $d -Force
}

# --- Platform.Domain ---
foreach ($f in @('AuditLog', 'OtpCode', 'OutboxMessage', 'Notification')) {
    Copy-File "Dashboard.Domain\Entities\$f.cs"
}
Copy-File 'Dashboard.Domain\Entities\AuditLog.cs'
foreach ($f in @('OutboxEnums', 'NotificationType')) {
    Copy-File "Dashboard.Domain\Enums\$f.cs"
}
foreach ($f in @('BusinessRuleException', 'NotFoundException')) {
    Copy-File "Dashboard.Domain\Exceptions\$f.cs"
}
foreach ($f in @('IAuditLogRepository', 'IOtpRepository', 'IOutboxRepository',
    'INotificationRepository', 'ISmsSender', 'IEmailSender')) {
    Copy-File "Dashboard.Domain\Interfaces\$f.cs"
}

# --- Platform.Application ---
foreach ($f in @('CurrencyFormatter', 'PersianDateHelper', 'PersianNumberToWords', 'QuantityFormatter')) {
    Copy-File "Dashboard.Application\Helpers\$f.cs"
}
foreach ($f in @('ExcelExporter', 'PdfExporter')) {
    Copy-File "Dashboard.Application\Exports\$f.cs"
}
Copy-File 'Dashboard.Application\Assets\Fonts\vazirmatn-regular.ttf'
Copy-File 'Dashboard.Application\Assets\Fonts\vazirmatn-500.ttf'
Copy-File 'Dashboard.Application\Assets\Fonts\vazirmatn-700.ttf'
Copy-File 'Dashboard.Application\Validators\CommonValidations.cs'
foreach ($f in @('AuthDtos', 'PagedResult', 'RoleDto', 'UserAdminDtos')) {
    Copy-File "Dashboard.Application\DTOs\$f.cs"
}

# --- Platform.Infrastructure ---
foreach ($f in @('AuditLogRepository', 'OtpCodeRepository', 'OutboxRepository', 'NotificationRepository')) {
    Copy-File "Dashboard.Infrastructure\Repositories\$f.cs"
}
foreach ($f in @('AppUserClaimsPrincipalFactory', 'FakeSmsSender', 'KavenegarSmsSender', 'SmtpEmailSender')) {
    Copy-File "Dashboard.Infrastructure\Services\$f.cs"
}

# --- Platform.Web ---
foreach ($f in @('App.razor', 'Routes.razor')) {
    Copy-File "Dashboard.Web\Components\$f"
}
Copy-File 'Dashboard.Web\Components\Layout\ReconnectModal.razor'
Copy-File 'Dashboard.Web\Components\Layout\ReconnectModal.razor.css'
Copy-File 'Dashboard.Web\Components\Layout\ReconnectModal.razor.js'
foreach ($f in @('AppCheckbox', 'AppConfirmDialog', 'AppDataGrid', 'AppDateField', 'AppDropdown',
    'AppNumberField', 'AppPriceField', 'AppRadioGroup', 'AppTextArea', 'AppTextField',
    'LoadingSkeleton', 'Pager', 'PersianDateTimePicker', 'PrintButton', 'ToastContainer',
    'ReportTable', 'ReportPrintHeader', 'NotificationBell')) {
    Copy-File "Dashboard.Web\Components\Shared\$f.razor"
}
foreach ($f in @('AppComponentBase.cs', 'ErrorMessageHelper.cs')) {
    Copy-File "Dashboard.Web\Components\Shared\$f"
}
foreach ($f in @('Login', 'LoginWithPassword', 'VerifyOtp', 'Register', 'Profile',
    'NotFound', 'Error', 'AuditLogs', 'Notifications')) {
    Copy-File "Dashboard.Web\Components\Pages\$f.razor"
}
foreach ($f in @('Users', 'AddUser', 'EditUserRole', 'RolePermissions', 'Broadcast')) {
    Copy-File "Dashboard.Web\Components\Pages\Admin\$f.razor"
}
Copy-File 'Dashboard.Web\Endpoints\AccountEndpoints.cs'
Copy-File 'Dashboard.Web\Endpoints\NotificationsEndpoints.cs'
Copy-File 'Dashboard.Web\Services\ToastService.cs'
Copy-File 'Dashboard.Web\Services\OtpCleanupService.cs'
Copy-File 'Dashboard.Web\Services\OutboxProcessor.cs'

# --- wwwroot (بدون پوشهٔ lib که کتابخانهٔ vendored است) ---
foreach ($f in @('app.css', 'theme.js', 'persian-datepicker-interop.js', 'priceInput.js', 'robots.txt')) {
    Copy-File "Dashboard.Web\wwwroot\$f"
}
Copy-Item (Join-Path $Src 'Dashboard.Web\wwwroot\lib') (Join-Path $Dst 'src\Platform.Web\wwwroot\lib') -Recurse -Force
Copy-Item (Join-Path $Src 'Dashboard.Web\wwwroot\fonts') (Join-Path $Dst 'src\Platform.Web\wwwroot\fonts') -Recurse -Force

Write-Host "کپی تمام شد." -ForegroundColor Green