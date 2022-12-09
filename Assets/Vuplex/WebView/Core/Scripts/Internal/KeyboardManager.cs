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
// Only define BaseWebView.cs on supported platforms to avoid IL2CPP linking
// errors on unsupported platforms.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vuplex.WebView.Internal {

    /// <summary>
    /// Internal class that implements the KeyboardEnabled setting for WebViewPrefab and CanvasWebViewPrefab.
    /// </summary>
    class KeyboardManager : MonoBehaviour {

        public static KeyboardManager Instance {
            get {
                // Don't recreate the instance if it's already been destroyed due to the app closing, otherwise it will cause Unity to
                // log the error "Some objects were not cleaned up when closing the scene" when stopping the player in the editor.
                if (_instance == null && !_destroyed) {
                    _instance = new GameObject("WebView Keyboard Manager").AddComponent<KeyboardManager>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }

        public void AddKeyboard(BaseKeyboard keyboard) {

            _keyboards.Add(keyboard);
            keyboard.KeyPressed += OnScreenKeyboard_KeyPressed;
            keyboard.BaseWebViewPrefab.PointerEntered += OnScreenKeyboard_PointerEntered;
            keyboard.BaseWebViewPrefab.PointerExited += OnScreenKeyboard_PointerExited;
        }

        public void RemoveKeyboard(BaseKeyboard keyboard) {

            if (!_keyboards.Contains(keyboard)) {
                return;
            }
            _keyboards.Remove(keyboard);
            keyboard.KeyPressed -= OnScreenKeyboard_KeyPressed;
            keyboard.BaseWebViewPrefab.PointerEntered -= OnScreenKeyboard_PointerEntered;
            keyboard.BaseWebViewPrefab.PointerExited -= OnScreenKeyboard_PointerExited;
        }

        public void SetKeyboardEnabled(BaseWebViewPrefab webViewPrefab, bool enabled) {

            if (enabled) {
                _addWebViewPrefab(webViewPrefab);
            } else {
                _removeWebViewPrefab(webViewPrefab);
            }
        }

        static bool _destroyed;
        BaseWebViewPrefab _focusedWebViewPrefab;
        BaseWebViewPrefab _hoveredWebViewPrefab;
        static KeyboardManager _instance;
        HashSet<BaseKeyboard> _keyboards = new HashSet<BaseKeyboard>();
        NativeKeyboardListener _nativeKeyboardListener;
        bool _pointerIsHoveringOverKeyboard;
        HashSet<BaseWebViewPrefab> _webViewPrefabs = new HashSet<BaseWebViewPrefab>();

        void Awake() {

            _nativeKeyboardListener = NativeKeyboardListener.Instantiate();
            _nativeKeyboardListener.transform.parent = transform;
            _nativeKeyboardListener.KeyDownReceived += NativeKeyboardListener_KeyDownReceived;
            _nativeKeyboardListener.KeyUpReceived += NativeKeyboardListener_KeyUpReceived;
        }

        void OnDestroy() => _destroyed = true;

        void WebViewPrefab_Clicked(object sender, ClickedEventArgs eventArgs) {

            var webViewPrefab = (BaseWebViewPrefab)sender;
            _setFocusedWebViewPrefab(webViewPrefab);
            // Also set _hoveredWebViewPrefab here in case the input module doesn't support PointerEntered.
            _hoveredWebViewPrefab = webViewPrefab;
        }

        void _addWebViewPrefab(BaseWebViewPrefab webViewPrefab) {

            _webViewPrefabs.Add(webViewPrefab);
            // Automatically focus the new prefab.
            _setFocusedWebViewPrefab(webViewPrefab);
            webViewPrefab.Clicked += WebViewPrefab_Clicked;
            webViewPrefab.PointerEntered += WebViewPrefab_PointerEntered;
            webViewPrefab.PointerExited += WebViewPrefab_PointerExited;
        }

        void NativeKeyboardListener_KeyDownReceived(object sender, KeyboardEventArgs eventArgs) {

            var webViewWithKeyDown = _focusedWebViewPrefab?.WebView as IWithKeyDownAndUp;
            if (webViewWithKeyDown != null) {
                webViewWithKeyDown.KeyDown(eventArgs.Key, eventArgs.Modifiers);
            } else {
                _focusedWebViewPrefab?.WebView?.SendKey(eventArgs.Key);
            }
        }

        void NativeKeyboardListener_KeyUpReceived(object sender, KeyboardEventArgs eventArgs) {

            var webViewWithKeyUp = _focusedWebViewPrefab?.WebView as IWithKeyDownAndUp;
            webViewWithKeyUp?.KeyUp(eventArgs.Key, eventArgs.Modifiers);
        }

        void OnScreenKeyboard_KeyPressed(object sender, EventArgs<string> eventArgs) {

            _focusedWebViewPrefab?.WebView?.SendKey(eventArgs.Value);
        }

        void OnScreenKeyboard_PointerEntered(object sender, EventArgs eventArgs) => _pointerIsHoveringOverKeyboard = true;

        void OnScreenKeyboard_PointerExited(object sender, EventArgs eventArgs) => _pointerIsHoveringOverKeyboard = false;

        void Update() {

            var mouseDown = false;
            #if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
                mouseDown = Input.GetMouseButtonDown(0);
            #endif
            if (_hoveredWebViewPrefab == null && !_pointerIsHoveringOverKeyboard && mouseDown) {
                // Some area outside of a keyboard-enabled webview or Keyboard was clicked, so unfocus the
                // webview in case the object clicked was a Unity Input Field.
                _setFocusedWebViewPrefab(null);
            }
        }

        void _removeWebViewPrefab(BaseWebViewPrefab webViewPrefab) {

            if (!_webViewPrefabs.Contains(webViewPrefab)) {
                return;
            }
            _webViewPrefabs.Remove(webViewPrefab);
            if (_focusedWebViewPrefab == webViewPrefab) {
                _focusedWebViewPrefab = null;
            }
            webViewPrefab.Clicked -= WebViewPrefab_Clicked;
            webViewPrefab.PointerEntered -= WebViewPrefab_PointerEntered;
            webViewPrefab.PointerExited -= WebViewPrefab_PointerExited;
        }

        void _setFocusedWebViewPrefab(BaseWebViewPrefab webViewPrefab) {

            if (_focusedWebViewPrefab != null && _focusedWebViewPrefab != webViewPrefab) {
                // Unfocus the previous webview.
                _focusedWebViewPrefab.WebView?.SetFocused(false);
            }
            _focusedWebViewPrefab = webViewPrefab;
        }

        void WebViewPrefab_PointerEntered(object sender, EventArgs eventArgs) {

            _hoveredWebViewPrefab = (BaseWebViewPrefab)sender;
        }

        void WebViewPrefab_PointerExited(object sender, EventArgs eventArgs) {

            var webViewPrefab = (BaseWebViewPrefab)sender;
            if (_hoveredWebViewPrefab == webViewPrefab) {
                _hoveredWebViewPrefab = null;
            }
        }
    }
}
