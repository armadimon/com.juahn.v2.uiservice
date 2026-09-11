# UI Service V2

UiService owns registered screens, modal popups and nonmodal toasts. UiElement owns common initialization, parent/child ownership, display and binding lifetimes. The package does not depend on a game, DI container or motion backend. Install UniTask as a UPM package; uGUI 2.0.0 is declared as a dependency.

## Composition and ownership

- Set `Context` before opening a presenter and call `Init(configs, root)` with an explicit UI root.
- `OpenScreenAsync<T>` selects one screen; `PushPopupAsync<T>` adds a modal popup; `OpenToastAsync<T>` refreshes a nonmodal notification. Presenter types are cached once per service.
- `UiWidget` instances are owned by their nearest `UiElement`, not cached by type. Place any number of buttons, slots or progress bars beneath a presenter.
- A `UiFeature` is discovered by its nearest element. Use `OnInit`, `OnContextChanged`, `OnShow`, `OnHide` and `BindingToken` for local binding. Screen code need not register individual buttons.
- `LifetimeToken` is the current display lifetime. `BindingToken` also changes on rebind. `InstanceToken` ends on destruction. Use the appropriate token, especially for cached presenters.
- `UiWidget.Show/Hide` and `UiPanel` preserve a deliberately hidden child across parent reopen. Override widget `OnShow/OnHide`, or call the base method when overriding `OnEnable/OnDisable`.

Opening prepares context, initializes children, activates and updates layout, then awaits transition features before allowing input. Closing immediately cancels input and display subscriptions, retains the modal cover during End transitions, then deactivates and restores lower input. Same-layer popups use last-opened order; higher layers keep precedence. Toasts never take modal input. `PopAsync` closes a visible inner `UiPanel` first, then the top popup if `CloseOnBack` allows it; screens remain open.

`UiButton.InvokeClick()` is a programmatic accepted input. Pointer hold repetition suppresses only the final pointer release click. `UiHoldRepeatFeature` cancels on release, pointer exit, hide, rebind and input cancellation. Purchases and rewards remain the application's responsibility.

`UiVisibilityBinding<T>` reconciles rapidly changing desired visibility with asynchronous loading and transition cancellation. Use it for overlays controlled by game state. Dispose it before its owning service. `UiService.Dispose()` cancels pending loads/transitions and releases loaded presenters once, including external-destruction cleanup.

## Motion integration

Install `com.juahn.v2.uimotion.uiservice` to attach lifecycle, interaction, value and transient motion features. UiService itself has no knowledge of MotionPlayer. A closing transition must complete finitely and must not deactivate its owning presenter inside a graph.

## Validation

The IdleMine consumer has PlayMode coverage for multiple widgets, hidden children, context changes, dynamic ownership, fast navigation, canceled/late loads, external destruction, modal priorities, real prefab input and holds. Keep `.meta` files when moving a serialized MonoBehaviour. `PresenterFeatureBase` remains a presenter callback adapter over `UiFeature`, with no separate lifetime.
