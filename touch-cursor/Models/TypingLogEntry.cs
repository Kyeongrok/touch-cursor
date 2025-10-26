// Copyright © 2025. Ported to C# from original C++ TouchCursor by Martin Stone.
// Original project licensed under GNU GPL v3.

namespace touch_cursor.Models;

/// <summary>
/// 타이핑 로그 엔트리 - 머신러닝 분석용 데이터
/// </summary>
public class TypingLogEntry
{
    /// <summary>
    /// 로그 기록 시각 (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 활성화 키 (VK code)
    /// </summary>
    public int ActivationKey { get; set; }

    /// <summary>
    /// 활성화 키 이름 (예: "Space")
    /// </summary>
    public string ActivationKeyName { get; set; } = "";

    /// <summary>
    /// 입력한 원본 키 (VK code)
    /// </summary>
    public int SourceKey { get; set; }

    /// <summary>
    /// 원본 키 이름 (예: "P", "H")
    /// </summary>
    public string SourceKeyName { get; set; } = "";

    /// <summary>
    /// 매핑된 대상 키 (VK code)
    /// </summary>
    public int? TargetKey { get; set; }

    /// <summary>
    /// 대상 키 이름 (예: "Backspace", "Left")
    /// </summary>
    public string TargetKeyName { get; set; } = "";

    /// <summary>
    /// Modifier 플래그 (Ctrl, Alt, Shift, Win)
    /// </summary>
    public int Modifiers { get; set; }

    /// <summary>
    /// 활성화 키를 누른 후 경과 시간 (밀리초)
    /// </summary>
    public long ElapsedMs { get; set; }

    /// <summary>
    /// 롤오버 임계값 (밀리초)
    /// </summary>
    public int RolloverThreshold { get; set; }

    /// <summary>
    /// 롤오버가 감지되었는지 여부
    /// </summary>
    public bool RolloverDetected { get; set; }

    /// <summary>
    /// 이 키가 롤오버 예외 키인지 여부
    /// </summary>
    public bool IsRolloverException { get; set; }

    /// <summary>
    /// 이벤트 타입: "mapped" (매핑됨), "rollover" (롤오버), "unmapped" (매핑 없음)
    /// </summary>
    public string EventType { get; set; } = "";

    /// <summary>
    /// 이전 키 입력과의 시간 간격 (밀리초)
    /// </summary>
    public long? TimeSinceLastKey { get; set; }

    /// <summary>
    /// 이전 입력 키 (분석용)
    /// </summary>
    public string? PreviousKey { get; set; }

    /// <summary>
    /// Training Mode 여부
    /// </summary>
    public bool TrainingMode { get; set; }

    /// <summary>
    /// 잘못된 입력으로 표시됨 (사용자가 명시적으로 표시한 경우)
    /// </summary>
    public bool MarkedAsMistake { get; set; }

    /// <summary>
    /// 현재 프로세스 이름 (컨텍스트 분석용)
    /// </summary>
    public string? ProcessName { get; set; }

    /// <summary>
    /// 세션 ID (프로그램 시작 시 생성)
    /// </summary>
    public string SessionId { get; set; } = "";
}
