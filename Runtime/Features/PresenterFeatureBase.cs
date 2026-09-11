using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>
    /// 프리젠터에 곁들이는 부가 관심사를 컴포넌트로 떼어내는 베이스.
    /// 연출·세이프에어리어·사운드처럼 "화면마다 있을 수도 없을 수도 있는" 것을
    /// 프리젠터 코드가 아니라 프리팹 조립으로 정한다.
    ///
    /// <para>
    /// [같은 GameObject만 모은다] 프리젠터는 자기 GameObject의 컴포넌트만 피처로 인식한다.
    /// 자식까지 훑으면 중첩된 다른 프리젠터의 피처를 삼키게 되고, 어느 화면이 무엇을
    /// 소유하는지가 계층 깊이에 따라 달라진다.
    /// </para>
    ///
    /// </summary>
    public abstract class PresenterFeatureBase : UiFeature
    {
        /// <summary>이 피처가 붙은 프리젠터. <see cref="OnPresenterInitialized"/> 이후에 유효하다.</summary>
        protected UiPresenter Presenter { get; private set; }

        /// <summary>프리젠터가 최초 1회 초기화될 때. 참조 캐싱 자리다.</summary>
        public virtual void OnPresenterInitialized(UiPresenter presenter)
        {
            Presenter = presenter;
        }

        /// <summary>
        /// 열기 시작. <b>이 시점의 GameObject는 아직 비활성이다</b> —
        /// 활성 상태를 요구하는 호출(코루틴 시작, 연출 발사)은 여기서 하면 조용히 무시된다.
        /// </summary>
        public virtual void OnPresenterOpening() { }

        /// <summary>
        /// 활성화 직후. 연출을 발사할 자리다.
        /// <see cref="ITransitionFeature.OpenTransitionTask"/>는 이 통지 안에서 만들어져야 한다.
        /// </summary>
        public virtual void OnPresenterOpened() { }

        /// <summary>
        /// 닫기 시작. 닫힘 연출을 발사할 자리이며,
        /// 프리젠터는 이 통지 직후 <see cref="ITransitionFeature.CloseTransitionTask"/>를 기다린다.
        /// </summary>
        public virtual void OnPresenterClosing() { }

        /// <summary>닫힘 연출까지 끝난 뒤, 비활성화 직전.</summary>
        public virtual void OnPresenterClosed() { }
    }
}
