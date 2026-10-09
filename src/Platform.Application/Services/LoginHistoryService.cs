using Platform.Domain.Entities;
using Platform.Domain.Interfaces;

namespace Platform.Application.Services;

/// <summary>ثبت تاریخچهٔ ورودها (موفق و ناموفق) با IP و عامل کاربر.</summary>
public interface ILoginHistoryService
{
    Task RecordAsync(string? userId, string? userName, bool succeeded, string method,
        string? ipAddress, string? userAgent, string? failureReason = null);
}

public class LoginHistoryService : ILoginHistoryService
{
    private readonly ILoginHistoryRepository _repository;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public LoginHistoryService(ILoginHistoryRepository repository, IPlatformUnitOfWork unitOfWork,
        TimeProvider? clock = null)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task RecordAsync(string? userId, string? userName, bool succeeded, string method,
        string? ipAddress, string? userAgent, string? failureReason = null)
    {
        await _repository.AddAsync(new LoginHistory
        {
            UserId = userId,
            UserName = userName,
            Succeeded = succeeded,
            Method = method,
            IpAddress = ipAddress,
            UserAgent = userAgent is { Length: > 512 } ? userAgent[..512] : userAgent,
            FailureReason = failureReason,
            OccurredAtUtc = _clock.GetUtcNow().UtcDateTime
        });
        await _unitOfWork.CompleteAsync();
    }
}
