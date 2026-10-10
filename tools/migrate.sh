#!/usr/bin/env bash
# ساخت مایگریشن + بازتولید اسکریپت دیتابیس در یک دستور تا docs/database.sql
# با مدل drift نکند.
# استفاده: ./tools/migrate.sh AddSomething
set -euo pipefail

if [ $# -ne 1 ]; then
  echo "Usage: $0 <MigrationName>" >&2
  exit 1
fi

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

dotnet ef migrations add "$1" --project Platform.App --startup-project Platform.App

TMP="$(mktemp)"
dotnet ef migrations script --project Platform.App --startup-project Platform.App -o "$TMP" --no-build

{
  cat <<'HEADER'
-- ============================================================
-- اسکیمای دیتابیس فریم‌ورک (تولیدشده از مایگریشن‌ها — دستی ویرایش نکنید)
-- بازسازی با: ./tools/migrate.sh <Name>
-- ------------------------------------------------------------
-- ریستور روی دیتابیس تازه (SQL Server):
--   sqlcmd -S <server> -U sa -P '<pass>' -Q "CREATE DATABASE [EmptyFramework]"
--   sqlcmd -S <server> -U sa -P '<pass>' -d EmptyFramework -i docs/database.sql
-- روی داکر (سرویس sqlserver همین ریپو):
--   docker compose up -d sqlserver
--   docker compose cp docs/database.sql sqlserver:/tmp/database.sql
--   docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Dev_Pass123!' -C -Q "CREATE DATABASE [EmptyFramework]"
--   docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Dev_Pass123!' -C -d EmptyFramework -i /tmp/database.sql
-- نکته: نقش‌ها (Admin/User) را خودِ اپ هنگام استارتاپ می‌سازد (RoleSeeder) و
-- ادمین اول با Identity:FirstAdminPhoneNumber در اولین ورود OTP ساخته می‌شود؛
-- پس سید اضافه لازم نیست. خودِ اپ هم با MigrateAsync همین اسکیما را می‌سازد؛
-- این فایل برای وقتی است که بخواهید دیتابیس را بیرون از اپ بسازید/ریستور کنید.
-- ============================================================

HEADER
  # حذف BOM ابتدای خروجی ef تا الحاق تمیز بماند
  tail -c +4 "$TMP"
} > docs/database.sql
rm -f "$TMP"

echo "OK: migration '$1' + docs/database.sql regenerated"
