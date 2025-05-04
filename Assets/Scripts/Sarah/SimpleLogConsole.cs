using UnityEngine;
using TMPro;
using System.Text;
using System.Collections.Generic;

public class SimpleLogConsole : MonoBehaviour
{
    [Tooltip("A TextMeshProUGUI element that will show the last few log lines.")]
    public TMP_Text outputText;

    [Tooltip("How many lines to keep in the on-screen buffer.")]
    public int maxLines = 10;

    private readonly StringBuilder _buffer = new StringBuilder();
    private readonly Queue<string> _lines = new Queue<string>();

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Format: [Type] message
        string entry = $"[{type}] {logString}";

        // Add to queue
        _lines.Enqueue(entry);

        // Trim oldest lines
        while (_lines.Count > maxLines)
            _lines.Dequeue();

        // Rebuild buffer
        _buffer.Clear();
        foreach (var line in _lines)
            _buffer.AppendLine(line);

        // Push to UI
        if (outputText != null)
            outputText.text = _buffer.ToString();
    }
}
