namespace TouchCursor.Support.Local.Helpers;

public interface IKeyMappingService
{
    /// <summary>
    /// 수정자 상태 업데이트 - 주입된 키를 포함한 모든 키에 대해 호출되어야 함
    /// </summary>
    void UpdateModifierState(int vkCode, bool isKeyDown, bool isKeyUp);

    /// <summary>
    /// 키 이벤트를 처리하고 차단 또는 재매핑 여부 결정
    /// </summary>
    /// <returns>키 이벤트를 차단해야 하면 True, 통과시키려면 false</returns>
    bool ProcessKey(int vkCode, bool isKeyDown, bool isKeyUp);

    /// <summary>
    /// 상태 초기화
    /// </summary>
    void Reset();

    /// <summary>
    /// 키 전송 요청 이벤트
    /// </summary>
    event Action<int, bool, int>? SendKeyRequested;

    /// <summary>
    /// 활성화 상태 변경 이벤트 (activationKey, isActive)
    /// </summary>
    event Action<int, bool>? ActivationStateChanged;
}
