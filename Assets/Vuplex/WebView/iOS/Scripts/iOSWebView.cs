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
#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView {

    /// <summary>
    /// The IWebView implementation used by 3D WebView for iOS.
    /// </summary>
    public class iOSWebView : BaseWebView,
                              IWebView,
                              IWithFallbackVideo,
                              IWithMovablePointer,
                              IWithNative2DMode,
                              IWithNativeOnScreenKeyboard,
                              IWithPdfCreation,
                              IWithPointerDownAndUp,
                              IWithPopups,
                              IWithSettableUserAgent {

        /// <see cref="IWithFallbackVideo"/>
        public bool FallbackVideoEnabled { get; private set; }

        /// <see cref="IWithNative2DMode"/>
        public bool Native2DModeEnabled { get; private set; }

        public WebPluginType PluginType { get; } = WebPluginType.iOS;

        /// <see cref="IWithNative2DMode"/>
        public Rect Rect { get { return _rect; }}

        /// <see cref="IWithFallbackVideo"/>
        public new Texture2D VideoTexture { get; private set; }

        /// <see cref="IWithNative2DMode"/>
        public bool Visible { get; private set; }

        /// <see cref="IWithPopups"/>
        public event EventHandler<PopupRequestedEventArgs> PopupRequested;

        /// <see cref="IWithFallbackVideo"/>
        public new event EventHandler<EventArgs<Rect>> VideoRectChanged;

        /// <seealso cref="IWithNative2DMode"/>
        public void BringToFront() {

            _assertValidState();
            _assertNative2DModeEnabled();
            WebView_bringToFront(_nativeWebViewPtr);
        }

        public static void ClearAllData() => WebView_clearAllData();

        public override void Click(int xInPixels, int yInPixels, bool preventStealingFocus = false) {

            _assertValidState();
            _assertPointIsWithinBounds(xInPixels, yInPixels);
            if (preventStealingFocus) {
                WebView_clickWithoutStealingFocus(_nativeWebViewPtr, xInPixels, yInPixels);
            } else {
                WebView_click(_nativeWebViewPtr, xInPixels, yInPixels);
            }
        }

        // Override because BaseWebView.CaptureScreenshot() uses too much memory on iOS.
        public override Task<byte[]> CaptureScreenshot() {

            _assertValidState();
            IntPtr unmanagedBytes = IntPtr.Zero;
            int unmanagedBytesLength = 0;
            WebView_captureScreenshot(_nativeWebViewPtr, ref unmanagedBytes, ref unmanagedBytesLength);
            // Load the results into a managed array.
            var managedBytes = new byte[unmanagedBytesLength];
            Marshal.Copy(unmanagedBytes, managedBytes, 0, unmanagedBytesLength);
            WebView_freeMemory(unmanagedBytes);
            return Task.FromResult(managedBytes);
        }

        public override Material CreateMaterial() {

            var material = new Material(Resources.Load<Material>("iOSWebMaterial"));
            material.mainTexture = Texture;
            return material;
        }

        /// <see cref="IWithPdfCreation"/>
        public Task<string> CreatePdf() {

            _assertValidState();
            var taskSource = new TaskCompletionSource<string>();
            var resultCallbackId = Guid.NewGuid().ToString();
            _pendingCreatePdfTaskSources[resultCallbackId] = taskSource;
            var pdfSubdirectory = Path.Combine(Application.temporaryCachePath, "Vuplex.WebView", "pdfs");
            Directory.CreateDirectory(pdfSubdirectory);
            var pdfPath = Path.Combine(pdfSubdirectory, resultCallbackId + ".pdf");
            WebView_createPdf(_nativeWebViewPtr, resultCallbackId, pdfPath);
            return taskSource.Task;
        }

        /// <see cref="IWithFallbackVideo"/>
        public Material CreateVideoMaterial() {

            var material = new Material(Resources.Load<Material>("iOSVideoMaterial"));
            material.mainTexture = VideoTexture;
            return material;
        }

        public static Task<bool> DeleteCookies(string url, string cookieName = null) {

            if (url == null) {
                throw new ArgumentException("The url cannot be null.");
            }
            var taskSource = new TaskCompletionSource<bool>();
            var resultCallbackId = Guid.NewGuid().ToString();
            _pendingDeleteCookiesResultCallbacks[resultCallbackId] = taskSource.SetResult;
            WebView_deleteCookies(url, cookieName, resultCallbackId);
            return taskSource.Task;
        }

        public static Task<Cookie[]> GetCookies(string url, string cookieName = null) {

            if (url == null) {
                throw new ArgumentException("The url cannot be null.");
            }
            var taskSource = new TaskCompletionSource<Cookie[]>();
            var resultCallbackId = Guid.NewGuid().ToString();
            _pendingGetCookiesResultCallbacks[resultCallbackId] = taskSource.SetResult;
            WebView_getCookies(url, cookieName, resultCallbackId);
            return taskSource.Task;
        }

        /// <summary>
        /// Returns a pointer to the instance's native Objective-C WKWebView.
        /// </summary>
        /// <remarks>
        /// Warning: Adding code that interacts with the native WKWebView directly
        /// may interfere with 3D WebView's functionality
        /// and vice versa. So, it's highly recommended to stick to 3D WebView's
        /// C# APIs whenever possible and only use GetNativeWebView() if
        /// truly necessary. If 3D WebView is missing an API that you need,
        /// feel free to [contact us](https://vuplex.com/contact).
        /// </remarks>
        /// <example>
        /// <code>
        /// // Example of defining a native Objective-C function that sets WKWebView.allowsLinkPreview.
        /// // Place this in a .m file in your project, like Assets/Plugins/WebViewCustom.m
        /// #import &lt;Foundation/Foundation.h&gt;
        /// #import &lt;WebKit/WebKit.h&gt;
        ///
        /// void WebViewCustom_SetAllowsLinkPreview(WKWebView *webView, BOOL allowsLinkPreview) {
        ///
        ///     webView.allowsLinkPreview = allowsLinkPreview;
        /// }
        /// </code>
        /// <code>
        /// // Example of calling the native Objective-C function from C#.
        /// async void EnableLinkPreviews(WebViewPrefab webViewPrefab) {
        ///
        ///     await webViewPrefab.WaitUntilInitialized();
        ///     #if UNITY_IOS &amp;&amp; !UNITY_EDITOR
        ///         var wkWebViewPtr = (webViewPrefab.WebView as iOSWebView).GetNativeWebView();
        ///         WebViewCustom_SetAllowsLinkPreview(wkWebViewPtr, true);
        ///     #endif
        /// }
        ///
        /// [System.Runtime.InteropServices.DllImport("__Internal")]
        /// static extern void WebViewCustom_SetAllowsLinkPreview(System.IntPtr webViewPtr, bool allowsLinkPreview);
        /// </code>
        /// </example>
        public IntPtr GetNativeWebView() {

            _assertValidState();
            return WebView_getNativeWebView(_nativeWebViewPtr);
        }

        // Overrides because BaseWebView.GetRawTextureData() uses too much memory on iOS.
        public override Task<byte[]> GetRawTextureData() {

            _assertValidState();
            IntPtr unmanagedBytes = IntPtr.Zero;
            int unmanagedBytesLength = 0;
            WebView_getRawTextureData(_nativeWebViewPtr, ref unmanagedBytes, ref unmanagedBytesLength);
            // Load the results into a managed array.
            var managedBytes = new byte[unmanagedBytesLength];
            Marshal.Copy(unmanagedBytes, managedBytes, 0, unmanagedBytesLength);
            WebView_freeMemory(unmanagedBytes);
            return Task.FromResult(managedBytes);
        }

        public static void GloballySetUserAgent(bool mobile) => WebView_globallySetUserAgentToMobile(mobile);

        public static void GloballySetUserAgent(string userAgent) => WebView_globallySetUserAgent(userAgent);

        public async Task Init(int width, int height) => await _initIOS3D(width, height, IntPtr.Zero);

        /// <see cref="IWithNative2DMode"/>
        public async Task InitInNative2DMode(Rect rect) =>  await _initIOS2D(rect, IntPtr.Zero);

        public static iOSWebView Instantiate() => new GameObject().AddComponent<iOSWebView>();

        /// <see cref="IWithMovablePointer"/>
        public void MovePointer(Vector2 normalizedPoint, bool pointerLeave = false) {

            _assertValidState();
            var pixelsPoint = _convertNormalizedToPixels(normalizedPoint);
            WebView_movePointer(_nativeWebViewPtr, pixelsPoint.x, pixelsPoint.y, pointerLeave);
        }

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerDown(Vector2 point) => _pointerDown(point, MouseButton.Left, 1);

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerDown(Vector2 point, PointerOptions options) {

            if (options == null) {
                options = new PointerOptions();
            }
            _pointerDown(point, options.Button, options.ClickCount);
        }

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerUp(Vector2 point) => _pointerUp(point, MouseButton.Left, 1);

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerUp(Vector2 point, PointerOptions options) {

            if (options == null) {
                options = new PointerOptions();
            }
            _pointerUp(point, options.Button, options.ClickCount);
        }

        /// <summary>
        /// Sets whether horizontal swipe gestures trigger backward and forward page navigation.
        /// The default is `false`.
        /// </summary>
        /// <example>
        /// <code>
        /// await webViewPrefab.WaitUntilInitialized();
        /// #if UNITY_IOS &amp;&amp; !UNITY_EDITOR
        ///     var iOSWebViewInstance = webViewPrefab.Webview as iOSWebView;
        ///     iOSWebViewInstance.SetAllowsBackForwardNavigationGestures(true);
        /// #endif
        /// </code>
        /// </example>
        /// <seealso href="https://developer.apple.com/documentation/webkit/wkwebview/1414995-allowsbackforwardnavigationgestu">WKWebView.allowsBackForwardNavigationGestures</seealso>
        public void SetAllowsBackForwardNavigationGestures(bool allow) {

            _assertValidState();
            WebView_setAllowsBackForwardNavigationGestures(_nativeWebViewPtr, allow);
        }

        /// <summary>
        /// Sets whether HTML5 videos play inline or use the native full-screen controller.
        /// The default is `true`. This method is static because the WKWebView's configuration
        /// cannot be modified at runtime after the webview is created.
        /// </summary>
        /// <example>
        /// <code>
        /// #if UNITY_IOS &amp;&amp; !UNITY_EDITOR
        ///     iOSWebView.SetAllowsInlineMediaPlayback(false);
        /// #endif
        /// </code>
        /// </example>
        /// <seealso href="https://developer.apple.com/documentation/webkit/wkwebviewconfiguration/1614793-allowsinlinemediaplayback">WKWebViewConfiguration.allowsInlineMediaPlayback</seealso>
        public static void SetAllowsInlineMediaPlayback(bool allow) => WebView_setAllowsInlineMediaPlayback(allow);

        public static void SetAutoplayEnabled(bool enabled) => WebView_setAutoplayEnabled(enabled);

        public static Task<bool> SetCookie(Cookie cookie) {

            if (cookie == null) {
                throw new ArgumentException("Cookie cannot be null.");
            }
            if (!cookie.IsValid) {
                throw new ArgumentException("Cannot set invalid cookie: " + cookie);
            }
            WebView_setCookie(cookie.ToJson());
            return Task.FromResult(true);
        }

        /// <summary>
        /// Sets whether deep links are enabled. The default is `false`. In order to
        /// open a link with a custom URI scheme, that scheme must also be listed in
        /// the app's Info.plist using the key [LSApplicationQueriesSchemes](https://developer.apple.com/library/archive/documentation/General/Reference/InfoPlistKeyReference/Articles/LaunchServicesKeys.html),
        /// otherwise iOS will block the custom URI scheme from being loaded.
        /// </summary>
        /// <example>
        /// C# example:
        /// <code>
        /// async void Awake() {
        ///     #if UNITY_IOS &amp;&amp; !UNITY_EDITOR
        ///         iOSWebView.SetDeepLinksEnabled(true);
        ///     #endif
        ///     await webViewPrefab.WaitUntilInitialized();
        ///     // Load a page with a link that opens the YouTube app.
        ///     webViewPrefab.WebView.LoadHtml("&lt;a href='vnd.youtube://grP0iDrSjso'&gt;Click to launch YouTube&lt;/a&gt;");
        /// }
        /// </code>
        /// Example Info.plist entry:
        /// <code>
        /// &lt;key&gt;LSApplicationQueriesSchemes&lt;/key&gt;
        /// &lt;array&gt;
        ///     &lt;string&gt;vnd.youtube&lt;/string&gt;
        /// &lt;/array&gt;
        /// </code>
        /// </example>
        public static void SetDeepLinksEnabled(bool enabled) => WebView_setDeepLinksEnabled(enabled);

        /// <see cref="IWithFallbackVideo"/>
        public void SetFallbackVideoEnabled(bool enabled) {

            if (_initState != InitState.Uninitialized) {
                throw new InvalidOperationException("IWithFallbackVideo.SetFallbackVideoEnabled() can only be called prior to initializing the webview.");
            }
            FallbackVideoEnabled = enabled;
        }

        public static void SetIgnoreCertificateErrors(bool ignore) => WebView_setIgnoreCertificateErrors(ignore);

        /// <summary>
        /// When Native 2D Mode is enabled, this method sets whether long press
        /// gestures are enabled. The default is `true`. When Native 2D Mode is
        /// not enabled, this method has no effect.
        /// </summary>
        /// <example>
        /// <code>
        /// await webViewPrefab.WaitUntilInitialized();
        /// #if UNITY_IOS &amp;&amp; !UNITY_EDITOR
        ///     var iOSWebViewInstance = webViewPrefab.WebView as iOSWebView;
        ///     iOSWebViewInstance.SetLongPressGesturesEnabled(false);
        /// #endif
        /// </code>
        /// </example>
        public void SetLongPressGesturesEnabled(bool enabled) {

            _assertValidState();
            WebView_setLongPressGesturesEnabled(_nativeWebViewPtr, enabled);
        }

        /// <see cref="IWithNativeOnScreenKeyboard"/>
        public void SetNativeOnScreenKeyboardEnabled(bool enabled) {

            _assertValidState();
            WebView_setNativeOnScreenKeyboardEnabled(_nativeWebViewPtr, enabled);
        }

        /// <see cref="IWithNative2DMode"/>
        public void SetNativeZoomEnabled(bool enabled) {

            _assertValidState();
            _assertNative2DModeEnabled();
            WebView_setNativeZoomEnabled(_nativeWebViewPtr, enabled);
        }

        /// <see cref="IWithPopups"/>
        public void SetPopupMode(PopupMode popupMode) {

            _assertValidState();
            WebView_setPopupMode(_nativeWebViewPtr, (int)popupMode);
        }

        /// <see cref="IWithNative2DMode"/>
        public void SetRect(Rect rect) {

            _assertValidState();
            _assertNative2DModeEnabled();
            _rect = rect;
            WebView_setRect(_nativeWebViewPtr, (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        }

        public override void SetRenderingEnabled(bool enabled) {

            if (Native2DModeEnabled) {
                VXUtils.LogNative2DModeWarning("SetRenderingEnabled");
                return;
            }
            base.SetRenderingEnabled(enabled);
            if (enabled && _currentVideoNativeTexture != IntPtr.Zero) {
                VideoTexture.UpdateExternalTexture(_currentVideoNativeTexture);
            }
        }

        /// <summary>
        /// When Native 2D Mode is enabled, this method sets whether the scroll view bounces past
        /// the edge of content and back again. The default is `true`. When Native 2D Mode is
        /// disabled, this method has no effect.
        /// </summary>
        /// <example>
        /// <code>
        /// await webViewPrefab.WaitUntilInitialized();
        /// #if UNITY_IOS &amp;&amp; !UNITY_EDITOR
        ///     var iOSWebViewInstance = webViewPrefab.WebView as iOSWebView;
        ///     iOSWebViewInstance.SetScrollViewBounces(false);
        /// #endif
        /// </code>
        /// </example>
        /// <seealso href="https://developer.apple.com/documentation/uikit/uiscrollview/1619420-bounces">UIScrollView.bounces</seealso>
        public void SetScrollViewBounces(bool bounces) {

            _assertValidState();
            WebView_setScrollViewBounces(_nativeWebViewPtr, bounces);
        }

        public static void SetStorageEnabled(bool enabled) => WebView_setStorageEnabled(enabled);

        /// <summary>
        /// Sets the target web frame rate. The default is `30`, which is also the maximum value.
        /// This method can be used to lower the target web frame rate in order to decrease energy and CPU usage.
        /// 3D WebView's rendering speed is limited by the speed of the underlying iOS APIs, so
        /// the actual web frame rate achieved is always lower than the default target of 30 FPS.
        /// This method is only used for the default render mode and is ignored when Native 2D Mode is enabled.
        /// </summary>
        /// <example>
        /// <code>
        /// await webViewPrefab.WaitUntilInitialized();
        /// #if UNITY_IOS &amp;&amp; !UNITY_EDITOR
        ///     var iOSWebViewInstance = webViewPrefab.WebView as iOSWebView;
        ///     iOSWebViewInstance.SetTargetFrameRate(15);
        /// #endif
        /// </code>
        /// </example>
        public void SetTargetFrameRate(uint targetFrameRate) {

            if (Native2DModeEnabled) {
                VXUtils.LogNative2DModeWarning("SetTargetFrameRate");
                return;
            }
            if (targetFrameRate == 0 || targetFrameRate > 30) {
                throw new ArgumentException($"SetTargetFrameRate() called with invalid frame rate: {targetFrameRate}. The target frame rate must be between 1 and 30.");
            }
            WebView_setTargetFrameRate(_nativeWebViewPtr, targetFrameRate);
        }

        /// <see cref="IWithSettableUserAgent"/>
        public void SetUserAgent(bool mobile) {

            _assertValidState();
            WebView_setUserAgentToMobile(_nativeWebViewPtr, mobile);
        }

        /// <see cref="IWithSettableUserAgent"/>
        public void SetUserAgent(string userAgent) {

            _assertValidState();
            WebView_setUserAgent(_nativeWebViewPtr, userAgent);
        }

        /// <see cref="IWithNative2DMode"/>
        public void SetVisible(bool visible) {

            _assertValidState();
            _assertNative2DModeEnabled();
            Visible = visible;
            WebView_setVisible(_nativeWebViewPtr, visible);
        }

        public override void ZoomIn() {

            if (Native2DModeEnabled) {
                VXUtils.LogNative2DModeWarning("ZoomIn");
                return;
            }
            base.ZoomIn();
        }

        public override void ZoomOut() {

            if (Native2DModeEnabled) {
                VXUtils.LogNative2DModeWarning("ZoomOut");
                return;
            }
            base.ZoomOut();
        }

    #region Non-public members
        IntPtr _currentVideoNativeTexture;
        Dictionary<string, TaskCompletionSource<string>> _pendingCreatePdfTaskSources = new Dictionary<string, TaskCompletionSource<string>>();
        static Dictionary<string, Action<bool>> _pendingDeleteCookiesResultCallbacks = new Dictionary<string, Action<bool>>();
        static Dictionary<string, Action<Cookie[]>> _pendingGetCookiesResultCallbacks = new Dictionary<string, Action<Cookie[]>>();
        Rect _videoRect;
        readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

        void _applyVideoTexture() {

            if (_currentVideoNativeTexture == IntPtr.Zero) {
                return;
            }
            var previousNativeTexturePtr = VideoTexture.GetNativeTexturePtr();
            VideoTexture.UpdateExternalTexture(_currentVideoNativeTexture);
            VideoTexture.Apply();
            var newNativeTexturePtr = VideoTexture.GetNativeTexturePtr();
            if (!(previousNativeTexturePtr == IntPtr.Zero || previousNativeTexturePtr == newNativeTexturePtr)) {
                WebView_destroyTexture(previousNativeTexturePtr, SystemInfo.graphicsDeviceType.ToString());
            }
        }

        void _assertNative2DModeEnabled() {

            if (!Native2DModeEnabled) {
                throw new InvalidOperationException("IWithNative2DMode methods can only be called on a webview with Native 2D Mode enabled.");
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Action<string>))]
        static void _handleDeleteCookiesResult(string resultCallbackId) {

            var callback = _pendingDeleteCookiesResultCallbacks[resultCallbackId];
            _pendingDeleteCookiesResultCallbacks.Remove(resultCallbackId);
            callback(true);
        }

        [AOT.MonoPInvokeCallback(typeof(Action<string, string>))]
        static void _handleGetCookiesResult(string resultCallbackId, string serializedCookies) {

            var callback = _pendingGetCookiesResultCallbacks[resultCallbackId];
            _pendingGetCookiesResultCallbacks.Remove(resultCallbackId);
            var cookies = Cookie.ArrayFromJson(serializedCookies);
            callback(cookies);
        }

        // Invoked by the native plugin.
        // Override to handle videoRectChanged messages.
        protected override void HandleMessageEmitted(string serializedMessage) {

            var messageType = serializedMessage.Contains("vuplex.webview") ? BridgeMessage.ParseType(serializedMessage) : null;
            if (messageType != "vuplex.webview.videoRectChanged") {
                base.HandleMessageEmitted(serializedMessage);
                return;
            }
            var value = JsonUtility.FromJson<VideoRectChangedMessage>(serializedMessage).value;
            var newRect = value.rect.ToRect();
            if (_videoRect != newRect) {
                _videoRect = newRect;
                VideoRectChanged?.Invoke(this, new EventArgs<Rect>(newRect));
            }
        }

        // Invoked by the native plugin.
        void HandlePdfCreated(string message) {

            var components = message.Split(new char[] { ',' }, 3);
            var resultCallbackId = components[0];
            var succeeded = Boolean.Parse(components[1]);
            var filePathOrErrorMessage = components[2];
            var taskSource = _pendingCreatePdfTaskSources[resultCallbackId];
            _pendingCreatePdfTaskSources.Remove(resultCallbackId);
            if (succeeded) {
                taskSource.SetResult(filePathOrErrorMessage);
            } else {
                taskSource.SetException(new Exception("Error while creating the PDF: " + filePathOrErrorMessage));
            }
        }

        // Invoked by the native plugin.
        async void HandlePopup(string paramsString) {

            var parameters = paramsString.Split(new char[] { ',' });
            if (!(parameters.Length == 1 || parameters.Length == 2)) {
                WebViewLogger.LogError($"HandlePopup received an unexpected number of parameters ({parameters.Length}): {paramsString}");
                return;
            }
            var url = parameters[0];
            iOSWebView popupWebView = null;
            if (parameters.Length == 2) {
                var nativePopupWebViewPtr = new IntPtr(Int64.Parse(parameters[1]));
                popupWebView = Instantiate();
                if (Native2DModeEnabled) {
                    await popupWebView._initIOS2D(Rect, nativePopupWebViewPtr);
                } else {
                    await popupWebView._initIOS3D(Size.x, Size.y, nativePopupWebViewPtr);
                }
            }
            PopupRequested?.Invoke(this, new PopupRequestedEventArgs(url, popupWebView));
        }

        // Invoked by the native plugin.
        void HandleVideoTextureChanged(string textureString) {

            var nativeTexture = new IntPtr(Int64.Parse(textureString));
            if (nativeTexture == _currentVideoNativeTexture) {
                return;
            }
            var previousNativeTexture = _currentVideoNativeTexture;
            _currentVideoNativeTexture = nativeTexture;
            if (_renderingEnabled) {
                VideoTexture.UpdateExternalTexture(_currentVideoNativeTexture);
            }

            if (previousNativeTexture != IntPtr.Zero && previousNativeTexture != _currentVideoNativeTexture) {
                WebView_destroyTexture(previousNativeTexture, SystemInfo.graphicsDeviceType.ToString());
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void _initializePlugin() {

            WebView_setCookieCallbacks(
                Marshal.GetFunctionPointerForDelegate<Action<string, string>>(_handleGetCookiesResult),
                Marshal.GetFunctionPointerForDelegate<Action<string>>(_handleDeleteCookiesResult)
            );
        }

        async Task _initIOS2D(Rect rect, IntPtr popupNativeWebView) {

            Native2DModeEnabled = true;
            _rect = rect;
            Visible = true;
            await _initBase((int)rect.width, (int)rect.height, createTexture: false);
            _nativeWebViewPtr = WebView_newInNative2DMode(
                gameObject.name,
                (int)rect.x,
                (int)rect.y,
                (int)rect.width,
                (int)rect.height,
                popupNativeWebView
            );
        }

        async Task _initIOS3D(int width, int height, IntPtr popupNativeWebView) {

            await _initBase(width, height);
            if (FallbackVideoEnabled) {
                VideoTexture = await _createTexture(width, height);
            }
            _nativeWebViewPtr = WebView_new(
                gameObject.name,
                width,
                height,
                FallbackVideoEnabled,
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal,
                popupNativeWebView
            );
        }

        // Start the coroutine from OnEnable so that the coroutine
        // is restarted if the object is deactivated and then reactivated.
        void OnEnable() => StartCoroutine(_renderPluginOncePerFrame());

        void _pointerDown(Vector2 normalizedPoint, MouseButton mouseButton, int clickCount) {

            _assertValidState();
            var pixelsPoint = _convertNormalizedToPixels(normalizedPoint);
            WebView_pointerDown(_nativeWebViewPtr, pixelsPoint.x, pixelsPoint.y, (int)mouseButton, clickCount);
        }

        void _pointerUp(Vector2 normalizedPoint, MouseButton mouseButton, int clickCount) {

            _assertValidState();
            var pixelsPoint = _convertNormalizedToPixels(normalizedPoint);
            WebView_pointerUp(_nativeWebViewPtr, pixelsPoint.x, pixelsPoint.y, (int)mouseButton, clickCount);
        }

        IEnumerator _renderPluginOncePerFrame() {

            while (true) {
                yield return _waitForEndOfFrame;
                if (Native2DModeEnabled) {
                    break;
                }
                if (!_renderingEnabled || IsDisposed) {
                    continue;
                }
                int pointerId = WebView_depositPointer(_nativeWebViewPtr);
                GL.IssuePluginEvent(WebView_getRenderFunction(), pointerId);
            }
        }

        #pragma warning disable CS0649
        [Serializable]
        class VideoRectChangedMessage : BridgeMessage {
            public VideoRectChangedMessageValue value;
        }

        [Serializable]
        class VideoRectChangedMessageValue {
            public SerializableRect rect;
        }

        [Serializable]
        class SerializableRect {
            public float left;
            public float top;
            public float width;
            public float height;
            public Rect ToRect() => new Rect(left, top, width, height);
        }

        [DllImport(_dllName)]
        static extern void WebView_bringToFront(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_captureScreenshot(IntPtr webViewPtr, ref IntPtr bytes, ref int length);

        [DllImport(_dllName)]
        static extern void WebView_clearAllData();

        [DllImport(_dllName)]
        static extern void WebView_clickWithoutStealingFocus(IntPtr webViewPtr, int x, int y);

        [DllImport(_dllName)]
        static extern void WebView_createPdf(IntPtr webViewPtr, string resultCallbackId, string filePath);

        [DllImport(_dllName)]
        static extern void WebView_deleteCookies(string url, string cookieName, string resultCallbackId);

        [DllImport(_dllName)]
        static extern int WebView_depositPointer(IntPtr pointer);

        [DllImport(_dllName)]
        static extern void WebView_freeMemory(IntPtr bytes);

        [DllImport(_dllName)]
        static extern void WebView_getCookies(string url, string cookieName, string resultCallbackId);

        [DllImport(_dllName)]
        static extern IntPtr WebView_getNativeWebView(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_getRawTextureData(IntPtr webViewPtr, ref IntPtr bytes, ref int length);

        [DllImport(_dllName)]
        static extern IntPtr WebView_getRenderFunction();

        [DllImport(_dllName)]
        static extern void WebView_globallySetUserAgentToMobile(bool mobile);

        [DllImport(_dllName)]
        static extern void WebView_globallySetUserAgent(string userAgent);

        [DllImport (_dllName)]
        static extern void WebView_movePointer(IntPtr webViewPtr, int x, int y, bool pointerLeave);

        [DllImport(_dllName)]
        static extern IntPtr WebView_new(
            string gameObjectName,
            int width,
            int height,
            bool fallbackVideoSupportEnabled,
            bool useOpenGL,
            IntPtr popupNativeWebView
        );

        [DllImport(_dllName)]
        static extern IntPtr WebView_newInNative2DMode(
            string gameObjectName,
            int x,
            int y,
            int width,
            int height,
            IntPtr popupNativeWebView
        );

        [DllImport (_dllName)]
        static extern void WebView_pointerDown(IntPtr webViewPtr, int x, int y, int mouseButton, int clickCount);

        [DllImport (_dllName)]
        static extern void WebView_pointerUp(IntPtr webViewPtr, int x, int y, int mouseButton, int clickCount);

        [DllImport(_dllName)]
        static extern void WebView_setAllowsBackForwardNavigationGestures(IntPtr webViewPtr, bool allow);

        [DllImport(_dllName)]
        static extern void WebView_setAllowsInlineMediaPlayback(bool allow);

        [DllImport(_dllName)]
        static extern void WebView_setAutoplayEnabled(bool ignore);

        [DllImport(_dllName)]
        static extern void WebView_setCookie(string serializedCookie);

        [DllImport(_dllName)]
        static extern int WebView_setCookieCallbacks(IntPtr getCookiesCallback, IntPtr deleteCookiesCallback);

        [DllImport(_dllName)]
        static extern void WebView_setDeepLinksEnabled(bool enabled);

        [DllImport(_dllName)]
        static extern void WebView_setIgnoreCertificateErrors(bool ignore);

        [DllImport(_dllName)]
        static extern void WebView_setLongPressGesturesEnabled(IntPtr webViewPtr, bool enabled);

        [DllImport(_dllName)]
        static extern void WebView_setPopupMode(IntPtr webViewPtr, int popupMode);

        [DllImport(_dllName)]
        static extern void WebView_setNativeOnScreenKeyboardEnabled(IntPtr webViewPtr, bool enabled);

        [DllImport(_dllName)]
        static extern void WebView_setNativeZoomEnabled(IntPtr webViewPtr, bool enabled);

        [DllImport (_dllName)]
        static extern void WebView_setRect(IntPtr webViewPtr, int x, int y, int width, int height);

        [DllImport (_dllName)]
        static extern void WebView_setScrollViewBounces(IntPtr webViewPtr, bool bounces);

        [DllImport(_dllName)]
        static extern void WebView_setStorageEnabled(bool enabled);

        [DllImport(_dllName)]
        static extern void WebView_setTargetFrameRate(IntPtr webViewPtr, uint targetFrameRate);

        [DllImport(_dllName)]
        static extern void WebView_setUserAgentToMobile(IntPtr webViewPtr, bool mobile);

        [DllImport(_dllName)]
        static extern void WebView_setUserAgent(IntPtr webViewPtr, string userAgent);

        [DllImport (_dllName)]
        static extern void WebView_setVisible(IntPtr webViewPtr, bool visible);
    #endregion

    #region Obsolete APIs
        // Added in v3.15, deprecated in v4.0.
        [Obsolete("iOSWebView.GetCookie() is now deprecated. Please use Web.CookieManager.GetCookies() instead: https://developer.vuplex.com/webview/CookieManager#GetCookies")]
        public static async Task<Cookie> GetCookie(string url, string cookieName) {
            var cookies = await GetCookies(url, cookieName);
            return cookies.Length > 0 ? cookies[0] : null;
        }

        // Added in v3.15, deprecated in v4.0.
        [Obsolete("iOSWebView.GetCookie() is now deprecated. Please use Web.CookieManager.GetCookies() instead: https://developer.vuplex.com/webview/CookieManager#GetCookies")]
        public static async void GetCookie(string url, string cookieName, Action<Cookie> callback) {
            var cookie = await GetCookie(url, cookieName);
            callback(cookie);
        }

        // Added in v1.0, deprecated in v3.7, removed in v4.0.
        [Obsolete("iOSWebView.GetFileUrlForBundleResource has been removed. You can now use LoadUrl(\"streaming-assets://{path}\") to load a file from StreamingAssets instead: https://support.vuplex.com/articles/how-to-load-local-files", true)]
        public static string GetFileUrlForBundleResource(string fileName) { return null; }

        // Added in v3.1, removed in v3.11.
        [Obsolete("iOSWebView.SetCustomUriSchemesEnabled() has been removed. Now when a page redirects to a URI with a custom scheme, 3D WebView will automatically emit the UrlChanged and LoadProgressChanged events for the navigation, but a deep link (i.e. to an external application) won't occur.", true)]
        public static void SetCustomUriSchemesEnabled(bool enabled) {}

        // Added in v2.6.1, deprecated in v3.10, removed in v4.0.
        [Obsolete("iOSWebView.SetNativeKeyboardEnabled() has been removed. Please use the NativeOnScreenKeyboardEnabled property of WebViewPrefab / CanvasWebViewPrefab or the IWithNativeOnScreenKeyboard interface instead: https://developer.vuplex.com/webview/WebViewPrefab#NativeOnScreenKeyboardEnabled", true)]
        public static void SetNativeKeyboardEnabled(bool enabled) {}
    #endregion
    }
}
#endif
