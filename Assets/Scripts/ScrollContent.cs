using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

// TODO :: automatize visible item count it while considering grid layout
// TODO :: create new methods for item create and delete to get rid of code duplication of it
public class ScrollContent : MonoBehaviour
{
    [SerializeField] private Vector2 _spacing = Vector2.zero;
    public Vector2 Spacing => _spacing;
    
    [SerializeField] private Vector2 _visibleItemCount = Vector2.zero;

    [SerializeField] private ScrollItem _refItem = null;

    private DynamicScrollRect _dynamicScrollRect;
    private DynamicScrollRect DynamicScrollRect
    {
        get
        {
            if (_dynamicScrollRect == null)
            {
                _dynamicScrollRect = GetComponentInParent<DynamicScrollRect>();
            }

            return _dynamicScrollRect;
        }
    }

    private List<ScrollItem> _activatedItems = new List<ScrollItem>();

    private List<ScrollItemData> _datum;

    private float _itemWidth => (_refItem.transform as RectTransform).rect.width;
    public float ItemWidth => _itemWidth;
    
    private float _itemHeight => (_refItem.transform as RectTransform).rect.height;
    public float ItemHeight => _itemHeight;
    
    public void InitScrollContent(List<ScrollItemData> contentDatum)
    {
        if (DynamicScrollRect.vertical)
        {
            InitItemsVertical(contentDatum);
        }
    }

    public bool CanAddNewItemIntoTail()
    {
        if (_activatedItems == null || _activatedItems.Count == 0)
        {
            return false;
        }
        
        return _activatedItems[_activatedItems.Count - 1].Index < _datum.Count - 1;
    }

    public bool CanAddNewItemIntoHead()
    {
        if (_activatedItems == null || _activatedItems.Count == 0)
        {
            return false;
        }
        
        return _activatedItems[0].Index - 1 > 0;
    }

    public Vector2 GetFirstItemPos()
    {
        return _activatedItems[0].RectTransform.anchoredPosition;
    }

    public Vector2 GetLastItemPos()
    {
        return _activatedItems[_activatedItems.Count - 1].RectTransform.anchoredPosition;
    }

    public void AddIntoHead()
    {
        for (int i = 0; i < _visibleItemCount.x; i++)
        {
            AddItemToHead();
        }
    }

    public void AddIntoTail()
    {
        for (int i = 0; i < _visibleItemCount.x; i++)
        {
            AddItemToTail();
        }
    }

    public void DeleteFirstRow()
    {
        int firstRowIndex = (int) _activatedItems[0].GridIndex.y;
        
        DeleteRow(firstRowIndex);
    }
    
    public void DeleteLastRow()
    {
        int lastRowIndex = (int) _activatedItems[_activatedItems.Count - 1].GridIndex.y;
        
        DeleteRow(lastRowIndex);
    }

    private void Awake()
    {
        _refItem.gameObject.SetActive(false);
    }

    private void InitItemsVertical(List<ScrollItemData> contentDatum)
    {
        _datum = contentDatum;
        
        int itemCount = 0;

        for (int col = 0; col < _visibleItemCount.y + 2; col++)
        {
            for (int row = 0; row < _visibleItemCount.x; row++)
            {
                ScrollItem item = CreateItem(itemCount);
                
                _activatedItems.Add(item);

                itemCount++;
                
                if (itemCount == contentDatum.Count)
                {
                    return;
                }
            }
        }
    }

    private void DeleteRow(int rowIndex)
    {
        // Debug.Log($"Row {rowIndex} <color=red>deleted</color>.");
        
        List<ScrollItem> items = _activatedItems.FindAll(i => (int) i.GridIndex.y == rowIndex);

        foreach (ScrollItem item in items)
        {
            item.gameObject.SetActive(false);
            
            _activatedItems.Remove(item);
        }
    }

    private void AddItemToTail()
    {
        int itemIndex = _activatedItems[_activatedItems.Count - 1].Index + 1;

        if (itemIndex == _datum.Count)
        {
            return;
        }

        ScrollItem item = CreateItem(itemIndex);
        
        _activatedItems.Add(item);
    }

    private void AddItemToHead()
    {
        int itemIndex = _activatedItems[0].Index - 1;

        if (itemIndex < 0)
        {
            return;
        }

        ScrollItem item = CreateItem(itemIndex);
        
        _activatedItems.Insert(0, item);
    }

    private ScrollItem CreateItem(int itemIndex)
    {
        Vector2 gridPos = GetGridPosition(itemIndex);

        Vector2 anchoredPos = GetAnchoredPosition(gridPos);

        GameObject item = Instantiate(_refItem.gameObject, transform);
        item.name = $"{gridPos.x}_{gridPos.y}";
        
        ScrollItem scrollItem = item.GetComponent<ScrollItem>();
        scrollItem.RectTransform.pivot = new Vector2(0, 1);
        scrollItem.RectTransform.anchoredPosition = anchoredPos;
        scrollItem.Init(itemIndex, gridPos, _datum[itemIndex]);
        
        // Debug.Log($"<color=green>Item created {itemIndex}</color>.");

        return scrollItem;
    }

    private Vector2 GetGridPosition(int itemIndex)
    {
        if (DynamicScrollRect.vertical)
        {
            int col = itemIndex / (int) _visibleItemCount.x;

            int row = itemIndex - (col * (int) _visibleItemCount.x);
            
            return new Vector2(row, col);
        }

        return Vector2.zero;
    }

    private Vector2 GetAnchoredPosition(Vector2 gridPosition)
    {
        return new Vector2(
            gridPosition.x * _itemWidth + gridPosition.x * _spacing.x,
            -gridPosition.y * _itemHeight - gridPosition.y * _spacing.y);
    }
}
