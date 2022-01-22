using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// TODO :: Manage Items Deactivating and Activating etc.
// TODO :: Hidden Rules Between TryRestrictionMovement - GetContentPosition etc.
public class DynamicScrollRect : ScrollRect
{
    private bool _isDragging = false;

    private bool _runningBack = false;
    private bool _needRunBack = false;

    private Vector2 _contentStartPos = Vector2.zero;
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

        _contentStartPos = content.anchoredPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport,
            eventData.position,
            eventData.pressEventCamera,
            out _dragStartingPosition);

        _dragCurPosition = _dragStartingPosition;
    }
    
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
        
        if (!IsDragValid(localCursor - _dragCurPosition))
        {
            Vector2 restrictedPos = GetRestrictedContentPositionOnDrag(eventData);
            
            _needRunBack = true;

            SetContentAnchoredPosition(restrictedPos);

            return;
        }

        UpdateBounds();
        
        _needRunBack = false;

        _lastDragDelta = localCursor - _dragCurPosition;

        _dragCurPosition = localCursor;
        
        SetContentAnchoredPosition(CalculateContentPos(localCursor));

        UpdateItems(_lastDragDelta);
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
        
        if (!IsDragValid(delta))
        {
            Vector2 contentPos = GetRubberContentPositionOnScroll(delta);
            
            SetContentAnchoredPosition(contentPos);
            
            if ((velocity * Time.deltaTime).magnitude < 25)
            {
                StopMovement();
                
                StartRunBackRoutine();
            }
            
            return;
        }
        
        UpdateItems(delta);
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

    // TODO : Handle Horizontal Movement
    private void UpdateItems(Vector2 delta)
    {
        if (vertical)
        {
            bool positiveDelta = delta.y > 0;
           
            if (positiveDelta &&
                -_Content.GetLastItemPos().y - content.anchoredPosition.y <= viewport.rect.height + _Content.Spacing.y)
            {
                _Content.AddIntoTail();
            }

            if (positiveDelta &&
                content.anchoredPosition.y - -_Content.GetFirstItemPos().y >= 2 * _Content.ItemHeight + _Content.Spacing.y)
            {
                _Content.DeleteFromHead();
            }

            if (!positiveDelta &&
                content.anchoredPosition.y + _Content.GetFirstItemPos().y <= _Content.ItemHeight + _Content.Spacing.y)
            {
                _Content.AddIntoHead();
            }

            if (!positiveDelta &&
                -_Content.GetLastItemPos().y - content.anchoredPosition.y >= viewport.rect.height + _Content.ItemHeight + _Content.Spacing.y)
            {
                _Content.DeleteFromTail();
            }
        }
    }

    // TODO :: Handle Horizontal Movement
    private bool IsDragValid(Vector2 delta)
    {
        if (vertical)
        {
            bool isDragValid = CheckDragValidVertical(delta);
            
            return isDragValid;
        }
        
        return false;
    }

    private bool CheckDragValidVertical(Vector2 delta)
    {
        bool positiveDelta = delta.y > 0;

        if (positiveDelta)
        {
            Vector2 lastItemPos = _Content.GetLastItemPos();
            
            if (!_Content.CanAddNewItemIntoTail() && 
                content.anchoredPosition.y + viewport.rect.height + lastItemPos.y - _Content.ItemHeight > 0)
            {
                return false;
            }
        }
        else
        {
            if (!_Content.CanAddNewItemIntoHead() &&
                content.anchoredPosition.y <= 0)
            {
                return false;
            }
        }
        
        return true;
    }

    // TODO : Handle Vertical Movement
    private Vector2 GetRestrictedContentPositionOnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewRect,
            eventData.position,
            eventData.pressEventCamera, out Vector2 localCursor);

        Vector2 delta = localCursor - _dragCurPosition;

        Vector2 position = CalculateContentPos(localCursor);
        
        if (vertical)
        {
            float restriction = GetVerticalRestrictionWeight(delta);

            Vector2 result = CalculateRestrictedPosition(content.anchoredPosition, position, restriction);
            
            result.x = content.anchoredPosition.x;
            
            return result;
        }

        return Vector2.zero;
    }

    // TODO : Rename Method
    private Vector2 GetRubberContentPositionOnScroll(Vector2 delta)
    {
        float restriction = GetVerticalRestrictionWeight(delta);

        Vector2 deltaPos = velocity * Time.deltaTime;

        Vector2 res = Vector2.zero;

        if (vertical)
        {
            deltaPos.x = 0;
            
            Vector2 curPos = content.anchoredPosition;
        
            Vector2 nextPos = curPos + deltaPos;
            
            res = CalculateRestrictedPosition(curPos, nextPos, restriction);
            
            res.x = 0;
        }

        // TODO : create some variable to slow down free movement
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

    // TODO :: Handle Horizontal Movement
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
                
        return Vector2.zero;
    }
    
    private Vector2 CalculateContentPos(Vector2 localCursor)
    {
        Vector2 dragDelta = localCursor - _dragStartingPosition;

        Vector2 position = _contentStartPos + dragDelta;
        
        return position;
    }
    
    private Vector2 CalculateRestrictedPosition(Vector2 curPos, Vector2 nextPos, float restrictionWeight)
    {
        Vector2 weightedPrev = curPos * restrictionWeight;
        
        Vector2 weightedNext = nextPos * (1 - restrictionWeight);

        Vector2 result = weightedPrev + weightedNext;

        return result;
    }
    
    // TODO :: Consider Renaming
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
