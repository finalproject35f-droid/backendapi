# EpiCare Backend AI Flow Report

## الهدف

فصل الحقيقة التجريبية الموجودة داخل Proteus عن قرار النظام، بحيث يكون الموديل والحساسات هما مصدر القرار الافتراضي، مع الاحتفاظ بوضع Demo اختياري.

## المسار بعد التعديل

1. يرسل Proteus قراءات EEG وECG وEMG وACC الخام بدون `state` افتراضيًا.
2. يبني الباك إند نافذة الإدخال ويرسلها إلى موديل الـAI.
3. قرار `Normal` ينتج أمر `N`.
4. توقع `final_prediction=1` أو `Warning/Preictal` أو احتمال يتجاوز الحد ينتج `P`.
5. خطر من الموديل مع حركة ACC غير طبيعية ينتج `S`.
6. نتيجة صريحة `Seizure/Ictal` من الموديل تنتج `S` مباشرة.
7. يعيد IoT Bridge أمر `N/P/S` إلى Arduino عند تشغيل `--write-command true`.

## حماية المسار الحقيقي

- `Decision:DemoMode` مغلق افتراضيًا.
- قيمة `state` القادمة من Proteus لا تؤثر في القرار عند غلق Demo Mode.
- وجود مصفوفة ACC وحده لم يعد يعني Seizure؛ يتم حساب حركة فعلية باستخدام X وY وانحراف Z عن الجاذبية.
- عند تعطل الموديل لا يتم نسخ `state` القادمة من المحاكي كأنها نتيجة AI.

## إعدادات التشغيل

```text
Decision__DemoMode=false
Decision__AccGravityBaseline=980
Decision__AccVerticalScale=50
Decision__AccMovementThreshold=2.5
```

للديمو الإجباري فقط:

```text
Decision__DemoMode=true
Arduino: SEND_DEMO_STATE=true
```

## ملاحظات

- قيم ACC الحالية متوافقة مع محاكي Proteus الذي يستخدم Z قريبًا من 980. عند توصيل حساس حقيقي يجب معايرة baseline والـthreshold حسب وحدة الحساس.
- التخزين ما زال In-Memory، وبالتالي سجل التنبيهات والحالات يُمسح عند إعادة تشغيل السيرفر.

## التحقق

- ملف `appsettings.json` تم التحقق من صحته كـJSON.
- فحص `git diff --check` انتهى بدون أخطاء مسافات أو patch.
- تم التأكد من وجود دالة إرسال Arduino واحدة، وأن `SEND_DEMO_STATE=false` افتراضيًا.
- تعذر تنفيذ `dotnet build` لأن .NET SDK غير مثبت أو غير موجود في PATH على الجهاز الحالي. يلزم تشغيل البناء بعد تثبيت .NET 8 SDK.
