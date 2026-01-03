namespace ImageAIRenamer.Application.Common;

public static class ImageStatusConstants
{
    public const string Pending = "في الانتظار";
    public const string Processing = "جاري المعالجة...";
    public const string Renaming = "جاري إعادة التسمية...";
    public const string Matched = "مطابق";
    public const string NotMatched = "غير مطابق";
    public const string Completed = "تم";
    public const string Error = "خطأ";
    public const string Cancelled = "ملغي";
    public const string Copied = "تم النسخ";
    public const string CopyError = "خطأ في النسخ";
}

public static class ErrorMessages
{
    public const string NoApiKeys = "الرجاء إدخال مفتاح Gemini API واحد على الأقل.";
    public const string NoSearchDescription = "الرجاء إدخال وصف البحث.";
    public const string CannotCreateOutputFolder = "تعذر إنشاء مجلد الإخراج.";
    public const string NoOutputFolder = "الرجاء اختيار مجلد الإخراج أولاً.";
    public const string NoImagesSelected = "الرجاء تحديد صورة واحدة على الأقل للنسخ.";
    public const string LoadImagesError = "حدث خطأ أثناء تحميل الصور: {0}";
}

public static class SuccessMessages
{
    public const string ProcessingCompleted = "اكتملت المعالجة!";
    public const string SearchCompleted = "اكتمل البحث! تم العثور على {0} صورة مطابقة من أصل {1}.";
    public const string ImagesLoaded = "تم تحميل {0} صورة.";
    public const string ImagesCopied = "تم نسخ {0} صورة بنجاح إلى مجلد الإخراج.";
    public const string NoImagesCopied = "لم يتم نسخ أي صورة.";
    public const string CopyFailed = "{0} صورة فشل نسخها.";
}

