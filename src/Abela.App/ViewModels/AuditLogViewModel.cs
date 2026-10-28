using System.Collections.ObjectModel;
using Abela.App.Common;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class AuditLogViewModel : ObservableObject
{
    private readonly AuditLogService _auditService;
    private readonly IDialogService _dialog;

    [ObservableProperty] private DateTime fromDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime toDate = DateTime.Today;

    public ObservableCollection<AuditLog> Logs { get; } = new();

    public AuditLogViewModel(AuditLogService auditService, IDialogService dialog)
    {
        _auditService = auditService;
        _dialog = dialog;
        SearchAsync().FireAndForgetSafe(_dialog, "سجل النشاط");
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var results = await _auditService.SearchAsync(FromDate, ToDate.AddDays(1).AddSeconds(-1));
        Logs.Clear();
        foreach (var l in results) Logs.Add(l);
    }
}
