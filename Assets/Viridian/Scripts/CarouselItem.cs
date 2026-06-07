using System;
using System.Collections.Generic;
using UnityEngine;

namespace Viridian.Utils
{
    public class CarouselItem : MonoBehaviour
    {
        public int Index { get; set; }

        [SerializeField] private List<GameObject> _enabledObjects = new();
        public List<GameObject> EnabledObjects => _enabledObjects;

        public event Action OnFocus;
        public event Action OnLoseFocus;
        public event Action OnAppear;
        public event Action OnDisappear;

        private bool _isFocused;
        private bool _isVisible;

        public void SetFocused(bool focused)
        {
            if (focused == _isFocused) return;
            _isFocused = focused;

            foreach (var obj in _enabledObjects)
            {
                if (obj != null)
                    obj.SetActive(focused);
            }

            if (focused)
                OnFocus?.Invoke();
            else
                OnLoseFocus?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            if (visible == _isVisible) return;
            _isVisible = visible;
            if (visible)
                OnAppear?.Invoke();
            else
                OnDisappear?.Invoke();
        }
    }
}
