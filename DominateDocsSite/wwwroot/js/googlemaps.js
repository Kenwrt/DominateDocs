// Unified Google Maps interop for Blazor
// - Single source of truth for window.AppMaps
// - Robust init (wait for Google + wait for map element)
// - Markers + rebuild + update/remove
// - Autocomplete predictions
// - parseAddressDetails returns DTO-friendly keys (PascalCase + camelCase mirror)

(function () {
    "use strict";

    let map = null;
    let markers = [];                 // indexed markers
    let geocoder = null;
    let autocompleteService = null;
    let placesService = null;

    // ---- Helpers ----
    function haveApi() {
        return !!(window.google && google.maps);
    }

    function ensureServices() {
        if (!haveApi()) return false;

        geocoder ||= new google.maps.Geocoder();

        // places may not be loaded if you didn't request libraries=places
        if (google.maps.places) {
            autocompleteService ||= new google.maps.places.AutocompleteService();
            if (map) {
                placesService ||= new google.maps.places.PlacesService(map);
            }
        }

        return true;
    }

    function safeGet(elOrSelector) {
        if (!elOrSelector) return null;
        if (typeof elOrSelector === "string") return document.querySelector(elOrSelector);
        return elOrSelector;
    }

    function sleep(ms) {
        return new Promise(r => setTimeout(r, ms));
    }

    // ---- Core map lifecycle ----
    async function initializeGoogleMaps() {
        // Called by the timers at bottom, safe if already initialized
        if (!haveApi()) return;

        // If map already exists, don't re-init.
        if (map) return;

        // Try to find common map element IDs/classes
        const mapEl =
            document.getElementById("map") ||
            document.getElementById("googleMap") ||
            document.querySelector(".google-map") ||
            document.querySelector("[data-google-map]");

        if (!mapEl) return;

        map = new google.maps.Map(mapEl, {
            center: { lat: 36.1627, lng: -86.7816 }, // Nashville default
            zoom: 12,
            mapTypeControl: true,
            streetViewControl: false,
            fullscreenControl: false
        });

        ensureServices();
        debugMapStatus();
    }

    async function forceMapInit() {
        // Explicit init requested from Blazor.
        // Wait for google maps library and for the map DOM element to exist.
        for (let i = 0; i < 50; i++) {
            if (haveApi()) break;
            await sleep(100);
        }
        for (let i = 0; i < 50; i++) {
            const mapEl =
                document.getElementById("map") ||
                document.getElementById("googleMap") ||
                document.querySelector(".google-map") ||
                document.querySelector("[data-google-map]");
            if (mapEl) break;
            await sleep(100);
        }

        await initializeGoogleMaps();
    }

    function initMap(elementOrSelector, options) {
        if (!haveApi()) {
            console.error("❌ Google Maps API not loaded (initMap).");
            return false;
        }

        const el = safeGet(elementOrSelector);
        if (!el) {
            console.error("❌ Map element not found (initMap).");
            return false;
        }

        map = new google.maps.Map(el, options || {
            center: { lat: 36.1627, lng: -86.7816 },
            zoom: 12,
            mapTypeControl: true,
            streetViewControl: false,
            fullscreenControl: false
        });

        ensureServices();
        debugMapStatus();
        console.log("✅ Map initialized");
        return true;
    }

    function blazorMapReady(elementOrSelector) {
        // Back-compat function if older code calls it.
        return initMap(elementOrSelector);
    }

    function debugMapStatus() {
        try {
            console.log("✅ Map status:", {
                haveApi: haveApi(),
                mapExists: !!map,
                geocoder: !!geocoder,
                autocompleteService: !!autocompleteService,
                placesService: !!placesService,
                markers: markers.length
            });
        } catch { }
    }

    // ---- Geocode helper ----
    async function geocodeAddress(address) {
        const trimmed = (address || "").trim();
        if (!trimmed) return null;

        if (!haveApi() || !ensureServices() || !geocoder) return null;

        return new Promise((resolve) => {
            geocoder.geocode(
                { address: trimmed, componentRestrictions: { country: "US" } },
                (results, status) => {
                    if (status === "OK" && results && results.length) resolve(results[0]);
                    else resolve(null);
                }
            );
        });
    }

    function clearMarkersInternal() {
        try {
            markers.forEach(m => { try { if (m) m.setMap(null); } catch { } });
        } catch { }
        markers = [];
    }

    // ---- Markers ----
    async function addMapMarker(address, index) {
        if (!address) return;

        if (!map) {
            console.error("❌ Map not ready");
            return;
        }

        // Support either a string address OR an AddressDTO object.
        let title = "";
        let location = null;

        // If passed a DTO with Lat/Lng, use it directly (fast, no geocode).
        if (typeof address === "object") {
            const lat = address.Lat ?? address.lat ?? null;
            const lng = address.Lng ?? address.lng ?? null;

            title = address.FullAddress ?? address.fullAddress ?? "";

            if (lat != null && lng != null) {
                location = new google.maps.LatLng(Number(lat), Number(lng));
            } else if (title) {
                // Fall back to geocoding the full address if coordinates are missing.
                const result = await geocodeAddress(title);
                if (!result) return;
                location = result.geometry.location;
            } else {
                console.warn("⚠ addMapMarker missing address/lat/lng:", address);
                return;
            }
        } else {
            // String input: geocode it.
            title = String(address);
            const result = await geocodeAddress(title);
            if (!result) return;
            location = result.geometry.location;
        }

        const idx = (typeof index === "number" && index >= 0) ? index : markers.filter(m => m).length;

        // replace marker at index if exists
        if (markers[idx]) markers[idx].setMap(null);

        const colors = ["red", "blue", "green", "yellow", "purple"];
        const markerColor = colors[idx % colors.length];

        const marker = new google.maps.Marker({
            position: location,
            map,
            title: title,
            label: { text: String(idx + 1), color: "white", fontWeight: "bold" },
            icon: {
                url: `https://maps.google.com/mapfiles/ms/micons/${markerColor}.png`,
                scaledSize: new google.maps.Size(32, 32),
                anchor: new google.maps.Point(16, 32)
            }
        });

        markers[idx] = marker;

        const active = markers.filter(m => m);
        if (active.length === 1) {
            map.setCenter(location);
            map.setZoom(15);
        } else {
            const bounds = new google.maps.LatLngBounds();
            active.forEach(m => bounds.extend(m.getPosition()));
            map.fitBounds(bounds);
        }
    }

    // Back-compat alias: Blazor currently calls AppMaps.addMarker(...)
    async function addMarker(address, index) {
        return await addMapMarker(address, index);
    }

    async function updateMapMarker(index, address) {
        if (typeof index !== "number" || index < 0) return;

        if (markers[index]) markers[index].setMap(null);
        await addMapMarker(address, index);
    }

    function removeMapMarker(index) {
        if (typeof index !== "number" || index < 0) return;

        if (markers[index]) {
            markers[index].setMap(null);
            markers[index] = null;
        }

        // re-label remaining markers by index
        markers.forEach((m, i) => {
            if (m) {
                m.setLabel({ text: String(i + 1), color: "white", fontWeight: "bold" });
            }
        });
    }

    async function rebuildAllMarkers(addressesOrDtos) {
        if (!map) return;

        clearMarkersInternal();

        if (!Array.isArray(addressesOrDtos) || !addressesOrDtos.length) return;

        for (let i = 0; i < addressesOrDtos.length; i++) {
            // eslint-disable-next-line no-await-in-loop
            await addMapMarker(addressesOrDtos[i], i);
        }
    }

    function clearMarkers() {
        clearMarkersInternal();
    }

    // ---- Autocomplete predictions ----
    async function getAddressPredictions(input) {
        if (!input) return [];
        if (!haveApi() || !ensureServices() || !autocompleteService) {
            // If places library isn't available, return empty.
            return [];
        }

        return new Promise((resolve) => {
            autocompleteService.getPlacePredictions(
                { input: input },
                (predictions, status) => {
                    if (status !== google.maps.places.PlacesServiceStatus.OK || !predictions) {
                        resolve([]);
                        return;
                    }
                    resolve(predictions.map(p => p.description));
                }
            );
        });
    }

    // ---- Address parsing (DTO-friendly) ----
    // Returns BOTH PascalCase and camelCase properties so deserialization can't "mysteriously" fail.
    async function parseAddressDetails(address) {
        const trimmed = (address || "").trim();
        if (!trimmed) return null;

        const r = await geocodeAddress(trimmed);

        const empty = {
            // PascalCase
            FullAddress: trimmed,
            StreetAddress: null,
            City: null,
            State: null,
            ZipCode: null,
            County: null,
            Country: "US",
            Lat: null,
            Lng: null,
            PlaceId: null,
            // camelCase
            fullAddress: trimmed,
            streetAddress: null,
            city: null,
            state: null,
            zipCode: null,
            county: null,
            country: "US",
            lat: null,
            lng: null,
            placeId: null
        };

        if (!r) return empty;

        const parts = {};
        for (const c of (r.address_components || [])) {
            for (const t of (c.types || [])) {
                parts[t] = { long: c.long_name, short: c.short_name };
            }
        }

        const streetNumber = parts.street_number?.long || null;
        const route = parts.route?.long || null;

        const city =
            parts.locality?.long ||
            parts.postal_town?.long ||
            parts.administrative_area_level_3?.long ||
            parts.sublocality_level_1?.long ||
            parts.neighborhood?.long ||
            null;

        const state = parts.administrative_area_level_1?.short || parts.administrative_area_level_1?.long || null;
        const zip = parts.postal_code?.long || null;
        const county = parts.administrative_area_level_2?.long || null;
        const country = parts.country?.short || parts.country?.long || "US";

        const streetAddress = [streetNumber, route].filter(Boolean).join(" ").trim() || null;

        const lat = r.geometry?.location ? r.geometry.location.lat() : null;
        const lng = r.geometry?.location ? r.geometry.location.lng() : null;

        const dto = {
            // PascalCase
            FullAddress: r.formatted_address || trimmed,
            StreetAddress: streetAddress,
            City: city,
            State: state,
            ZipCode: zip,
            County: county,
            Country: country,
            Lat: lat,
            Lng: lng,
            PlaceId: r.place_id || null,

            // camelCase
            fullAddress: r.formatted_address || trimmed,
            streetAddress: streetAddress,
            city: city,
            state: state,
            zipCode: zip,
            county: county,
            country: country,
            lat: lat,
            lng: lng,
            placeId: r.place_id || null
        };

        console.log("✅ Parsed address DTO:", dto);
        return dto;
    }

    // ---- Export (ONE namespace, ONE definition) ----
    window.AppMaps = {
        // init / diagnostics
        initMap,
        blazorMapReady,
        forceMapInit,
        debugMapStatus,

        // predictions + parsing
        getAddressPredictions,
        parseAddressDetails,

        // markers
        addMapMarker,
        addMarker,
        updateMapMarker,
        removeMapMarker,
        rebuildAllMarkers,
        clearMarkers
    };

    // Back-compat aliases (if older code calls these)
    window.initializeGoogleMaps = initializeGoogleMaps;
    window.getAddressPredictions = getAddressPredictions;
    window.addMapMarker = addMapMarker;
    window.addMarker = addMarker;
    window.updateMapMarker = updateMapMarker;
    window.removeMapMarker = removeMapMarker;
    window.rebuildAllMarkers = rebuildAllMarkers;
    window.parseAddressDetails = parseAddressDetails;

    // Safe auto-init attempts (won't double-init)
    setTimeout(() => { if (haveApi()) initializeGoogleMaps(); }, 2000);
    document.addEventListener("DOMContentLoaded", () => {
        setTimeout(() => { if (haveApi()) initializeGoogleMaps(); }, 1000);
    });
})();
