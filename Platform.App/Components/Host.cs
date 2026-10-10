using System.Reflection;

namespace Platform.App.Components;

/// <summary>
/// شل وب میزبان: مونتاژ صفحات پروژهٔ میزبان برای روتر پایه.
/// (میزبان نمونه صفحه‌ای ندارد؛ پروژهٔ واقعی صفحات دامنه‌اش را همین‌جا می‌گذارد.)
/// </summary>
public static class AppRoutes
{
    public static Assembly[] AdditionalAssemblies { get; } = [typeof(AppRoutes).Assembly];
}
