using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public sealed class WindowManager
	{
		private Dictionary<WindowInfo, IWindow> mShownWindowDic = new Dictionary<WindowInfo, IWindow>();

		private Dictionary<WindowInfo, IWindow> mCachedWindowDic = new Dictionary<WindowInfo, IWindow>();

		private Stack<WindowStackData> mWindowStack = new Stack<WindowStackData>();

		private GameObject mUiCamera;
		private WindowInfo _mainWindowInfo = null;
		public WindowInfo mainWindowInfo { set { _mainWindowInfo = value; } }

		public void Init()
		{
			SetupCanvas();
		}

		public void Cleanup()
		{
			foreach (var wnd in mShownWindowDic)
			{
				if (wnd.Value != null)
					GameObject.Destroy(wnd.Value.gameObject);
			}
			mShownWindowDic.Clear();

			foreach (var wnd in mCachedWindowDic)
			{
				if (wnd.Value != null)
					GameObject.Destroy(wnd.Value.gameObject);
			}
			mCachedWindowDic.Clear();

			mWindowStack.Clear();

			Resources.UnloadUnusedAssets();
		}

		public IWindow GetWindow(WindowInfo windowInfo)
		{
			IWindow iwindow = null;
			mShownWindowDic.TryGetValue(windowInfo, out iwindow);
			return iwindow;
		}
		/// <summary>
		/// open a window
		/// </summary>
		/// <param name="windowInfo"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public IWindow OpenWindow(WindowInfo windowInfo, IContext context = null)
		{
			IWindow script = null;

			try
			{
				if (mShownWindowDic.ContainsKey(windowInfo))
				{
					Debug.LogWarning(string.Format("{0} is already open ", windowInfo.name));
					return mShownWindowDic[windowInfo];
				}

				script = this.PullFromWindowCache(windowInfo, true, context);

				if (mWindowStack.Count > 0)
					script.PreviousWindowInfo = mWindowStack.Peek().windowInfo;

				// do action to affected windows
				List<IWindow> historyWindows = BuildAffectedWindowList(windowInfo);
				if (historyWindows != null)
				{
					for (int i = 0; i < historyWindows.Count; ++i)
					{
						this.PushToWindowCache(historyWindows[i], false);
					}
				}

				if (windowInfo.showMode == ShowMode.Normal)
				{
					WindowStackData newStackData = new WindowStackData();
					newStackData.windowInfo = windowInfo;
					newStackData.windowScript = script;
					newStackData.historyWindows = historyWindows;
					mWindowStack.Push(newStackData);
				}

			}
			catch (UnityException ue)
			{
				Debug.LogError(ue.ToString());
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}

			return script;
		}
		public void CloseWindow(WindowInfo windowInfo)
		{
			IWindow iwindow = this.GetWindow(windowInfo);
			this.CloseWindow(iwindow);
		}

		public void CloseWindow(IWindow iwindow)
		{
			try
			{
				WindowInfo windowInfo = iwindow.windowInfo;

				if (!mShownWindowDic.ContainsKey(windowInfo))
				{
					Debug.LogError(string.Format("mAllWindowDic does not contains {0}. THAT IS SO NOT RIGHT !", windowInfo.name));
					return;
				}

				if (windowInfo.showMode == ShowMode.Normal)
				{
					if (mWindowStack.Count > 0)
					{
						var topStackdata = mWindowStack.Peek();
						if (topStackdata.windowInfo != windowInfo)
						{
							Debug.LogError(string.Format("You shall not close {0} in this way !", windowInfo.name));
							return;
						}

						this.PushToWindowCache(iwindow, true);

						switch (windowInfo.openAction)
						{
							case OpenAction.HideAll:
							case OpenAction.HideNormalsMains:
								for (int i = 0; i < topStackdata.historyWindows.Count; ++i)
								{
									IWindow tempScript = topStackdata.historyWindows[i];
									this.PullFromWindowCache(tempScript.windowInfo, false, null);
								}
								break;
							case OpenAction.DoNothing:
							default:
								break;
						}
						mWindowStack.Pop();
					}
					else // some error happened in stack...
					{
						WindowInfo previousWindowInfo = iwindow.PreviousWindowInfo;
						this.PushToWindowCache(iwindow, true);
						// open default previous window.
						OpenWindow(previousWindowInfo);
					}
				}
				else if (windowInfo.showMode == ShowMode.Popup)
				{
					this.PushToWindowCache(iwindow, true);
				}
				else
				{
					Debug.LogError("You are trying to close a window with ShowMode which is neither 'Normal' nor 'Popup', which shouldn't happen. Check the window's WindowInfo.");
				}

			}
			catch (UnityException ue)
			{
				Debug.LogError(ue.ToString());
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}

		}
		/// <summary>
		/// Hide all normal windows and show main window.
		/// </summary>
		public void BackToMainWindow()
		{
			List<WindowInfo> selecteds = new List<WindowInfo>();
			foreach (var wnd in mShownWindowDic)
			{
				if (wnd.Key.showMode != ShowMode.Normal)
					continue;
				selecteds.Add(wnd.Key);
			}
			for (int i = 0; i < selecteds.Count; ++i)
			{
				WindowInfo tmpInfo = selecteds[i];
				IWindow tmpScript = mShownWindowDic[tmpInfo];
				this.PushToWindowCache(tmpScript, true);
			}

			mWindowStack.Clear();

			this.OpenWindow(_mainWindowInfo, null);
		}

		public void CleanWindow(bool cleanFollow = true)
		{
			foreach (var wnd in mShownWindowDic)
			{
				if (wnd.Value != null)
					GameObject.Destroy(wnd.Value.gameObject);
			}
			mShownWindowDic.Clear();

			foreach (var wnd in mCachedWindowDic)
			{
				if (wnd.Value != null)
					GameObject.Destroy(wnd.Value.gameObject);
			}
			mCachedWindowDic.Clear();

			mWindowStack.Clear();

			if (cleanFollow)
			{
				var followRoot = GetModeRoot(ShowMode.Follow);
				for (int i = 0; i < followRoot.transform.childCount; i++)
				{
					UnityEngine.Object.Destroy(followRoot.transform.GetChild(i).gameObject);
				}
			}

			Resources.UnloadUnusedAssets();

		}

		private IWindow PullFromWindowCache(WindowInfo windowInfo, bool isEntering, IContext context)
		{
			IWindow script = null;
			if (mCachedWindowDic.ContainsKey(windowInfo))
			{
				script = mCachedWindowDic[windowInfo];
				mCachedWindowDic.Remove(windowInfo);
				var modeRoot = this.GetModeRoot(windowInfo.showMode);
				var rectTran = script.gameObject.GetComponent<RectTransform>();
				rectTran.SetParent(modeRoot.transform);
				rectTran.localPosition = Vector3.zero;
			}
			else
			{
				script = CreateWindowInstance(windowInfo);
			}
			mShownWindowDic.Add(windowInfo, script);
			if (isEntering)
				script._Enter(context);
			else
				script._Resume(context);
			MakeAsTopWindow(script.gameObject);
			return script;
		}

		private void PushToWindowCache(IWindow script, bool isExit)
		{
			mShownWindowDic.Remove(script.windowInfo);
			mCachedWindowDic.Add(script.windowInfo, script);
			var cacheRoot = this.GetCacheRoot();
			var rectTran = script.gameObject.GetComponent<RectTransform>();
			rectTran.SetParent(cacheRoot.transform);
			rectTran.localPosition = Vector3.zero;
			if (isExit)
				script._Exit(null);
			else
				script._Pause(null);
		}

		private IWindow CreateWindowInstance(WindowInfo windowInfo)
		{
			var prefab = Resources.Load(windowInfo.prefabPath) as GameObject;
			var go = GameObject.Instantiate(prefab);
			if (go == null)
			{
				Debug.LogError(string.Format("file {0} does not exist.", windowInfo.prefabPath));
				return null;
			}
			go.name = windowInfo.prefabPath.Substring(windowInfo.prefabPath.LastIndexOf('/') + 1);

			//TwFramework.LuaManager luaManager = TwFramework.GameFramework.GetLuaManager();
			//luaManager.DoFile("UI/" + go.name);

			IWindow script = go.AddComponent<IWindow>();

			script.windowInfo = windowInfo;

			var modeRoot = this.GetModeRoot(windowInfo.showMode);
			var rectTran = go.GetComponent<RectTransform>();
			rectTran.SetParent(modeRoot.transform);
			rectTran.localPosition = Vector3.zero;
			rectTran.localScale = Vector3.one;

			MakeWindowBackground(windowInfo, go);

			script._Instantiate();

			return script;
		}

		private void MakeWindowBackground(WindowInfo windowInfo, GameObject windowObj)
		{
			GameObject newGo = null;
			Image img = null;
			switch (windowInfo.backgroundMode)
			{
				case BackgroundMode.Transparent:
					newGo = new GameObject("_auto_genereted_background_", typeof(RectTransform), typeof(Image));
					img = newGo.GetComponent<Image>();
					img.color = new Color(0, 0, 0, 0);
					img.raycastTarget = true;
					break;
				case BackgroundMode.Dark:
					newGo = new GameObject("_auto_genereted_background_", typeof(RectTransform), typeof(Image));
					img = newGo.GetComponent<Image>();
					img.color = new Color(0, 0, 0, 100 / 255.0f);
					img.raycastTarget = true;
					break;
				case BackgroundMode.None:
				default:
					newGo = null;
					break;
			}
			if (newGo != null)
			{
				var rectTran = newGo.GetComponent<RectTransform>();
				rectTran.SetParent(windowObj.transform);
				rectTran.SetSiblingIndex(0);
				rectTran.localPosition = Vector3.zero;
				rectTran.anchorMin = new Vector2(0.5f, 0.5f);
				rectTran.anchorMax = new Vector2(0.5f, 0.5f);
				rectTran.pivot = new Vector2(0.5f, 0.5f);
				rectTran.sizeDelta = new Vector2(9000, 9000);
			}
		}

		private void MakeAsTopWindow(GameObject windowObj)
		{
			var siblingCount = windowObj.transform.parent.childCount;
			windowObj.transform.SetSiblingIndex(siblingCount - 1);
		}

		private List<IWindow> BuildAffectedWindowList(WindowInfo windowInfo)
		{
			List<IWindow> historyWindows = null;
			switch (windowInfo.openAction)
			{
				case OpenAction.HideAll:
					historyWindows = new List<IWindow>();
					foreach (var wnd in mShownWindowDic)
					{
						if (wnd.Key == windowInfo)
							continue;
						IWindow tmpWindow = wnd.Value;
						if (!tmpWindow.IsActived)
							continue;
						historyWindows.Add(tmpWindow);
					}
					break;
				case OpenAction.HideNormalsMains:
					historyWindows = new List<IWindow>();
					foreach (var wnd in mShownWindowDic)
					{
						if (wnd.Key == windowInfo)
							continue;
						if (wnd.Key.showMode != ShowMode.Normal && wnd.Key.showMode != ShowMode.Main)
							continue;
						IWindow tmpWindow = wnd.Value;
						if (!tmpWindow.IsActived)
							continue;
						historyWindows.Add(tmpWindow);
					}
					break;
				case OpenAction.DoNothing:
				default:
					historyWindows = null;
					break;
			}

			return historyWindows;
		}

		public void SetupCanvas()
		{
			GameObject uiRoot = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			uiRoot.transform.position = Vector3.zero;
			uiRoot.transform.localScale = Vector3.one;

			var canvas = uiRoot.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			var scaler = uiRoot.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1366, 768);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 0;

			var raycaster = uiRoot.GetComponent<GraphicRaycaster>();
			raycaster.ignoreReversedGraphics = true;
			raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

			GameObject eventRoot = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
			eventRoot.transform.localPosition = Vector3.zero;
			eventRoot.transform.localScale = Vector3.one;

			mUiCamera = new GameObject("Camera", typeof(UnityEngine.Camera));
			mUiCamera.transform.localPosition = Vector3.zero;
			mUiCamera.transform.localScale = Vector3.one;

			GameObject followRoot = new GameObject("follow");
			followRoot.transform.parent = uiRoot.transform;
			followRoot.transform.localPosition = Vector3.zero;
			followRoot.transform.localScale = Vector3.one;

			GameObject mainRoot = new GameObject("main");
			mainRoot.transform.parent = uiRoot.transform;
			mainRoot.transform.localPosition = Vector3.zero;
			mainRoot.transform.localScale = Vector3.one;

			GameObject normalRoot = new GameObject("normal");
			normalRoot.transform.parent = uiRoot.transform;
			normalRoot.transform.localPosition = Vector3.zero;
			normalRoot.transform.localScale = Vector3.one;

			GameObject fixedRoot = new GameObject("fixed");
			fixedRoot.transform.parent = uiRoot.transform;
			fixedRoot.transform.localPosition = Vector3.zero;
			fixedRoot.transform.localScale = Vector3.one;

			GameObject popupRoot = new GameObject("popup");
			popupRoot.transform.parent = uiRoot.transform;
			popupRoot.transform.localPosition = Vector3.zero;
			popupRoot.transform.localScale = Vector3.one;

			GameObject cacheRoot = new GameObject("_cached_");
			cacheRoot.transform.parent = uiRoot.transform;
			cacheRoot.transform.localPosition = Vector3.zero;
			cacheRoot.transform.localScale = Vector3.one;
		}

		public void RemoveCamera()
		{
			GameObject.Destroy(mUiCamera);
			mUiCamera = null;
		}

		public void AddCamera()
		{
			mUiCamera = new GameObject("Camera", typeof(UnityEngine.Camera));
			mUiCamera.transform.localPosition = Vector3.zero;
			mUiCamera.transform.localScale = Vector3.one;
		}

		public GameObject GetModeRoot(ShowMode mode)
		{
			switch (mode)
			{
				case ShowMode.Normal:
					return GameObject.Find("Canvas/normal");
				case ShowMode.Main:
					return GameObject.Find("Canvas/main");
				case ShowMode.Fixed:
					return GameObject.Find("Canvas/fixed");
				case ShowMode.Popup:
					return GameObject.Find("Canvas/popup");
				case ShowMode.Follow:
					return GameObject.Find("Canvas/follow");
			}
			return null;
		}

		private GameObject GetCacheRoot()
		{
			return GameObject.Find("Canvas/_cached_");
		}

	}
}
