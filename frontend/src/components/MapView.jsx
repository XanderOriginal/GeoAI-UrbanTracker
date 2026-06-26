import { useEffect, useRef, useCallback } from 'react';
import { MapContainer, TileLayer, useMapEvents, Circle, Marker } from 'react-leaflet';
import { motion, AnimatePresence } from 'framer-motion';
import L from 'leaflet';
import './MapView.css';

// Fix leaflet default icon
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
});

// Custom pulse marker icon
function createPulseIcon() {
  return L.divIcon({
    className: '',
    iconSize: [24, 24],
    iconAnchor: [12, 12],
    html: `
      <div class="map-pulse-marker">
        <div class="pulse-ring pulse-ring-1"></div>
        <div class="pulse-ring pulse-ring-2"></div>
        <div class="pulse-core"></div>
      </div>
    `,
  });
}

function ClickHandler({ onMapClick, step }) {
  useMapEvents({
    click(e) {
      if (step <= 2) {
        onMapClick(e.latlng.lat, e.latlng.lng);
      }
    },
  });
  return null;
}

export default function MapView({ selectedPoint, radius, onMapClick, step }) {
  const pulseIcon = useRef(createPulseIcon());

  return (
    <div className="map-wrapper">
      <MapContainer
        center={[48.3794, 31.1656]}
        zoom={6}
        className="map-container"
        zoomControl={false}
        attributionControl={false}
      >
        <TileLayer
          url="https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png"
          attribution=""
          maxZoom={19}
        />

        <ClickHandler onMapClick={onMapClick} step={step} />

        {selectedPoint && (
          <>
            <Marker
              position={[selectedPoint.lat, selectedPoint.lng]}
              icon={pulseIcon.current}
            />
            <Circle
              center={[selectedPoint.lat, selectedPoint.lng]}
              radius={radius}
              pathOptions={{
                color: '#3b82f6',
                fillColor: '#3b82f6',
                fillOpacity: 0.08,
                weight: 2,
                opacity: 0.7,
                dashArray: step === 3 ? '8 4' : undefined,
              }}
            />
          </>
        )}
      </MapContainer>

      {/* Click hint overlay */}
      <AnimatePresence>
        {step === 1 && (
          <motion.div
            className="map-hint"
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 10 }}
            transition={{ delay: 0.5 }}
          >
            <div className="map-hint-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M12 22s-8-4.5-8-11.8A8 8 0 0112 2a8 8 0 018 8.2c0 7.3-8 11.8-8 11.8z"/>
                <circle cx="12" cy="10" r="3"/>
              </svg>
            </div>
            <span>Click anywhere on the map to select analysis point</span>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Scan overlay when running */}
      <AnimatePresence>
        {step === 3 && (
          <motion.div
            className="scan-overlay"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
          >
            <div className="scan-line" />
          </motion.div>
        )}
      </AnimatePresence>

      {/* Attribution */}
      <div className="map-attribution">
        © CartoDB · Sentinel-2 / Copernicus
      </div>
    </div>
  );
}