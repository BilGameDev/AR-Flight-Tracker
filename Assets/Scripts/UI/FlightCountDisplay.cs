using TMPro;
using UnityEngine;

namespace FlightTracker.UI
{
    public class FlightCountDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private string prefix = "Aircraft: ";

        private void Awake()
        {
            if (countText == null)
                countText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void UpdateCount(int count)
        {
            if (countText != null)
                countText.text = $"{prefix}{count}";
        }
    }
}
