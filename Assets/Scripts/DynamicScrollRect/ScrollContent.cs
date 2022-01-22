using System;
using UnityEngine;
using System.Collections.Generic;

namespace DynamicScrollRect
{
    // TODO : handle if datum updated in runtime
    public class ScrollContent : MonoBehaviour
    {
        [SerializeField] private Vector2 _spacing = Vector2.zero;
        public Vector2 Spacing => _spacing;

        [Tooltip("it will fill content rect in main axis(horizontal or vertical) automatically simply ignores _fixedItemCount")]
        [SerializeField] private bool _fillContent = false;
        
        [Tooltip("if scroll is vertical it is item count in each row vice versa for horizontal")]
        [Min(1)][SerializeField] private int _fixedItemCount = 1;

        private DynamicScrollRect _dynamicScrollRect;
        private DynamicScrollRect DynamicScrollRect
        {
            get
            {
                if (_dynamicScrollRect == null)
                {
                    _dynamicScrollRect = GetComponent<DynamicScrollRect>();
                }

                return _dynamicScrollRect;
            }
        }

        private ScrollItem _referenceItem;

        private ScrollItem _ReferenceItem
        {
            get
            {
                if (_referenceItem == null)
                {
                    _referenceItem = GetComponentInChildren<ScrollItem>();

                    if (_referenceItem == null)
                    {
                        throw new Exception($"No Scroll Item found under scroll rect content." +
                                            $"You should create reference scroll item under DynamicScroll Content first.");
                    }
                }

                return _referenceItem;
            }
        }

        private List<ScrollItem> _activatedItems = new List<ScrollItem>();

        private List<ScrollItem> _deactivatedItems = new List<ScrollItem>();

        private List<ScrollItemData> _datum;

        private float _itemWidth => _ReferenceItem.RectTransform.rect.width;
        public float ItemWidth => _itemWidth;
    
        private float _itemHeight => _ReferenceItem.RectTransform.rect.height;
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
            for (int i = 0; i < _fixedItemCount; i++)
            {
                AddItemToHead();
            }
        }

        public void AddIntoTail()
        {
            for (int i = 0; i < _fixedItemCount; i++)
            {
                AddItemToTail();
            }
        }

        // TODO : Handle Horizontal Layout
        public void DeleteFromHead()
        {
            int firstRowIndex = (int) _activatedItems[0].GridIndex.y;
        
            DeleteRow(firstRowIndex);
        }
    
        // TODO : Handle Horizontal Layout
        public void DeleteFromTail()
        {
            int lastRowIndex = (int) _activatedItems[_activatedItems.Count - 1].GridIndex.y;
        
            DeleteRow(lastRowIndex);
        }

        private void Awake()
        {
            _ReferenceItem.gameObject.SetActive(false);
        }

        private void InitItemsVertical(List<ScrollItemData> contentDatum)
        {
            _datum = contentDatum;
        
            int itemCount = 0;

            Vector2Int initialGridSize = CalculateInitialGridSize();

            for (int col = 0; col < initialGridSize.y; col++)
            {
                for (int row = 0; row < initialGridSize.x; row++)
                {
                    ActivateItem(itemCount);
                
                    itemCount++;
                
                    if (itemCount == contentDatum.Count)
                    {
                        return;
                    }
                }
            }
        }

        private Vector2Int CalculateInitialGridSize()
        {
            Vector2 contentSize = DynamicScrollRect.content.rect.size;

            if (DynamicScrollRect.vertical)
            {
                int verticalItemCount = 4 + (int) (contentSize.y / (ItemHeight + _spacing.y));
                
                if (_fillContent)
                {
                    int horizontalItemCount = (int) ((contentSize.x + _spacing.x) / (ItemWidth + _spacing.x));

                    _fixedItemCount = horizontalItemCount;

                    return new Vector2Int(horizontalItemCount, verticalItemCount);
                }
                
                
                return new Vector2Int(_fixedItemCount, verticalItemCount);
            }
            
            return Vector2Int.zero;
        }

        private void DeleteRow(int rowIndex)
        {
            List<ScrollItem> items = _activatedItems.FindAll(i => (int) i.GridIndex.y == rowIndex);

            foreach (ScrollItem item in items)
            {
                DeactivateItem(item);
            }
        }

        private void AddItemToTail()
        {
            int itemIndex = _activatedItems[_activatedItems.Count - 1].Index + 1;

            if (itemIndex == _datum.Count)
            {
                return;
            }

            ActivateItem(itemIndex);
        }

        private void AddItemToHead()
        {
            int itemIndex = _activatedItems[0].Index - 1;

            if (itemIndex < 0)
            {
                return;
            }

            ActivateItem(itemIndex);
        }

        private ScrollItem ActivateItem(int itemIndex)
        {
            Vector2 gridPos = GetGridPosition(itemIndex);

            Vector2 anchoredPos = GetAnchoredPosition(gridPos);

            ScrollItem scrollItem = null;
        
            if (_deactivatedItems.Count == 0)
            {
                scrollItem = CreateNewScrollItem();
            }
            else
            {
                scrollItem = _deactivatedItems[0];

                _deactivatedItems.Remove(scrollItem);
            }

            scrollItem.gameObject.name = $"{gridPos.x}_{gridPos.y}";
        
            scrollItem.RectTransform.anchoredPosition = anchoredPos;
            
            scrollItem.InitItem(itemIndex, gridPos, _datum[itemIndex]);

            bool insertHead = (_activatedItems.Count == 0 ||
                               (_activatedItems.Count > 0 && _activatedItems[0].Index > itemIndex));

            if (insertHead)
            {
                _activatedItems.Insert(0, scrollItem);
            }
            else
            {
                _activatedItems.Add(scrollItem);
            }
        
            scrollItem.Activated();
        
            return scrollItem;
        }

        private void DeactivateItem(ScrollItem item)
        {
            _activatedItems.Remove(item);
        
            _deactivatedItems.Add(item);
        
            item.Deactivated();
        }

        private ScrollItem CreateNewScrollItem()
        {
            GameObject item = Instantiate(_ReferenceItem.gameObject, DynamicScrollRect.content);
        
            ScrollItem scrollItem = item.GetComponent<ScrollItem>();
            scrollItem.RectTransform.pivot = new Vector2(0, 1);

            return scrollItem;
        }

        // TODO : Handle Horizontal Layout
        private Vector2 GetGridPosition(int itemIndex)
        {
            if (DynamicScrollRect.vertical)
            {
                int col = itemIndex / _fixedItemCount;

                int row = itemIndex - (col * _fixedItemCount);
            
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
}
