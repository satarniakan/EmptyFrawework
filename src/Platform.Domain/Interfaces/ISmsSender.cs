namespace Platform.Domain.Interfaces;

public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message);
}