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
            // PlacesService needs an element; we can use the map if it exists, else a detached div
            placesService ||= new google.maps.places.PlacesService(map || document.createElement("div"));
        }

        return true;
    }

    function waitForGoogle(timeoutMs = 8000) {
        if (haveApi()) return Promise.resolve(true);

        const start = Date.now();
        return new Promise((resolve) => {
            const tick = () => {
                if (haveApi()) return resolve(true);
                if (Date.now() - start > timeoutMs) return resolve(false);
                setTimeout(tick, 50);
            };
            tick();
        });
    }

    function waitForElement(getEl, timeoutMs = 8000) {
        const start = Date.now();
        return new Promise((resolve) => {
            const tick = () => {
                const el = getEl();
                if (el) return resolve(el);
                if (Date.now() - start > timeoutMs) return resolve(null);
                setTimeout(tick, 50);
            };
            tick();
        });
    }

    function clearMarkersInternal() {
        for (const m of markers) {
            if (m && typeof m.setMap === "function") m.setMap(null);
        }
        markers = [];
    }

    // ---- Init ----
    async function initMap(elOrId, options) {
        const ready = await waitForGoogle(8000);
        if (!ready) {
            console.error("❌ Google Maps API did not load in time.");
            return false;
        }

        const el = typeof elOrId === "string"
            ? document.getElementById(elOrId)
            : elOrId;

        if (!el) {
            console.error("❌ initMap: element not found.");
            return false;
        }

        ensureServices();

        // already initialized
        if (map) return true;

        const defaults = {
            center: { lat: 36.1627, lng: -86.7816 }, // Nashville
            zoom: 12,
            mapTypeId: google.maps.MapTypeId.ROADMAP
        };

        map = new google.maps.Map(el, Object.assign(defaults, options || {}));

        // Now that map exists, placesService can bind to it
        if (google.maps.places) {
            placesService = new google.maps.places.PlacesService(map);
        }

        console.log("✅ Map initialized");
        return true;
    }

    // Back-compat: your old code used initializeGoogleMaps and a hardcoded element id="map"
    async function initializeGoogleMaps() {
        const ready = await waitForGoogle(8000);
        if (!ready) {
            console.error("❌ Google Maps API not ready.");
            return;
        }

        const el = await waitForElement(() => document.getElementById("map"), 8000);
        if (!el) {
            console.error("❌ Map element #map not found.");
            return;
        }

        await initMap(el, null);
    }

    // Google callback name commonly used: &callback=initMap
    window.initMap = initializeGoogleMaps;

    async function blazorMapReady() {
        // call from OnAfterRender when your component is ready
        await initializeGoogleMaps();
    }

    function forceMapInit() {
        console.log("💪 Force initializing map...");
        initializeGoogleMaps();
    }

    function debugMapStatus() {
        console.log("🔍 Debug Status:");
        console.log("- Google available:", typeof google !== "undefined");
        console.log("- Maps available:", haveApi());
        console.log("- Map object:", map);
        console.log("- Map element:", document.getElementById("map"));
        console.log("- Markers:", markers);
        console.log("- Active markers:", markers.filter(m => m).length);
    }

    // ---- Autocomplete ----
    async function getAddressPredictions(input) {
        if (!input || input.length < 3) return [];

        const ready = await waitForGoogle(8000);
        if (!ready) return [];

        ensureServices();
        if (!autocompleteService) return [];

        return new Promise((resolve) => {
            autocompleteService.getPlacePredictions(
                {
                    input,
                    types: ["address"],
                    componentRestrictions: { country: "us" }
                },
                (predictions, status) => {
                    if (status === google.maps.places.PlacesServiceStatus.OK && predictions) {
                        resolve(predictions.map(p => p.description));
                    } else {
                        resolve([]);
                    }
                }
            );
        });
    }

    // ---- Geocoding ----
    async function geocodeAddress(address) {
        if (!address) return null;

        const ready = await waitForGoogle(8000);
        if (!ready) return null;

        ensureServices();
        if (!geocoder) return null;

        const trimmed = address.trim();
        if (!trimmed) return null;

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

    // ---- Markers ----
    async function addMapMarker(address, index) {
        if (!address) return;

        if (!map) {
            console.error("❌ Map not ready");
            return;
        }

        const result = await geocodeAddress(address);
        if (!result) return;

        const location = result.geometry.location;

        const idx = (typeof index === "number" && index >= 0) ? index : markers.filter(m => m).length;

        // replace marker at index if exists
        if (markers[idx]) markers[idx].setMap(null);

        const colors = ["red", "blue", "green", "yellow", "purple"];
        const markerColor = colors[idx % colors.length];

        const marker = new google.maps.Marker({
            position: location,
            map,
            title: address,
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

    async function rebuildAllMarkers(addresses) {
        if (!map) return;

        clearMarkersInternal();

        if (!Array.isArray(addresses) || !addresses.length) return;

        for (let i = 0; i < addresses.length; i++) {
            // eslint-disable-next-line no-await-in-loop
            await addMapMarker(addresses[i], i);
        }
    }

    function clearMarkers() {
        clearMarkersInternal();
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

        let zip = parts.postal_code?.long || null;
        const zipSuffix = parts.postal_code_suffix?.long || null;
        if (zip && zipSuffix) zip = `${zip}-${zipSuffix}`;

        const streetAddress = (streetNumber || route)
            ? `${streetNumber ? streetNumber : ""}${streetNumber && route ? " " : ""}${route ? route : ""}`.trim()
            : null;

        const fullAddress = r.formatted_address || trimmed;
        const state = parts.administrative_area_level_1?.short || null;
        const county = parts.administrative_area_level_2?.long || null;
        const country = parts.country?.short || "US";
        const lat = r.geometry?.location ? r.geometry.location.lat() : null;
        const lng = r.geometry?.location ? r.geometry.location.lng() : null;
        const placeId = r.place_id || null;

        const dto = {
            // PascalCase
            FullAddress: fullAddress,
            StreetAddress: streetAddress,
            City: city,
            State: state,
            ZipCode: zip,
            County: county,
            Country: country,
            Lat: lat,
            Lng: lng,
            PlaceId: placeId,

            // camelCase mirror
            fullAddress: fullAddress,
            streetAddress: streetAddress,
            city: city,
            state: state,
            zipCode: zip,
            county: county,
            country: country,
            lat: lat,
            lng: lng,
            placeId: placeId
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
        updateMapMarker,
        removeMapMarker,
        rebuildAllMarkers,
        clearMarkers
    };

    // Back-compat aliases (if older code calls these)
    window.initializeGoogleMaps = initializeGoogleMaps;
    window.getAddressPredictions = getAddressPredictions;
    window.addMapMarker = addMapMarker;
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
