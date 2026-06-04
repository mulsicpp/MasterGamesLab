using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Map.Hoverables
{
    public class HoverablePicker
    {
        [Flags]
        public enum HoverableLayer
        {
            Tiles = 1 << 0,
            Edges = 1 << 1,
            Vehicles = 1 << 2,
            All = ~0,
            None = 0,
        }

        public static HoverablePicker Instance
        {
            get
            {
                instance ??= new HoverablePicker();
                return instance;
            }
        }

        private static HoverablePicker instance;

        public bool RequestPending { get; private set; }
        public Vector2Int MousePosition { get; private set; }
        public int LayerMask { get; private set; }
        public Action<AsyncGPUReadbackRequest> Callback { get; private set; }
        public RTHandle Persistent1X1RT { get; private set; }

        public void RequestPick(Vector2Int screenPos, Action<AsyncGPUReadbackRequest> callback)
        {
            MousePosition = screenPos;
            SetLayerMask();
            Callback = callback;
            RequestPending = true;

            Persistent1X1RT ??= RTHandles.Alloc(
                1, 1,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R32_UInt,
                name: "TilePicker1x1"
            );
        }

        public void ConsumeRequest()
        {
            RequestPending = false;
        }

        public void Cleanup()
        {
            Persistent1X1RT?.Release();
            Persistent1X1RT = null;
        }

        private void SetLayerMask()
        {
            LayerMask = 0;

            var layers = Map.Instance.HoverLayers;
            var isAll = layers.HasFlag(HoverableLayer.All);

            if (isAll || layers.HasFlag(HoverableLayer.Tiles))
            {
                LayerMask |= 1 << Map.TileLayer;
            }

            if (isAll || layers.HasFlag(HoverableLayer.Edges))
            {
                LayerMask |= 1 << Map.EdgeLayer;
                LayerMask |= 1 << Map.EdgeOutlineLayer;
                LayerMask |= 1 << Map.EdgeOutlineTransparentLayer;
            }

            if (isAll || layers.HasFlag(HoverableLayer.Vehicles))
            {
                LayerMask |= 1 << Map.VehicleLayer;
                LayerMask |= 1 << Map.VehicleOutlineLayer;
                LayerMask |= 1 << Map.VehicleOutlineTransparentLayer;
            }
        }
    }
}