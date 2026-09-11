using System.Threading;
using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>로컬 기능은 소유 요소의 수명에 참여하며 독립적인 표시 상태를 만들지 않는다.</summary>
    public abstract class UiFeature : MonoBehaviour
    {
        private bool _initialized;
        private bool _subscribed;
        public UiElement Owner { get; private set; }
        public object Context => Owner?.Context;
        protected CancellationToken LifetimeToken => Owner?.LifetimeToken ?? new CancellationToken(true);
        protected CancellationToken BindingToken => Owner?.BindingToken ?? new CancellationToken(true);

        internal void Initialize(UiElement owner)
        {
            if (_initialized && Owner == owner) return;
            SetVisible(false);
            if (Owner != null) Owner.UnregisterFeature(this);
            Owner = owner;
            owner.RegisterFeature(this);
            if (!_initialized) { _initialized = true; OnInit(); }
            OnContextChanged();
            if (owner.IsShown) SetVisible(true);
        }

        internal void ContextChanged()
        {
            var active = _subscribed;
            SetVisible(false);
            OnContextChanged();
            if (active) SetVisible(true);
        }

        internal void SetVisible(bool visible)
        {
            visible &= isActiveAndEnabled;
            if (!_initialized || visible == _subscribed) return;
            _subscribed = visible;
            if (visible) OnShow(); else OnHide();
        }

        protected virtual void OnEnable()
        {
            if (!Application.IsPlaying(gameObject)) return;
            if (!_initialized)
            {
                var owner = GetComponentInParent<UiElement>(true);
                if (owner != null) { owner.EnsureInitialized(); Initialize(owner); }
            }
            if (Owner != null && Owner.IsShown) SetVisible(true);
        }
        protected virtual void OnDisable() => SetVisible(false);
        protected virtual void OnTransformParentChanged()
        {
            if (!_initialized) return;
            var owner = GetComponentInParent<UiElement>(true);
            if (owner != null) { owner.EnsureInitialized(); Initialize(owner); }
            else { SetVisible(false); if (Owner != null) Owner.UnregisterFeature(this); Owner = null; }
        }
        protected virtual void OnDestroy()
        {
            SetVisible(false);
            if (Owner != null) Owner.UnregisterFeature(this);
        }
        protected virtual void OnInit() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnContextChanged() { }
    }
}
