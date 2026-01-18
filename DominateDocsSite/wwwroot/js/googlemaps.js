// Global variables
let map = null;
let markers = [];
let autocompleteService = null;
let placesService = null;

// Initialize Google Maps (called by API callback)
window.initializeGoogleMaps = function () {
    console.log('🗺️ Google Maps API loaded, initializing...');

    const mapElement = document.getElementById("map");
    if (!mapElement) {
        console.log('⏳ Map element not found yet, will retry...');
        setTimeout(window.initializeGoogleMaps, 500);
        return;
    }

    try {
        // Clear any loading content
        mapElement.innerHTML = '';

        // Create the map
        map = new google.maps.Map(mapElement, {
            zoom: 12,
            center: { lat: 36.1627, lng: -86.7816 }, // Nashville, TN
            mapTypeId: google.maps.MapTypeId.ROADMAP
        });

        // Initialize services
        if (google.maps.places) {
            autocompleteService = new google.maps.places.AutocompleteService();
            placesService = new google.maps.places.PlacesService(map);
        }

        console.log('✅ Google Maps initialized successfully!');
    } catch (error) {
        console.error('❌ Error initializing map:', error);
    }
};

// API callback function
window.initMap = window.initializeGoogleMaps;

// Blazor-callable functions
window.blazorMapReady = function () {
    console.log('⚡ Blazor component ready');
    if (typeof google !== 'undefined' && google.maps) {
        window.initializeGoogleMaps();
    } else {
        console.log('⏳ Waiting for Google Maps API...');
        setTimeout(window.blazorMapReady, 1000);
    }
};

// Force map initialization (your existing method)
window.forceMapInit = function () {
    console.log('💪 Force initializing map...');
    window.initializeGoogleMaps();
};

// Debug map status (your existing method)
window.debugMapStatus = function () {
    console.log('🔍 Debug Status:');
    console.log('- Google available:', typeof google !== 'undefined');
    console.log('- Maps available:', typeof google !== 'undefined' && google.maps);
    console.log('- Map object:', map);
    console.log('- Map element:', document.getElementById('map'));
    console.log('- Markers array:', markers);
    console.log('- Active markers:', markers.filter(m => m).length);
};

// Get address predictions for autocomplete
window.getAddressPredictions = function (input) {
    return new Promise((resolve) => {
        if (!autocompleteService || !input || input.length < 3) {
            resolve([]);
            return;
        }

        autocompleteService.getPlacePredictions({
            input: input,
            types: ['address'],
            componentRestrictions: { country: 'us' }
        }, (predictions, status) => {
            if (status === google.maps.places.PlacesServiceStatus.OK && predictions) {
                const addresses = predictions.map(p => p.description);
                resolve(addresses);
            } else {
                resolve([]);
            }
        });
    });
};

// Add map marker (enhanced version)
window.addMapMarker = function (address, index) {
    console.log('📍 Adding marker:', address, 'at index:', index);

    if (!map) { console.error('❌ Map not ready'); return; }
    if (!address || address.trim() === '') { console.error('❌ Empty address'); return; }
    if (typeof index !== 'number' || index < 0) {
        console.warn('⚠️ Invalid index, coercing to append.');
        index = markers.filter(m => m).length;
    }

    const geocoder = new google.maps.Geocoder();

    geocoder.geocode({ address: address.trim(), componentRestrictions: { country: 'US' } }, (results, status) => {
        console.log('🔍 Geocoding status:', status);

        if (status === 'OK' && results && results[0]) {
            const location = results[0].geometry.location;

            if (markers[index]) markers[index].setMap(null);

            const colors = ['red', 'blue', 'green', 'yellow', 'purple'];
            const markerColor = colors[index % colors.length];

            const marker = new google.maps.Marker({
                position: location,
                map: map,
                title: address,
                label: { text: (index + 1).toString(), color: 'white', fontWeight: 'bold' },
                icon: {
                    url: `https://maps.google.com/mapfiles/ms/micons/${markerColor}.png`,
                    scaledSize: new google.maps.Size(32, 32),
                    anchor: new google.maps.Point(16, 32)
                }
            });

            markers[index] = marker;

            const activeMarkers = markers.filter(m => m);
            if (activeMarkers.length === 1) {
                map.setCenter(location);
                map.setZoom(15);
            } else {
                const bounds = new google.maps.LatLngBounds();
                activeMarkers.forEach(m => bounds.extend(m.getPosition()));
                map.fitBounds(bounds);
            }

            console.log('✅ Marker added successfully!');
        } else {
            console.error('❌ Geocoding failed:', status, 'for address:', address);
        }
    });
};
// Update map marker (your existing method)
window.updateMapMarker = function (index, address) {
    console.log('🔄 Updating marker at index', index, 'with address:', address);

    // Remove old marker if it exists
    if (markers[index]) {
        markers[index].setMap(null);
    }

    // Add new marker at the same index
    window.addMapMarker(address, index);
};

// Remove map marker
window.removeMapMarker = function (index) {
    console.log('🗑️ Removing marker at index:', index);
    if (markers[index]) {
        markers[index].setMap(null);
        markers[index] = null;

        // Update labels for remaining markers
        markers.forEach((marker, i) => {
            if (marker) {
                marker.setLabel({
                    text: (i + 1).toString(),
                    color: 'white',
                    fontWeight: 'bold'
                });
            }
        });
    }
};

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
                    if (types.includes('locality') || types.includes('postal_town')) {
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

// Auto-initialize fallback
setTimeout(function () {
    if (typeof google !== 'undefined' && google.maps) {
        window.initializeGoogleMaps();
    }
}, 2000);

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    setTimeout(function () {
        if (typeof google !== 'undefined' && google.maps) {
            window.initializeGoogleMaps();
        }
    }, 1000);
});

window.rebuildAllMarkers = function (addresses) {
    if (!map) return;

    // Clear existing
    markers.forEach(m => m && m.setMap(null));
    markers = [];

    const geocoder = new google.maps.Geocoder();
    const bounds = new google.maps.LatLngBounds();
    const colors = ['red', 'blue', 'green', 'yellow', 'purple'];

    addresses.forEach((addr, i) => {
        if (!addr || !addr.trim()) return;

        geocoder.geocode({ address: addr.trim(), componentRestrictions: { country: 'US' } }, (results, status) => {
            if (status === 'OK' && results && results[0]) {
                const location = results[0].geometry.location;
                const markerColor = colors[i % colors.length];

                const marker = new google.maps.Marker({
                    position: location,
                    map: map,
                    title: addr,
                    label: { text: (i + 1).toString(), color: 'white', fontWeight: 'bold' },
                    icon: {
                        url: `https://maps.google.com/mapfiles/ms/micons/${markerColor}.png`,
                        scaledSize: new google.maps.Size(32, 32),
                        anchor: new google.maps.Point(16, 32)
                    }
                });

                markers[i] = marker;
                bounds.extend(location);
                map.fitBounds(bounds);
            }
        });
    });
};