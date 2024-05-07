namespace UI.Model;

public class ChatMessage
{
    public string Text { get; set; }
    public bool IsUserMessage { get; set; }

    public ChatMessage(string text, bool isUserMessage)
    {
        Text = text;
        IsUserMessage = isUserMessage;
    }
}