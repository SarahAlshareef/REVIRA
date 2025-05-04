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
        // Subscribe to Unity's log events
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // If a prefix is set, only capture those lines
        if (!string.IsNullOrEmpty(filterPrefix) && !logString.StartsWith(filterPrefix))
            return;

        // Enqueue the new entry
        _lines.Enqueue(logString);

        // Trim old entries
        while (_lines.Count > maxLines)
            _lines.Dequeue();

        // Rebuild display buffer
        _buffer.Clear();
        foreach (var line in _lines)
            _buffer.AppendLine(line);

        // Update the on-screen text
        if (outputText != null)
            outputText.text = _buffer.ToString();
    }
}
