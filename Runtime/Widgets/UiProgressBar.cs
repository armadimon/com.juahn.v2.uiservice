using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Juahn.V2.UiService
{
    /// <summary>폭으로 채우는 공통 게이지. 보간은 ValueRequested를 받는 모션 피처가 담당한다.</summary>
    [AddComponentMenu("UI (Project)/UIProgressBar")]
    public class UiProgressBar : UiWidget
    {
        [Header("진행바")]
        [SerializeField] private Image _IMG_Background;
        [SerializeField] private Image _IMG_Fill;
        [SerializeField] private TMP_Text _TMP_Progress;
        [SerializeField] private float _animDuration = 0.3f;
        private float _width;
        private float _max;
        private float _value;
        private string _suffix = string.Empty;
        private float _target;
        public float DisplayedValue => _value;
        public float TargetValue => _target;
        public event Action<float, float> ValueRequested;

        /// <summary>진행바가 가득 찼을 때(또는 목표 도달) 발화. ShinyProgressBar 등 파생 연출의 트리거.</summary>
        public event Action Completed;

        protected override void OnInit() => EnsureWidth();
        protected virtual void OnRectTransformDimensionsChange()
        {
            _width = 0f;
            if (IsInitialized) Apply(_value);
        }

        /// <summary>
        /// 트랙 폭(= 배경 이미지의 실제 가로 크기)을 확정한다.
        /// <c>sizeDelta.x</c>가 아니라 <c>rect.width</c>를 읽는 이유: 배경을 부모에 stretch(anchorMin.x=0, anchorMax.x=1)로
        /// 깔면 sizeDelta.x는 0이고, 그 값을 폭으로 쓰면 게이지가 영원히 0폭이 된다. 고정폭 배경에서는 두 값이 같으므로
        /// 기존 사용처의 동작은 그대로다.
        ///
        /// Awake 시점에는 레이아웃(LayoutGroup 하위 등)이 아직 확정되지 않아 0이 나올 수 있어, 0이면 값을 그리는
        /// 시점에 다시 측정한다 — 한 번 유효한 폭을 얻으면 그대로 캐시한다.
        /// </summary>
        private void EnsureWidth()
        {
            if (_width > 0f || _IMG_Background == null)
            {
                return;
            }

            _width = ((RectTransform)_IMG_Background.transform).rect.width;
        }

        protected override void OnShow() => Apply(_target);


        public void Init(float value = 0) => Init(_max, value);

        public void Init(float max, float value = 0)
        {
            _max = max;
            _target = value;
            SetValue(value, 0f);
        }

        public void ToggleText(bool enable)
        {
            if (_TMP_Progress != null)
            {
                _TMP_Progress.gameObject.SetActive(enable);
            }
        }

        public void SetSuffix(string suffix)
        {
            _suffix = suffix ?? string.Empty;
            RefreshText();
        }

        public void SetValue(float value) => SetValue(value, _animDuration);

        public void SetValue(float value, float duration)
        {
            _target = value;
            if (ValueRequested != null && IsShown) ValueRequested(value, duration);
            else Apply(value);
        }

        public void SetDisplayedValue(float value) => Apply(value);

        private void Apply(float value)
        {
            EnsureWidth(); // Awake가 레이아웃 전이라 0을 잡았을 수 있다.
            var previous = _value;
            _value = value;
            float t = _max <= 0f ? 0f : Mathf.Clamp01(value / _max);

            if (_IMG_Fill != null)
            {
                // 진행도 0이면 fill을 숨긴다(9-slice 캡 폭 때문에 0에서도 바가 보이는 것 방지).
                _IMG_Fill.enabled = t > 0f;

                float fillWidth = _width * t;
                if (_IMG_Fill.sprite != null)
                {
                    // 9-slice 캡(곡률 영역)이 찌그러지지 않도록 좌우 보더 합~전체폭을 선형 보간.
                    var border = _IMG_Fill.sprite.border;
                    fillWidth = Mathf.Lerp(border.x + border.z, _width, t);
                }

                SetWidth(_IMG_Fill, fillWidth);
            }

            RefreshText();

            if (previous < _max && _max - value <= float.Epsilon)
            {
                Completed?.Invoke();
            }
        }

        private void RefreshText()
        {
            if (_TMP_Progress != null)
            {
                _TMP_Progress.SetText($"{Mathf.RoundToInt(_value)}/{Mathf.RoundToInt(_max)}{_suffix}");
            }
        }

        private static void SetWidth(Image img, float width)
        {
            if (img == null)
            {
                return;
            }

            var rect = (RectTransform)img.transform;
            var size = rect.sizeDelta;
            size.x = width;
            rect.sizeDelta = size;
        }
    }
}
