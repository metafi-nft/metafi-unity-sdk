// Copyright (c) 2022 Vuplex Inc. All rights reserved.
//
// Licensed under the Vuplex Commercial Software Library License, you may
// not use this file except in compliance with the License. You may obtain
// a copy of the License at
//
//     https://vuplex.com/commercial-library-license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#if (UNITY_STANDALONE_WIN && !UNITY_EDITOR) || UNITY_EDITOR_WIN
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView {

    /// <summary>
    /// The Windows IWebView implementation.
    /// </summary>
    public class WindowsWebView : StandaloneWebView, IWebView {

        public WebPluginType PluginType { get; } = WebPluginType.Windows;

        public override void Dispose() {

            // Cancel the render if it has been scheduled via GL.IssuePluginEvent().
            WebView_removePointer(_nativeWebViewPtr);
            base.Dispose();
        }

        public static WindowsWebView Instantiate() => new GameObject().AddComponent<WindowsWebView>();

        public static bool ValidateGraphicsApi() {

            var isValid = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11;
            if (!isValid) {
                WebViewLogger.LogError("Unsupported graphics API: 3D WebView for Windows requires Direct3D11. Please go to Player Settings and set \"Graphics APIs for Windows\" to Direct3D11.");
            }
            return isValid;
        }

        readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

        protected override StandaloneWebView _instantiate() => Instantiate();

        // Start the coroutine from OnEnable so that the coroutine
        // is restarted if the object is deactivated and then reactivated.
        void OnEnable() => StartCoroutine(_renderPluginOncePerFrame());

        IEnumerator _renderPluginOncePerFrame() {

            while (true) {
                if (Application.isBatchMode) {
                    // When Unity is launched in batch mode from the command line,
                    // WaitForEndOfFrame() never returns, which can cause automated tests to fail.
                    yield return null;
                } else {
                    yield return _waitForEndOfFrame;
                }

                if (_nativeWebViewPtr != IntPtr.Zero && !IsDisposed) {
                    int pointerId = WebView_depositPointer(_nativeWebViewPtr);
                    GL.IssuePluginEvent(WebView_getRenderFunction(), pointerId);
                }
            }
        }

        [DllImport(_dllName)]
        static extern int WebView_depositPointer(IntPtr pointer);

        [DllImport(_dllName)]
        static extern IntPtr WebView_getRenderFunction();

        [DllImport(_dllName)]
        static extern void WebView_removePointer(IntPtr pointer);
    }
}
#endif
