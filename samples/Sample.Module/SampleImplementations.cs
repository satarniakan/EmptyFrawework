using Microsoft.EntityFrameworkCore;
using Platform.Application.Services;
using Platform.Domain.Enums;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Repositories;
using SampleDomain;

namespace SampleDomain;

/// <summary>
/// پیاده‌سازی واحد کار دامنهٔ نمونه: همان چهار ریپازیتوری پایه + ریپازیتوری وظایف.
/// </summary>
public class SampleUnitOfWork : PlatformUnitOfWork, ISampleUnitOfWork
{
    public SampleUnitOfWork(
        PlatformDbContext context,
        IOtpRepository otpCodes,
        IAuditLogRepository auditLogs,
        INotificationRepository notifications,
        IOutboxRepository outbox,
        IWorkTaskRepository workTasks)
        : base(context, otpCodes, auditLogs, notifications, outbox)
    {
        WorkTasks = workTasks;
    }

    public IWorkTaskRepository WorkTasks { get; }
}

/// <summary>
/// ریپازیتوری وظایف. عمداً از dBContext مستقیم استفاده می‌کند (نه DbSet جداگانه)
/// تا وابستگی‌اش به ماژول شفاف بماند.
/// </summary>
public class WorkTaskRepository : IWorkTaskRepository
{
    private readonly PlatformDbContext _context;

    public WorkTaskRepository(PlatformDbContext context) => _context = context;

    public Task<List<WorkTask>> GetForUserAsync(string userId) =>
        _context.Set<WorkTask>()
            .Where(t => t.AssignedUserId == userId)
            .OrderByDescending(t => t.Id)
            .ToListAsync();

    public Task<WorkTask?> GetByIdAsync(int id) =>
        _context.Set<WorkTask>().FirstOrDefaultAsync(t => t.Id == id);

    public async Task AddAsync(WorkTask task) =>
        await _context.Set<WorkTask>().AddAsync(task);
}

/// <summary>
/// ماژول دامنهٔ نمونه: تنها جایی که PlatformDbContext از وجود «وظایف» باخبر می‌شود.
/// </summary>
public class SampleModule : IPlatformModule
{
    public string Name => "Sample";

    public void ConfigureModel(ModelBuilder builder)
    {
        builder.Entity<WorkTask>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
            e.Property(t => t.Description).HasMaxLength(1000);
            e.HasIndex(t => t.AssignedUserId);
        });
    }
}

/// <summary>
/// سرویس وظایف: نمونهٔ یک سرویس دامنه که فقط به واحد کارِ دامنه و سرویس‌های پایه
/// (اعلان) وصل است و هیچ دانش حسابداری ندارد.
/// </summary>
public interface IWorkTaskService
{
    Task<List<WorkTask>> GetMineAsync(string userId);
    Task<WorkTask> CreateAsync(string userId, string title, string? description);
    Task<bool> ToggleDoneAsync(string userId, int taskId);
}

public class WorkTaskService : IWorkTaskService
{
    private readonly ISampleUnitOfWork _unitOfWork;
    private readonly INotificationService _notifications;

    public WorkTaskService(ISampleUnitOfWork unitOfWork, INotificationService notifications)
    {
        _unitOfWork = unitOfWork;
        _notifications = notifications;
    }

    public Task<List<WorkTask>> GetMineAsync(string userId) =>
        _unitOfWork.WorkTasks.GetForUserAsync(userId);

    public async Task<WorkTask> CreateAsync(string userId, string title, string? description)
    {
        var task = new WorkTask
        {
            Title = title.Trim(),
            Description = description?.Trim(),
            AssignedUserId = userId
        };

        await _unitOfWork.WorkTasks.AddAsync(task);
        await _unitOfWork.CompleteAsync();

        // نمونه‌ای از استفادهٔ ماژول از سرویس پایهٔ اعلان — بدون دانش حسابداری:
        await _notifications.NotifyAsync(userId, "وظیفهٔ جدید",
            $"«{task.Title}» برای شما ساخته شد.", NotificationType.System, "/tasks");

        return task;
    }

    public async Task<bool> ToggleDoneAsync(string userId, int taskId)
    {
        var task = await _unitOfWork.WorkTasks.GetByIdAsync(taskId);
        if (task is null || task.AssignedUserId != userId) return false;

        task.IsDone = !task.IsDone;
        await _unitOfWork.CompleteAsync();
        return true;
    }
}