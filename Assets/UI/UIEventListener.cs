using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using XLua;

namespace TwUI
{
    [LuaCallCSharp]
    public class UIEventListener : UnityEngine.EventSystems.EventTrigger
    {
        public delegate void VoidDelegate(GameObject go);
        public VoidDelegate onClick;
        public VoidDelegate onDown;
        public VoidDelegate onEnter;
        public VoidDelegate onExit;
        public VoidDelegate onUp;
        public VoidDelegate onSelect;
        public VoidDelegate onUpdateSelect;

        static public UIEventListener Get(GameObject go)
        {
            UIEventListener listener = go.GetComponent<UIEventListener>();
            if (listener == null)
                listener = go.AddComponent<UIEventListener>();
            return listener;
        }
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (onClick != null) onClick(gameObject);
        }
        public override void OnPointerDown(PointerEventData eventData)
        {
            if (onDown != null) onDown(gameObject);
        }
        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (onEnter != null) onEnter(gameObject);
        }
        public override void OnPointerExit(PointerEventData eventData)
        {
            if (onExit != null) onExit(gameObject);
        }
        public override void OnPointerUp(PointerEventData eventData)
        {
            if (onUp != null) onUp(gameObject);
        }
        public override void OnSelect(BaseEventData eventData)
        {
            if (onSelect != null) onSelect(gameObject);
        }
        public override void OnUpdateSelected(BaseEventData eventData)
        {
            if (onUpdateSelect != null) onUpdateSelect(gameObject);
        }

        // for lua
        public static void SetOnClick(GameObject go, LuaFunction luafunc, LuaTable instance)
        {
            if (go == null || luafunc == null)
            {
                Debug.LogError("UIEventListener SetOnClick: " + (go == null ? "param GameObject is null." : "param LuaFunction is null."));
                return;
            }
            UnityEngine.UI.Button btn = go.GetComponent<UnityEngine.UI.Button>();
            if(btn != null)
            {
				btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(delegate () { if (instance != null) luafunc.Call(instance, go); else luafunc.Call(go); });
            }else
            {
				Get(go).onClick = delegate (GameObject obj) { if (instance != null) luafunc.Call(instance, obj); else luafunc.Call(obj); };
            }

        }

        public static void SetCanvasCamera(bool change)
        {
            Camera cam = GameObject.Find("Main Camera").GetComponent<Camera>();
            Canvas can = GameObject.Find("Canvas").GetComponent<Canvas>();
            if (change)
            {
                can.renderMode = RenderMode.ScreenSpaceCamera;
                can.worldCamera = cam;
                can.sortingOrder = 1000;
            }
            else
            {
                can.renderMode = RenderMode.ScreenSpaceOverlay;
            }

        }

        public static void SetButtonEvent(GameObject go,LuaFunction luafunc, EventTriggerType type, LuaTable instance)
        {
            if (go == null || luafunc == null)
            {
                Debug.LogError("UIEventListener SetButtonEvent: " + (go == null ? "param GameObject is null." : "param LuaFunction is null."));
                return;
            }

            var trigger = go.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = go.AddComponent<EventTrigger>();

            if (trigger.triggers == null)
                trigger.triggers = new List<EventTrigger.Entry>();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = type;
            entry.callback = new EventTrigger.TriggerEvent();
            UnityAction<BaseEventData> callback =
                new UnityAction<BaseEventData>(delegate (BaseEventData arg0) { if (instance != null) luafunc.Call(instance,go,arg0); else luafunc.Call(go,arg0); });
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);

        }

    }
}
