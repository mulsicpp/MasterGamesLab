using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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

        private void LateUpdate()
        {
            _tileGroups.Clear();
            _pinsHandledAsCarriedChildren.Clear();

            // Step 1: Pre-process freighters to identify which trucks are "carried children"
            // This prevents them from being treated as separate lone vehicles on the tile
            for (int i = 0; i < _activePins.Count; i++)
            {
                if (_activePins[i] is FreighterPin freighter && freighter.IsShowing() && freighter.CurrentTile != null)
                {
                    Pin loadedTruck = freighter.LinkedTruckPin;
                    if (loadedTruck != null)
                    {
                        _pinsHandledAsCarriedChildren.Add(loadedTruck);
                    }
                }
            }

            // Step 2: Bucket active standalone elements by tile group
            for (int i = 0; i < _activePins.Count; i++)
            {
                Pin pin = _activePins[i];
                if (!pin.IsShowing() || pin.CurrentTile == null) continue;
                if (_pinsHandledAsCarriedChildren.Contains(pin)) continue; // Handled down below via parent

                if (!_tileGroups.TryGetValue(pin.CurrentTile, out var list))
                {
                    list = new List<Pin>();
                    _tileGroups[pin.CurrentTile] = list;
                }
                list.Add(pin);
            }

            // Step 3: Run the central layout processing loops
            foreach (var group in _tileGroups.Values)
            {
                int pinsCount = group.Count;
                if (pinsCount == 0) continue;

                bool hasVehicles = false;
                Pin structurePin = null;

                for (int i = 0; i < pinsCount; i++)
                {
                    if (group[i].IsStructure) structurePin = group[i];
                    else hasVehicles = true;
                }

                if (structurePin != null && hasVehicles)
                {
                    structurePin.SetManagedOffset(new Vector2(0f, -structurePin.UnscaledHeight * 0.6f));
                }

                // Calculate horizontal widths taking compound sizes into account
                float totalWidth = 0f;
                int logicalVehicleCount = 0;

                for (int i = 0; i < pinsCount; i++)
                {
                    Pin pin = group[i];
                    if (pin.IsStructure) continue;

                    totalWidth += pin.UnscaledWidth;
                    logicalVehicleCount++;

                    // If this is a freighter carrying a truck, it takes up double layout slots
                    if (pin is FreighterPin fp && fp.LinkedTruckPin != null)
                    {
                        totalWidth += fp.LinkedTruckPin.UnscaledWidth;
                    }
                }

                // Spread logical slots from center out
                if (logicalVehicleCount > 1 || (pinsCount == 1 && group[0] is FreighterPin freighterWithTruck && freighterWithTruck.LinkedTruckPin != null))
                {
                    float runningXOffset = -totalWidth / 2f;

                    for (int i = 0; i < pinsCount; i++)
                    {
                        Pin pin = group[i];
                        if (pin.IsStructure) continue;

                        float halfWidth = pin.UnscaledWidth / 2f;
                        runningXOffset += halfWidth;

                        pin.SetManagedOffset(new Vector2(runningXOffset, 0f));
                        runningXOffset += halfWidth;

                        // If it's a compound unit, lock the child immediately adjacent to its parent
                        if (pin is FreighterPin fp && fp.LinkedTruckPin != null)
                        {
                            Pin truckPin = fp.LinkedTruckPin;
                            float truckHalfWidth = truckPin.UnscaledWidth / 2f;
                            runningXOffset += truckHalfWidth;

                            truckPin.SetManagedOffset(new Vector2(runningXOffset, 0f));
                            runningXOffset += truckHalfWidth;
                        }
                    }
                }
                else
                {
                    // Clean fallback single pin reset
                    for (int i = 0; i < pinsCount; i++)
                    {
                        if (!group[i].IsStructure) group[i].SetManagedOffset(Vector2.zero);
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