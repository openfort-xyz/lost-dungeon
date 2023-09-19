using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NoSelectButton : Button
{
    public override void OnSelect(BaseEventData eventData)
    {
        // Do nothing on select to disable selection by keyboard, mouse and touch.
    }
}
