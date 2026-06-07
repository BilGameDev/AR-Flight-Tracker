# Flight Tracker — AR Aircraft Browser

<img width="1080" height="2340" alt="Screenshot_20260607_220827_AR Flight Tracker" src="https://github.com/user-attachments/assets/a96b6a53-7501-469d-934c-f8d6c3049ead" />
<img width="1080" height="2340" alt="Screenshot_20260607_220655_AR Flight Tracker" src="https://github.com/user-attachments/assets/8cd9c58d-53f3-4db8-9e79-a86461edc6ad" />
<img width="1080" height="2340" alt="Screenshot_20260607_220721_AR Flight Tracker" src="https://github.com/user-attachments/assets/ab6de4c6-1f46-4dd5-889c-02842fc9f21a" />

An augmented reality app that lets you point your phone at the sky and see all aircraft in that direction projected onto a 3D dome, with flight details and photos.

Built with **Unity 6** • **AR Foundation 6** • **URP** • **OpenSky Network API**

---

## How it works

1. **Point & Scan** — Aim your phone at the sky and tap SCAN. The app queries OpenSky in the camera's bearing direction.
2. **Tap a plane** — See callsign, origin, altitude, velocity, heading, and coordinates. Previous/Next cycles through visible flights.

---

## Setup

### Requirements
- Unity 6000.0.x or later
- AR Foundation 6.3.2
- URP

### Credentials
1. Create `Assets/Resources/Secrets/credentials.json`:
   ```json
   {
     "client_id": "your-opensky-client-id",
     "client_secret": "your-opensky-client-secret"
   }
   ```
2. Register an application at [OpenSky Network Auth](https://auth.opensky-network.org/auth/realms/opensky-network/protocol/openid-connect/auth) to get credentials.

### Project Settings
- **Android**: `SkipPermissionsDialog = true` in Project Settings → Player → Android Settings (location permission is requested at runtime).
- **Location**: Enable the Location service in Project Settings.

### Test Mode
Set `Use Test Location` in the `FlightTrackerConfig` asset with test lat/lon values. The app falls back to these when GPS is unavailable.

---

### Key scripts

| Script | Responsibility |
|--------|---------------|
| `FlightTrackerManager` | Orchestration — scan, search, tap handling, popup reuse, orbit toggle |
| `FlightServiceManager` | Registers all services in AppContext |
| `BootstrapManager` | Auth gate → loads Home |
| `OpenSkyService` | OAuth2 + OpenSky API calls |
| `UserLocationService` | GPS init with runtime permission flow |
| `FlightQueryService` | Prefix-based callsign search |
| `AircraftInstanceRenderer` | Dome projection, billboard sprites, GPU instancing, heading rotation, selection highlight |
| `AircraftTapHandler` | Tap/click detection via New Input System EnhancedTouch |
| `FlightPointer` | Off-screen indicator — creates own Canvas + arrow sprite |
| `OrbitCameraController` | Swipe-to-orbit around the dome |
| `GeoAnchorService` | Geo origin for real-world → Unity coordinate conversion |
| `AircraftMovementSimulator` | Smooths position/heading between API updates |
| `FlightDataCache` | Caches states by ICAO24 |

### Data flow
```
Scan button → bearing from Camera.main.forward
  → ScanDirection(bearing)
    → GeoUtils.DestinationPoint (4 corners of bearing-aligned rectangle)
    → OpenSky states/all?lamin=...&lomax=...
    → AircraftMovementSimulator.UpdateData(states)
    → AircraftInstanceRenderer.UpdateInstances (dome projection)
    → Graphics.DrawMeshInstanced each frame
```

---

## Controls

| Input | Action |
|-------|--------|
| Tap aircraft | Select + show details |
| SCAN button | Query OpenSky in camera direction |
| SEARCH button | Open prefix search popup |
| Camera toggle | Enable/disable AR camera background |
| Orbit toggle | Switch between AR tracking and swipe-to-orbit |
| Prev / Next (details) | Cycle through visible flights |

---

## OpenSky API

- **Auth**: OAuth2 (client credentials). Falls back to unauthenticated if token fails.
- **Rate limits**: ~10 req/min anonymous, 400/day with OAuth2.
- The app uses manual scan mode — no auto-refresh to stay within limits.

---

## Tech notes

- `SlideUpPopup` uses LitMotion Tweens for animated open/close.
- All aircraft rendered with GPU instancing (`Graphics.DrawMeshInstanced`) for performance.
