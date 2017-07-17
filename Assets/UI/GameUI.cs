using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TwUI
{
    public class GameUI
    {
        // for flexible access.
        private static Dictionary<string, WindowInfo> mWindowInfoMap = new Dictionary<string, WindowInfo>();
        public static WindowInfo GetWindowInfo(string windowName)
        {
            WindowInfo info = null;
            mWindowInfoMap.TryGetValue(windowName, out info);
            return info;
        }
        public static WindowInfo AddWindowInfo(string path, ShowMode showMode, OpenAction openAct, BackgroundMode bgMode ,string scriptName)
        {
            var info = new WindowInfo(path, showMode, openAct, bgMode , scriptName);
            mWindowInfoMap.Add(info.name, info);
            return info;
        }
    }

}