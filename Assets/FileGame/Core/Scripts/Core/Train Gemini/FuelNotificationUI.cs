using UnityEngine;
using TMPro;

public class FuelNotificationUI : MonoBehaviour
{
    private TextMeshPro _textMesh;
    private float _timer;
    private float _duration = 1.5f;
    private Vector3 _startPos;
    private Color _startColor;

    public void Setup(string text, Color color)
    {
        _textMesh = GetComponent<TextMeshPro>();
        if (_textMesh == null)
        {
            _textMesh = gameObject.AddComponent<TextMeshPro>();
        }

        _textMesh.text = text;
        _textMesh.color = color;
        _textMesh.fontSize = 5;
        _textMesh.alignment = TextAlignmentOptions.Center;
        
        _startPos = transform.position;
        _startColor = color;
        _timer = 0;
        
        // Ensure it faces the camera (optional but good for world space)
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
            Camera.main.transform.rotation * Vector3.up);
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        float progress = _timer / _duration;

        // Animate: Float up
        transform.position = _startPos + Vector3.up * (progress * 1.5f);

        // Animate: Fade out
        if (_textMesh != null)
        {
            Color c = _startColor;
            c.a = 1f - (progress * progress); // Ease out
            _textMesh.color = c;
        }

        if (_timer >= _duration)
        {
            Destroy(gameObject);
        }
    }

    public static void Create(Vector3 position, string text)
    {
        GameObject go = new GameObject("FuelNotification");
        go.transform.position = position;
        FuelNotificationUI ui = go.AddComponent<FuelNotificationUI>();
        
        // Add TMP
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        
        // Yellow color as requested
        ui.Setup(text, Color.yellow);
    }
}
