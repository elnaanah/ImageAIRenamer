# ImageAIRenamer Tests

مشروع اختبار البرمجيات لتطبيق ImageAIRenamer.

## هيكل المشروع

```
ImageAIRenamer.Tests/
├── Unit/                           # اختبارات الوحدات
│   ├── Services/                   # اختبارات الخدمات
│   │   ├── FileServiceTests.cs     # اختبارات FileService
│   │   └── ConfigurationServiceTests.cs  # اختبارات ConfigurationService
│   └── ViewModels/                 # اختبارات ViewModels
│       ├── ImageRenameViewModelTests.cs
│       └── ImageSearchViewModelTests.cs
├── Integration/                    # اختبارات التكامل
│   └── RenameWorkflowTests.cs
└── Mocks/                          # Mock Objects
    └── MockServices.cs
```

## تشغيل الاختبارات

```bash
# تشغيل جميع الاختبارات
dotnet test

# تشغيل مع تقرير التغطية
dotnet test --collect:"XPlat Code Coverage"

# تشغيل اختبارات محددة
dotnet test --filter "FullyQualifiedName~FileServiceTests"
```

## أنواع الاختبارات

### Unit Tests (اختبارات الوحدات)
- **FileServiceTests**: اختبار SanitizeFilename، LoadImageFiles، CopyFile
- **ConfigurationServiceTests**: اختبار GetApiKeys، GetGeminiModel، GetSupportedExtensions
- **ViewModelTests**: اختبار الأوامر والخصائص والسلوك الأولي

### Integration Tests (اختبارات التكامل)
- **RenameWorkflowTests**: اختبار التفاعل بين الخدمات والـ ViewModels

## Mocking
يستخدم المشروع **Moq** لإنشاء Mock Objects للواجهات:
- `MockServices.CreateGeminiService()` - لمحاكاة خدمة Gemini AI
- `MockServices.CreateFileService()` - لمحاكاة عمليات الملفات
- `MockServices.CreateConfigurationService()` - لمحاكاة الإعدادات
- `MockServices.CreateNavigationService()` - لمحاكاة التنقل

## تقرير التغطية (Code Coverage)
بعد تشغيل الاختبارات مع `--collect:"XPlat Code Coverage"`، ستجد ملف التغطية في:
```
TestResults/{guid}/coverage.cobertura.xml
```

## المكتبات المستخدمة
- **xUnit**: إطار عمل الاختبارات
- **Moq**: مكتبة Mocking
- **coverlet.collector**: تجميع تقارير التغطية
