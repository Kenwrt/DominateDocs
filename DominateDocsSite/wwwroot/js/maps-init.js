// wwwroot/js/maps-init.js
// Simple Google Maps interop for Blazor with classic markers.
//
// Exposes window.AppMaps:
//   initMap(el, options)
//   addMarker(address, labelIndex)
//   rebuildAllMarkers(addresses)
//   clearMarkers()
//   getAddressPredictions(query)
//   parseAddressDetails(fullAddress)

(function () {
    let map;
    let geocoder;
    let placesService;
    let autocompleteService;
    let markers = [];

    function haveApi() {
        return !!(window.google && google.maps);
    }

    function waitForGoogle(timeoutMs = 5000) {
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

    function ensureServices() {
        if (!haveApi()) return false;
        geocoder ||= new google.maps.Geocoder();
        placesService ||= new google.maps.places.PlacesService(document.createElement("div"));
        // AutocompleteService is legacy, still works, we’re using it anyway.
        autocompleteService ||= new google.maps.places.AutocompleteService();
        return true;
    }

    async function initMap(el, options) {
        if (!el) return;

        const ready = await waitForGoogle(5000);
        if (!ready) return;
        if (!ensureServices()) return;
        if (map) return; // already initialized

        const defaults = {
            center: { lat: 36.1627, lng: -86.7816 },
            zoom: 11
        };

        map = new google.maps.Map(el, Object.assign(defaults, options || {}));
    }

    async function geocodeAddress(address) {
        if (!address || !ensureServices()) return null;

        return new Promise((resolve) => {
            geocoder.geocode({ address }, (results, status) => {
                if (status === "OK" && results && results.length) {
                    resolve(results[0]);
                } else {
                    resolve(null);
                }
            });
        });
    }

    async function addMarker(address, labelIndex) {
        if (!address || !map) return;

        const result = await geocodeAddress(address);
        if (!result) return;

        const pos = result.geometry.location;
        const label = (labelIndex ?? markers.length) + 1;

        const marker = new google.maps.Marker({
            position: pos,
            map,
            label: String(label)
        });

        markers.push(marker);

        if (markers.length === 1) {
            map.setCenter(pos);
        }
    }

    function clearMarkers() {
        for (const m of markers) {
            if (m && typeof m.setMap === "function") {
                m.setMap(null);
            }
        }
        markers = [];
    }

    async function rebuildAllMarkers(addresses) {
        clearMarkers();
        if (!Array.isArray(addresses) || !addresses.length) return;

        for (let i = 0; i < addresses.length; i++) {
            // eslint-disable-next-line no-await-in-loop
            await addMarker(addresses[i], i);
        }
    }

    async function getAddressPredictions(query) {
        if (!query || !ensureServices()) return [];

        return new Promise((resolve) => {
            autocompleteService.getPlacePredictions({ input: query }, (predictions, status) => {
                if (status !== google.maps.places.PlacesServiceStatus.OK || !predictions) {
                    resolve([]);
                    return;
                }
                resolve(predictions.map((p) => p.description));
            });
        });
    }

    async function parseAddressDetails(fullAddress) {
        const r = await geocodeAddress(fullAddress);
        if (!r) return null;

        const parts = {};
        for (const c of r.address_components) {
            for (const t of c.types) {
                parts[t] = { long: c.long_name, short: c.short_name };
            }
        }

        return {
            fullAddress: r.formatted_address,
            latitude: r.geometry.location.lat(),
            longitude: r.geometry.location.lng(),
            streetNumber: parts.street_number?.long || "",
            route: parts.route?.long || "",
            locality: parts.locality?.long || parts.sublocality?.long || "",
            administrativeArea: parts.administrative_area_level_1?.short || "",
            postalCode: parts.postal_code?.long || "",
            country: parts.country?.short || ""
        };
    }

    window.AppMaps = {
        initMap,
        addMarker,
        rebuildAllMarkers,
        clearMarkers,
        getAddressPredictions,
        parseAddressDetails
    };
})();