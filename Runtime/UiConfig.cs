using System;

namespace Juahn.V2.UiService
{
    /// <summary>
    /// 프리젠터 한 종류의 등록 정보. 서비스는 이 세 값만 알면 화면을 열 수 있다.
    ///
    /// <para>
    /// [주소가 아니라 타입이 열쇠다] 화면을 문자열로 열면 오타가 런타임까지 살아남는다.
    /// 조회 키를 <see cref="PresenterType"/>으로 두어 <c>OpenScreenAsync&lt;LobbyScreen&gt;()</c>가
    /// 컴파일 타임에 검증되게 한다. <see cref="Address"/>는 로더에게만 의미가 있는 값이고,
    /// 프리팹을 직접 참조하는 로더는 이 값을 쓰지 않는다.
    /// </para>
    /// </summary>
    public readonly struct UiConfig
    {
        /// <summary>이 설정이 가리키는 프리젠터 타입. 서비스의 조회 키다.</summary>
        public readonly Type PresenterType;

        /// <summary>로더가 해석하는 위치 문자열. 프리팹 직참조 로더에서는 진단용 이름일 뿐이다.</summary>
        public readonly string Address;

        /// <summary>
        /// 프리팹 루트 <c>Canvas.sortingOrder</c>에 대입할 값.
        /// 프리팹에 Canvas가 없으면 아무 효과가 없다(<see cref="UiService"/>가 경고한다).
        /// </summary>
        public readonly int Layer;

        public UiConfig(Type presenterType, string address, int layer)
        {
            PresenterType = presenterType;
            Address = address;
            Layer = layer;
        }

        public bool IsValid => PresenterType != null;
    }
}
