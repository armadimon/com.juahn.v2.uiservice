using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>프리젠터의 수명 단계. 같은 요청이 겹쳐 들어왔을 때 무시할지 판단하는 데 쓴다.</summary>
    public enum UiPresenterState
    {
        Created = 0,
        Initialized = 1,
        Opening = 2,
        Opened = 3,
        Closing = 4,
        Closed = 5,
    }

    /// <summary>화면의 표시·바인딩 수명과 열기/닫기 전환 수명을 분리한다.
    /// 닫기 요청에서 표시 구독을 종료하고, 전환이 끝난 뒤 비활성화한다.</summary>
    [DisallowMultipleComponent]
    public abstract class UiPresenter : UiElement
    {
        private static readonly PresenterFeatureBase[] EmptyFeatures = Array.Empty<PresenterFeatureBase>();
        private static readonly ITransitionFeature[] EmptyTransitions = Array.Empty<ITransitionFeature>();

        internal event Action<UiPresenter> Destroyed;
        private IUiService _service;
        private PresenterFeatureBase[] _features = EmptyFeatures;
        private ITransitionFeature[] _transitions = EmptyTransitions;
        private UniTask[] _transitionBuffer;
        private CancellationTokenSource _transitionCts;
        private bool _navigationInput = true;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("_canvasGroup")] private CanvasGroup _inputGroup;
        private UiPresenterState _state = UiPresenterState.Created;

        /// <summary>현재 수명 단계.</summary>
        public UiPresenterState State => _state;

        /// <summary>열려 있는가(열리는 중 포함).</summary>
        public bool IsOpen => _state == UiPresenterState.Opening || _state == UiPresenterState.Opened;

        /// <summary>프리팹 루트 Canvas에 대입된 정렬 순서. 서비스가 등록 시점에 넣어 준다.</summary>
        public int Layer { get; internal set; }

        /// <summary>이 프리젠터를 소유한 서비스. 서비스 없이 직접 쓰는 경우 null이다.</summary>
        protected IUiService Service => _service;

        // ── 서비스 전용 진입점 ───────────────────────────────────────────────────

        internal void InternalInitialize(IUiService service, int layer)
        {
            if (_state != UiPresenterState.Created)
            {
                return;
            }

            _service = service;
            Layer = layer;
            EnsureInitialized();
            CacheFeatures();

            _state = UiPresenterState.Initialized;
            OnInitialized();

            for (var i = 0; i < _features.Length; i++)
            {
                _features[i].OnPresenterInitialized(this);
            }
        }

        internal async UniTask InternalOpenAsync(CancellationToken ct)
        {
            if (IsOpen) return;
            CancelTransition();
            _transitionCts = CancellationTokenSource.CreateLinkedTokenSource(ct, InstanceToken);
            var token = _transitionCts.Token;
            _state = UiPresenterState.Opening;
            RefreshInput();
            try
            {
                OnOpening();
                NotifyOpening();
                gameObject.SetActive(true);
                BeginDisplay();
                Canvas.ForceUpdateCanvases();
                OnOpened();
                NotifyOpened();
                await WaitForTransitionsAsync(true, token);
                token.ThrowIfCancellationRequested();
                _state = UiPresenterState.Opened;
                RefreshInput();
                OnOpenTransitionCompleted();
            }
            catch
            {
                ForceClosed();
                throw;
            }
        }

        internal async UniTask InternalCloseAsync(CancellationToken ct)
        {
            if (!IsOpen && _state != UiPresenterState.Closing) return;
            CancelTransition();
            _transitionCts = CancellationTokenSource.CreateLinkedTokenSource(ct, InstanceToken);
            var token = _transitionCts.Token;
            _state = UiPresenterState.Closing;
            EndDisplay();
            RefreshInput();
            try
            {
                OnClosing();
                NotifyClosing();
                await WaitForTransitionsAsync(false, token);
            }
            finally { ForceClosed(); }
        }

        internal void CancelTransition() => Cancel(ref _transitionCts);

        internal void SetNavigationInput(bool allowed)
        {
            _navigationInput = allowed;
            RefreshInput();
        }

        private void RefreshInput()
        {
            var allowed = _navigationInput && _state == UiPresenterState.Opened;
            SetInputAllowed(allowed);
            if (_inputGroup == null) _inputGroup = GetComponent<CanvasGroup>();
            // Unity's missing-component sentinel is not CLR null; do not use ?? here.
            if (_inputGroup == null) _inputGroup = gameObject.AddComponent<CanvasGroup>();
            _inputGroup.interactable = allowed;
            // 닫힘 중에도 최상단 덮개가 레이캐스트를 받아 아래 화면으로 전달하지 않는다.
            _inputGroup.blocksRaycasts = _navigationInput && (IsOpen || _state == UiPresenterState.Closing);
        }

        private void ForceClosed()
        {
            if (this == null) return;
            var notify = _state != UiPresenterState.Closed;
            _state = UiPresenterState.Closed;
            EndDisplay();
            RefreshInput();
            if (notify) { OnClosed(); NotifyClosed(); }
            gameObject.SetActive(false);
        }

        protected virtual void OnDisable()
        {
            CancelTransition();
            EndDisplay();
            if (IsOpen || _state == UiPresenterState.Closing) ForceClosed();
        }

        // ── 파생 훅 ─────────────────────────────────────────────────────────────

        /// <summary>최초 1회. 자식 참조 캐싱 등 일회성 준비.</summary>
        protected virtual void OnInitialized() { }

        /// <summary>열기 직전. GameObject는 아직 비활성이다.</summary>
        protected virtual void OnOpening() { }

        /// <summary>활성화 직후. 상태를 읽어 표시를 덮어쓴다(증분 갱신에 의존하지 않는다).</summary>
        protected virtual void OnOpened() { }

        /// <summary>열림 연출까지 끝난 뒤. 연출이 없으면 <c>OnOpened</c> 같은 프레임에 온다.</summary>
        protected virtual void OnOpenTransitionCompleted() { }

        /// <summary>닫기 직전. 아직 활성이다.</summary>
        protected virtual void OnClosing() { }

        /// <summary>닫힘 연출까지 끝난 뒤, 비활성화 직전.</summary>
        protected virtual void OnClosed() { }

        /// <summary>자기 자신을 닫아 달라고 서비스에 요청한다. 닫기 버튼이 부르는 것.</summary>
        protected void RequestClose()
        {
            if (_service == null)
            {
                InternalCloseAsync(CancellationToken.None).Forget();
                return;
            }

            _service.CloseAsync(this).Forget();
        }

        // ── 내부 ────────────────────────────────────────────────────────────────

        private void CacheFeatures()
        {
            // 최초 1회만 훑는다. 자식은 보지 않는다 - 중첩 프리젠터의 피처를 삼키지 않기 위해.
            var found = GetComponents<PresenterFeatureBase>();
            if (found == null || found.Length == 0)
            {
                _features = EmptyFeatures;
                _transitions = EmptyTransitions;
                return;
            }

            _features = found;

            var transitionCount = 0;
            for (var i = 0; i < found.Length; i++)
            {
                if (found[i] is ITransitionFeature)
                {
                    transitionCount++;
                }
            }

            if (transitionCount == 0)
            {
                _transitions = EmptyTransitions;
                return;
            }

            _transitions = new ITransitionFeature[transitionCount];
            var cursor = 0;
            for (var i = 0; i < found.Length; i++)
            {
                if (found[i] is ITransitionFeature transition)
                {
                    _transitions[cursor++] = transition;
                }
            }

            if (transitionCount > 1)
            {
                _transitionBuffer = new UniTask[transitionCount];
            }
        }

        private async UniTask WaitForTransitionsAsync(bool opening, CancellationToken ct)
        {
            if (_transitions.Length == 0)
            {
                return; // 연출 피처가 없으면 대기 기계 자체를 만들지 않는다.
            }

            if (_transitions.Length == 1)
            {
                await Attach(opening ? _transitions[0].OpenTransitionTask : _transitions[0].CloseTransitionTask, ct);
                return;
            }

            for (var i = 0; i < _transitions.Length; i++)
            {
                _transitionBuffer[i] = opening
                    ? _transitions[i].OpenTransitionTask
                    : _transitions[i].CloseTransitionTask;
            }

            await Attach(UniTask.WhenAll(_transitionBuffer), ct);
        }

        private static UniTask Attach(UniTask task, CancellationToken ct)
        {
            return ct.CanBeCanceled ? task.AttachExternalCancellation(ct) : task;
        }

        private void NotifyOpening()
        {
            for (var i = 0; i < _features.Length; i++)
            {
                _features[i].OnPresenterOpening();
            }
        }

        private void NotifyOpened()
        {
            for (var i = 0; i < _features.Length; i++)
            {
                _features[i].OnPresenterOpened();
            }
        }

        private void NotifyClosing()
        {
            for (var i = 0; i < _features.Length; i++)
            {
                _features[i].OnPresenterClosing();
            }
        }

        private void NotifyClosed()
        {
            for (var i = 0; i < _features.Length; i++)
            {
                _features[i].OnPresenterClosed();
            }
        }

        protected override void OnDestroy()
        {
            CancelTransition();
            base.OnDestroy();
            Destroyed?.Invoke(this);
            Destroyed = null;
        }
    }
}
