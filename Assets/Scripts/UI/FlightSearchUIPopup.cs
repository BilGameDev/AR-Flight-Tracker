using System;
using System.Collections;
using System.Collections.Generic;
using FlightTracker.Data;
using FlightTracker.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Viridian.Utils;

public class FlightSearchUIPopup : SlideUpPopup
{
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Button searchButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform resultsContainer;
    [SerializeField] private FlightListItem listItemPrefab;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private float debounceSeconds = 0.3f;

    public Action<string> OnSearchRequested;
    public Action<AircraftState> OnFlightSelected;

    private Coroutine debounceCoroutine;

    void Start()
    {
        if (searchButton != null)
            searchButton.onClick.AddListener(HandleSearch);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(_ => DebounceSearch());
            searchInput.onSubmit.AddListener(_ => HandleSearch());
        }

        if (searchInput != null)
        {
            searchInput.text = "";
            searchInput.Select();
        }

        SetStatus("Type at least 3 characters to search.");
        Open();
    }

    protected override void OnDestroy()
    {
        if (searchButton != null)
            searchButton.onClick.RemoveListener(HandleSearch);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        base.OnDestroy();
    }

    private void DebounceSearch()
    {
        if (debounceCoroutine != null)
            StopCoroutine(debounceCoroutine);
        debounceCoroutine = StartCoroutine(DebounceRoutine());
    }

    private IEnumerator DebounceRoutine()
    {
        yield return new WaitForSeconds(debounceSeconds);
        HandleSearch();
    }

    public static FlightSearchUIPopup Show()
    {
        var prefab = Resources.Load<FlightSearchUIPopup>("Popups/FlightSearchPopup");
        if (prefab == null)
        {
            Debug.LogError("FlightSearchUIPopup prefab not found at Resources/Popups/FlightSearchPopup");
            return null;
        }

        return Instantiate(prefab);
    }

    public void DisplayResults(AircraftState flight)
    {
        ClearResults();

        if (flight != null)
        {
            SetStatus("Flight found!");
            var item = Instantiate(listItemPrefab, resultsContainer);
            item.Setup(flight, () => OnFlightSelected?.Invoke(flight));
        }
        else
        {
            SetStatus("No flight found with that callsign.");
        }
    }

    public void DisplayResults(List<AircraftState> flights)
    {
        ClearResults();

        if (flights == null || flights.Count == 0)
        {
            SetStatus("No flights found.");
            return;
        }

        SetStatus($"Found {flights.Count} flight(s):");

        foreach (var flight in flights)
        {
            var captured = flight;
            var item = Instantiate(listItemPrefab, resultsContainer);
            item.Setup(flight, () => OnFlightSelected?.Invoke(captured));
        }
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void HandleSearch()
    {
        string query = searchInput?.text?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        ClearResults();
        SetStatus("Searching...");
        OnSearchRequested?.Invoke(query);
    }

    public void ClearResults()
    {
        if (resultsContainer == null) return;
        foreach (Transform child in resultsContainer)
            Destroy(child.gameObject);
    }
}
