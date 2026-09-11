using System.Collections.Generic;
using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>
    /// 어떤 화면이 있고 어디서 가져오는지를 한 에셋에 모은 것.
    /// 서비스는 이것 하나만 받아 초기화된다.
    ///
    /// <para>
    /// [설정이 로더까지 정한다] 등록 목록과 로딩 방식은 함께 바뀐다 — 주소 목록을 쓰면
    /// 주소 로더가, 프리팹 목록을 쓰면 직참조 로더가 필요하다. 둘을 따로 넘기게 하면
    /// 짝이 어긋난 조합이 컴파일된다.
    /// </para>
    /// </summary>
    public abstract class UiConfigs : ScriptableObject
    {
        /// <summary>이 설정에 맞는 로더를 만든다.</summary>
        public abstract IUiAssetLoader CreateLoader();

        /// <summary>등록된 화면 설정을 <paramref name="into"/>에 채운다.</summary>
        public abstract void CollectConfigs(List<UiConfig> into);
    }
}
