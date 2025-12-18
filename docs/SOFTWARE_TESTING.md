# تقرير اختبار البرمجيات - مشروع ImageAIRenamer

## نظرة عامة على المشروع

**ImageAIRenamer** هو تطبيق Windows Desktop مبني بـ WPF و .NET 10 يستخدم الذكاء الاصطناعي (Google Gemini API) لإعادة تسمية الصور تلقائياً بناءً على محتواها.

---

## 1. منهجية الاختبار (Testing Methodology)

اتبعنا منهجية **اختبار متعدد المستويات (Multi-Level Testing)** تشمل:

| المستوى | النوع | الهدف |
|---------|-------|-------|
| 1 | Unit Testing | اختبار كل وحدة منفصلة |
| 2 | Integration Testing | اختبار التفاعل بين الوحدات |
| 3 | Mocking | محاكاة الخدمات الخارجية |

---

## 2. أدوات الاختبار المستخدمة (Testing Tools)

| الأداة | الغرض |
|--------|-------|
| **xUnit** | إطار عمل الاختبارات الرئيسي |
| **Moq** | إنشاء Mock Objects للواجهات |
| **Coverlet** | قياس تغطية الكود (Code Coverage) |

### لماذا اخترنا هذه الأدوات؟

- **xUnit**: الأكثر شيوعاً في .NET، يدعم الاختبارات المتوازية، سهل الاستخدام
- **Moq**: يسمح بمحاكاة الواجهات دون الحاجة لتطبيق حقيقي
- **Coverlet**: متوافق مع جميع أنظمة التشغيل، يولد تقارير بصيغ متعددة

---

## 3. هيكل مشروع الاختبارات

```
ImageAIRenamer.Tests/
├── Unit/                              # اختبارات الوحدات
│   ├── Services/                      # اختبارات الخدمات
│   │   ├── FileServiceTests.cs        # 17 اختبار
│   │   └── ConfigurationServiceTests.cs # 9 اختبارات
│   └── ViewModels/                    # اختبارات نماذج العرض
│       ├── ImageRenameViewModelTests.cs  # 9 اختبارات
│       └── ImageSearchViewModelTests.cs  # 9 اختبارات
├── Integration/                       # اختبارات التكامل
│   └── RenameWorkflowTests.cs         # 5 اختبارات
└── Mocks/                             # كائنات المحاكاة
    └── MockServices.cs
```

**المجموع: 49 اختبار**

---

## 4. اختبارات الوحدات (Unit Tests)

### 4.1 FileService Tests

نختبر خدمة التعامل مع الملفات:

#### أ) اختبار تنظيف أسماء الملفات (SanitizeFilename)

```csharp
[Fact]
public void SanitizeFilename_WithSpaces_ReplacesWithUnderscores()
{
    // Arrange - تحضير البيانات
    var input = "File Name With Spaces";

    // Act - تنفيذ العملية
    var result = _fileService.SanitizeFilename(input);

    // Assert - التحقق من النتيجة
    Assert.Equal("File_Name_With_Spaces", result);
}
```

**حالات الاختبار:**
| الحالة | المدخل | المتوقع | النوع |
|--------|--------|---------|-------|
| اسم صحيح | `"ValidFileName"` | `"ValidFileName"` | Positive |
| مسافات | `"File Name"` | `"File_Name"` | Positive |
| رموز خاصة | `"File@#$Name"` | `"FileName"` | Positive |
| نص عربي | `"صورة_اختبار"` | `"صورة_اختبار"` | Positive |
| فارغ | `""` | `"صورة"` | Boundary |
| null | `null` | `"صورة"` | Negative |
| رموز فقط | `"!@#$%"` | `"صورة"` | Negative |

#### ب) اختبار تحميل الصور (LoadImageFilesAsync)

```csharp
[Fact]
public async Task LoadImageFilesAsync_WithValidFolder_ReturnsMatchingFiles()
{
    // Arrange
    var tempDir = CreateTempDirectoryWithImages();
    var extensions = new[] { ".jpg", ".png" };

    // Act
    var result = await _fileService.LoadImageFilesAsync(tempDir, extensions);

    // Assert
    Assert.Equal(2, result.Count());
}
```

**حالات الاختبار:**
| الحالة | الوصف | النتيجة المتوقعة |
|--------|-------|-----------------|
| مجلد صحيح | مجلد يحتوي صور | قائمة الصور |
| مجلد غير موجود | مسار خاطئ | قائمة فارغة |
| مجلد فارغ | بدون ملفات | قائمة فارغة |

#### ج) اختبار ضمان اسم فريد (EnsureUniqueFilename)

```csharp
[Fact]
public void EnsureUniqueFilename_WhenFileExists_ReturnsIncrementedPath()
{
    // Arrange - إنشاء ملف موجود مسبقاً
    File.WriteAllText(Path.Combine(tempDir, "test.jpg"), "");

    // Act
    var result = _fileService.EnsureUniqueFilename(tempDir, "test", ".jpg");

    // Assert
    Assert.Equal(Path.Combine(tempDir, "test_1.jpg"), result);
}
```

---

### 4.2 ConfigurationService Tests

نختبر خدمة إدارة الإعدادات:

```csharp
[Fact]
public void GetGeminiModel_WithNoConfig_ReturnsDefault()
{
    // Arrange
    var service = CreateService(); // بدون إعدادات

    // Act
    var result = service.GetGeminiModel();

    // Assert
    Assert.Equal("gemini-2.0-flash", result);
}

[Fact]
public async Task SaveAndGetApiKeys_RoundTrip_Success()
{
    // Arrange
    var keys = new[] { "key1", "key2", "key3" };

    // Act
    await service.SaveApiKeysAsync(keys);
    var result = await service.GetApiKeysAsync();

    // Assert
    Assert.Equal(3, result.Length);
}
```

---

### 4.3 ViewModel Tests

نختبر نماذج العرض (ViewModels) مع استخدام Mock Objects:

```csharp
[Fact]
public void ClearListCommand_ClearsAllData()
{
    // Arrange
    var viewModel = CreateViewModel();
    viewModel.Images.Add(new ImageItem { FilePath = "test.jpg", OriginalName = "test" });
    viewModel.SourceFolder = @"C:\Source";
    viewModel.OutputFolder = @"C:\Output";

    // Act
    viewModel.ClearListCommand.Execute(null);

    // Assert
    Assert.Empty(viewModel.Images);
    Assert.Equal(string.Empty, viewModel.SourceFolder);
    Assert.Equal(string.Empty, viewModel.OutputFolder);
}

[Fact]
public void BackToHomeCommand_NavigatesToWelcome()
{
    // Arrange
    var navMock = MockServices.CreateNavigationService();
    var viewModel = CreateViewModel(navigationService: navMock);

    // Act
    viewModel.BackToHomeCommand.Execute(null);

    // Assert - التحقق من استدعاء الدالة
    navMock.Verify(x => x.NavigateToWelcome(), Times.Once);
}
```

---

## 5. المحاكاة (Mocking)

### لماذا نستخدم Mocking؟

1. **عزل الوحدة**: اختبار كل وحدة بشكل مستقل
2. **السرعة**: تجنب استدعاء APIs خارجية بطيئة
3. **التحكم**: تحديد سلوك معين للاختبار
4. **التكلفة**: تجنب استهلاك API المدفوع

### مثال على Mock Object:

```csharp
public static Mock<IGeminiService> CreateGeminiService(string? defaultTitle = "TestTitle")
{
    var mock = new Mock<IGeminiService>();
    
    // إعداد السلوك المتوقع
    mock.Setup(x => x.GenerateTitleAsync(
            It.IsAny<string>(),      // أي مسار ملف
            It.IsAny<string?>(),     // أي تعليمات
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(defaultTitle); // إرجاع قيمة ثابتة

    return mock;
}
```

### استخدام Mock في الاختبار:

```csharp
[Fact]
public async Task ViewModel_LoadsApiKeysFromConfiguration()
{
    // Arrange - إنشاء Mock بقيم محددة
    var configMock = MockServices.CreateConfigurationService(
        apiKeys: new[] { "key1", "key2" }
    );
    
    var viewModel = CreateViewModel(configurationService: configMock);

    // Assert - التحقق من استدعاء الدالة
    configMock.Verify(x => x.GetApiKeysAsync(), Times.Once);
}
```

---

## 6. اختبارات التكامل (Integration Tests)

نختبر التفاعل بين عدة وحدات معاً:

```csharp
[Fact]
public async Task Workflow_SanitizeMultipleFilenames_HandlesCollisions()
{
    // Arrange
    var fileService = new FileService(); // خدمة حقيقية
    var names = new[] { "Test Image", "Test Image", "Test Image" };

    // Act - محاكاة سيناريو حقيقي
    foreach (var name in names)
    {
        var sanitized = fileService.SanitizeFilename(name);
        // ... معالجة التكرارات
    }

    // Assert
    var files = Directory.GetFiles(tempDir);
    Assert.Equal(3, files.Length);
    Assert.Contains(files, f => Path.GetFileName(f) == "Test_Image.jpg");
    Assert.Contains(files, f => Path.GetFileName(f) == "Test_Image_1.jpg");
    Assert.Contains(files, f => Path.GetFileName(f) == "Test_Image_2.jpg");
}
```

---

## 7. أنماط الاختبار المستخدمة

### 7.1 نمط AAA (Arrange-Act-Assert)

```csharp
[Fact]
public void TestExample()
{
    // Arrange - تحضير البيانات والكائنات
    var input = "test data";
    
    // Act - تنفيذ العملية المراد اختبارها
    var result = ProcessData(input);
    
    // Assert - التحقق من النتيجة
    Assert.Equal("expected", result);
}
```

### 7.2 Equivalence Partitioning (تقسيم التكافؤ)

قسمنا المدخلات إلى مجموعات متكافئة:

| المجموعة | الوصف | مثال |
|----------|-------|------|
| صحيحة | مدخلات صالحة | `"ValidName"` |
| حدية | حالات الحدود | `""`, `null` |
| خاطئة | مدخلات غير صالحة | `"!@#$%"` |

### 7.3 Boundary Testing (اختبار الحدود)

```csharp
// اختبار الحد الأدنى
[Fact]
public void SanitizeFilename_WithEmptyString_ReturnsDefault()

// اختبار القيمة الخاصة
[Fact]
public void SanitizeFilename_WithNullString_ReturnsDefault()

// اختبار ملف موجود
[Fact]
public void EnsureUniqueFilename_WhenFileExists_ReturnsIncrementedPath()
```

---

## 8. تشغيل الاختبارات

### الأوامر الأساسية:

```bash
# تشغيل جميع الاختبارات
dotnet test

# تشغيل مع تفاصيل
dotnet test --verbosity detailed

# تشغيل اختبارات محددة
dotnet test --filter "FullyQualifiedName~FileServiceTests"

# تشغيل مع تقرير التغطية
dotnet test --collect:"XPlat Code Coverage"
```

### نتيجة التشغيل:

```
Passed!  - Failed: 0, Passed: 49, Skipped: 0, Total: 49
```

---

## 9. تغطية الكود (Code Coverage)

بعد تشغيل الاختبارات مع Coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

يتم إنشاء ملف `coverage.cobertura.xml` يحتوي على:
- نسبة الأسطر المختبرة (Line Coverage)
- نسبة الفروع المختبرة (Branch Coverage)
- تفاصيل كل ملف ودالة

---

## 10. الدروس المستفادة

### ما تعلمناه من تطبيق اختبار البرمجيات:

1. **أهمية فصل الاهتمامات**: تصميم MVVM سهّل الاختبار
2. **Dependency Injection**: أتاح استبدال الخدمات بـ Mocks
3. **الواجهات (Interfaces)**: ضرورية لإنشاء Mocks
4. **اختبار الحالات الحدية**: كشف أخطاء غير متوقعة
5. **التوثيق**: الاختبارات توثق السلوك المتوقع

### التحسينات المستقبلية:

- [ ] إضافة UI Tests باستخدام Appium
- [ ] إضافة Performance Tests
- [ ] زيادة نسبة Code Coverage
- [ ] إضافة Mutation Testing

---

## 11. المراجع

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Microsoft Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/)
- [Code Coverage in .NET](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)
