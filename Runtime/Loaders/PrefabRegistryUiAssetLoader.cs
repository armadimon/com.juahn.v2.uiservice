using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>
    /// 프리팹을 직접 참조해서 만드는 로더. 이 패키지의 기본값이다.
    ///
    /// <para>
    /// [기본값이 직참조인 이유] 주소 문자열은 오타가 런타임까지 살아남고, 비동기 로딩은
    /// 첫 표시에 프레임을 튀게 한다. 프리팹을 참조로 들고 있으면 씬 로드 시점에 함께 올라와
    /// 둘 다 없다. UI 프리팹은 수십 개 규모라 메모리도 문제가 되지 않는다.
    /// 화면 수가 크게 늘거나 다운로드 콘텐츠가 생기면 그때 Addressables 로더로 갈아탄다.
    /// </para>
    /// </summary>
    public sealed class PrefabRegistryUiAssetLoader : IUiAssetLoader
    {
        private readonly Dictionary<Type, UiPresenter> _prefabs;

        public PrefabRegistryUiAssetLoader(IReadOnlyDictionary<Type, UiPresenter> prefabs)
        {
            _prefabs = prefabs == null
                ? new Dictionary<Type, UiPresenter>()
                : new Dictionary<Type, UiPresenter>(prefabs.Count);

            if (prefabs == null)
            {
                return;
            }

            foreach (var pair in prefabs)
            {
                if (pair.Key == null || pair.Value == null)
                {
                    continue;
                }

                _prefabs[pair.Key] = pair.Value;
            }
        }

        public UniTask<UiPresenter> InstantiateAsync(UiConfig config, Transform parent, CancellationToken ct = default)
        {
            if (!config.IsValid || !_prefabs.TryGetValue(config.PresenterType, out var prefab) || prefab == null)
            {
                return UniTask.FromResult<UiPresenter>(null);
            }

            // 비활성으로 만든 뒤 켠다. 프리팹이 활성 상태로 저작돼 있어도 Awake가
            // 열기 절차보다 먼저 돌아 한 프레임 깜빡이는 일이 없게 한다.
            var wasActive = prefab.gameObject.activeSelf;
            if (wasActive)
            {
                prefab.gameObject.SetActive(false);
            }

            UiPresenter instance;
            try
            {
                instance = UnityEngine.Object.Instantiate(prefab, parent);
            }
            finally
            {
                if (wasActive)
                {
                    prefab.gameObject.SetActive(true);
                }
            }

            instance.name = prefab.name; // "(Clone)" 접미사를 떼어 계층에서 읽기 쉽게.
            instance.gameObject.SetActive(false);
            return UniTask.FromResult(instance);
        }

        public void Release(UiPresenter presenter)
        {
            if (presenter == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(presenter.gameObject);
        }
    }
}
