using System;
using System.Collections.Generic;
using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>
    /// 프리팹을 직접 물려 등록하는 설정 에셋.
    ///
    /// <para>
    /// [타입을 손으로 적지 않는다] 프리젠터 타입은 물린 프리팹에서 읽는다. 타입 이름을
    /// 문자열로 적어 두는 방식은 클래스를 리네임하는 순간 조용히 끊어지고, 그 사실을
    /// 해당 화면을 열어 보기 전까지 알 수 없다.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Juahn/UI Service/Prefab Registry Ui Configs", fileName = "UiConfigs")]
    public sealed class PrefabRegistryUiConfigs : UiConfigs
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("등록할 프리젠터 프리팹. 이 프리팹의 타입이 조회 키가 된다.")]
            public UiPresenter Prefab;

            [Tooltip("프리팹 루트 Canvas의 sortingOrder로 쓸 값. 큰 값이 위에 그려진다.")]
            public int Layer;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => _entries;

        public override IUiAssetLoader CreateLoader()
        {
            var prefabs = new Dictionary<Type, UiPresenter>(_entries.Count);

            for (var i = 0; i < _entries.Count; i++)
            {
                var prefab = _entries[i].Prefab;
                if (prefab == null)
                {
                    continue;
                }

                prefabs[prefab.GetType()] = prefab;
            }

            return new PrefabRegistryUiAssetLoader(prefabs);
        }

        public override void CollectConfigs(List<UiConfig> into)
        {
            if (into == null)
            {
                return;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.Prefab == null)
                {
                    UiServiceLog.WarnEmptyEntry(name, i);
                    continue;
                }

                into.Add(new UiConfig(entry.Prefab.GetType(), entry.Prefab.name, entry.Layer));
            }
        }
    }
}
