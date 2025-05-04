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

    [Tooltip("Only show log entries that start with this prefix. Leave blank to show all.")]
    public string filterPrefix = "[DEBUG]";

    private readonly StringBuilder _buffer = new StringBuilder();
    private readonly Queue<string> _lines = new Queue<string>();

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        Application.logMessageReceivedThreaded += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
        Application.logMessageReceivedThreaded -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // If filterPrefix is non-empty, only capture those starting with it
        if (!string.IsNullOrEmpty(filterPrefix) && !logString.StartsWith(filterPrefix))
            return;

        // Enqueue new line
        _lines.Enqueue(logString);

        // Trim oldest lines
        while (_lines.Count > maxLines)
            _lines.Dequeue();

        // Rebuild buffer
        _buffer.Clear();
        foreach (var line in _lines)
            _buffer.AppendLine(line);

        // Display
        if (outputText != null)
            outputText.text = _buffer.ToString();
    }
}
