namespace eventsynс.Application.Interfaces.Notifications;

public interface ITelegramNotificationService
{
    Task SendMessageAsync(long telegramId, string message);
}