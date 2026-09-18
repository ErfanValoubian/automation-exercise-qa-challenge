# راهنمای اجرای پروژه

این پروژه از صفر برای صورت تسک نوشته شده است. کد برنامه یا backend داخل آن وجود ندارد؛ فقط تست، ابزار گزارش‌دهی و مستندات است.

**در اجرای نهایی، هر ۲۸ تست اصلی پاس شدند:** ۱۱ تست API، چهار جریان واقعی UI/Hybrid و ۱۳ تست داخلی. تست مشاهدهٔ قرارداد هم جداگانه پاس شد. فایل `docs/VALIDATION.md` جزئیات اجرای واقعی با Edge، cleanup و محدودیت‌ها را مشخص کرده است. پایپ‌لاین ابری هنوز روی حساب شما اجرا نشده است.

## شروع در ویندوز

فایل ZIP را Extract کنید و PowerShell را در پوشه‌ای باز کنید که `Challenge.sln` داخل آن است. به .NET SDK نسخهٔ 8.0.4xx نیاز دارید.

```powershell
dotnet restore Challenge.sln --configfile NuGet.Config
dotnet build Challenge.sln -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/install-browsers.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run.ps1 -Suite all -NoBuild
```

اجرای جداگانه:

```powershell
powershell -File scripts/run.ps1 -Suite api
powershell -File scripts/run.ps1 -Suite ui
powershell -File scripts/run.ps1 -Suite hybrid
powershell -File scripts/run.ps1 -Suite observations
```

اگر Edge نصب است، می‌توانید بدون دانلود Chromium جداگانه از همان مرورگرِ اجرای تأییدشده استفاده کنید:

```powershell
$env:AE_BROWSER_CHANNEL = 'msedge'
powershell -File scripts/run.ps1 -Suite all
```

برای دیدن مرورگر:

```powershell
$env:AE_HEADLESS = 'false'
powershell -File scripts/run.ps1 -Suite hybrid
```

در خروجی، مسیر گزارش نوشته می‌شود. داخل `artifacts`، آخرین پوشهٔ اجرا را باز کنید:

- `evidence/index.html`: گزارش اصلی با نتیجه، علت شکست، cleanup و لینک جزئیات هر تست.
- `runner/results.trx`: گزارش ماشینی برای CI.
- `runner/runner.html`: گزارش استاندارد اجرای تست.

گزارش اصلی برای تست‌های موفق هم ساخته می‌شود. برای ارسال گزارش، کل پوشهٔ اجرا را ارسال کنید تا لینک فایل‌ها حفظ شود.

## جست‌وجوی خالی و شکست عمدی

جست‌وجوی خالی در `observations` بررسی می‌شود. چون قرارداد سرویس رفتار آن را دقیق مشخص نکرده، برگشتن کل کاتالوگ به‌تنهایی «باگ قطعی» اعلام نمی‌شود؛ پاسخ و شناسهٔ محصولات در گزارش ثبت می‌شوند. پاسخ خراب، HTML یا ساختار نامعتبر همچنان شکست محسوب می‌شود.

```powershell
powershell -File scripts/run.ps1 -Suite demo
powershell -File scripts/run.ps1 -Suite report-demo
```

دستور اول شکست عمدی با مرورگر و دستور دوم شکست عمدی بدون مرورگر ایجاد می‌کند. خروجی ۱ برای این دو مورد طبیعی است، به شرط آنکه علت شکست همان assertion عمدی باشد، نه مشکل نصب یا محیط.

## تحویل به ارزیاب

مستندات انگلیسی در `docs` و Azure pipeline در ریشه آماده‌اند. قبل از ارسال، روی سیستم خودتان `all` و `demo` را اجرا کنید، شواهد واقعی تازه را ذخیره کنید، کد را در مخزن خودتان قرار دهید و نام، commit مورد بررسی و لینک اجرای CI را ارائه کنید. اطلاعات هویتی و اجرای موفق CI در این بسته جعل نشده‌اند.
