using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PixelMoments
{
    // One surface owns the gesture, so fast drags do not depend on pointer-enter events.
    public sealed class BoardInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<int,int> Begin;
        public Action<int,int> Move;
        public Action End;
        private int? activePointer;
        public void OnInitializePotentialDrag(PointerEventData data) { data.useDragThreshold=false; }
        private bool Position(PointerEventData data,out int row,out int col)
        {
            row=col=0;
            var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,data.position,data.pressEventCamera,out var local))return false;
            col=Mathf.Clamp(Mathf.FloorToInt((local.x-rect.rect.xMin)/rect.rect.width*Puzzle.Size),0,Puzzle.Size-1);
            row=Mathf.Clamp(Mathf.FloorToInt((rect.rect.yMax-local.y)/rect.rect.height*Puzzle.Size),0,Puzzle.Size-1);
            return true;
        }
        public void OnPointerDown(PointerEventData data)
        {
            if(activePointer.HasValue || data.button!=PointerEventData.InputButton.Left)return;
            if(!Position(data,out int r,out int c))return;
            activePointer=data.pointerId;Begin?.Invoke(r,c);
        }
        public void OnDrag(PointerEventData data)
        {
            if(activePointer!=data.pointerId)return;
            if(Position(data,out int r,out int c))Move?.Invoke(r,c);
        }
        public void OnPointerUp(PointerEventData data) { if(activePointer==data.pointerId)Cancel(); }
        public void OnEndDrag(PointerEventData data) { if(activePointer==data.pointerId)Cancel(); }
        private void OnDisable() { Cancel(); }
        private void OnApplicationFocus(bool focus) { if(!focus)Cancel(); }
        private void Cancel() { activePointer=null;End?.Invoke(); }
    }
}
