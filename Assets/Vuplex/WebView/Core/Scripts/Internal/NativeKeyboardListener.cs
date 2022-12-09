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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Vuplex.WebView.Internal {

    public class KeyboardEventArgs : EventArgs {

        public KeyboardEventArgs(string key, KeyModifier modifiers) {
            Key = key;
            Modifiers = modifiers;
        }
        public readonly string Key;
        public readonly KeyModifier Modifiers;
        public override string ToString() => $"Key: {Key}, Modifiers: {Modifiers}";
    }

    /// <summary>
    /// Internal class that detects keys pressed on the native hardware keyboard.
    /// </summary>
    public class NativeKeyboardListener : MonoBehaviour {

        public event EventHandler<KeyboardEventArgs> KeyDownReceived;

        public event EventHandler<KeyboardEventArgs> KeyUpReceived;

        public static NativeKeyboardListener Instantiate() {

            return new GameObject("NativeKeyboardListener").AddComponent<NativeKeyboardListener>();
        }

        Regex _alphanumericRegex = new Regex("[a-zA-Z0-9]");
        static readonly Func<string, bool> _hasValidUnityKeyName = _memoize<string, bool>(
            javaScriptKeyName => {
                try {
                    foreach (var keyName in _getPotentialUnityKeyNames(javaScriptKeyName)) {
                        Input.GetKey(keyName);
                    }
                    return true;
                } catch {
                    return false;
                }
            }
        );
        List<string> _keysDown = new List<string>();
        // Keys that don't show up correctly in Input.inputString. Must be defined before _keyValues.
        static readonly string[] _keyValuesUndetectableThroughInputString = new string[] {
            // Note: "Backspace" is included here for the Hololens system TouchScreenKeyboard. In other scenarios, \b is detectable through Input.inputString.
            "Tab", "ArrowUp", "ArrowDown", "ArrowRight", "ArrowLeft", "Escape", "Delete", "Home", "End", "Insert", "PageUp", "PageDown", "Help", "Backspace"
        };
        static readonly string[] _keyValues = new string[] {
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "`", "-", "=", "[", "]", "\\", ";", "'", ",", ".", "/", " ", "Enter"
        }.Concat(_keyValuesUndetectableThroughInputString).ToArray();
        bool _legacyInputManagerDisabled;

        bool _areKeysUndetectableThroughInputStringPressed() {

            foreach (var key in _keyValuesUndetectableThroughInputString) {
                foreach (var keyName in _getPotentialUnityKeyNames(key)) {
                    // Use GetKey instead of GetKeyDown because on macOS, Input.inputString
                    // contains garbage when the arrow keys are held down.
                    if (Input.GetKey(keyName)) {
                        return true;
                    }
                }
            }
            return false;
        }

        void Awake() {

            #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                _legacyInputManagerDisabled = true;
                WebViewLogger.LogWarning("3D WebView's support for automatically detecting input from the native keyboard currently requires Unity's Legacy Input Manager, which is currently disabled for the project. So, automatic detection of input from the native keyboard will be disabled. For details, please see this page: https://support.vuplex.com/articles/keyboard");
            #endif
        }

        KeyModifier _getModifiers() {

            var modifiers = KeyModifier.None;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                modifiers |= KeyModifier.Shift;
            }
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
                modifiers |= KeyModifier.Control;
            }
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) {
                modifiers |= KeyModifier.Alt;
            }
            if (Input.GetKey(KeyCode.LeftWindows) ||
                Input.GetKey(KeyCode.RightWindows)) {
                modifiers |= KeyModifier.Meta;
            }
            // Don't pay attention to the command keys on Windows because Unity has a bug
            // where it falsly reports the command keys are pressed after switching languages
            // with the windows+space shortcut.
            #if !(UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
                if (Input.GetKey(KeyCode.LeftCommand) ||
                    Input.GetKey(KeyCode.RightCommand)) {
                    modifiers |= KeyModifier.Meta;
                }
            #endif

            return modifiers;
        }

        // https://docs.unity3d.com/Manual/class-InputManager.html#:~:text=the%20Input%20Manager.-,Key%20names,-follow%20these%20naming
        // For keys like Alt that have both left and right versions, it's important to include both for Android because
        // GetKeyUp("Alt") doesn't detect the right alt key keyup on Android. The same is true for Control, Meta, and Shift.
        // Memoize the results to prevent array allocation from creating a lot of garbage that will need collected.
        static readonly Func<string, string[]> _getPotentialUnityKeyNames = _memoize<string, string[]>(
            javaScriptKeyValue => {
                switch (javaScriptKeyValue) {
                    case " ":
                        return new [] {"space"};
                    case "Alt":
                        return new [] {"left alt", "right alt"};
                    case "ArrowUp":
                        return new [] {"up"};
                    case "ArrowDown":
                        return new [] {"down"};
                    case "ArrowRight":
                        return new [] {"right"};
                    case "ArrowLeft":
                        return new [] {"left"};
                    case "Control":
                        return new [] {"left ctrl", "right ctrl"};
                    case "Enter":
                        return new [] {"return"};
                    case "Meta":
                        return new [] {"left cmd", "right cmd"};
                    case "PageUp":
                        return new [] {"page up"};
                    case "PageDown":
                        return new [] {"page down"};
                    case "Shift":
                        return new [] {"left shift", "right shift"};
                }
                return new [] {javaScriptKeyValue.ToLowerInvariant()};
            }
        );

        /// <summary>
        /// Returns a memoized version of the given function.
        /// </summary>
        static Func<TArg, TReturn> _memoize<TArg, TReturn>(Func<TArg, TReturn> function) {

            var cache = new Dictionary<TArg, TReturn>();
            return arg => {
                TReturn result;
                if (cache.TryGetValue(arg, out result)) {
                    return result;
                }
                result = function(arg);
                cache.Add(arg, result);
                return result;
            };
        }

        bool _processInputString(KeyModifier modifiers) {

            var keyDownHandler = KeyDownReceived;
            var inputString = Input.inputString;
            // Some versions of Unity 2020.3 for macOS have a bug where Input.inputString includes characters
            // twice (for example: "aa" instead of "a"). This occurs specifically in the Player (not in the Editor)
            // and has been reproduced with 2020.3.39 and 2020.3.41.
            // https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-16427
            #if UNITY_STANDALONE_OSX && UNITY_2020_3
                if (inputString.Length == 2 && inputString[0] == inputString[1]) {
                    inputString = inputString[0].ToString();
                }
            #endif
            foreach (var character in inputString) {
                string characterString;
                switch (character) {
                    case '\b':
                        characterString = "Backspace";
                        break;
                    case '\n':
                    case '\r':
                        characterString = "Enter";
                        break;
                    case (char)0xF728:
                        // 0xF728 = NSDeleteFunctionKey on macOS
                        characterString = "Delete";
                        break;
                    default:
                        characterString = character.ToString();
                        break;
                }
                // For some keyboard layouts like AZERTY (e.g. French), Input.inputString will contain
                // the correct character for a ctr+alt+{} key combination (e.g. ctrl+alt+0 makes Input.inputString equal "@"), but
                // Input.GetKeyUp() won't return true for that key when the key combination is released
                // (e.g. Input.GetKeyUp("@") always returns false). So, as a workaround, we emit
                // the KeyUpReceived event immediately in that scenario instead of adding it to _keysDown.
                var skipGetKeyUpBecauseUnityBug = modifiers != KeyModifier.None && characterString.Length == 1 && !_alphanumericRegex.IsMatch(characterString);
                // We also need to skip calling Input.GetKeyUp() if the character isn't compatible with GetKeyUp(). For example, on
                // Azerty keyboards, the 2 key (without modifiers) triggers "é", which can't be passed to GetKeyUp().
                var skipGetKeyUpBecauseIncompatibleCharacter = !_hasValidUnityKeyName(characterString);
                if (skipGetKeyUpBecauseUnityBug || skipGetKeyUpBecauseIncompatibleCharacter) {
                    if (keyDownHandler != null) {
                        keyDownHandler(this, new KeyboardEventArgs(characterString, KeyModifier.None));
                    }
                    var keyUpHandler = KeyUpReceived;
                    if (keyUpHandler != null) {
                        keyUpHandler(this, new KeyboardEventArgs(characterString, KeyModifier.None));
                    }
                } else {
                    if (keyDownHandler != null) {
                        keyDownHandler(this, new KeyboardEventArgs(characterString, modifiers));
                    }
                    // It's a character that works with Input.GetKeyUp(), so add it to _keysDown.
                    _keysDown.Add(characterString);
                }
            }
            return Input.inputString.Length > 0;
        }

        void _processKeysPressed(KeyModifier modifiers) {

            if (!(Input.anyKeyDown || Input.inputString.Length > 0)) {
                return;
            }
            var nonInputStringKeysDetected = _processKeysUndetectableThroughInputString(modifiers);
            if (nonInputStringKeysDetected) {
                return;
            }
            // Using Input.inputString when possible is preferable since it
            // handles different languages and characters that would be hard
            // to support using Input.GetKeyDown().
            var inputStringKeysDetected = _processInputString(modifiers);
            if (inputStringKeysDetected) {
                return;
            }
            // If we've made it to this point, then only modifier keys by themselves have been pressed.
            _processModifierKeysOnly(modifiers);
        }

        void _processKeysReleased(KeyModifier modifiers) {

            if (_keysDown.Count == 0) {
                return;
            }
            var keysDownCopy = new List<string>(_keysDown);
            foreach (var key in keysDownCopy) {
                bool keyUp = false;
                try {
                    foreach (var keyName in _getPotentialUnityKeyNames(key)) {
                        if (Input.GetKeyUp(keyName)) {
                            keyUp = true;
                            break;
                        }
                    }
                } catch (ArgumentException ex) {
                    // This would only happen if an invalid key is added to _keyValuesUndetectableThroughInputString
                    // because other keys are verified via _hasValidUnityKeyName.
                    WebViewLogger.LogError("Invalid key value passed to Input.GetKeyUp: " + ex);
                    _keysDown.Remove(key);
                    return;
                }
                if (keyUp) {
                    var handler = KeyUpReceived;
                    if (handler != null) {
                        handler(this, new KeyboardEventArgs(key, modifiers));
                    }
                    _keysDown.Remove(key);
                }
            }
        }

        bool _processKeysUndetectableThroughInputString(KeyModifier modifiers) {

            var keyDownHandler = KeyDownReceived;
            var modifierKeysPressed = !(modifiers == KeyModifier.None || modifiers == KeyModifier.Shift);
            var keysUndetectableThroughInputStringArePressed = _areKeysUndetectableThroughInputStringPressed();
            var oneOrMoreKeysProcessed = false;
            // On Windows, when modifier keys are held down, Input.inputString is blank
            // even if other keys are pressed. So, use Input.GetKeyDown() in that scenario.
            if (keysUndetectableThroughInputStringArePressed || (Input.inputString.Length == 0 && modifierKeysPressed)) {
                foreach (var key in _keyValues) {
                    foreach (var keyName in _getPotentialUnityKeyNames(key)) {
                        if (Input.GetKeyDown(keyName)) {
                            if (keyDownHandler != null) {
                                keyDownHandler(this, new KeyboardEventArgs(key, modifiers));
                            }
                            _keysDown.Add(key);
                            oneOrMoreKeysProcessed = true;
                            break;
                        }
                    }
                }
            }
            return oneOrMoreKeysProcessed;
        }

        void _processModifierKeysOnly(KeyModifier modifiers) {

            var keyDownHandler = KeyDownReceived;
            foreach (var value in Enum.GetValues(typeof(KeyModifier))) {
                var modifierValue = (KeyModifier)value;
                if (modifierValue == KeyModifier.None) {
                    continue;
                }
                if ((modifiers & modifierValue) != 0) {
                    var key = modifierValue.ToString();
                    if (keyDownHandler != null) {
                        keyDownHandler(this, new KeyboardEventArgs(key, KeyModifier.None));
                    }
                    _keysDown.Add(key);
                }
            }
        }

        void Update() {

            if (_legacyInputManagerDisabled) {
                return;
            }
            var modifiers = _getModifiers();
            _processKeysPressed(modifiers);
            _processKeysReleased(modifiers);
        }
    }
}
