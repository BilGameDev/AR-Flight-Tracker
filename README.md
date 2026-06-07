# Flight Tracker — AR Aircraft Browser

An augmented reality app that lets you point your phone at the sky and see all aircraft in that direction projected onto a 3D dome, with flight details and photos.

Built with **Unity 6** • **AR Foundation 6** • **URP** • **OpenSky Network API**

---

## How it works

1. **Point & Scan** — Aim your phone at the sky and tap SCAN. The app queries OpenSky in the camera's bearing direction.
2. **Dome projection** — Aircraft positions are projected onto a 30-unit dome around the camera. Altitude maps from horizon (0m) to zenith (12,000m+).
3. **Tap a plane** — See callsign, origin, altitude, velocity, heading, and coordinates. Previous/Next cycles through visible flights.
4. **Off-screen arrows** — When a selected plane leaves the viewport, a green arrow points toward it from the screen edge.
5. **Orbit mode** — Disable AR tracking and swipe to orbit the dome manually.
6. **Search** — Type a partial callsign (3+ chars) to find flights by prefix.

---

## Scenes

| Scene | Purpose |
|-------|---------|
| `Bootstrap` | Authenticates with OpenSky (OAuth2), then loads Home |
| `Home` | Main AR experience — scan, tap, search, details |

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

## Architecture

### Core pattern
- **Services** are registered in `AppContext` (a lightweight service locator in `Viridian.Utils`) on the Bootstrap scene.
- **Scene components** use `[SerializeField]` with `FindFirstObjectByType` / `AppContext.Get` as fallback.
- No `_` prefix on private fields.

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

### UI popups

| Popup | extends | Use |
|-------|---------|-----|
| `FlightDetailsUIPopup` | `SlideUpPopup` | Flight info + Next/Prev cycle |
| `FlightSearchUIPopup` | `SlideUpPopup` | Prefix search with live results |

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
| Tap empty (not on UI) | Nothing (selection cleared on popup close) |
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

- Dependencies found via `AppContext` or `FindFirstObjectByType` — no tight coupling.
- `FlightPointer` generates its own Canvas + arrow at runtime if no inspector references are assigned.
- `SlideUpPopup` uses LitMotion for animated open/close.
- All aircraft rendered with GPU instancing (`Graphics.DrawMeshInstanced`) for performance.
- Selection highlight draws as a separate scaled-up mesh in `selectedColor` outside the instanced batch.
