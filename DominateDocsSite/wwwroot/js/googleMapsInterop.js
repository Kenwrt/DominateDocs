// /wwwroot/js/googleMapsInterop.js
// Minimal, robust loader + Places Autocomplete + Map + red marker handling.
// You must provide a valid API key with Maps JavaScript API + Places API enabled.

export const maps = (() => {
    let googleLoaded = false;
    let loaderPromise = null;
    let map, marker, autocomplete, geocoder, dotnetRef;
    let freeTextTimer;

    function loadGoogle(apiKey) {
        if (loaderPromise) return loaderPromise;

        loaderPromise = new Promise((resolve, reject) => {
            if (window.google && window.google.maps) {
                googleLoaded = true;
                resolve();
                return;
            }
            const script = document.createElement("script");
            const params = new URLSearchParams({
                key: apiKey,
                libraries: "places",
                v: "weekly"
            });
            script.src = `https://maps.googleapis.com/maps/api/js?${params.toString()}`;
            script.async = true;
            script.defer = true;
            script.onload = () => { googleLoaded = true; resolve(); };
            script.onerror = () => reject(new Error("Failed to load Google Maps JS"));
            document.head.appendChild(script);
        });

        return loaderPromise;
    }

    function parseAddressComponents(components) {
        const get = (type, want = "long_name") => {
            const c = components.find(x => x.types.includes(type));
            return c ? c[want] : null;
        };

        return {
            StreetNumber: get("street_number", "short_name"),
            Route: get("route"),
            Subpremise: get("subpremise"),
            Locality: get("locality"), // city proper
            Sublocality: get("sublocality") || get("sublocality_level_1"),
            Neighborhood: get("neighborhood"),
            County: get("administrative_area_level_2"),
            StateShort: get("administrative_area_level_1", "short_name"),
            PostalCode: get("postal_code"),
            Country: get("country"),
        };
    }

    async function initAutocompleteAndMap(args) {
        // You can stash your key in a meta tag to avoid hardcoding in code:
        // <meta name="google-maps-key" content="YOUR_KEY">
        const meta = document.querySelector('meta[name="google-maps-key"]');
        const key = meta?.content || window.__GOOGLE_MAPS_KEY__ || "REPLACE_WITH_YOUR_API_KEY";

        await loadGoogle(key);

        const input = args.input;
        dotnetRef = args.dotNetRef;
        geocoder = new google.maps.Geocoder();

        // Map
        map = new google.maps.Map(document.getElementById(args.mapDivId), {
            zoom: 14,
            center: { lat: 36.1627, lng: -86.7816 }, // Nashville default because obviously
            mapTypeControl: false,
            streetViewControl: false,
            fullscreenControl: false,
        });

        marker = new google.maps.Marker({
            map,
            icon: "http://maps.google.com/mapfiles/ms/icons/red-dot.png"
        });

        // Autocomplete
        autocomplete = new google.maps.places.Autocomplete(input, {
            fields: ["formatted_address", "address_components", "geometry"],
            componentRestrictions: args.country ? { country: [args.country] } : undefined
        });

        autocomplete.addListener("place_changed", () => {
            const place = autocomplete.getPlace();
            if (!place || !place.geometry || !place.address_components) return;

            const parts = parseAddressComponents(place.address_components);
            const loc = place.geometry.location;
            map.setCenter(loc);
            marker.setPosition(loc);

            dotnetRef.invokeMethodAsync("OnPlaceSelected", {
                ...parts,
                Formatted: place.formatted_address || null,
                Lat: loc.lat(),
                Lng: loc.lng()
            });
        });
    }

    function queueFreeTextGeocode(text) {
        clearTimeout(freeTextTimer);
        if (!text || text.trim().length < 5) return; // avoid junk
        freeTextTimer = setTimeout(() => setFromFreeText(text), 400);
    }

    function setFromFreeText(text) {
        if (!geocoder) return;
        geocoder.geocode({ address: text }, (results, status) => {
            if (status !== "OK" || !results || results.length === 0) return;
            const r = results[0];
            const loc = r.geometry.location;
            map.setCenter(loc);
            marker.setPosition(loc);

            const parts = parseAddressComponents(r.address_components || []);
            dotnetRef.invokeMethodAsync("OnPlaceSelected", {
                ...parts,
                Formatted: r.formatted_address || text,
                Lat: loc.lat(),
                Lng: loc.lng()
            });
        });
    }

    // Parse address details using Google Geocoding API
    window.parseAddressDetails = function (address) {
        return new Promise((resolve) => {
            if (!address || address.trim() === '') {
                resolve(null);
                return;
            }

            const geocoder = new google.maps.Geocoder();

            geocoder.geocode({
                address: address.trim()
            }, (results, status) => {
                if (status === 'OK' && results && results[0]) {
                    const result = results[0];
                    const location = result.geometry.location;
                    const components = result.address_components;

                    // Parse address components
                    const addressDto = {
                        fullAddress: result.formatted_address,
                        streetAddress: '',
                        city: '',
                        state: '',
                        zipCode: '',
                        county: '',
                        country: '',
                        lat: location.lat(),
                        lng: location.lng()
                    };

                    // Extract components
                    components.forEach(component => {
                        const types = component.types;

                        if (types.includes('street_number')) {
                            addressDto.streetAddress = component.long_name + ' ';
                        }
                        if (types.includes('route')) {
                            addressDto.streetAddress += component.long_name;
                        }
                        if (types.includes('locality')) {
                            addressDto.city = component.long_name;
                        }
                        if (types.includes('administrative_area_level_1')) {
                            addressDto.state = component.short_name;
                        }
                        if (types.includes('postal_code')) {
                            addressDto.zipCode = component.long_name;
                        }
                        if (types.includes('administrative_area_level_2')) {
                            addressDto.county = component.long_name;
                        }
                        if (types.includes('country')) {
                            addressDto.country = component.long_name;
                        }
                    });

                    console.log('📍 Parsed address:', addressDto);
                    resolve(addressDto);
                } else {
                    console.error('❌ Failed to parse address:', status);
                    // Return basic address info if geocoding fails
                    resolve({
                        fullAddress: address,
                        streetAddress: '',
                        city: '',
                        state: '',
                        zipCode: '',
                        county: '',
                        country: '',
                        lat: null,
                        lng: null
                    });
                }
            });
        });
    };

    return {
        initAutocompleteAndMap,
        queueFreeTextGeocode,
        setFromFreeText
    };
})();
