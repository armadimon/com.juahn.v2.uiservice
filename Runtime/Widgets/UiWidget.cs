namespace Juahn.V2.UiService
{
    public class UiWidget : UiElement
    {
        public void Show()
        {
            EnsureInitialized();
            gameObject.SetActive(true);
            BeginDisplay();
        }
        public void Hide()
        {
            EndDisplay();
            gameObject.SetActive(false);
        }
        protected virtual void OnEnable()
        {
            EnsureInitialized();
            BeginDisplay();
        }
        protected virtual void OnDisable() => EndDisplay();
    }
}
