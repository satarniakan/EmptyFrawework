# کتابچهٔ API موبایل (`/api/v1`)

پایه: `http://localhost:5000` (توسعه). مستندات خودکار فقط در Development: `/openapi/v1.json`.
همهٔ ورودی/خروجی‌ها JSON با `Content-Type: application/json` هستند.

## ورود

### ۱. درخواست کد پیامکی

```bash
curl -X POST http://localhost:5000/api/v1/auth/otp/request \
  -H 'Content-Type: application/json' \
  -d '{"phoneNumber":"09120000000"}'
# 200 {"message":"کد ورود ارسال شد."}
# 400 {"message":"..."} — قفل موقت یا سقف درخواست
# 429 — محدودیت نرخ (۳ درخواست در ۵ دقیقه برای هر IP)
```

### ۲-الف. تأیید کد و گرفتن توکن

```bash
curl -X POST http://localhost:5000/api/v1/auth/otp/verify \
  -H 'Content-Type: application/json' \
  -d '{"phoneNumber":"09120000000","code":"123456","deviceName":"گوشی من"}'
# 200 {"token":"pat_...","expiresAtUtc":"2026-...Z"}
# 401 — کد اشتباه/منقضی (پس از ۵ تلاش، شماره ۱۰ دقیقه قفل می‌شود)
```

### ۲-ب. ورود با رمز (وقتی پیامک قطع است)

```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"09120000000","password":"...","deviceName":"گوشی من"}'
# 200 {"token":"pat_...","expiresAtUtc":"..."}
# 401 — نام کاربری/رمز اشتباه یا حساب قفل‌شده
```

### فراموشی رمز (با کد پیامکی)

```bash
# اول کد بگیرید (مرحله ۱)، بعد:
curl -X POST http://localhost:5000/api/v1/auth/password/reset \
  -H 'Content-Type: application/json' \
  -d '{"phoneNumber":"09120000000","code":"123456",
       "newPassword":"...","confirmPassword":"..."}'
# 200 {"message":"رمز عبور تعیین شد."}
# 401 — کد نامعتبر | 404 — کاربر نیست | 400 — عدم تطابق یا سیاست رمز
```

## استفاده از توکن

متن خام توکن **فقط یک‌بار** دیده می‌شود؛ ذخیره‌اش کنید (Keychain/Keystore):

```bash
TOKEN='pat_...'
curl http://localhost:5000/api/v1/profile \
  -H "Authorization: Bearer $TOKEN"
# 200 {"userName":"...","phoneNumber":"...","fullName":"...","email":"...","hasPassword":true}
```

## مدیریت نشست‌ها

```bash
# فهرست توکن‌های فعال من
curl http://localhost:5000/api/v1/auth/tokens -H "Authorization: Bearer $TOKEN"

# لغو یکی
curl -X DELETE http://localhost:5000/api/v1/auth/tokens/7 -H "Authorization: Bearer $TOKEN"
# 204 — موفق | 404 — مال من نیست

# خروج از همهٔ دستگاه‌ها
curl -X DELETE http://localhost:5000/api/v1/auth/tokens -H "Authorization: Bearer $TOKEN"
# 200 {"revoked":2}
```

## پرداخت (موبایل)

```bash
# شروع پرداخت (مبلغ به تومان) → آدرس درگاه در مرورگر باز شود
curl -X POST http://localhost:5000/api/v1/payments \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"amountTomans":50000,"description":"..."}'
# 200 {"paymentId":12,"paymentUrl":"https://www.zarinpal.com/pg/StartPay/..."}
# برگشت درگاه به /payments/callback می‌آید و به /payment-result هدایت می‌شود
```

## اعلان‌ها

```bash
# ۲۰ اعلان آخر + تعداد خوانده‌نشده
curl http://localhost:5000/api/v1/notifications/recent -H "Authorization: Bearer $TOKEN"

# خوانده‌شدن یکی / همه
curl -X POST http://localhost:5000/api/v1/notifications/5/read -H "Authorization: Bearer $TOKEN"
curl -X POST http://localhost:5000/api/v1/notifications/read-all -H "Authorization: Bearer $TOKEN"
```

## وب‌پوش

وب‌پوش مخصوص مرورگر است، نه اپ نیتیو.
کلید عمومی (بدون احراز هویت): `GET /api/v1/push/public-key`

## خطاها و محدودیت‌ها

| وضعیت | معنی |
|---|---|
| `400` با `{"message":"..."}` | خطای قابل‌انتظار؛ پیام فارسی را مستقیم نشان بدهید |
| `401` | توکن نیست/نامعتبر/منقضی/لغوشده → دوباره وارد شوید |
| `404` | منبع مال شما نیست یا نیست |
| `429` | محدودیت نرخ — `Retry-After` را رعایت کنید |

نکتهٔ امنیتی: توکن را هرگز در کد، لاگ یا گیت نگه ندارید؛ انقضای پیش‌فرض ۱۸۰ روز
(`ApiTokens:LifetimeDays`) و لغو تکی همیشه ممکن است.
