using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Juahn.V2.UiService
{
    /// <summary>입력만 소유하는 공통 버튼. 연출과 게임 명령은 로컬 기능이 연결한다.</summary>
    public class UiButton : UiWidget, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform _hitArea;
        [SerializeField] private bool _interactable = true;
        private readonly List<CanvasGroup> _groups = new List<CanvasGroup>();
        private bool _pressed;
        private bool _suppressNextClick;
        private int _pointerId;
        public event Action Pressed;
        public event Action Released;
        public event Action Canceled;
        public event Action Clicked;
        public event Action HoverEntered;
        public event Action HoverExited;
        public bool Interactable
        {
            get => _interactable;
            set { _interactable = value; if (!value) CancelPointer(); }
        }

        protected virtual void Awake()
        {
            if (_hitArea == null) _hitArea = transform as RectTransform;
            InputCanceled += CancelPointer;
        }
        public void AddClickListener(Action listener) => Clicked += listener;
        public void RemoveClickListener(Action listener) => Clicked -= listener;
        public void SuppressNextClick() => _suppressNextClick = true;
        public void InvokeClick() { if (AcceptsInput()) Clicked?.Invoke(); }
        private bool AcceptsInput()
        {
            if (!_interactable || !CanInteract) return false;
            for (var current = transform; current != null; current = current.parent)
            {
                _groups.Clear(); current.GetComponents(_groups);
                var stop = false;
                foreach (var group in _groups)
                {
                    if (!group.enabled) continue;
                    if (!group.interactable || !group.blocksRaycasts) return false;
                    stop |= group.ignoreParentGroups;
                }
                if (stop) break;
            }
            return true;
        }
        public void OnPointerDown(PointerEventData data)
        {
            if (_pressed || data.button != PointerEventData.InputButton.Left || !AcceptsInput()) return;
            if (_hitArea == null || !RectTransformUtility.RectangleContainsScreenPoint(_hitArea, data.position, data.pressEventCamera)) return;
            _suppressNextClick = false; _pointerId = data.pointerId; _pressed = true;
            Pressed?.Invoke();
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (!_pressed || _pointerId != data.pointerId) return;
            _pressed = false;
            Released?.Invoke();
            var suppress = _suppressNextClick; _suppressNextClick = false;
            if (!suppress && !data.dragging && AcceptsInput() && RectTransformUtility.RectangleContainsScreenPoint(_hitArea, data.position, data.pressEventCamera))
                Clicked?.Invoke();
        }
        public void OnPointerEnter(PointerEventData data) { if (AcceptsInput()) HoverEntered?.Invoke(); }
        public void OnPointerExit(PointerEventData data) => HoverExited?.Invoke();
        public void CancelPointer()
        {
            _suppressNextClick = false;
            if (!_pressed) return;
            _pressed = false;
            Released?.Invoke(); Canceled?.Invoke();
        }
        protected override void OnDisable() { CancelPointer(); base.OnDisable(); }
        private void OnApplicationFocus(bool focus) { if (!focus) CancelPointer(); }
        protected override void OnDestroy() { InputCanceled -= CancelPointer; base.OnDestroy(); }
    }
}
