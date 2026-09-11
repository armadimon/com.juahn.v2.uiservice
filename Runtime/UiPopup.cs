using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>
    /// 화면 위에 쌓이는 팝업. 서비스가 스택으로 관리하며, 뒤로가기는 최상단 하나만 걷어낸다.
    ///
    /// <para>
    /// [최상단만 입력을 받는다] 팝업이 겹쳤을 때 아래 팝업의 버튼이 눌리면 사용자는 자기가
    /// 무엇을 눌렀는지 알 수 없다. 서비스가 스택이 바뀔 때마다
    /// <see cref="SetTopmost"/>를 불러 최상단이 아닌 팝업의 입력을 끈다.
    /// </para>
    ///
    /// <para>
    /// [입력 차단은 CanvasGroup으로 한다] 개별 <c>Graphic.raycastTarget</c>을 순회하며 끄면
    /// 원래 꺼져 있던 것까지 되살리게 되고, 캔버스도 그만큼 더러워진다.
    /// <see cref="CanvasGroup"/> 한 곳만 만지면 자식 전체가 한 번에 처리된다.
    /// </para>
    /// </summary>
    public abstract class UiPopup : UiPresenter
    {
        [Header("Popup")]
        [Tooltip("뒤로가기(백버튼)나 딤 탭으로 이 팝업이 닫히는가. 강제 선택 팝업은 꺼 둔다.")]
        [SerializeField] private bool _closeOnBack = true;

        private bool _isTopmost = true;

        /// <summary>뒤로가기로 닫을 수 있는 팝업인가.</summary>
        public bool CloseOnBack => _closeOnBack;

        /// <summary>지금 스택 최상단인가.</summary>
        public bool IsTopmost => _isTopmost;

        /// <summary>
        /// 스택에서의 위치가 바뀌었을 때 서비스가 부른다.
        /// 최상단이 아니면 입력을 끊되, 보이는 것은 그대로 둔다(겹친 팝업이 보여야 맥락이 읽힌다).
        /// </summary>
        internal void SetTopmost(bool topmost)
        {
            _isTopmost = topmost;

            SetNavigationInput(topmost);

            OnTopmostChanged(topmost);
        }

        /// <summary>스택 위치가 바뀌었을 때. 흐림 처리 같은 추가 표현이 필요하면 override.</summary>
        protected virtual void OnTopmostChanged(bool topmost) { }


    }

    /// <summary>열 때 값을 받는 팝업. 보상 결과처럼 "값을 넣고 여는" 화면에 쓴다.</summary>
    public abstract class UiPopup<TData> : UiPopup, IUiPresenterData<TData>
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
