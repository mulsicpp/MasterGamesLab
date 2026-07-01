using System;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class AlphaPickableElement : VisualElement
{
    private bool _isPointerCurrentlyOverShape;

    public AlphaPickableElement()
    {
        RegisterCallback<PointerMoveEvent>(OnPointerMove);
        RegisterCallback<PointerLeaveEvent>(OnPointerLayoutLeave);
    }

    public override bool ContainsPoint(Vector2 localPoint)
    {
        if (!base.ContainsPoint(localPoint)) return false;

        Background resolvedBg = resolvedStyle.backgroundImage;
        Sprite sprite = resolvedBg.sprite;
        Texture2D tex = (sprite != null) ? sprite.texture : resolvedBg.texture;

        if (tex == null) return true;

        float xPct = localPoint.x / layout.width;
        float yPct = 1.0f - (localPoint.y / layout.height);

        int pixelX;
        int pixelY;

        if (sprite != null)
        {
            Rect spriteRect = sprite.rect;
            float actualX = spriteRect.x + (xPct * spriteRect.width);
            float actualY = spriteRect.y + (yPct * spriteRect.height);
            
            pixelX = Mathf.Clamp(Mathf.RoundToInt(actualX), 0, tex.width - 1);
            pixelY = Mathf.Clamp(Mathf.RoundToInt(actualY), 0, tex.height - 1);
        }
        else
        {
            pixelX = Mathf.Clamp(Mathf.RoundToInt(xPct * tex.width), 0, tex.width - 1);
            pixelY = Mathf.Clamp(Mathf.RoundToInt(yPct * tex.height), 0, tex.height - 1);
        }

        try
        {
            Color pixelColor = tex.GetPixel(pixelX, pixelY);
            return pixelColor.a > 0.1f;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        bool isOverShapeNow = ContainsPoint(evt.localPosition);

        if (isOverShapeNow && !_isPointerCurrentlyOverShape)
        {
            _isPointerCurrentlyOverShape = true;
            
            using (PointerEnterEvent enterEvt = PointerEnterEvent.GetPooled(evt))
            {
                enterEvt.target = this;
                SendEvent(enterEvt);
            }
        }
        else if (!isOverShapeNow && _isPointerCurrentlyOverShape)
        {
            _isPointerCurrentlyOverShape = false;

            using (PointerLeaveEvent leaveEvt = PointerLeaveEvent.GetPooled(evt))
            {
                leaveEvt.target = this;
                SendEvent(leaveEvt);
            }
        }
    }

    private void OnPointerLayoutLeave(PointerLeaveEvent evt)
    {
        _isPointerCurrentlyOverShape = false;
    }
}