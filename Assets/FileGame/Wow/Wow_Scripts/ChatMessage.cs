public class ChatMessage : ChatMessageUI
{
    public void SetText(string str)
    {
        Bind(str, false);
    }
}
