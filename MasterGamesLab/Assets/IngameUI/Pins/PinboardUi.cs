using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Map.Fleet;
using Map.Infrastructure;

namespace UI
{
    public class PinboardUi : MonoBehaviour
    {
        public static PinboardUi Instance { get; private set; }
        public VisualElement root { get; private set; }

        private readonly List<Pin> _activePins = new List<Pin>();
        private readonly Dictionary<object, List<Pin>> _tileGroups = new Dictionary<object, List<Pin>>();
        private readonly HashSet<Pin> _pinsHandledAsCarriedChildren = new HashSet<Pin>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        protected virtual void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");
        }

        public void RegisterPin(Pin pin) => _activePins.Add(pin);
        public void UnregisterPin(Pin pin) => _activePins.Remove(pin);

        public bool isStructurePin(Pin pin)
        {
            return pin is ConsumerPin || pin is ProducerPin;
        }

        private Pin GetLinkedTruckPin(FreighterPin freighterPin)
        {
            if (freighterPin.VehicleRenderer.Vehicle is Freighter freighter)
            {
                if (freighter.Truck?.Renderer is TruckRenderer truckRenderer)
                {
                    return truckRenderer.Pin;
                }
            }
            return null;
        }

        // Helper 3: Pulls spatial tracking coordinates from dynamic/movable components.
        // RoutePin is intentionally omitted here so it returns null and stays locked to its world position.
        private object GetGroupingKey(Pin pin)
        {
            if (pin is ConsumerPin consumerPin)
            {
                return consumerPin.structureRenderer?.Structure?.Tile;
            }
            if (pin is ProducerPin producerPin)
            {
                return producerPin.structureRenderer?.Structure?.Tile;
            }
            if (pin is FreighterPin freighterPin)
            {
                var vehicle = freighterPin.VehicleRenderer?.Vehicle;
                return vehicle != null && vehicle.IsParked ? vehicle.ParkedTile : null;
            }
            if (pin is TruckPin truckPin)
            {
                var vehicle = truckPin.vehicleRenderer?.Vehicle;
                return vehicle != null && vehicle.IsParked ? vehicle.ParkedTile : null;
            }
            return null;
        }

        private void LateUpdate()
        {

            _tileGroups.Clear();
            _pinsHandledAsCarriedChildren.Clear();

            for (int i = 0; i < _activePins.Count; i++)
            {
                if (_activePins[i] is FreighterPin freighter && freighter.IsShowing())
                {
                    Pin loadedTruck = GetLinkedTruckPin(freighter);
                    if (loadedTruck != null) _pinsHandledAsCarriedChildren.Add(loadedTruck);
                }
            }
            for (int i = 0; i < _activePins.Count; i++)
            {
                Pin pin = _activePins[i];
                bool isCarried = _pinsHandledAsCarriedChildren.Contains(pin);
                object groupKey = GetGroupingKey(pin);

                if (!pin.IsShowing() || (groupKey == null && !isCarried))
                {
                    pin.SetManagedOffset(Vector2.zero);
                    continue;
                }

                if (isCarried) continue;

                if (!_tileGroups.TryGetValue(groupKey, out var list))
                {
                    list = new List<Pin>();
                    _tileGroups[groupKey] = list;
                }
                list.Add(pin);
            }

            // Step 3: Run spatial group offset calculations
            foreach (var group in _tileGroups.Values)
            {
                int pinsCount = group.Count;
                if (pinsCount == 0) continue;

                bool hasVehicles = false;
                Pin structurePin = null;

                for (int i = 0; i < pinsCount; i++)
                {
                    if (isStructurePin(group[i])) structurePin = group[i];
                    else hasVehicles = true;
                }

                if (structurePin != null && hasVehicles)
                {
                    structurePin.SetManagedOffset(new Vector2(0f, -structurePin.UnscaledHeight * 0.6f));
                }

                float totalWidth = 0f;
                int logicalVehicleCount = 0;

                // Diagnostic Trackers

                for (int i = 0; i < pinsCount; i++)
                {
                    Pin pin = group[i];

                    if (isStructurePin(pin)) continue;

                    totalWidth += pin.UnscaledWidth;
                    logicalVehicleCount++;

                    if (pin is FreighterPin fp)
                    {
                        Pin loadedTruck = GetLinkedTruckPin(fp);
                        bool truckFound = loadedTruck != null;
                        if (truckFound) totalWidth += loadedTruck.UnscaledWidth;
                    }
                }

                bool isSingleFreighterWithTruck = pinsCount == 1 && group[0] is FreighterPin singleFp && GetLinkedTruckPin(singleFp) != null;

                // Print the comprehensive group snapshot to your console

                // If multiple vehicles share this tile, arrange them side-by-side using a running X offset
                if (logicalVehicleCount > 1 || isSingleFreighterWithTruck)
                {
                    float runningXOffset = -totalWidth / 2f;

                    for (int i = 0; i < pinsCount; i++)
                    {
                        Pin pin = group[i];
                        if (isStructurePin(pin)) continue;

                        float halfWidth = pin.UnscaledWidth / 2f;
                        runningXOffset += halfWidth;

                        pin.SetManagedOffset(new Vector2(runningXOffset, 0f));
                        runningXOffset += halfWidth;

                        if (pin is FreighterPin fp)
                        {
                            Pin truckPin = GetLinkedTruckPin(fp);
                            if (truckPin != null)
                            {
                                float truckHalfWidth = truckPin.UnscaledWidth / 2f;
                                runningXOffset += truckHalfWidth;

                                truckPin.SetManagedOffset(new Vector2(runningXOffset, 0f));
                                runningXOffset += truckHalfWidth;
                            }
                        }
                    }
                }
                else
                {
                    // Fallback: If a vehicle is alone on the tile, center it perfectly at zero
                    for (int i = 0; i < pinsCount; i++)
                    {
                        Pin pin = group[i];
                        if (!isStructurePin(pin))
                        {
                            pin.SetManagedOffset(Vector2.zero);

                            if (pin is FreighterPin fp)
                            {
                                Pin truckPin = GetLinkedTruckPin(fp);
                                if (truckPin != null)
                                {
                                    truckPin.SetManagedOffset(Vector2.zero);
                                }
                            }
                        }
                    }
                }
            }
        }

        public VisualElement CreatePinElement(VisualTreeAsset template, float heightPercent, float aspectRatio)
        {
            VisualElement wrapper = new VisualElement();
            wrapper.style.height = Length.Percent(heightPercent);
            wrapper.style.aspectRatio = aspectRatio;
            wrapper.AddToClassList("element");
            wrapper.style.position = Position.Absolute;
            wrapper.style.left = 0;
            wrapper.style.top = 0;
            wrapper.style.transformOrigin = new TransformOrigin(Length.Percent(50f), Length.Percent(100f));

            VisualElement visualContent = template.Instantiate();
            wrapper.Add(visualContent);
            root.Add(wrapper);
            return wrapper;
        }
    }
}