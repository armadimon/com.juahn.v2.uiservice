using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>화면과 위젯이 공유하는 초기화·소유자·표시·바인딩 수명.</summary>
    public abstract class UiElement : MonoBehaviour
    {
        private readonly List<UiElement> _children = new List<UiElement>();
        private readonly List<UiFeature> _features = new List<UiFeature>();
        private CancellationTokenSource _instance;
        private CancellationTokenSource _display;
        private CancellationTokenSource _binding;
        private bool _initialized;
        private bool _shown;
        private bool _inputAllowed = true;
        private bool _destroyed;
        public UiElement Owner { get; private set; }
        public object Context { get; private set; }
        public bool IsShown => _shown && isActiveAndEnabled;
        public bool IsInitialized => _initialized;
        public bool CanInteract => IsShown && _inputAllowed && (Owner == null || Owner.CanInteract);
        public CancellationToken LifetimeToken => _display?.Token ?? new CancellationToken(true);
        public CancellationToken BindingToken => _binding?.Token ?? new CancellationToken(true);
        public CancellationToken InstanceToken => _instance?.Token ?? new CancellationToken(true);
        public event Action InputCanceled;

        public void EnsureInitialized()
        {
            if (_initialized || _destroyed) return;
            _initialized = true;
            _instance = new CancellationTokenSource();
            AttachToParent();
            foreach (var feature in GetComponentsInChildren<UiFeature>(true))
            {
                var nearest = feature.GetComponentInParent<UiElement>(true);
                if (nearest == this) RegisterFeature(feature);
            }
            OnInit();
            foreach (var feature in _features.ToArray()) feature.Initialize(this);
            RegisterChildren();
        }

        internal void RegisterFeature(UiFeature feature)
        {
            if (!_features.Contains(feature)) _features.Add(feature);
        }

        internal void UnregisterFeature(UiFeature feature) => _features.Remove(feature);

        public void BindContext(object context)
        {
            EnsureInitialized();
            Context = context;
            Cancel(ref _binding);
            if (_shown) _binding = new CancellationTokenSource();
            foreach (var feature in _features.ToArray()) feature.ContextChanged();
            foreach (var child in _children.ToArray()) if (child != null) child.BindContext(context);
        }

        public void RegisterChildren()
        {
            foreach (var child in GetComponentsInChildren<UiElement>(true))
            {
                if (child == this) continue;
                child.AttachToParent();
                if (child.Owner != this) continue;
                child.EnsureInitialized();
                if (!ReferenceEquals(child.Context, Context)) child.BindContext(Context);
                if (_shown && child.isActiveAndEnabled) child.BeginDisplay();
            }
        }

        public void AttachToParent()
        {
            UiElement next = null;
            for (var parent = transform.parent; parent != null && next == null; parent = parent.parent)
                next = parent.GetComponent<UiElement>();
            if (Owner == next) return;
            EndDisplay();
            Owner?._children.Remove(this);
            Owner = next;
            if (Owner != null && !Owner._children.Contains(this)) Owner._children.Add(this);
            Context = Owner?.Context;
        }

        public void SetInputAllowed(bool allowed)
        {
            if (_inputAllowed == allowed) return;
            _inputAllowed = allowed;
            if (!allowed) CancelInput();
        }

        public void CancelInput()
        {
            InputCanceled?.Invoke();
            foreach (var child in _children.ToArray()) if (child != null) child.CancelInput();
        }

        protected internal void BeginDisplay()
        {
            EnsureInitialized();
            if (_shown || !isActiveAndEnabled || (Owner != null && !Owner.IsShown)) return;
            _shown = true;
            _display = new CancellationTokenSource();
            _binding = new CancellationTokenSource();
            OnShow();
            foreach (var feature in _features.ToArray()) feature.SetVisible(true);
            foreach (var child in _children.ToArray()) if (child != null && child.isActiveAndEnabled) child.BeginDisplay();
        }

        protected internal void EndDisplay()
        {
            if (!_shown) return;
            _shown = false;
            CancelInput();
            foreach (var child in _children.ToArray()) if (child != null) child.EndDisplay();
            foreach (var feature in _features.ToArray()) if (feature != null) feature.SetVisible(false);
            Cancel(ref _binding);
            Cancel(ref _display);
            OnHide();
        }

        protected static void Cancel(ref CancellationTokenSource source)
        {
            var previous = source;
            source = null;
            if (previous == null) return;
            previous.Cancel();
            previous.Dispose();
        }

        protected virtual void OnInit() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnTransformParentChanged()
        {
            if (!_initialized) return;
            AttachToParent();
            BindContext(Owner?.Context);
            if (this is UiWidget && isActiveAndEnabled && (Owner == null || Owner.IsShown)) BeginDisplay();
        }
        protected virtual void OnDestroy()
        {
            _destroyed = true;
            EndDisplay();
            Cancel(ref _binding);
            Cancel(ref _display);
            Cancel(ref _instance);
            Owner?._children.Remove(this);
            _children.Clear();
            InputCanceled = null;
        }
    }
}
