using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GroceryManagement
{
    public static class Extensions
    {
        public static bool IsAjax(this HttpRequest request)
        {
            return request.Headers.XRequestedWith == "XMLHttpRequest";
        }

        public static bool IsValid(this ModelStateDictionary ms, string key)
        {
            return ms.GetFieldValidationState(key) == ModelValidationState.Valid;
        }
        public static DateOnly ToDateOnly(this DateTime dt)
        {
            return DateOnly.FromDateTime(dt);
        }
    }
}
