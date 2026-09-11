using System;
using UnityEngine;
using UnityEngine.UI;

namespace Juahn.V2.UiService
{
    /// <summary>
    /// 체크 on/off 토글 위젯. (ForgeDefense UIToggle 이식)
    /// 입력은 자체 구현하지 않고 자식 <see cref="UiButton"/>의 <see cref="UiButton.Clicked"/>를 받아 상태를 뒤집는다
    /// (uGUI Toggle 미사용 — 05-ui.md의 "위젯도 UiButton 계보로 묶는다"). 상태만 소유하는 humble 위젯이다.
    ///
    /// [표시 모드]
    /// 켬/끔을 그림으로 보여주는 방식은 아트마다 갈린다. 체크마크 하나를 껐다 켜는 것과, on 전용 그림과
    /// off 전용 그림을 번갈아 보여주는 것은 필요한 참조가 다르므로 <see cref="DisplayMode"/>로 분기한다.
    /// 상태(<see cref="IsOn"/>)는 모드와 무관하게 하나이고, 모드는 그 상태를 무엇으로 그릴지만 정한다.
    ///
    /// [상태의 정의처]
    /// 기본은 토글 자신이다 — 누르면 뒤집는다(자동 판매·자동 플레이 같은 설정값). 그런데 on/off의 정의처가 밖에 있는 자리도 있다
    /// (장착 여부 = sim, 선택 탭 = 탭 바). 거기서 토글이 클릭으로 스스로 뒤집으면 정의처와 그림이 어긋나므로
    /// <see cref="_toggleOnClick"/>을 끈다 — 버튼은 그대로 눌리고(호스트가 그 클릭을 받아 갈래를 고른다),
    /// 상태는 호스트가 <see cref="IsOn"/>으로만 지정한다. <see cref="Interactable"/>과 다르다: 그건 버튼 자체를 막는다.
    ///
    /// [연출 위임]
    /// 그림 한 장을 껐다 켜는 것으로 표현할 수 없는 연출(손잡이 슬라이드 등)은 이 위젯에 넣지 않는다.
    /// <see cref="DisplayMode.External"/>로 두고 <see cref="UIToggleModule"/> 파생 모듈을 붙인다
    /// (UiButton ↔ UiButtonModule과 같은 분리). 그래서 연출이 늘어도 이 파일의 <see cref="ApplyVisual"/>은
    /// 커지지 않는다.
    /// </summary>
    [AddComponentMenu("UI (Project)/UIToggle")]
    public class UiToggle : UiWidget
    {
        /// <summary>on/off 상태를 화면에 그리는 방식.</summary>
        public enum DisplayMode
        {
            /// <summary>체크마크 한 장을 on일 때만 보여준다.</summary>
            CheckMark = 0,

            /// <summary>on 그림과 off 그림을 서로 배타적으로 보여준다.</summary>
            OnOffImage = 1,

            /// <summary>이 위젯은 아무것도 그리지 않고, 표시를 <see cref="UIToggleModule"/> 파생 모듈에 맡긴다.</summary>
            External = 2,
        }

        [Header("참조")]
        [SerializeField] private UiButton _BTN_Body;
        [SerializeField] private GameObject _IMG_Cover;

        [Header("참조 - 체크마크 모드")]
        [SerializeField] private Image _IMG_CheckMark;

        [Header("참조 - On/Off 이미지 모드")]
        [Tooltip("on일 때만 켜지는 오브젝트. 이미지 한 장이 아니라 라벨까지 묶은 그룹을 넣어도 된다.")]
        [SerializeField] private GameObject _IMG_On;
        [Tooltip("off일 때만 켜지는 오브젝트.")]
        [SerializeField] private GameObject _IMG_Off;

        [Header("설정")]
        [SerializeField] private DisplayMode _displayMode = DisplayMode.CheckMark;
        [SerializeField] private bool _interactable = true;
        [SerializeField] private bool _isOn = false;

        [Tooltip("누르면 스스로 on/off를 뒤집는다. 끄면 버튼은 눌리되 상태는 바뀌지 않는다 — " +
                 "정의처가 밖에 있는 자리(장착 여부, 선택 탭)에서 호스트가 IsOn으로만 지정할 때 끈다.")]
        [SerializeField] private bool _toggleOnClick = true;

        /// <summary>클릭으로 상태가 뒤집혔을 때 발화. 인자는 새 on 여부. 논리(커맨드) 콜백용.</summary>
        public event Action<bool> Toggled;

        /// <summary>
        /// 상태가 바뀔 때마다 발화하는 연출용 채널. <see cref="Toggled"/>와 달리 코드가 <see cref="IsOn"/>에
        /// 값을 넣은 경우에도 발화한다. 두 번째 인자는 "연출을 재생해도 되는가" — 클릭은 true,
        /// 코드가 상태를 맞춰 넣는 초기화(팝업 열기 등)는 false다. 초기화에서 스위치가 스르륵 미끄러지면
        /// 안 되므로 이 구분이 필요하다.
        /// </summary>
        public event Action<bool, bool> StateChanged;

        public bool Interactable
        {
            get => _interactable;
            set
            {
                _interactable = value;
                if (_BTN_Body != null)
                {
                    _BTN_Body.Interactable = value;
                }

                if (_IMG_Cover != null)
                {
                    _IMG_Cover.SetActive(!_interactable);
                }
            }
        }

        /// <summary>현재 on 여부. 대입은 "코드가 상태를 맞춰 넣는" 경로이므로 연출 없이 즉시 반영된다.</summary>
        public bool IsOn
        {
            get => _isOn;
            set => SetOn(value, false);
        }

        /// <summary>표시 모드. 바꾸면 현재 상태를 새 모드로 다시 그린다.</summary>
        public DisplayMode Mode
        {
            get => _displayMode;
            set
            {
                _displayMode = value;
                ApplyVisual();
            }
        }

        // 중첩 위젯이라 화면 단위 Show()가 아닌 Unity 수명(Awake/OnDestroy)으로 배선한다(RedDotView·UiButtonModule 관례).
        private void Awake()
        {
            IsOn = _isOn;             // 인스펙터 초기값을 표시에 반영.
            Interactable = _interactable;

            if (_BTN_Body != null)
            {
                _BTN_Body.AddClickListener(Toggle);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_BTN_Body != null)
            {
                _BTN_Body.RemoveClickListener(Toggle);
            }
        }

        private void Toggle()
        {
            if (!_toggleOnClick)
            {
                return; // 상태는 호스트의 것 — 클릭은 버튼 구독자(호스트)가 받는다.
            }

            SetOn(!_isOn, true);
            Toggled?.Invoke(_isOn);
        }

        private void SetOn(bool value, bool animate)
        {
            _isOn = value;
            ApplyVisual();
            StateChanged?.Invoke(_isOn, animate);
        }

        /// <summary>
        /// 현재 상태를 현재 모드로 그린다. 모드가 쓰지 않는 참조는 꺼두므로, 런타임에 모드를 바꿔도
        /// 이전 모드의 그림이 남지 않는다. 모드를 더할 때는 각 참조의 표시 조건 한 줄만 늘리면 된다.
        /// </summary>
        private void ApplyVisual()
        {
            bool showCheckMark = _displayMode == DisplayMode.CheckMark && _isOn;
            bool showOnImage = _displayMode == DisplayMode.OnOffImage && _isOn;
            bool showOffImage = _displayMode == DisplayMode.OnOffImage && !_isOn;

            if (_IMG_CheckMark != null)
            {
                _IMG_CheckMark.enabled = showCheckMark;
            }

            if (_IMG_On != null)
            {
                _IMG_On.SetActive(showOnImage);
            }

            if (_IMG_Off != null)
            {
                _IMG_Off.SetActive(showOffImage);
            }
        }
    }
}
