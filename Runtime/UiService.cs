using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>화면 탐색·인스턴스 캐시·로드 취소를 소유한다. 자식 위젯은 각 화면이 소유한다.</summary>
    public sealed class UiService : IUiServiceInit
    {
        private readonly Dictionary<Type, UiConfig> _configs = new Dictionary<Type, UiConfig>();
        private readonly Dictionary<Type, UiPresenter> _instances = new Dictionary<Type, UiPresenter>();
        private readonly Dictionary<Type, UniTask<UiPresenter>> _pending = new Dictionary<Type, UniTask<UiPresenter>>();
        private readonly Dictionary<Type, int> _generations = new Dictionary<Type, int>();
        private readonly List<UiPopup> _popupStack = new List<UiPopup>();
        private readonly SemaphoreSlim _navigation = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly CancellationToken _token;
        private IUiAssetLoader _loader;
        private Transform _root;
        private GameObject _ownedRoot;
        private GameObject _staging;
        private UiScreen _currentScreen;
        private bool _initialized;
        private bool _disposed;
        public object Context { get; set; }
        public event Action<UiPresenter> Preparing;
        public UiScreen CurrentScreen => _currentScreen;
        public IReadOnlyList<UiPopup> PopupStack => _popupStack;

        public UiService() { _token = _lifetime.Token; }

        public void Init(UiConfigs configs, Transform uiRoot = null, IUiAssetLoader loaderOverride = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UiService));
            if (_initialized) throw new InvalidOperationException("UiService is already initialized.");
            if (configs == null) throw new ArgumentNullException(nameof(configs));
            _loader = loaderOverride ?? configs.CreateLoader();
            var entries = new List<UiConfig>();
            configs.CollectConfigs(entries);
            foreach (var config in entries)
            {
                if (!config.IsValid) continue;
                if (_configs.ContainsKey(config.PresenterType)) UiServiceLog.WarnDuplicate(config.PresenterType, configs.name);
                _configs[config.PresenterType] = config;
            }
            if (uiRoot == null)
            {
                _ownedRoot = new GameObject("Ui", typeof(RectTransform));
                _root = _ownedRoot.transform;
            }
            else _root = uiRoot;
            _staging = new GameObject("UiLoading");
            _staging.transform.SetParent(_root, false);
            _staging.SetActive(false);
            _initialized = true;
        }

        public bool IsOpen<T>() where T : UiPresenter => Get<T>() != null && Get<T>().IsOpen;
        public T Get<T>() where T : UiPresenter => _instances.TryGetValue(typeof(T), out var value) ? value as T : null;
        public async UniTask<T> PreloadAsync<T>(CancellationToken ct = default) where T : UiPresenter
            => await EnsureInstanceAsync(typeof(T), ct) as T;

        public UniTask<T> OpenScreenAsync<T>(CancellationToken ct = default) where T : UiScreen
            => OpenScreenAsync<T>(null, ct);
        public UniTask<T> OpenScreenAsync<T, TData>(TData data, CancellationToken ct = default)
            where T : UiScreen, IUiPresenterData<TData> where TData : struct
            => OpenScreenAsync<T>(view => view.Data = data, ct);
        public UniTask<T> OpenScreenAsync<T>(Action<T> bind, CancellationToken ct = default) where T : UiScreen
            => Navigate(async token =>
            {
                var next = await EnsureInstanceAsync(typeof(T), token) as T;
                if (next == null) return null;
                Prepare(next, bind);
                if (_currentScreen == next && next.IsOpen) return next;
                await PopAllInternal(token);
                if (_currentScreen != null) await _currentScreen.InternalCloseAsync(token);
                _currentScreen = next;
                RefreshTopmost();
                next.transform.SetAsLastSibling();
                try { await next.InternalOpenAsync(token); }
                catch { if (_currentScreen == next) _currentScreen = null; RefreshTopmost(); throw; }
                return next;
            }, ct);

        public UniTask<T> PushPopupAsync<T>(CancellationToken ct = default) where T : UiPopup
            => PushPopupAsync<T>(null, ct);
        public UniTask<T> PushPopupAsync<T, TData>(TData data, CancellationToken ct = default)
            where T : UiPopup, IUiPresenterData<TData> where TData : struct
            => PushPopupAsync<T>(view => view.Data = data, ct);
        public UniTask<T> PushPopupAsync<T>(Action<T> bind, CancellationToken ct = default) where T : UiPopup
            => Navigate(async token =>
            {
                var popup = await EnsureInstanceAsync(typeof(T), token) as T;
                if (popup == null) return null;
                Prepare(popup, bind);
                _popupStack.Remove(popup);
                var index=_popupStack.FindIndex(existing=>existing!=null && existing.Layer>popup.Layer);
                if(index<0)_popupStack.Add(popup);else _popupStack.Insert(index,popup);
                popup.transform.SetAsLastSibling();
                RefreshTopmost();
                try { await popup.InternalOpenAsync(token); }
                catch { _popupStack.Remove(popup); RefreshTopmost(); throw; }
                return popup;
            }, ct);

        /// <summary>입력을 점유하지 않는 일시 알림. 같은 타입은 재사용하고 최신 데이터를 적용한다.</summary>
        public UniTask<T> OpenToastAsync<T>(Action<T> bind = null, CancellationToken ct = default) where T : UiToast
            => Navigate(async token =>
            {
                var toast = await EnsureInstanceAsync(typeof(T), token) as T;
                if (toast == null) return null;
                Prepare(toast, bind);
                toast.SetNavigationInput(false);
                toast.transform.SetAsLastSibling();
                await toast.InternalOpenAsync(token);
                toast.RefreshPresentation();
                return toast;
            }, ct);

        private void Prepare<T>(T view, Action<T> bind) where T : UiPresenter
        {
            view.BindContext(Context);
            bind?.Invoke(view);
            Preparing?.Invoke(view);
        }

        public UniTask<bool> PopAsync(CancellationToken ct = default)
        {
            var requested = _popupStack.Count == 0 ? null : _popupStack[_popupStack.Count - 1];
            if (requested != null && requested.CloseOnBack && requested.State == UiPresenterState.Opening) requested.CancelTransition();
            return Navigate(async token =>
            {
                if (requested != null && !_popupStack.Contains(requested)) return true;
                var owner = _popupStack.Count > 0 ? (UiPresenter)_popupStack[_popupStack.Count - 1] : _currentScreen;
                if (owner != null)
                {
                    var panels = owner.GetComponentsInChildren<UiPanel>(false);
                    for (var i = panels.Length - 1; i >= 0; i--)
                        if (panels[i].CloseOnBack && panels[i].IsShown) { panels[i].Hide(); return true; }
                }
                if (_popupStack.Count == 0) return requested != null;
                var top = _popupStack[_popupStack.Count - 1];
                if (top != null && !top.CloseOnBack) return false;
                await CloseInternal(top, token);
                return true;
            }, ct);
        }
        public async UniTask PopAllAsync(CancellationToken ct = default)
            => await Navigate(async token => { await PopAllInternal(token); return true; }, ct);
        private async UniTask PopAllInternal(CancellationToken ct)
        {
            while (_popupStack.Count > 0) await CloseInternal(_popupStack[_popupStack.Count - 1], ct);
        }

        public async UniTask CloseAsync(UiPresenter presenter, CancellationToken ct = default)
        {
            if (presenter == null) return;
            if (presenter.State == UiPresenterState.Opening) presenter.CancelTransition();
            await Navigate(async token => { await CloseInternal(presenter, token); return true; }, ct);
        }
        public UniTask CloseAsync<T>(CancellationToken ct = default) where T : UiPresenter => CloseAsync(Get<T>(), ct);
        private async UniTask CloseInternal(UiPresenter presenter, CancellationToken ct)
        {
            try { if (presenter != null) await presenter.InternalCloseAsync(ct); }
            finally
            {
                if (presenter is UiPopup popup) _popupStack.Remove(popup);
                if (presenter == null) _popupStack.RemoveAll(x => x == null);
                if (ReferenceEquals(_currentScreen, presenter)) _currentScreen = null;
                RefreshTopmost();
            }
        }

        private async UniTask<T> Navigate<T>(Func<CancellationToken, UniTask<T>> action, CancellationToken ct)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _token);
            await _navigation.WaitAsync(linked.Token);
            try { linked.Token.ThrowIfCancellationRequested(); return await action(linked.Token); }
            finally { _navigation.Release(); }
        }

        public void Unload<T>() where T : UiPresenter
        {
            var type = typeof(T);
            _generations.TryGetValue(type, out var generation);
            _generations[type] = generation + 1;
            _pending.Remove(type);
            if (!_instances.TryGetValue(type, out var view)) return;
            _instances.Remove(type);
            if (view is UiPopup popup) _popupStack.Remove(popup);
            if (_currentScreen == view) _currentScreen = null;
            Release(view, _loader);
            RefreshTopmost();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _initialized = false;
            _lifetime.Cancel();
            foreach (var view in new List<UiPresenter>(_instances.Values)) Release(view, _loader);
            _instances.Clear(); _pending.Clear(); _popupStack.Clear(); _configs.Clear();
            _currentScreen = null; Context = null; Preparing = null;
            if (_staging != null) UnityEngine.Object.Destroy(_staging);
            if (_ownedRoot != null) UnityEngine.Object.Destroy(_ownedRoot);
            _root = null; _loader = null;
            _lifetime.Dispose();
        }
        private static void Release(UiPresenter view, IUiAssetLoader loader)
        {
            if (view == null) return;
            view.CancelTransition();
            view.EndDisplay();
            loader?.Release(view);
        }

        private async UniTask<UiPresenter> EnsureInstanceAsync(Type type, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!_initialized || _disposed) { UiServiceLog.WarnNotInitialized(type.Name); return null; }
            if (_instances.TryGetValue(type, out var existing) && existing != null) return existing;
            if (!_pending.TryGetValue(type, out var task))
            {
                if (!_configs.TryGetValue(type, out var config)) { UiServiceLog.WarnNotRegistered(type); return null; }
                var source = new UniTaskCompletionSource<UiPresenter>();
                task = source.Task.Preserve();
                _pending[type] = task;
                _generations.TryGetValue(type, out var generation);
                LoadInstance(config, generation, source).Forget();
            }
            return await task.AttachExternalCancellation(ct);
        }
        private async UniTaskVoid LoadInstance(UiConfig config, int generation, UniTaskCompletionSource<UiPresenter> source)
        {
            var loader = _loader;
            UiPresenter instance = null;
            try
            {
                instance = await loader.InstantiateAsync(config, _staging.transform, _token);
                _generations.TryGetValue(config.PresenterType, out var current);
                if (_disposed || generation != current) { Release(instance, loader); source.TrySetCanceled(); return; }
                if (instance == null) { UiServiceLog.WarnLoadFailed(config); source.TrySetResult(null); return; }
                instance.gameObject.SetActive(false);
                instance.transform.SetParent(_root, false);
                instance.Layer = config.Layer;
                if (instance.TryGetComponent<Canvas>(out var canvas)) { canvas.overrideSorting = true; canvas.sortingOrder = config.Layer; }
                instance.Destroyed += OnPresenterDestroyed;
                instance.InternalInitialize(this, config.Layer);
                instance.BindContext(Context);
                _instances[config.PresenterType] = instance;
                source.TrySetResult(instance);
            }
            catch (OperationCanceledException) { Release(instance, loader); source.TrySetCanceled(); }
            catch (Exception error) { Release(instance, loader); source.TrySetException(error); }
            finally
            {
                _generations.TryGetValue(config.PresenterType, out var current);
                if (generation == current) _pending.Remove(config.PresenterType);
            }
        }
        private void OnPresenterDestroyed(UiPresenter view)
        {
            var type = view.GetType();
            if (_instances.TryGetValue(type, out var cached) && ReferenceEquals(cached, view)) _instances.Remove(type);
            if (view is UiPopup popup) _popupStack.Remove(popup);
            if (ReferenceEquals(_currentScreen, view)) _currentScreen = null;
            if (!_disposed) RefreshTopmost();
        }

        private void RefreshTopmost()
        {
            if (_currentScreen != null) _currentScreen.SetNavigationInput(_popupStack.Count == 0);
            for (var i = 0; i < _popupStack.Count; i++)
                if (_popupStack[i] != null) _popupStack[i].SetTopmost(i == _popupStack.Count - 1);
        }
    }
}
