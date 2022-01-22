using DynamicScrollRect;
using TMPro;
using UnityEngine;

public class ScrollItemDefault : ScrollItem<ScrollItemData>
{
    [SerializeField] private TextMeshProUGUI _text = null;
    
    public override void InitItem(int index, Vector2 gridPos, ScrollItemData data)
    {
        _text.SetText(index.ToString());
        
        base.InitItem(index, gridPos, data);
    }
}
