window.addrAuto = (function () {
    const instances = new Map();

    function init(inputId, mapId, dotnetRef) {
        const input = document.getElementById(inputId);
        if (!input) return;

        const ac = new google.maps.places.Autocomplete(input, {
            fields: ["address_components", "formatted_address", "geometry", "place_id", "name"],
            types: ["address"]
        });

        let map = null, marker = null;
        if (mapId) {
            const mapEl = document.getElementById(mapId);
            if (mapEl) {
                map = new google.maps.Map(mapEl, { zoom: 14, center: { lat: 39.5, lng: -98.35 } });
                marker = new google.maps.Marker({ map });
            }
        }

        const handler = () => {
            const place = ac.getPlace();
            if (!place || !place.geometry) return;

            const lat = place.geometry.location.lat();
            const lng = place.geometry.location.lng();

            // Push parsed components to .NET
            dotnetRef.invokeMethodAsync("OnPlaceChanged", {
                formattedAddress: place.formatted_address || input.value,
                placeId: place.place_id || null,
                lat, lng,
                components: (place.address_components || []).map(c => ({
                    long_name: c.long_name, short_name: c.short_name, types: c.types
                }))
            });

            if (map && marker) {
                map.setCenter({ lat, lng });
                marker.setPosition({ lat, lng });
                marker.setTitle(place.formatted_address || "");
            }
        };

        ac.addListener("place_changed", handler);
        instances.set(inputId, { ac, map, marker });
    }

    function destroy(inputId) {
        const inst = instances.get(inputId);
        if (!inst) return;
        instances.delete(inputId);
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

    return { init, destroy };
})();
