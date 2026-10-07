using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Sample.Web.Components;

/// <summary>
/// شل وب میزبان: روتر پایه + مونتاژ صفحات پروژهٔ میزبان.
/// </summary>
public static class SampleRoutes
{
    public static Assembly[] AdditionalAssemblies { get; } = [typeof(SampleRoutes).Assembly];
}

/// <summary>
/// ریدایرکت ریشه به داشبورد جلسات.
/// </summary>
[Route("/")]
public class HomeRedirector : ComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized() => Navigation.NavigateTo("/my-meetings", replace: true);
}
