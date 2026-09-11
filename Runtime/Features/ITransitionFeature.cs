using Cysharp.Threading.Tasks;

namespace Juahn.V2.UiService
{
    /// <summary>
    /// 열림/닫힘 연출이 끝날 때까지 프리젠터를 기다리게 만드는 피처의 계약.
    ///
    /// <para>
    /// [완료원은 대기보다 먼저 만들어져야 한다] 프리젠터는 <c>OnOpened</c> 통지를 보낸
    /// <b>다음에</b> <see cref="OpenTransitionTask"/>를 읽는다. 그러므로 피처는 통지를 받는
    /// 순간 완료원을 만들어 두어야 하고, 통지보다 늦게 만들면 프리젠터가 이미 지나간 뒤라
    /// 연출을 기다리지 못한다.
    /// </para>
    ///
    /// <para>
    /// [닫힘은 Closing에서 걸고 Closed 전에 풀린다] 프리젠터는 <c>OnClosing</c> 통지 직후
    /// <see cref="CloseTransitionTask"/>를 <c>await</c> 하고, 그것이 끝난 뒤에야 <c>OnClosed</c>를
    /// 부르고 오브젝트를 끈다. 닫힘 연출을 발사할 자리는 <c>OnPresenterClosing</c>이다.
    /// </para>
    ///
    /// <para>
    /// 연출이 없을 때는 <see cref="UniTask.CompletedTask"/>를 돌려주면 된다.
    /// </para>
    /// </summary>
    public interface ITransitionFeature
    {
        /// <summary>열림 연출이 끝나면 완료되는 작업.</summary>
        UniTask OpenTransitionTask { get; }

        /// <summary>닫힘 연출이 끝나면 완료되는 작업.</summary>
        UniTask CloseTransitionTask { get; }
    }
}
