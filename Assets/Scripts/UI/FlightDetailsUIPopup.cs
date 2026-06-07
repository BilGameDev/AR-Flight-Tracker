using System.Collections.Generic;
using System.Linq;
using FlightTracker.AR;
using FlightTracker.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Viridian.Utils;

public class FlightDetailsUIPopup : SlideUpPopup
{
    [SerializeField] private TextMeshProUGUI callsignText;
    [SerializeField] private TextMeshProUGUI originText;
    [SerializeField] private TextMeshProUGUI altitudeText;
    [SerializeField] private TextMeshProUGUI velocityText;
    [SerializeField] private TextMeshProUGUI headingText;
    [SerializeField] private TextMeshProUGUI longitudeText;
    [SerializeField] private TextMeshProUGUI latitudeText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    public event System.Action<AircraftState> OnFlightChanged;
    public event System.Action OnClose;

    private AircraftState currentFlight;
    private List<AircraftState> visibleFlights;
    private int currentIndex;

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (previousButton != null)
            previousButton.onClick.AddListener(OnPrevious);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNext);

        var renderer = AppContext.Get<AircraftInstanceRenderer>();
        if (renderer != null)
            visibleFlights = renderer.VisibleFlights.ToList();
    }

    protected override void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
        if (previousButton != null)
            previousButton.onClick.RemoveListener(OnPrevious);
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNext);

        base.OnDestroy();
    }

    public static FlightDetailsUIPopup Show(AircraftState flight)
    {
        var prefab = Resources.Load<FlightDetailsUIPopup>("Popups/FlightDetailsPopup");
        if (prefab == null)
        {
            Debug.LogError("FlightDetailsUIPopup prefab not found at Resources/Popups/FlightDetailsPopup");
            return null;
        }

        var popup = Instantiate(prefab);
        popup.ShowDetails(flight);
        return popup;
    }

    public void ShowDetails(AircraftState flight)
    {
        if (flight == null) return;

        currentFlight = flight;
        RefreshText();

        Open();
    }

    public void UpdateDetails(AircraftState flight)
    {
        if (flight == null) return;

        currentFlight = flight;
        RefreshText();
    }

    private void RefreshText()
    {
        SetText(callsignText, currentFlight.DisplayCallsign);
        SetText(originText, currentFlight.OriginCountry);
        SetText(altitudeText, currentFlight.Altitude.HasValue ? $"{currentFlight.Altitude.Value:F0} m" : "N/A");
        SetText(velocityText, currentFlight.Velocity.HasValue ? $"{currentFlight.Velocity.Value:F1} m/s" : "N/A");
        SetText(headingText, currentFlight.Heading.HasValue ? $"{currentFlight.Heading.Value:F1}°" : "N/A");
        SetText(longitudeText, currentFlight.HasPosition ? $"{currentFlight.Longitude.Value:F4}" : "N/A");
        SetText(latitudeText, currentFlight.HasPosition ? $"{currentFlight.Latitude.Value:F4}" : "N/A");
    }

    private void OnPrevious()
    {
        if (visibleFlights == null || visibleFlights.Count == 0) return;

        currentIndex--;
        if (currentIndex < 0) currentIndex = visibleFlights.Count - 1;

        CycleTo(visibleFlights[currentIndex]);
    }

    private void OnNext()
    {
        if (visibleFlights == null || visibleFlights.Count == 0) return;

        currentIndex++;
        if (currentIndex >= visibleFlights.Count) currentIndex = 0;

        CycleTo(visibleFlights[currentIndex]);
    }

    private void CycleTo(AircraftState flight)
    {
        currentFlight = flight;
        RefreshText();
        OnFlightChanged?.Invoke(flight);
    }

    public override void Close()
    {
        OnClose?.Invoke();
        base.Close();
    }

    private static void SetText(TextMeshProUGUI textField, string value)
    {
        if (textField != null)
            textField.text = value;
    }
}
