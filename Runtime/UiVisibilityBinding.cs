using System;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace Juahn.V2.UiService
{
    /// <summary>게임이 요청한 표시 상태를 최신 요청으로 수렴시킨다. 종료된 단계의 늦은 로드는 표시하지 않는다.</summary>
    public sealed class UiVisibilityBinding<T> : IDisposable where T : UiPresenter
    {
        private readonly Func<T> _get;
        private readonly Func<CancellationToken,UniTask<T>> _open;
        private readonly Func<CancellationToken,UniTask> _close;
        private readonly CancellationToken _lifetime;
        private CancellationTokenSource _request;
        private bool _visible,_pending,_disposed;
        public UiVisibilityBinding(Func<T> get,Func<CancellationToken,UniTask<T>> open,Func<CancellationToken,UniTask> close,CancellationToken lifetime)
        { _get=get;_open=open;_close=close;_lifetime=lifetime; }
        public void SetVisible(bool visible)
        {
            if(_disposed || _lifetime.IsCancellationRequested)return;
            if(_visible!=visible){_visible=visible;_request?.Cancel();}
            if(!_pending && _visible!=(_get()!=null && _get().IsOpen)) Sync().Forget();
        }
        private async UniTask Sync()
        {
            _pending=true;_request=CancellationTokenSource.CreateLinkedTokenSource(_lifetime);
            var desired=_visible;var canceled=false;
            try { if(desired)await _open(_request.Token);else await _close(_request.Token); }
            catch(OperationCanceledException) { canceled=true; }
            finally
            {
                _request.Dispose();_request=null;_pending=false;
                if(!_disposed && (desired!=_visible || canceled))SetVisible(_visible);
            }
        }
        public void Dispose(){if(_disposed)return;_disposed=true;_request?.Cancel();}
    }
}
