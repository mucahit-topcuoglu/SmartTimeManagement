using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SmartTimeManagement.MAUI.Messages;

public class ReminderChangedMessage : ValueChangedMessage<string>
{
    public ReminderChangedMessage(string value) : base(value)
    {
    }
}
