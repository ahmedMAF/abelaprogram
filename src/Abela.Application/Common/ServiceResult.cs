namespace Abela.Application.Common;

public class ServiceResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ServiceResult Ok(string message = "تمت العملية بنجاح") => new() { Success = true, Message = message };
    public static ServiceResult Fail(string message) => new() { Success = false, Message = message };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }

    public static ServiceResult<T> Ok(T data, string message = "تمت العملية بنجاح") => new() { Success = true, Message = message, Data = data };
    public static new ServiceResult<T> Fail(string message) => new() { Success = false, Message = message };
}

public interface IDialogService
{
    void ShowInfo(string message, string title = "تنبيه");
    void ShowError(string message, string title = "خطأ");
    void ShowSuccess(string message, string title = "نجاح");
    bool Confirm(string message, string title = "تأكيد");
    string? SaveFile(string filter, string defaultFileName, string title = "حفظ باسم");
    string? OpenFile(string filter, string title = "اختر ملفًا");
    string? SelectFolder(string title = "اختر مجلدًا");
}

public interface ICurrentUserService
{
    int UserId { get; }
    string UserName { get; }
    string RoleName { get; }
    HashSet<string> Permissions { get; }
    bool HasPermission(string code);
    void SetUser(int userId, string userName, string roleName, IEnumerable<string> permissions);
    void Clear();
}

public class CurrentUserService : ICurrentUserService
{
    public int UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string RoleName { get; private set; } = string.Empty;
    public HashSet<string> Permissions { get; private set; } = new();

    public bool HasPermission(string code) => Permissions.Contains(code);

    public void SetUser(int userId, string userName, string roleName, IEnumerable<string> permissions)
    {
        UserId = userId;
        UserName = userName;
        RoleName = roleName;
        Permissions = new HashSet<string>(permissions);
    }

    public void Clear()
    {
        UserId = 0;
        UserName = string.Empty;
        RoleName = string.Empty;
        Permissions.Clear();
    }
}
