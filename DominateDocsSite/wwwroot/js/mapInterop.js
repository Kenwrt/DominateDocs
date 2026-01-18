// Make it global for JS interop string invocation
window.mapInterop = (function () {
    let map;
    let geocoder;
    const markers = {};

    function initMap() {
        map = new google.maps.Map(document.getElementById("map"), {
            center: { lat: 36.1627, lng: -86.7816 },
            zoom: 12
        });
        geocoder = new google.maps.Geocoder();
    }

    function initAutocomplete(inputEl, markerId, dotNetHelper) {
        if (!inputEl) return;
        const ac = new google.maps.places.Autocomplete(inputEl, { types: ["geocode"] });

        ac.addListener("place_changed", () => {
            const place = ac.getPlace();
            if (!place?.geometry?.location) return;

            const dto = toDtoFromPlace(place);
            dto.Lat = place.geometry.location.lat();
            dto.Lng = place.geometry.location.lng();

            setMarker(place.geometry.location, markerId);
            dotNetHelper.invokeMethodAsync("UpdateAddress", markerId, dto);
        });
    }

    function geocodeAddress(address, markerId, dotNetHelper) {
        if (!address) return;
        geocoder.geocode({ address }, (results, status) => {
            if (status === "OK" && results?.[0]) {
                const res = results[0];
                const loc = res.geometry.location;

                const dto = toDtoFromPlace(res);
                dto.Lat = loc.lat();
                dto.Lng = loc.lng();

                setMarker(loc, markerId);
                dotNetHelper.invokeMethodAsync("UpdateAddress", markerId, dto);
            }
        });
    }

    function setMarker(location, markerId) {
        if (markers[markerId]) markers[markerId].setMap(null);
        markers[markerId] = new google.maps.Marker({ position: location, map });
        map.setCenter(location);
    }

    function toDtoFromPlace(place) {
        const dto = {
            FullAddress: place.formatted_address || "",
            StreetAddress: "",
            City: "",
            State: "",
            ZipCode: "",
            County: "",
            Country: "",
            Lat: null,
            Lng: null
        };

        const parts = place.address_components || [];
        for (const c of parts) {
            const t = c.types || [];
            if (t.includes("street_number")) dto.StreetAddress = `${c.long_name} ${dto.StreetAddress}`.trim();
            if (t.includes("route")) dto.StreetAddress = `${dto.StreetAddress} ${c.long_name}`.trim();
            if (t.includes("locality")) dto.City = c.long_name;
            if (t.includes("administrative_area_level_1")) dto.State = c.short_name;
            if (t.includes("postal_code")) dto.ZipCode = c.long_name;
            if (t.includes("administrative_area_level_2")) dto.County = c.long_name;
            if (t.includes("country")) dto.Country = c.long_name;
        }
        return dto;
    }

    return { initMap, initAutocomplete, geocodeAddress };
})();
