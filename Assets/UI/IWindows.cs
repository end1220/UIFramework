using UnityEngine;

namespace TwUI{
    public /*abstract*/ class IWindow : MonoBehaviour
    {
        private WindowInfo _windowInfo = null;
        public WindowInfo windowInfo { set { _windowInfo = value; } get { return _windowInfo; } }

        private WindowInfo _previousWindowInfo = null;
        public WindowInfo PreviousWindowInfo { set { _previousWindowInfo = value; } get { return _previousWindowInfo; } }

        private bool _isActived = false;
        public bool IsActived { get { return _isActived; } }
        
        public static object[] CallMethod(string func, params object[] args)
        {
			if(TwFramework.GameFramework.Instance != null)
			{
				TwFramework.LuaManager luaMgr = TwFramework.GameFramework.GetLuaManager();
				return luaMgr.CallMethod(func, args);
			}
			return null;
        }

        #region Module internal functions, do not call them manually !

        /// <summary>
        /// Invoked when instantiated.
        /// Do not call this manually ! 
        /// </summary>
        public void _Instantiate()
        {
            this.OnInit();
        }

        /// <summary>
        /// Invoked when open.
        /// Do not call this manually ! 
        /// </summary>
        /// <param name="context"></param>
        public void _Enter(IContext context = null)
        {
            if (this._isActived)
                return;
            this.gameObject.SetActive(true);
            this._isActived = true;
            this.OnEnter(context);
        }

        /// <summary>
        /// Do not call this manually ! 
        /// </summary>
        /// <param name="context"></param>
        public void _Exit(IContext context = null)
        {
            this.OnExit(context);
            this.gameObject.SetActive(false);
            this._isActived = false;
        }

        /// <summary>
        /// Do not call this manually ! 
        /// </summary>
        /// <param name="context"></param>
        public void _Pause(IContext context = null)
        {
            if (!this._isActived)
                return;
            this.gameObject.SetActive(false);
            this._isActived = false;
            this.OnPause(context);
        }

        /// <summary>
        /// Do not call this manually ! 
        /// </summary>
        /// <param name="context"></param>
        public void _Resume(IContext context = null)
        {
            if (this._isActived)
                return;
            this.gameObject.SetActive(true);
            this._isActived = true;
            this.OnResume(context);
        }

        #endregion

        protected virtual void OnInit()
        {
            //去掉
            //CallMethod("CSharpPort.Window_OnInit", name);
        }

        protected virtual void OnEnter(IContext context)
        {
            CallMethod("CSharpPort.Window_OnEnter", windowInfo.scriptName, gameObject);
        }

        protected virtual void OnExit(IContext context)
        {
            CallMethod("CSharpPort.Window_OnExit", windowInfo.scriptName);
        }

        protected virtual void OnPause(IContext context)
        {
            CallMethod("CSharpPort.Window_OnPause", windowInfo.scriptName);
        }

        protected virtual void OnResume(IContext context)
        {
            CallMethod("CSharpPort.Window_OnResume", windowInfo.scriptName);
        }

        protected virtual void Update()
        {
 /*           if (windowInfo.isTick == true )
                CallMethod("CSharpPort.Window_OnTick", name);*/
        }

        public virtual void OnDestroy()
        {
            _Exit();
        }

        /// <summary>
        /// Get a component in a child of the window.
        /// </summary>
        /// <param name="context"></param>
        /// 
        public T FindWidget<T>(string path) where T : Component
        {
            var child = this.transform.FindChild(path);
            if(child == null)
            {
                Debug.LogError(string.Format("cannot find child at {0}", path));
                return null;
            }
            var com = child.GetComponent<T>();
            if (com == null)
            {
                Debug.LogError(string.Format("cannot find component named {0}", typeof(T).Name));
                return null;
            }
            return com;
        }

    }
}
