using TMPro;
using UnityEngine;

public class ScrollItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text = null;
    
    public RectTransform RectTransform => transform as RectTransform;
    
    public int Index { get; private set; }
    
    public Vector2 GridIndex { get; private set; }

    public void Init(int index, Vector2 gridPos, ScrollItemData data = null)
    {
        if (data != null)
        {
            _text.SetText(data.Index.ToString());
        }

        Index = index;
        GridIndex = gridPos;
        gameObject.SetActive(true);
    }
}
