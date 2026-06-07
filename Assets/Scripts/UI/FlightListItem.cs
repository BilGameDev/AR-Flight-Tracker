using System;
using FlightTracker.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlightTracker.UI
{
    public class FlightListItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI callsignText;
        [SerializeField] private TextMeshProUGUI originText;
        [SerializeField] private TextMeshProUGUI velocityText;
        [SerializeField] private Button selectButton;

        private AircraftState flight;

        public void Setup(AircraftState flight, Action onClick)
        {
            this.flight = flight;

            if (callsignText != null)
                callsignText.text = flight.DisplayCallsign;

            if (originText != null)
                originText.text = flight.OriginCountry;

            if (velocityText != null)
                velocityText.text = flight.Velocity.HasValue ? $"{flight.Velocity.Value:F0}m/s" : "---";

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => onClick?.Invoke());
            }
        }

    }
}
