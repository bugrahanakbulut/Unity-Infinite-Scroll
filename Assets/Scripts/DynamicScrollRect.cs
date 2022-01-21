using System;
using System.Collections;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Debug = UnityEngine.Debug;

// TODO :: Elastic Run Back
// TODO :: Manage Items Deactivating and Activating etc.
public class DynamicScrollRect : ScrollRect
{
    private bool _isDragging = false;

    private bool _runningBack = false;
    private bool _needRunBack = false;

    private Vector2 _contentStartPos = Vector2.zero;
    private Vector2 _contentPos = Vector2.zero;
    private Vector2 _dragStartingPosition = Vector2.zero;
    private Vector2 _dragCurPosition = Vector2.zero;
    private Vector2 _lastDragDelta = Vector2.zero;

    private IEnumerator _runBackRoutine;
    
    private ScrollContent _content;
    private ScrollContent _Content
    {
        get
        {
            if (_content == null)
            {
                _content = GetComponentInChildren<ScrollContent>();
            }

            return _content;
        }
    }

    #region Event Callbacks

    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);

        StopRunBackRoutine();
        
        _isDragging = true;

        _contentPos = content.anchoredPosition;

        _contentStartPos = _contentPos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport,
            eventData.position,
            eventData.pressEventCamera,
            out _dragStartingPosition);

        _dragCurPosition = _dragStartingPosition;
    }

    /*public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            ScrollRect.viewport, 
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localCursor);

        _lastDragDelta = localCursor - _dragCurPosition;

        _dragCurPosition = localCursor;

        if (_lastDragDelta.y < 0.1f)
        {
            return;
        }

        if (TryRestrictContentMovement(_lastDragDelta))
        {
            // float restriction = GetVerticalRestrictionWeight(_lastDragDelta); 
            
            // SetContentPosition(restriction, true);
            
            // _needRunBack = true;
            
            // SetContentPosition(1, true);
            
            ScrollRect.content.anchoredPosition = _contentPos;
            
            ScrollRect.StopMovement();

            return;
        }

        UpdateItems(_lastDragDelta);
        
        _contentPos = ScrollRect.content.anchoredPosition;
    }*/

    public override void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (!IsActive())
        {
            return;
        }
        
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewRect,
                eventData.position,
                eventData.pressEventCamera, out Vector2 localCursor))
        {
            return;
        }
        
        StopRunBackRoutine();
        
        if (TryRestrictContentMovement(localCursor - _dragCurPosition))
        {
            Vector2 rubberedPos = GetRubberContentPositionOnDrag(eventData);
            
            _contentPos = rubberedPos;

            _needRunBack = true;

            SetContentAnchoredPosition(_contentPos);

            return;
        }

        _needRunBack = false;
        
        UpdateBounds();
        
        _lastDragDelta = localCursor - _dragCurPosition;

        Vector2 dragDelta = localCursor - _dragStartingPosition;

        Vector2 position = _contentStartPos + dragDelta;
        
        _dragCurPosition = localCursor;
        
        SetContentAnchoredPosition(position);

        UpdateItems(_lastDragDelta);
        
        _contentPos = content.anchoredPosition;
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        
        _isDragging = false;

        if (_needRunBack)
        {
            StopMovement();

            StartRunBackRoutine();
        }
    }

    private void OnScrollRectValueChanged(Vector2 val)
    {
        if (_runningBack || _isDragging)
        {
            return;
        }
        
        Vector2 delta = velocity.normalized;
        
        if (TryRestrictContentMovement(delta))
        {
            _contentPos = GetRubberContentPositionOnScroll(delta);
            
            SetContentAnchoredPosition(_contentPos);
            
            if ((velocity * Time.deltaTime).magnitude < 25)
            {
                StopMovement();
                
                StartRunBackRoutine();
            }
            
            return;
        }
        
        UpdateItems(delta);
        
        _contentPos = content.anchoredPosition;
    }

    #endregion

    protected override void Awake()
    {
        movementType = MovementType.Unrestricted;

        onValueChanged.AddListener(OnScrollRectValueChanged);
        
        base.Awake();
    }

    protected override void OnDestroy()
    {
        onValueChanged.RemoveListener(OnScrollRectValueChanged);
        
        base.OnDestroy();
    }

    private void UpdateItems(Vector2 delta)
    {
        if (vertical)
        {
            bool positiveDelta = delta.y > 0;
           
            if (positiveDelta &&
                -_Content.GetLastItemPos().y - content.anchoredPosition.y <= viewport.rect.height + _Content.Spacing.y)
            {
                // Debug.Log($"Positive Delta : {content.anchoredPosition.y} ::: {_Content.GetFirstItemPos().y} ::: {_Content.GetLastItemPos().y}");
                
                _Content.AddIntoTail();
            }

            if (positiveDelta &&
                content.anchoredPosition.y - -_Content.GetFirstItemPos().y >= 2 * _Content.ItemHeight + _Content.Spacing.y)
            {
                _Content.DeleteFirstRow();
            }

            if (!positiveDelta &&
                content.anchoredPosition.y + _Content.GetFirstItemPos().y <= _Content.ItemHeight + _Content.Spacing.y)
            {
                // Debug.Log($"Neg Delta Add Item Into Head : {ScrollRect.content.anchoredPosition.y} ::: {_Content.GetFirstItemPos().y} ::: {_Content.GetLastItemPos().y}");
                
                _Content.AddIntoHead();
            }

            if (!positiveDelta &&
                -_Content.GetLastItemPos().y - content.anchoredPosition.y >= viewport.rect.height + _Content.ItemHeight + _Content.Spacing.y)
            {
                // Debug.Log($"Neg Delta Delete Last Row : {ScrollRect.content.anchoredPosition.y} ::: {_Content.GetFirstItemPos().y} ::: {_Content.GetLastItemPos().y}");
                
                _Content.DeleteLastRow();
            }
        }
    }

    private bool TryRestrictContentMovement(Vector2 delta)
    {
        if (vertical)
        {
            bool canRestrict = TryRestrictContentVerticalMovement(delta);
            
            return canRestrict;
        }
        
        return false;
    }

    private bool TryRestrictContentVerticalMovement(Vector2 delta)
    {
        bool positiveDelta = delta.y > 0;

        if (positiveDelta)
        {
            Vector2 lastItemPos = _Content.GetLastItemPos();
            
            if (!_Content.CanAddNewItemIntoTail() && 
                content.anchoredPosition.y + viewport.rect.height + lastItemPos.y - _Content.ItemHeight > 0)
            {
                return true;
            }
        }
        else
        {
            if (!_Content.CanAddNewItemIntoHead() &&
                content.anchoredPosition.y <= 0)
            {
                return true;
            }
        }
        
        return false;
    }

    private Vector2 GetRubberContentPositionOnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewRect,
            eventData.position,
            eventData.pressEventCamera, out Vector2 localCursor);

        Vector2 delta = localCursor - _dragCurPosition;

        float restriction = GetVerticalRestrictionWeight(delta);

        Vector2 dragDelta = localCursor - _dragStartingPosition;

        Vector2 position = _contentStartPos + dragDelta;
        
        Vector2 weightedPrev = _contentPos * restriction;
        
        Vector2 weightedNext = position * (1 - restriction);

        Vector2 res = weightedPrev + weightedNext;

        if (vertical)
        {
            res.x = 0;
        }
        
        return res;
    }

    private Vector2 GetRubberContentPositionOnScroll(Vector2 delta)
    {
        float restriction = GetVerticalRestrictionWeight(delta);

        Vector2 deltaPos = velocity * Time.deltaTime;

        if (vertical)
        {
            deltaPos.x = 0;
        }

        Vector2 position = content.anchoredPosition + deltaPos;

        Vector2 weightedPrev = _contentPos * restriction;

        Vector2 weightedNext = position * (1 - restriction);

        Vector2 res = weightedPrev + weightedNext;

        if (vertical)
        {
            res.x = 0;
        }
        
        velocity /= 2;

        return res;
    }

    private float GetVerticalRestrictionWeight(Vector2 delta)
    {
        bool positiveDelta = delta.y > 0;

        float maxLimit = _Content.ItemHeight + _Content.Spacing.y / 2;

        if (positiveDelta)
        {
            Vector2 lastItemPos = _Content.GetLastItemPos();

            if (Mathf.Abs(lastItemPos.y) <= viewport.rect.height - _Content.ItemHeight)
            {
                float max = lastItemPos.y + maxLimit;
            
                float cur = content.anchoredPosition.y + lastItemPos.y;

                float diff = max - cur;

                return 1f - Mathf.Clamp(diff / maxLimit, 0, 1);
            }
            else
            {
                float max = -(viewport.rect.height - maxLimit - _Content.ItemHeight);
                
                float cur = content.anchoredPosition.y + lastItemPos.y;

                float diff = max - cur;

                return 1f - Mathf.Clamp(diff / maxLimit, 0, 1);
            }
        }

        float restrictionVal = Mathf.Clamp(Mathf.Abs(content.anchoredPosition.y) / maxLimit, 0, 1);
        
        return restrictionVal;
    }

    private Vector2 CalculateSnapPosition()
    {
        if (vertical)
        {
            if (content.anchoredPosition.y < 0)
            {
                return Vector2.zero;
            }
            else
            {
                Vector2 lastItemPos = _Content.GetLastItemPos();

                if (Mathf.Abs(lastItemPos.y) <= viewport.rect.height - _Content.ItemHeight)
                {                    
                    return Vector2.zero;
                }
                else
                {
                    float target = -(viewport.rect.height - _Content.ItemHeight);
                    
                    float cur = content.anchoredPosition.y + lastItemPos.y;

                    float diff = target - cur;

                    return content.anchoredPosition + new Vector2(0, diff);
                }
            }
        }
        
        Debug.Log("check");
        
        return Vector2.zero;
    }
    
    #region Run Back Routine

    private void StartRunBackRoutine()
    {
        StopRunBackRoutine();
        
        _runBackRoutine = RunBackProgress();

        StartCoroutine(_runBackRoutine);
    }

    private void StopRunBackRoutine()
    {
        if (_runBackRoutine != null)
        {
            StopCoroutine(_runBackRoutine);

            _runningBack = false;
        }
    }

    private IEnumerator RunBackProgress()
    {
        _runningBack = true;

        float timePassed = 0;

        float duration = 0.25f;

        Vector2 startPos = content.anchoredPosition;

        Vector2 endPos = CalculateSnapPosition();

        while (timePassed < duration)
        {
            timePassed += Time.deltaTime;

            Vector2 pos = Vector2.Lerp(startPos, endPos, timePassed / duration);
            
            SetContentAnchoredPosition(pos);
            
            yield return null;
        }

        SetContentAnchoredPosition(endPos);
        
        _runningBack = false;
    }
    
    #endregion
}
