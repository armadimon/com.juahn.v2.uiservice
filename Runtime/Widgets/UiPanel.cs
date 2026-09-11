using UnityEngine;
namespace Juahn.V2.UiService
{
    public class UiPanel : UiWidget
    {
        [SerializeField] private bool _closeOnBack = true;
        public bool CloseOnBack => _closeOnBack;
    }
}
