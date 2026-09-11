using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Juahn.V2.UiService
{
    /// <summary>
    /// 아이콘·프레임·레벨을 표시하는 재사용 슬롯 위젯의 공통 베이스. (ForgeDefense SlotView 이식)
    /// 장비·스킬·유닛 등 "칸에 무언가가 담긴" 모든 UI가 이 위젯을 시각 요소로 쓴다.
    ///
    /// [humble View — 이 클래스의 핵심 규약]
    /// 이 위젯은 <b>sim(GameSim)을 모른다.</b> 어떤 상태를 어떻게 보여줄지는 <see cref="SlotDispatcher"/>가
    /// 결정하고, 이 위젯은 넘겨받은 스프라이트/숫자를 화면에 꽂기만 한다(05-ui.md "월드 연출 뷰"의 humble 규약).
    /// 그래서 상태를 어디서 어떻게 받아오든 이 위젯은 손댈 필요가 없다.
    ///
    /// [베이스 클래스 수정 경고]
    /// 이 파일은 베이스 코드다. 슬롯 종류가 부가 시각(쿨타임·잠금·선택표식 등)을 필요로 하면
    /// 이 위젯을 <b>수정하지 말고</b>, 그 시각을 다루는 디스패처(<see cref="SlotDispatcher"/> 파생)에 필드로 두거나
    /// 이 클래스를 상속한 별개 슬롯 위젯을 만든다. (CLAUDE.md "베이스 클래스 수정 규칙")
    /// </summary>
    [AddComponentMenu("UI (Project)/SlotView")]
    public class UiSlot : UiWidget
    {
        [Tooltip("담긴 대상의 아이콘. 비우면 슬롯은 아이콘 없이 프레임만 보인다.")]
        [SerializeField] private Image _icon;

        [Tooltip("등급/종류 프레임 이미지.")]
        [SerializeField] private Image _frame;

        [Tooltip("레벨 텍스트. 빈 슬롯이면 숨긴다.")]
        [SerializeField] private TMP_Text _level;

        [Tooltip("레벨이 아닌 수량을 표시하는 자리(선택 — 보상 개수 등). 비워두면 이 슬롯은 수량 표시를 쓰지 않는다.")]
        [SerializeField] private TMP_Text _TMP_Amount;

        /// <summary>아이콘 스프라이트를 세팅한다. null이면 아이콘을 숨긴다(빈 칸 표현).</summary>
        public void SetIcon(Sprite sprite)
        {
            if (_icon == null)
            {
                return;
            }

            _icon.sprite = sprite;
            _icon.enabled = sprite != null;
        }

        /// <summary>프레임 스프라이트를 세팅한다.</summary>
        public void SetFrame(Sprite sprite)
        {
            if (_frame != null)
            {
                _frame.sprite = sprite;
            }
        }

        /// <summary>레벨을 표시한다. 텍스트는 "Lv.N" 형식.</summary>
        public void SetLevel(int level)
        {
            if (_level == null)
            {
                return;
            }

            _level.gameObject.SetActive(true);
            _level.text = $"Lv.{level}";
        }

        /// <summary>레벨 표시를 숨긴다(빈 칸 등 레벨이 의미 없는 상태).</summary>
        public void HideLevel()
        {
            if (_level != null)
            {
                _level.gameObject.SetActive(false);
            }
        }

        /// <summary>수량을 그대로 표시한다("Lv." 접두사 없이 — <see cref="SetLevel"/>과 별개 자리다).
        /// <see cref="_TMP_Amount"/>가 배선되지 않았으면 아무 일도 하지 않는다.</summary>
        /// <summary>
        /// 수량 자리에 완성된 문자열을 꽂는다. long으로 담을 수 없는 수량(<c>BigNumber</c> 등)을 쓰는 파생 슬롯이
        /// 자기 필드를 따로 두지 않고 이 자리를 쓰게 하려는 통로다 — 같은 이름의 직렬화 필드를 파생이 다시 선언하면
        /// 유니티가 둘 중 하나만 직렬화해 배선이 조용히 유실된다.
        /// </summary>
        protected void SetAmountText(string text)
        {
            if (_TMP_Amount == null)
            {
                return;
            }

            _TMP_Amount.gameObject.SetActive(true);
            _TMP_Amount.text = text;
        }

        /// <summary>빈 칸으로 되돌린다: 아이콘 제거 + 레벨 숨김. 프레임은 디스패처가 별도로 지정한다.</summary>
        public void Clear()
        {
            SetIcon(null);
            HideLevel();
        }
    }
}
