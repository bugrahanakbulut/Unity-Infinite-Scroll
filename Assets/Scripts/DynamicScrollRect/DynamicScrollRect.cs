using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DynamicScrollRect
{
    [Serializable]    
    public class DynamicScrollRestrictionSettings
    {
        [SerializeField] private float _contentOverflowRange = 125f;
        public float ContentOverflowRange => _contentOverflowRange;

        [SerializeField] [Range(0, 1)] private float _contentDecelerationInOverflow = 0.5f;
        public float ContentDecelerationInOverflow => _contentDecelerationInOverflow;
    }
    
    [Serializable] 
    public class FocusSettings
    {
        [SerializeField] private float _focusOffset = 0;
        public float FocusOffset =>  _focusOffset;

        [SerializeField] private float _focusDuration = 0.25f;
        public float FocusDuration => _focusDuration;
    }

    // TODO :: Hidden Rules Between TryRestrictionMovement - GetContentPosition etc.
    public class DynamicScrollRect : ScrollRect
    {
        [SerializeField] private DynamicScrollRestrictionSettings _restrictionSettings = null;

        [SerializeField] private FocusSettings _focusSettings = null;
        
        private bool _isDragging = false;

        private bool _runningBack = false;
        private bool _needRunBack = false;

        private bool _isFocusActive = false;
        
        private Vector2 _contentStartPos = Vector2.zero;
        private Vector2 _dragStartingPosition = Vector2.zero;
        private Vector2 _dragCurPosition = Vector2.zero;
        private Vector2 _lastDragDelta = Vector2.zero;

        private IEnumerator _runBackRoutine;
        private IEnumerator _focusRoutine;
    
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

        public void ResetContent()
        {
            StopMovement();
            
            StopRunBackRoutine();
            
            content.anchoredPosition = Vector2.zero;
        }

        public void StartFocus(ScrollItem focusItem)
        {
            StartFocusItemRoutine(focusItem);
        }

        public void CancelFocus()
        {
            StopFocusItemRoutine();
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
            
            CancelFocus();
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
            if (_runningBack || _isDragging || _isFocusActive)
            {
                return;
            }
        
            Vector2 delta = velocity.normalized;
        
            if (!IsDragValid(delta))
            {
                Vector2 contentPos = GetRestrictedContentPositionOnScroll(delta);
            
                SetContentAnchoredPosition(contentPos);
            
                if ((velocity * Time.deltaTime).magnitude < 5)
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
            
            vertical = !horizontal;
        
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
                    _Content.AddIntoTail();
                }

                if (positiveDelta &&
                    content.anchoredPosition.y - -_Content.GetFirstItemPos().y >= (2 * _Content.ItemHeight) + _Content.Spacing.y)
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

            if (horizontal)
            {
                bool positiveDelta = delta.x > 0;

                if (positiveDelta &&
                    _Content.GetFirstItemPos().x + content.anchoredPosition.x <= _Content.ItemWidth + _Content.Spacing.x)
                {
                    _Content.AddIntoHead();
                }

                if (positiveDelta &&
                    _Content.GetLastItemPos().x + content.anchoredPosition.x >= viewport.rect.width + _Content.ItemWidth + _Content.Spacing.x)
                {
                    _Content.DeleteFromTail();
                }

                if (!positiveDelta &&
                    _Content.GetLastItemPos().x + content.anchoredPosition.x <= viewport.rect.width + _Content.Spacing.x)
                {
                    _Content.AddIntoTail();
                }

                if (!positiveDelta &&
                    _Content.GetFirstItemPos().x + content.anchoredPosition.x >= (2 * _Content.ItemWidth) + _Content.Spacing.x)
                {
                    _Content.DeleteFromHead();
                }
            }
        }

        private bool IsDragValid(Vector2 delta)
        {
            if (vertical)
            {
                return CheckDragValidVertical(delta);
            }
            
            if (horizontal)
            {
                return CheckDragValidHorizontal(delta);
            }
        
            return false;
        }

        private bool CheckDragValidVertical(Vector2 delta)
        {
            bool positiveDelta = delta.y > 0;

            if (positiveDelta)
            {
                Vector2 lastItemPos = _Content.GetLastItemPos();
            
                // Calculate local position of last item's end position in viewport rect
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

        private bool CheckDragValidHorizontal(Vector2 delta)
        {
            bool positiveDelta = delta.x > 0;

            if (positiveDelta)
            {
                if (!_Content.CanAddNewItemIntoHead() && content.anchoredPosition.x >= 0)
                {
                    return false;
                }
            }
            else
            {
                Vector2 lastItemPos = _Content.GetLastItemPos();

                // Calculate local position of last item's end position in viewport rect 
                if (!_Content.CanAddNewItemIntoTail() &&
                    content.anchoredPosition.x + viewport.rect.width + lastItemPos.x + _Content.ItemWidth > 0)
                {
                    return false;
                }
            }
            
            return true;
        }

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

            if (horizontal)
            {
                float restriction = GetHorizontalRestrictionWeight(delta);

                Vector2 result = CalculateRestrictedPosition(content.anchoredPosition, position, restriction);

                result.y = content.anchoredPosition.y;

                return result;
            }
            
            return Vector2.zero;
        }

        private Vector2 GetRestrictedContentPositionOnScroll(Vector2 delta)
        {
            float restriction = 0;
            
            if (vertical)
            {
                restriction = GetVerticalRestrictionWeight(delta);
            }
            else
            {
                restriction = GetHorizontalRestrictionWeight(delta);
            }

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

            velocity *= _restrictionSettings.ContentDecelerationInOverflow;

            return res;
        }

        private float GetVerticalRestrictionWeight(Vector2 delta)
        {
            bool positiveDelta = delta.y > 0;

            float maxLimit = _restrictionSettings.ContentOverflowRange;

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

        private float GetHorizontalRestrictionWeight(Vector2 delta)
        {
            bool positiveDelta = delta.x > 0;
            
            float maxLimit = _restrictionSettings.ContentOverflowRange;

            if (!positiveDelta)
            {
                Vector2 lastItemPos = _Content.GetLastItemPos();
                
                if (lastItemPos.x <= viewport.rect.width - _Content.ItemWidth)
                {
                    float max = lastItemPos.x - maxLimit;
            
                    float cur = content.anchoredPosition.x + lastItemPos.x;

                    float diff = cur - max;
                    
                    return 1f - Mathf.Clamp(diff / maxLimit, 0, 1);
                }
                else
                {
                    float max = -(viewport.rect.width - maxLimit - _Content.ItemWidth);
                
                    float cur = content.anchoredPosition.x + lastItemPos.x;

                    float diff = cur - max;
                    
                    Debug.Log($"{max} {cur}");

                    return 1f - Mathf.Clamp(diff / maxLimit, 0, 1);
                }
            }
            
            float restrictionVal = Mathf.Clamp(Mathf.Abs(content.anchoredPosition.x) / maxLimit, 0, 1);
            
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
                    
                    float target = -(viewport.rect.height - _Content.ItemHeight);
                
                    float cur = content.anchoredPosition.y + lastItemPos.y;

                    float diff = target - cur;

                    return content.anchoredPosition + new Vector2(0, diff);
                }
            }

            if (horizontal)
            {
                if (content.anchoredPosition.x > 0)
                {
                    return Vector2.zero;
                }

                Vector2 lastItemPos = _Content.GetLastItemPos();

                if (lastItemPos.x <= viewport.rect.width - _Content.ItemWidth)
                {
                    return Vector2.zero;
                }

                float target = -(viewport.rect.width - _Content.ItemWidth);

                float cur = content.anchoredPosition.x + lastItemPos.x;
                
                float diff = target - cur;

                return content.anchoredPosition + new Vector2(0, diff);
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

        // TODO : Consider Renaming
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
        
        #region Focus
        
        private Vector2 GetFocusPosition(ScrollItem focusItem)
        {
            Vector2 contentPos = content.anchoredPosition;
            
            if (vertical)
            {
                // focus item above the viewport
                if (contentPos.y + focusItem.RectTransform.anchoredPosition.y > 0)
                {
                    float diff = contentPos.y + focusItem.RectTransform.anchoredPosition.y + _focusSettings.FocusOffset;
                    
                    Vector2 focusPos = contentPos - new Vector2(0, diff);

                    focusPos.y = Mathf.Max(focusPos.y, 0);
                    
                    return focusPos;
                }

                // focus item under the viewport
                if (viewport.rect.height + (contentPos.y + focusItem.RectTransform.anchoredPosition.y - _Content.ItemHeight) < 0)
                {
                    float diff = -contentPos.y - viewport.rect.height +
                                 -focusItem.RectTransform.anchoredPosition.y + _Content.ItemHeight + _focusSettings.FocusOffset;
                    
                    if (_Content.AtTheEndOfContent(focusItem))
                    {
                        return CalculateSnapPosition();
                    }
                    
                    Vector2 focusPos = contentPos + new Vector2(0, diff);

                    float contentMovementLimit = contentPos.y + _Content.GetLastItemPos().y - _Content.ItemHeight +
                                                   viewport.rect.height;

                    focusPos.y = Mathf.Max(focusPos.y, contentMovementLimit);

                    return focusPos;
                }
            }

            if (horizontal)
            {
                // focus item at the left of the viewport
                if (contentPos.x + focusItem.RectTransform.anchoredPosition.x < 0)
                {
                    float diff = contentPos.x + focusItem.RectTransform.anchoredPosition.x - _focusSettings.FocusOffset;

                    Vector2 focusPos = contentPos - new Vector2(0, diff);

                    focusPos.x = Mathf.Max(focusPos.x, 0);

                    return focusPos;
                }

                // focus item at the right of the viewport
                if (viewport.rect.width + (-contentPos.x - focusItem.RectTransform.anchoredPosition.x - _Content.ItemWidth) < 0)
                {
                    float diff = -viewport.rect.width + contentPos.x + focusItem.RectTransform.anchoredPosition.x 
                                 + _Content.ItemWidth - _focusSettings.FocusOffset;

                    if (_Content.AtTheEndOfContent(focusItem))
                    {
                        return CalculateSnapPosition();
                    }

                    Vector2 focusPos = contentPos + new Vector2(0, diff);

                    float contentMoveLimit = -contentPos.x - _Content.GetLastItemPos().x + _Content.ItemWidth +
                                             -viewport.rect.width;

                    focusPos.x = Mathf.Max(focusPos.x, contentMoveLimit);

                    return focusPos;
                }
            }

            return content.anchoredPosition;
        }

        private void StartFocusItemRoutine(ScrollItem scrollItem)
        {
            StopFocusItemRoutine();

            _focusRoutine = FocusProgress(GetFocusPosition(scrollItem));

            StartCoroutine(_focusRoutine);
        }

        private void StopFocusItemRoutine()
        {
            if (_focusRoutine != null)
            {
                StopCoroutine(_focusRoutine);
            }

            _isFocusActive = false;
        }

        private IEnumerator FocusProgress(Vector2 focusPos)
        {
            _isFocusActive = true;

            float timePassed = 0;

            Vector2 startPos = content.anchoredPosition;

            while (timePassed < _focusSettings.FocusDuration)
            {
                timePassed += Time.deltaTime;
                
                Vector2 pos = Vector2.Lerp(startPos, focusPos, timePassed / _focusSettings.FocusDuration);

                Vector2 delta = pos - content.anchoredPosition;
                
                UpdateItems(delta);
                
                SetContentAnchoredPosition(pos);
                
                yield return null;
            }
            
            SetContentAnchoredPosition(focusPos);
            
            _isFocusActive = false;
        }
        
        #endregion
    }
}
