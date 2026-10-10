namespace Platform.Application.DTOs;

/// <summary>درخواست شروع پرداخت (مبلغ به تومان). CallbackUrl خالی یعنی callback همین میزبان.</summary>
public record StartPaymentDto(long AmountTomans, string Description, string? CallbackUrl = null);
