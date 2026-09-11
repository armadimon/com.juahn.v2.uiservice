using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>
    /// <c>Resources.Load</c>로 가져오는 로더. <see cref="UiConfig.Address"/>가 Resources 상대 경로다.
    ///
    /// <para>
    /// 로드한 프리팹은 캐시한다. Resources는 같은 경로를 다시 물어도 디스크를 두 번 읽지는
    /// 않지만, 경로 문자열 해석 비용이 매번 든다.
    /// </para>
    /// </summary>
    public sealed class ResourcesUiAssetLoader : IUiAssetLoader
    {
        private readonly Dictionary<string, UiPresenter> _cache = new Dictionary<string, UiPresenter>();

        public UniTask<UiPresenter> InstantiateAsync(UiConfig config, Transform parent, CancellationToken ct = default)
        {
            if (!config.IsValid || string.IsNullOrEmpty(config.Address))
            {
                return UniTask.FromResult<UiPresenter>(null);
            }

            if (!_cache.TryGetValue(config.Address, out var prefab) || prefab == null)
            {
                prefab = Resources.Load<UiPresenter>(config.Address);
                if (prefab == null)
                {
                    return UniTask.FromResult<UiPresenter>(null);
                }

                _cache[config.Address] = prefab;
            }

            var wasActive = prefab.gameObject.activeSelf;
            if (wasActive)
            {
                prefab.gameObject.SetActive(false);
            }

            UiPresenter instance;
            try
            {
                instance = Object.Instantiate(prefab, parent);
            }
            finally
            {
                if (wasActive)
                {
                    prefab.gameObject.SetActive(true);
                }
            }

            instance.name = prefab.name;
            instance.gameObject.SetActive(false);
            return UniTask.FromResult(instance);
        }

        public void Release(UiPresenter presenter)
        {
            if (presenter == null)
            {
                return;
            }

            // 런타임에는 Destroy를 쓴다. DestroyImmediate는 진행 중인 순회를 무너뜨린다.
            Object.Destroy(presenter.gameObject);
        }
    }
}
