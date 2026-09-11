using System;
using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>
    /// 진단 로그를 한곳에 모은다. 메시지 문구가 흩어지면 같은 사고를 두 가지 문장으로
    /// 보게 되어 검색이 안 된다.
    /// </summary>
    internal static class UiServiceLog
    {
        private const string Tag = "[UiService] ";

        internal static void WarnEmptyEntry(string assetName, int index)
        {
            Debug.LogWarning($"{Tag}{assetName}의 {index}번 항목에 프리팹이 비어 있어 건너뛴다.");
        }

        internal static void WarnDuplicate(Type type, string assetName)
        {
            Debug.LogWarning($"{Tag}{type.Name}이(가) {assetName}에 두 번 등록됐다. 나중 것이 이긴다.");
        }

        internal static void WarnNotRegistered(Type type)
        {
            Debug.LogError($"{Tag}{type.Name}이(가) 등록돼 있지 않다. UiConfigs에 프리팹을 추가했는지 확인한다.");
        }

        internal static void WarnLoadFailed(UiConfig config)
        {
            Debug.LogError(
                $"{Tag}{config.PresenterType.Name}의 프리팹을 만들지 못했다(주소 \"{config.Address}\"). " +
                "로더가 찾을 수 있는 자리에 있는지 확인한다.");
        }

        internal static void WarnMissingCanvas(UiPresenter presenter)
        {
            Debug.LogWarning(
                $"{Tag}{presenter.name}의 루트에 Canvas가 없어 Layer({presenter.Layer})가 무시된다. " +
                "화면·팝업 프리팹은 자기 Canvas를 가져야 겹침 순서가 정해진다.");
        }

        internal static void WarnNotInitialized(string operation)
        {
            Debug.LogError($"{Tag}Init을 부르기 전에 {operation}이(가) 호출됐다.");
        }

        internal static void WarnWrongKind(Type type, string expected)
        {
            Debug.LogError($"{Tag}{type.Name}은(는) {expected}이(가) 아니다.");
        }
    }
}
