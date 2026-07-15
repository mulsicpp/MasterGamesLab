using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Map.Fleet;
using Map.Infrastructure;
using NUnit.Framework;

namespace UI
{
    public class PinboardUi : MonoBehaviour
    {
        public static PinboardUi Instance { get; private set; }
        public VisualElement root { get; private set; }

        private readonly List<TruckPin> _activeTruckPins = new List<TruckPin>();
        private readonly List<FreighterPin> _activeFreighterPins = new List<FreighterPin>();
        private readonly List<ConsumerPin> _activeConsumerPins = new List<ConsumerPin>();
        private readonly List<ProducerPin> _activeProducerPins = new List<ProducerPin>();
        private readonly List<RoutePin> _activeRoutePins = new List<RoutePin>();

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

        public void RegisterPin(Pin pin)
        {
            switch (pin)
            {
                case FreighterPin f:
                    _activeFreighterPins.Add(f);
                    break;
                case TruckPin t:
                    _activeTruckPins.Add(t);
                    break;
                case ConsumerPin c:
                    _activeConsumerPins.Add(c);
                    break;
                case ProducerPin p:
                    _activeProducerPins.Add(p);
                    break;
                case RoutePin r:
                    _activeRoutePins.Add(r);
                    break;
            }
        }
        public void UnregisterPin(Pin pin)
        {
            switch (pin)
            {
                case FreighterPin f:
                    _activeFreighterPins.Remove(f);
                    break;
                case TruckPin t:
                    _activeTruckPins.Remove(t);
                    break;
                case ConsumerPin c:
                    _activeConsumerPins.Remove(c);
                    break;
                case ProducerPin p:
                    _activeProducerPins.Remove(p);
                    break;
                case RoutePin r:
                    _activeRoutePins.Remove(r);
                    break;
            }
        }

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
                return vehicle.ParkedTile;
            }
            if (pin is TruckPin truckPin)
            {
                var vehicle = truckPin.VehicleRenderer?.Vehicle;
                return vehicle.ParkedTile;
            }
            return null;
        }

        private void LateUpdate()
        {

            _tileGroups.Clear();

            object groupKey;
            foreach (ConsumerPin pin in _activeConsumerPins)
            {
                pin.SetManagedOffset(new Vector2(0f, 0f));
                if (pin.IsShowing())
                {
                    groupKey = GetGroupingKey(pin);
                    var list = new List<Pin>();
                    _tileGroups[groupKey] = list;
                    list.Add(pin);
                }
            }
            foreach (ProducerPin pin in _activeProducerPins)
            {
                pin.SetManagedOffset(new Vector2(0f, 0f));
                if (pin.IsShowing())
                {
                    groupKey = GetGroupingKey(pin);
                    var list = new List<Pin>();
                    _tileGroups[groupKey] = list;
                    list.Add(pin);
                }
            }
            foreach (FreighterPin pin in _activeFreighterPins)
            {
                pin.SetManagedOffset(new Vector2(0f, 0f));
                if (pin.IsShowing())
                {
                    groupKey = GetGroupingKey(pin);
                    Pin loadedTruck = GetLinkedTruckPin(pin);
                    if (loadedTruck != null && !pin.VehicleRenderer.Vehicle.IsParked)
                    {
                        pin.SetManagedOffset(new Vector2(-pin.PaddedWidth * 0.5f, 0f));
                        loadedTruck.SetManagedOffset(new Vector2(pin.PaddedWidth * 0.5f, 0f));
                        continue;
                    }
                    if (groupKey != null)
                    {
                        if (!_tileGroups.TryGetValue(groupKey, out var list))
                        {
                            list = new List<Pin>();
                            _tileGroups[groupKey] = list;
                        }
                        list.Add(pin);
                        if (loadedTruck != null)
                            list.Add(loadedTruck);
                    }
                }
            }
            foreach (TruckPin pin in _activeTruckPins)
            {
                if (pin.IsShowing() && pin.VehicleRenderer.Vehicle is Truck t && t.Freighter == null)
                {
                    pin.SetManagedOffset(new Vector2(0f, 0f));
                    if (t.IsParked)
                    {
                        groupKey = GetGroupingKey(pin);
                        if (!_tileGroups.TryGetValue(groupKey, out var list))
                        {
                            list = new List<Pin>();
                            _tileGroups[groupKey] = list;
                        }
                        list.Add(pin);
                    }
                }
            }
            Debug.Log(_tileGroups.Count);
            foreach (var group in _tileGroups.Values)
            {
                Debug.Log(group.Count);
                if (group.Count < 2)
                    continue;
                var index = 0;
                if (isStructurePin(group[index]))
                {
                    group[index].SetManagedOffset(new Vector2(0f, -group[index].UnscaledHeight * 0.2f));
                    index++;
                }
                float totalwidth = 0;
                for (int i = index; i < group.Count; i++)
                {
                    totalwidth += group[i].PaddedWidth;
                }

                float currentXOffset = -totalwidth / 2f;

                for (int i = index; i < group.Count; i++)
                {
                    var element = group[i];
                    float elementXShift = currentXOffset + (element.PaddedWidth / 2f);
                    element.SetManagedOffset(new Vector2(elementXShift, 0f));
                    currentXOffset += element.PaddedWidth;
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
            wrapper.pickingMode = PickingMode.Ignore;
            root.Add(wrapper);
            return wrapper;
        }
    }
}