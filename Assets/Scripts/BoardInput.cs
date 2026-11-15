using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PixelMoments
{
    // One surface owns the gesture, so fast drags do not depend on pointer-enter events.
    public sealed class BoardInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<float,float> Begin;
        public Action<float,float> Move;
        public Action End;
        private int? activePointer;
        public void OnInitializePotentialDrag(PointerEventData data) { data.useDragThreshold=false; }
        private bool Position(PointerEventData data,out float row,out float col)
        {
            row=col=0;
            var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,data.position,data.pressEventCamera,out var local))return false;
            col=(local.x-rect.rect.xMin)/rect.rect.width*Puzzle.Size;
            row=(rect.rect.yMax-local.y)/rect.rect.height*Puzzle.Size;
            return true;
        }
        public void OnPointerDown(PointerEventData data)
        {
            if(activePointer.HasValue || data.button!=PointerEventData.InputButton.Left)return;
            if(!Position(data,out float r,out float c))return;
            activePointer=data.pointerId;Begin?.Invoke(r,c);
        }
        public void OnDrag(PointerEventData data)
        {
            if(activePointer!=data.pointerId)return;
            if(Position(data,out float r,out float c))Move?.Invoke(r,c);
        }
        public void OnPointerUp(PointerEventData data) { if(activePointer==data.pointerId)Cancel(); }
        public void OnEndDrag(PointerEventData data) { if(activePointer==data.pointerId)Cancel(); }
        private void OnDisable() { Cancel(); }
        private void OnApplicationFocus(bool focus) { if(!focus)Cancel(); }
        private void Cancel() { activePointer=null;End?.Invoke(); }
    }
}
