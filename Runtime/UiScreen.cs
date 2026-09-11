namespace Juahn.V2.UiService
{
    /// <summary>
    /// 동시에 하나만 활성인 화면. 새 화면을 열면 서비스가 이전 화면을 닫는다.
    /// 로비, 서브 콘텐츠 인게임처럼 "지금 어디에 있는가"를 정하는 것들이 여기에 해당한다.
    ///
    /// <para>
    /// 화면 위에 겹쳐 뜨는 것은 <see cref="UiPopup"/>이다. 둘을 나눈 이유는 닫기 규칙이
    /// 다르기 때문이다 — 화면은 교체되고, 팝업은 쌓였다가 위에서부터 걷힌다.
    /// </para>
    /// </summary>
    public abstract class UiScreen : UiPresenter
    {
    }

    /// <summary>열 때 값을 받는 화면. <see cref="IUiPresenterData{TData}.Data"/> 대입이 갱신을 부른다.</summary>
    public abstract class UiScreen<TData> : UiScreen, IUiPresenterData<TData>
        where TData : struct
    {
        private TData _data;

        public TData Data
        {
            get => _data;
            set
            {
                _data = value;
                OnSetData();
            }
        }

        /// <summary>데이터가 들어온 직후. 열리기 전에 불릴 수 있으므로 표시만 갱신한다.</summary>
        protected virtual void OnSetData() { }
    }
}
