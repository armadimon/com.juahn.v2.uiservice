using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Juahn.V2.UiService
{
    /// <summary>
    /// 화면과 팝업의 수명을 관리하는 창구.
    ///
    /// <para>
    /// [화면과 팝업을 나눈 것이 이 API의 뼈대다] 화면은 교체되고(<see cref="OpenScreenAsync{T}"/>),
    /// 팝업은 쌓였다가 위에서부터 걷힌다(<see cref="PushPopupAsync{T}"/> / <see cref="PopAsync"/>).
    /// 둘을 하나로 합치면 "지금 뒤로가기를 누르면 무엇이 닫히는가"를 호출자마다 다시 정해야 한다.
    /// </para>
    /// </summary>
    public interface IUiService
    {
        /// <summary>현재 활성인 화면. 아직 아무 화면도 열지 않았으면 null.</summary>
        UiScreen CurrentScreen { get; }

        /// <summary>팝업 스택. 마지막 원소가 최상단이다.</summary>
        IReadOnlyList<UiPopup> PopupStack { get; }

        /// <summary>등록된 프리젠터가 지금 열려 있는가.</summary>
        bool IsOpen<T>() where T : UiPresenter;

        /// <summary>이미 만들어진 인스턴스를 얻는다. 아직 만들지 않았으면 null.</summary>
        T Get<T>() where T : UiPresenter;

        /// <summary>
        /// 프리팹을 만들어 두되 열지는 않는다. 첫 표시의 인스턴스화 비용을 미리 치르고 싶을 때 쓴다.
        /// </summary>
        UniTask<T> PreloadAsync<T>(CancellationToken ct = default) where T : UiPresenter;

        /// <summary>화면을 연다. 다른 화면이 열려 있으면 닫고, 팝업 스택도 함께 비운다.</summary>
        UniTask<T> OpenScreenAsync<T>(CancellationToken ct = default) where T : UiScreen;

        UniTask<T> OpenScreenAsync<T>(Action<T> bind, CancellationToken ct = default) where T : UiScreen;

        /// <summary>값을 넣어 화면을 연다.</summary>
        UniTask<T> OpenScreenAsync<T, TData>(TData data, CancellationToken ct = default)
            where T : UiScreen, IUiPresenterData<TData>
            where TData : struct;

        /// <summary>입력을 차단하지 않는 알림을 표시하거나 최신 데이터로 갱신한다.</summary>
        UniTask<T> OpenToastAsync<T>(Action<T> bind = null, CancellationToken ct = default) where T : UiToast;

        /// <summary>팝업을 스택 위에 얹는다. 아래 팝업은 입력이 꺼진다.</summary>
        UniTask<T> PushPopupAsync<T>(CancellationToken ct = default) where T : UiPopup;

        UniTask<T> PushPopupAsync<T>(Action<T> bind, CancellationToken ct = default) where T : UiPopup;

        /// <summary>값을 넣어 팝업을 얹는다.</summary>
        UniTask<T> PushPopupAsync<T, TData>(TData data, CancellationToken ct = default)
            where T : UiPopup, IUiPresenterData<TData>
            where TData : struct;

        /// <summary>
        /// 최상단 팝업 하나를 걷는다. 뒤로가기가 부르는 것.
        /// 스택이 비었거나 최상단이 <see cref="UiPopup.CloseOnBack"/>을 끈 경우 아무 일도 하지 않고 false.
        /// </summary>
        UniTask<bool> PopAsync(CancellationToken ct = default);

        /// <summary>팝업 스택을 전부 걷는다. 위에서부터 차례로 닫는다.</summary>
        UniTask PopAllAsync(CancellationToken ct = default);

        /// <summary>특정 프리젠터를 닫는다. 팝업이면 스택 중간에서도 빠진다.</summary>
        UniTask CloseAsync(UiPresenter presenter, CancellationToken ct = default);

        /// <summary>타입으로 닫는다.</summary>
        UniTask CloseAsync<T>(CancellationToken ct = default) where T : UiPresenter;

        /// <summary>인스턴스를 파괴하고 등록만 남긴다. 다음에 열면 다시 만들어진다.</summary>
        void Unload<T>() where T : UiPresenter;
    }

    /// <summary>초기화와 정리까지 포함한 소유자용 인터페이스. 화면 코드는 <see cref="IUiService"/>만 본다.</summary>
    public interface IUiServiceInit : IUiService, IDisposable
    {
        /// <summary>
        /// 등록 목록을 읽고 로더를 준비한다.
        /// <paramref name="uiRoot"/>를 주면 그 아래에 화면을 만들고, 주지 않으면
        /// 서비스가 소유하고 Dispose에서 정리하는 루트를 하나 만든다.
        /// </summary>
        void Init(UiConfigs configs, Transform uiRoot = null, IUiAssetLoader loaderOverride = null);
    }
}
