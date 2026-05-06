using TMPro;
using UnityEngine;

public class ChatMessageUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Color ownMessageColor = new Color(0.65f, 0.85f, 1f, 1f);
    [SerializeField] private Color otherMessageColor = Color.white;

    private void Awake()
    {
        if (messageText == null)
            messageText = GetComponent<TMP_Text>();
    }

    public void Bind(string message, bool isOwnMessage)
    {
        if (messageText == null)
            return;

        messageText.text = message;
        messageText.color = isOwnMessage ? ownMessageColor : otherMessageColor;
        messageText.alignment = isOwnMessage
            ? TextAlignmentOptions.Right
            : TextAlignmentOptions.Left;
    }
}
