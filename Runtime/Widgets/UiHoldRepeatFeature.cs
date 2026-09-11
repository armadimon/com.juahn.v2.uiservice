using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>누르는 동안 입력만 반복한다. 명령 승인과 시각 연출은 각 기능의 책임이다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(UiButton))]
    public sealed class UiHoldRepeatFeature : UiFeature
    {
        [SerializeField, Min(.05f)] private float _delay = .4f;
        [SerializeField, Min(.02f)] private float _interval = .1f;
        private UiButton _button;
        private CancellationTokenSource _press;
        protected override void OnInit() => _button = GetComponent<UiButton>();
        protected override void OnShow()
        {
            _button.Pressed += StartRepeat; _button.Released += StopRepeat;
            _button.Canceled += StopRepeat; _button.HoverExited += StopRepeat;
        }
        protected override void OnHide()
        {
            _button.Pressed -= StartRepeat; _button.Released -= StopRepeat;
            _button.Canceled -= StopRepeat; _button.HoverExited -= StopRepeat; StopRepeat();
        }
        private void StartRepeat()
        {
            StopRepeat(); _press = CancellationTokenSource.CreateLinkedTokenSource(BindingToken);
            Repeat(_press.Token).Forget();
        }
        private async UniTaskVoid Repeat(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_delay), ignoreTimeScale: true, cancellationToken: token);
                while (_button != null && _button.CanInteract && _button.Interactable)
                {
                    _button.SuppressNextClick(); _button.InvokeClick();
                    await UniTask.Delay(TimeSpan.FromSeconds(_interval), ignoreTimeScale: true, cancellationToken: token);
                }
            }
            catch (OperationCanceledException) { }
        }
        private void StopRepeat()
        { var press = _press; _press = null; if (press == null) return; press.Cancel(); press.Dispose(); }
    }
}
