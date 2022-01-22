using TMPro;
using UnityEngine;
using DynamicScrollRect;

public class ScrollItemDefault : ScrollItem<ScrollItemData>
{
    [SerializeField] private TextMeshProUGUI _text = null;

    protected override void InitItemData(ScrollItemData data)
    {
        _text.SetText(data.Index.ToString());
        
        base.InitItemData(data);
    }
}
