namespace Juahn.V2.UiService
{
    /// <summary>서비스가 생성·정리하는 비모달 알림. 재표시도 같은 데이터 갱신 계약을 따른다.</summary>
    public abstract class UiToast : UiPresenter
    {
        internal void RefreshPresentation() => OnPresented();
        protected virtual void OnPresented() { }
    }
}
