// Copyright © 2025. Ported to C# from original C++ TouchCursor by Martin Stone.
// Original project licensed under GNU GPL v3.

using System.IO;
using System.Text.Json;
using touch_cursor.Models;

namespace touch_cursor.Services;

/// <summary>
/// 타이핑 로그를 기록하고 분석용 데이터를 수집하는 서비스
/// </summary>
public class TypingLogger : IDisposable
{
    private readonly string _logDirectory;
    private readonly string _sessionId;
    private DateTime _lastKeyPressTime;
    private string _lastKeyName = "";
    private StreamWriter? _writer;
    private readonly object _lock = new();
    private bool _enabled = false;
    private TypingLogEntry? _lastLogEntry = null;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                if (_enabled)
                {
                    OpenLogFile();
                }
                else
                {
                    CloseLogFile();
                }
            }
        }
    }

    public TypingLogger()
    {
        _sessionId = Guid.NewGuid().ToString("N")[..8]; // 짧은 세션 ID
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _logDirectory = Path.Combine(appData, "TouchCursor", "Logs");
        Directory.CreateDirectory(_logDirectory);
    }

    private void OpenLogFile()
    {
        lock (_lock)
        {
            CloseLogFile();

            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var logFileName = $"typing-log-{date}-{_sessionId}.jsonl"; // JSON Lines format
            var logPath = Path.Combine(_logDirectory, logFileName);

            _writer = new StreamWriter(logPath, append: true)
            {
                AutoFlush = true
            };
        }
    }

    private void CloseLogFile()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    public void LogKeyEvent(TypingLogEntry entry)
    {
        if (!_enabled || _writer == null)
            return;

        try
        {
            lock (_lock)
            {
                // 이전 키와의 시간 간격 계산
                if (_lastKeyPressTime != DateTime.MinValue)
                {
                    entry.TimeSinceLastKey = (long)(entry.Timestamp - _lastKeyPressTime).TotalMilliseconds;
                    entry.PreviousKey = _lastKeyName;
                }

                entry.SessionId = _sessionId;

                // JSON Lines 형식으로 기록 (한 줄에 하나의 JSON 객체)
                var json = JsonSerializer.Serialize(entry);
                _writer.WriteLine(json);

                _lastKeyPressTime = entry.Timestamp;
                _lastKeyName = entry.SourceKeyName;
                _lastLogEntry = entry; // 마지막 엔트리 저장 (오타 마킹용)
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TypingLogger] Error logging: {ex.Message}");
        }
    }

    /// <summary>
    /// 마지막으로 로깅된 이벤트를 오타로 표시하고 다시 기록
    /// </summary>
    public void MarkLastAsMistake()
    {
        if (!_enabled || _writer == null || _lastLogEntry == null)
            return;

        try
        {
            lock (_lock)
            {
                // 마지막 엔트리를 오타로 마킹
                _lastLogEntry.MarkedAsMistake = true;

                // 다시 JSON으로 기록 (코멘트 추가)
                var json = JsonSerializer.Serialize(_lastLogEntry);
                _writer.WriteLine($"// CORRECTION: Previous entry marked as mistake");
                _writer.WriteLine(json);

                System.Diagnostics.Debug.WriteLine("[TypingLogger] Marked last entry as mistake");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TypingLogger] Error marking as mistake: {ex.Message}");
        }
    }

    /// <summary>
    /// 로그 파일 목록 조회
    /// </summary>
    public List<string> GetLogFiles()
    {
        if (!Directory.Exists(_logDirectory))
            return new List<string>();

        return Directory.GetFiles(_logDirectory, "typing-log-*.jsonl")
            .OrderByDescending(f => f)
            .ToList();
    }

    /// <summary>
    /// 로그 데이터를 CSV로 내보내기 (분석용)
    /// </summary>
    public void ExportToCsv(string inputJsonlFile, string outputCsvFile)
    {
        var lines = File.ReadAllLines(inputJsonlFile);
        using var csv = new StreamWriter(outputCsvFile);

        // CSV 헤더
        csv.WriteLine("Timestamp,ActivationKey,SourceKey,TargetKey,ElapsedMs,RolloverThreshold," +
                     "RolloverDetected,IsRolloverException,EventType,TimeSinceLastKey,PreviousKey," +
                     "MarkedAsMistake,ProcessName");

        foreach (var line in lines)
        {
            try
            {
                var entry = JsonSerializer.Deserialize<TypingLogEntry>(line);
                if (entry == null) continue;

                csv.WriteLine($"{entry.Timestamp:O}," +
                             $"{entry.ActivationKeyName}," +
                             $"{entry.SourceKeyName}," +
                             $"{entry.TargetKeyName}," +
                             $"{entry.ElapsedMs}," +
                             $"{entry.RolloverThreshold}," +
                             $"{entry.RolloverDetected}," +
                             $"{entry.IsRolloverException}," +
                             $"{entry.EventType}," +
                             $"{entry.TimeSinceLastKey ?? 0}," +
                             $"{entry.PreviousKey ?? ""}," +
                             $"{entry.MarkedAsMistake}," +
                             $"{entry.ProcessName ?? ""}");
            }
            catch
            {
                // Skip malformed lines
            }
        }
    }

    /// <summary>
    /// 로그 통계 분석
    /// </summary>
    public TypingStatistics AnalyzeLogs(string logFile)
    {
        var stats = new TypingStatistics();
        var lines = File.ReadAllLines(logFile);

        foreach (var line in lines)
        {
            try
            {
                var entry = JsonSerializer.Deserialize<TypingLogEntry>(line);
                if (entry == null) continue;

                stats.TotalEvents++;

                if (entry.EventType == "mapped")
                    stats.MappedEvents++;
                else if (entry.EventType == "rollover")
                    stats.RolloverEvents++;

                if (entry.MarkedAsMistake)
                    stats.MarkedMistakes++;

                // 키별 통계
                var keyPair = $"{entry.ActivationKeyName}+{entry.SourceKeyName}";
                if (!stats.KeyPairFrequency.ContainsKey(keyPair))
                    stats.KeyPairFrequency[keyPair] = 0;
                stats.KeyPairFrequency[keyPair]++;

                // 타이핑 속도 분석
                if (entry.TimeSinceLastKey.HasValue)
                {
                    stats.TypingSpeeds.Add(entry.TimeSinceLastKey.Value);
                }
            }
            catch
            {
                // Skip malformed lines
            }
        }

        return stats;
    }

    public void Dispose()
    {
        CloseLogFile();
    }
}

/// <summary>
/// 타이핑 통계
/// </summary>
public class TypingStatistics
{
    public int TotalEvents { get; set; }
    public int MappedEvents { get; set; }
    public int RolloverEvents { get; set; }
    public int MarkedMistakes { get; set; }
    public Dictionary<string, int> KeyPairFrequency { get; set; } = new();
    public List<long> TypingSpeeds { get; set; } = new();

    public double AverageTypingSpeed => TypingSpeeds.Count > 0 ? TypingSpeeds.Average() : 0;
    public double RolloverRate => TotalEvents > 0 ? (double)RolloverEvents / TotalEvents * 100 : 0;
    public double MistakeRate => TotalEvents > 0 ? (double)MarkedMistakes / TotalEvents * 100 : 0;
}
