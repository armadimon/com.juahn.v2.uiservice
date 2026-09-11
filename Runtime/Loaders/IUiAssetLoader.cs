using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>
    /// 프리젠터 프리팹을 어디서 어떻게 가져오는지를 서비스 본체에서 떼어 낸 자리.
    ///
    /// <para>
    /// [본체는 로딩 방식을 모른다] 프리팹 직참조로 시작해 나중에 Addressables로 옮기더라도
    /// 서비스와 화면 코드는 그대로다. 반대로 본체가 특정 로딩 패키지를 참조하면, 그 패키지를
    /// 쓰지 않는 프로젝트에서도 설치를 강요받는다.
    /// </para>
    /// </summary>
    public interface IUiAssetLoader
    {
        /// <summary>
        /// 설정이 가리키는 프리젠터를 <paramref name="parent"/> 아래에 만든다.
        /// 만들어진 오브젝트는 <b>비활성 상태</b>여야 한다 — 열기 절차가 활성화를 담당한다.
        /// 찾지 못하면 null을 돌려준다(예외를 던지지 않는다. 서비스가 진단 로그를 남긴다).
        /// </summary>
        UniTask<UiPresenter> InstantiateAsync(UiConfig config, Transform parent, CancellationToken ct = default);

        /// <summary>인스턴스를 파괴하고, 로더가 쥔 자원이 있으면 함께 놓는다.</summary>
        void Release(UiPresenter presenter);
    }
}
