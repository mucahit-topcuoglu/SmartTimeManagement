using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SmartTimeManagement.MAUI.Messages;

public class TaskChangedMessage : ValueChangedMessage<string>
{
    public TaskChangedMessage(string value) : base(value)
    {
    }
}
