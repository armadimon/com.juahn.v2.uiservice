namespace Juahn.V2.UiService
{
    /// <summary>
    /// 열 때 값을 받는 프리젠터의 계약.
    ///
    /// <para>
    /// [struct로 제약하는 이유] 화면에 넘기는 값은 대개 몇 개의 숫자와 식별자다.
    /// 클래스로 두면 열 때마다 힙 할당이 생기고, 호출자가 같은 인스턴스를 재사용하면
    /// 화면이 모르는 사이에 값이 바뀐다. 값 타입이면 넘기는 순간 복사되어 그런 일이 없다.
    /// </para>
    /// </summary>
    public interface IUiPresenterData<TData> where TData : struct
    {
        /// <summary>표시할 값. 대입이 곧 갱신 신호다.</summary>
        TData Data { get; set; }
    }
}
