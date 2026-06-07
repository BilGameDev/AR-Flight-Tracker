using System;
using System.Collections.Generic;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Viridian.Utils
{
    [ExecuteAlways]
    public class Carousel : LayoutGroup, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private float _spacing = 350f;
        [SerializeField] private Vector3 _centerScale = Vector3.one;
        [SerializeField] private Vector3 _sideScale = new Vector3(0.75f, 0.75f, 1f);
        [SerializeField] private float _sideAlpha = 0.5f;
        [SerializeField] private float _transitionDuration = 0.35f;
        [SerializeField] private Ease _transitionEase = Ease.OutCubic;
        [SerializeField] private bool _wrapAround;
        [SerializeField] private bool _autoScroll;
        [SerializeField] private float _autoScrollInterval = 3f;
        [SerializeField] private bool _tapToSelect;
        [SerializeField] private bool _swipeToChange;
        [SerializeField] private float _swipeThreshold = 50f;

        private readonly Dictionary<RectTransform, CanvasGroup> _cache = new();
        private readonly Dictionary<RectTransform, SlotPos> _targetSlots = new();
        private int _currentIndex;
        private bool _layoutApplied;
        private bool _isTransitioning;
        private readonly CompositeMotionHandle _carouselMotions = new();
        private Vector2 _dragStartPos;
        private bool _isDraggingSwipe;

        public int CurrentIndex => _currentIndex;
        public int ItemCount => rectChildren.Count;
        public RectTransform CurrentItem => _currentIndex >= 0 && _currentIndex < rectChildren.Count ? rectChildren[_currentIndex] : null;

        public event Action<int> OnIndexChanged;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            var totalWidth = 2f * _spacing + padding.horizontal;
            SetLayoutInputForAxis(totalWidth, totalWidth, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            var totalHeight = padding.vertical;
            SetLayoutInputForAxis(totalHeight, totalHeight, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            EnsureCanvasGroups();
            CalculateSlots();
            ApplyLayout(false);
            if (!_layoutApplied)
            {
                _layoutApplied = true;
                UpdateItemFocus();
                UpdateItemVisibility();
                StartAutoScroll();
            }
            if (_tapToSelect)
                SetupTapHandlers();
        }

        public override void SetLayoutVertical() { }

        public void Next()
        {
            if (_isTransitioning || rectChildren.Count == 0) return;
            var next = _currentIndex + 1;
            if (next >= rectChildren.Count)
            {
                if (!_wrapAround) return;
                next = 0;
            }
            GoTo(next);
        }

        public void Previous()
        {
            if (_isTransitioning || rectChildren.Count == 0) return;
            var prev = _currentIndex - 1;
            if (prev < 0)
            {
                if (!_wrapAround) return;
                prev = rectChildren.Count - 1;
            }
            GoTo(prev);
        }

        public void GoTo(int index)
        {
            if (_isTransitioning || rectChildren.Count == 0) return;
            index = Mathf.Clamp(index, 0, rectChildren.Count - 1);
            if (index == _currentIndex) return;

            GetCarouselItem(_currentIndex)?.SetFocused(false);

            _currentIndex = index;
            CalculateSlots();
            ApplyLayout(true);
            ResetAutoScroll();

            GetCarouselItem(_currentIndex)?.SetFocused(true);
            UpdateItemVisibility();
            OnIndexChanged?.Invoke(_currentIndex);
        }

        private void EnsureCanvasGroups()
        {
            for (int i = 0; i < rectChildren.Count; i++)
            {
                var child = rectChildren[i];
                if (!_cache.ContainsKey(child))
                {
                    if (!child.TryGetComponent(out CanvasGroup cg))
                        cg = child.gameObject.AddComponent<CanvasGroup>();
                    _cache[child] = cg;
                }
                if (!child.TryGetComponent(out CarouselItem item))
                    item = child.gameObject.AddComponent<CarouselItem>();
                item.Index = i;
            }
        }

        private void SetupTapHandlers()
        {
            for (int i = 0; i < rectChildren.Count; i++)
            {
                var child = rectChildren[i];
                var index = i;

                if (!child.TryGetComponent(out Button btn))
                {
                    btn = child.gameObject.AddComponent<Button>();
                    var img = child.GetComponent<Graphic>();
                    if (img != null)
                        btn.targetGraphic = img;
                    else if (child.childCount > 0)
                    {
                        var childImg = child.GetComponentInChildren<Graphic>();
                        if (childImg != null)
                            btn.targetGraphic = childImg;
                    }
                }

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => GoTo(index));
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_swipeToChange || !isActiveAndEnabled) return;
            _dragStartPos = eventData.position;
            _isDraggingSwipe = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_swipeToChange || !_isDraggingSwipe) return;
            _isDraggingSwipe = false;

            var delta = eventData.position.x - _dragStartPos.x;
            if (Mathf.Abs(delta) >= _swipeThreshold)
            {
                if (delta < 0)
                    Next();
                else
                    Previous();
            }
        }

        private CarouselItem GetCarouselItem(int index)
        {
            if (index < 0 || index >= rectChildren.Count) return null;
            rectChildren[index].TryGetComponent(out CarouselItem item);
            return item;
        }

        private void UpdateItemFocus()
        {
            GetCarouselItem(_currentIndex)?.SetFocused(true);
        }

        private void UpdateItemVisibility()
        {
            for (int i = 0; i < rectChildren.Count; i++)
            {
                if (!_targetSlots.TryGetValue(rectChildren[i], out var slot)) continue;
                GetCarouselItem(i)?.SetVisible(slot != SlotPos.LeftExit && slot != SlotPos.RightExit);
            }
        }

        private void CalculateSlots()
        {
            _targetSlots.Clear();
            for (int i = 0; i < rectChildren.Count; i++)
            {
                _targetSlots[rectChildren[i]] = GetSlot(i);
            }
        }

        private SlotPos GetSlot(int itemIndex)
        {
            if (itemIndex == _currentIndex) return SlotPos.Center;

            bool isPrev;
            bool isNext;
            if (_wrapAround)
            {
                var prev = _currentIndex - 1;
                if (prev < 0) prev = rectChildren.Count - 1;
                var next = _currentIndex + 1;
                if (next >= rectChildren.Count) next = 0;
                isPrev = itemIndex == prev;
                isNext = itemIndex == next;
            }
            else
            {
                isPrev = itemIndex == _currentIndex - 1;
                isNext = itemIndex == _currentIndex + 1;
            }

            if (isPrev) return SlotPos.Left;
            if (isNext) return SlotPos.Right;

            return itemIndex < _currentIndex ? SlotPos.LeftExit : SlotPos.RightExit;
        }

        private void ApplyLayout(bool animated)
        {
            if (animated) _isTransitioning = true;

            _carouselMotions.Cancel();

            foreach (var child in rectChildren)
            {
                if (!_targetSlots.TryGetValue(child, out var slot)) continue;
                if (!_cache.TryGetValue(child, out var cg)) continue;

                var targetPos = GetSlotPosition(slot);
                var targetScale = GetSlotScale(slot);
                var targetAlpha = GetSlotAlpha(slot);

                if (animated)
                {
                    LMotion.Create(child.anchoredPosition, targetPos, _transitionDuration)
                        .WithEase(_transitionEase)
                        .Bind(x => child.anchoredPosition = x)
                        .AddTo(_carouselMotions);

                    LMotion.Create(child.localScale, targetScale, _transitionDuration)
                        .WithEase(_transitionEase)
                        .Bind(x => child.localScale = x)
                        .AddTo(_carouselMotions);

                    LMotion.Create(cg.alpha, targetAlpha, _transitionDuration)
                        .WithEase(_transitionEase)
                        .Bind(x => cg.alpha = x)
                        .AddTo(_carouselMotions);
                }
                else
                {
                    child.anchoredPosition = targetPos;
                    child.localScale = targetScale;
                    cg.alpha = targetAlpha;
                }
            }

            if (animated)
                LMotion.Create(0f, 1f, _transitionDuration)
                    .WithOnComplete(() => _isTransitioning = false)
                    .RunWithoutBinding()
                    .AddTo(_carouselMotions);
        }

        private Vector2 GetSlotPosition(SlotPos slot)
        {
            switch (slot)
            {
                case SlotPos.Left:
                    return new Vector2(-_spacing, 0f);
                case SlotPos.LeftExit:
                    return new Vector2(-_spacing * 1.8f, 0f);
                case SlotPos.Center:
                    return Vector2.zero;
                case SlotPos.Right:
                    return new Vector2(_spacing, 0f);
                case SlotPos.RightExit:
                    return new Vector2(_spacing * 1.8f, 0f);
                default:
                    return Vector2.zero;
            }
        }

        private Vector3 GetSlotScale(SlotPos slot)
        {
            switch (slot)
            {
                case SlotPos.Center: return _centerScale;
                case SlotPos.Left:
                case SlotPos.Right: return _sideScale;
                default: return Vector3.zero;
            }
        }

        private float GetSlotAlpha(SlotPos slot)
        {
            switch (slot)
            {
                case SlotPos.Center: return 1f;
                case SlotPos.Left:
                case SlotPos.Right: return _sideAlpha;
                default: return 0f;
            }
        }

        protected override void OnTransformChildrenChanged()
        {
            base.OnTransformChildrenChanged();
            _cache.Clear();
            _currentIndex = Mathf.Clamp(_currentIndex, 0, Mathf.Max(0, rectChildren.Count - 1));
            for (int i = 0; i < rectChildren.Count; i++)
            {
                if (rectChildren[i].TryGetComponent(out CarouselItem item))
                    item.Index = i;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            StopAutoScroll();
        }

        private void StartAutoScroll()
        {
            StopAutoScroll();
            if (!_autoScroll || rectChildren.Count < 2) return;
            InvokeRepeating(nameof(TickAutoScroll), _autoScrollInterval, _autoScrollInterval);
        }

        private void StopAutoScroll()
        {
            CancelInvoke(nameof(TickAutoScroll));
        }

        private void TickAutoScroll()
        {
            if (!_isTransitioning && rectChildren.Count >= 2 && isActiveAndEnabled)
                Next();
        }

        private void ResetAutoScroll()
        {
            if (!_autoScroll) return;
            StopAutoScroll();
            StartAutoScroll();
        }

        private enum SlotPos { LeftExit, Left, Center, Right, RightExit }
    }
}
