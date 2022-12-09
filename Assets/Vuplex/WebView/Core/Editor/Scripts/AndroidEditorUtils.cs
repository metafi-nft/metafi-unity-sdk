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
#if UNITY_ANDROID
#pragma warning disable CS0618
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView.Editor {

    public static class AndroidEditorUtils {

        public static void PreprocessBuild(string productName, string proguardRulesExpectedRelativePath, string nativeLibraryName, bool native2DSupported) {

            _validateGraphicsApi(native2DSupported);
            _forceInternetPermission();
            _assertThatOculusLowOverheadModeIsDisabled();
            _assertThatSrpBatcherIsDisabled();
            _setNativePluginsToPreloaded(nativeLibraryName);
            _updateProguardFileIfNeeded(productName, proguardRulesExpectedRelativePath);
        }

        /// <summary>
        /// Updates the proguard-user.txt file if needed to prevent the names of 3D WebView's Java classes from being obfuscated.
        /// https://support.vuplex.com/articles/android-minification
        /// </summary>
        static void _updateProguardFileIfNeeded(string productName, string neededProguardRulesExpectedRelativePath) {

            var minificationEnabled = false;
            // minifyDebug and minifyRelease were added in 2020.1.
            #if UNITY_2020_1_OR_NEWER
                minificationEnabled = EditorUserBuildSettings.development ? PlayerSettings.Android.minifyDebug : PlayerSettings.Android.minifyRelease;
            #endif
            if (!minificationEnabled) {
                return;
            }
            var neededProguardRulesFilePath = EditorUtils.FindFile(Path.Combine(Application.dataPath, neededProguardRulesExpectedRelativePath));
            var neededProguardRules = File.ReadAllText(neededProguardRulesFilePath);
            // Note: the prefix and suffix avoid using characters considered special characters in regexes, like parentheses.
            var prefix = $"# --- Start of section automatically included for {productName} - PLEASE DO NOT EDIT ---";
            var suffix = $"# --- End of section for {productName} ---";
            var fullTextToAdd = $"{prefix}\n{neededProguardRules}\n{suffix}";
            var androidPluginsFolderPath = Path.Combine(Application.dataPath, "Plugins", "Android");
            var proguardUserFilePath = Path.Combine(androidPluginsFolderPath, "proguard-user.txt");
            var proguardUserFileExists = File.Exists(proguardUserFilePath);
            if (proguardUserFileExists) {
                var existingFileText = File.ReadAllText(proguardUserFilePath);
                var existingWebViewProguardRulesRegex = new Regex($"{prefix}.*{suffix}", RegexOptions.Singleline);
                var existingWebViewProguardRulesResult = existingWebViewProguardRulesRegex.Match(existingFileText);
                if (existingWebViewProguardRulesResult.Success) {
                    if (existingWebViewProguardRulesResult.Value != fullTextToAdd) {
                        // The proguard-user.txt file contains an older version of the rules, so update them.
                        var newFileText = existingFileText.Replace(existingWebViewProguardRulesResult.Value, fullTextToAdd);
                        File.WriteAllText(proguardUserFilePath, newFileText);
                    }
                } else {
                    // A proguard-user.txt file exists, but it doesn't include the rules yet, so add them.
                    var newFileText = existingFileText + "\n\n" + fullTextToAdd;
                    File.WriteAllText(proguardUserFilePath, newFileText);
                }
            } else {
                // No proguard-user.txt file exists yet, so create the file.
                Directory.CreateDirectory(androidPluginsFolderPath);
                File.WriteAllText(proguardUserFilePath, fullTextToAdd);
            }
        }

        static void _assertThatSrpBatcherIsDisabled() {

            #if UNITY_2018_2_OR_NEWER && !VUPLEX_DISABLE_SRP_WARNING
                // Checking renderPipelineAsset is needed to verify that URP is enabled because useScriptableRenderPipelineBatching
                // can sometimes be true even when the built-in render pipeline is in use.
                if (GraphicsSettings.useScriptableRenderPipelineBatching && GraphicsSettings.renderPipelineAsset != null) {
                    throw new BuildFailedException("URP settings error: \"SRP Batcher\" is enabled in Universal Render Pipeline (URP) settings, but URP for Android has an issue that prevents 3D WebView's textures from showing up outside of a Canvas. If the project uses a WebViewPrefab, please go to \"UniversalRenderPipelineAsset\" -> \"Advanced\" and disable SRP Batcher. If the project only uses CanvasWebViewPrefab and not WebViewPrefab, you can instead add the scripting symbol VUPLEX_DISABLE_SRP_WARNING to the project to ignore this warning.");
                }
            #endif
        }

        static void _assertThatOculusLowOverheadModeIsDisabled() {

            if (!EditorUtils.XRSdkIsEnabled("oculus")) {
                return;
            }
            var lowOverheadModeEnabled = false;
            #if VUPLEX_OCULUS
                // The Oculus XR plugin is installed
                Unity.XR.Oculus.OculusLoader oculusLoader = Unity.XR.Oculus.OculusSettings.CreateInstance<Unity.XR.Oculus.OculusLoader>();
                Unity.XR.Oculus.OculusSettings oculusSettings = oculusLoader.GetSettings();
                lowOverheadModeEnabled = oculusSettings.LowOverheadMode;
            #elif UNITY_2019_2_OR_NEWER && !UNITY_2020_1_OR_NEWER
                // VROculus.lowOverheadMode is only supported from Unity 2019.2 - 2019.4
                lowOverheadModeEnabled = PlayerSettings.VROculus.lowOverheadMode;
            #endif
            if (lowOverheadModeEnabled) {
                throw new BuildFailedException("XR settings error: Vuplex 3D WebView requires that \"Low Overhead Mode\" be disabled in Oculus XR settings. Please disable Low Overhead Mode in Oculus XR settings.");
            }
        }

        static void _forceInternetPermission() {

            #if !VUPLEX_ANDROID_DISABLE_REQUIRE_INTERNET
                if (!PlayerSettings.Android.forceInternetPermission) {
                    PlayerSettings.Android.forceInternetPermission = true;
                    WebViewLogger.LogWarning("Just a heads-up: 3D WebView changed the Android player setting \"Internet Access\" to \"Require\" to ensure that it can fetch web pages from the internet. (This message will only be logged once.)");
                }
            #endif
        }

        /// <summary>
        /// Sets the libVuplexWebViewAndroid.so or libVuplexWebViewAndroidGecko.so plugin files to be preloaded, which is equivalent to
        /// enabling their "Load on Startup" checkbox. This is done via a script because the .meta files
        /// for these plugins was generated with an older version of Unity in order to be compatible with
        /// 2018.4, which doesn't support the preload option. Enabling preloading is required for Vulkan support.
        /// </summary>
        static void _setNativePluginsToPreloaded(string fileName) {

            #if UNITY_2019_1_OR_NEWER
                var pluginAbsolutePaths = Directory.GetFiles(Application.dataPath, fileName, SearchOption.AllDirectories).ToList();
                // PluginImporter.GetAtPath() requires a relative path and doesn't support absolute paths.
                var pluginRelativePaths = pluginAbsolutePaths.Select(path => path.Replace(Application.dataPath, "Assets"));
                foreach (var filePath in pluginRelativePaths) {
                    var pluginImporter = (PluginImporter)PluginImporter.GetAtPath(filePath);
                    if (!pluginImporter.isPreloaded) {
                        pluginImporter.isPreloaded = true;
                        pluginImporter.SaveAndReimport();
                    }
                }
            #endif
        }

        static void _validateGraphicsApi(bool native2DSupported) {

            #if !VUPLEX_DISABLE_GRAPHICS_API_WARNING
                var autoGraphicsApiEnabled = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
                var selectedGraphicsApi = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)[0];
                var vulkanEnabled = selectedGraphicsApi == GraphicsDeviceType.Vulkan;
                if (!(vulkanEnabled || autoGraphicsApiEnabled)) {
                    // OpenGLES is selected, so nothing to warn about.
                    return;
                }
                var warningPrefix = autoGraphicsApiEnabled ? "Auto Graphics API is enabled in Player Settings, which means that the Vulkan Graphics API may be used."
                                                           : "The Vulkan Graphics API is enabled in Player Settings.";
                #if UNITY_2020_2_OR_NEWER
                    // At build time, XRSettings.enabled is always false, so to check if XR is enabled,
                    // we must instead check whether XRSettings.supportedDevices[0] != "None".
                    var xrDevices = VXUtils.XRSettings.supportedDevices;
                    var xrIsEnabled = xrDevices.Length > 0 && xrDevices[0] != "None";
                    if (!xrIsEnabled) {
                        WebViewLogger.LogWarning($"{warningPrefix} 3D WebView for Android supports Vulkan, but{(native2DSupported ? " unless the application only uses webviews in Native 2D Mode, then" : "")} the target Android devices must support the Vulkan extension VK_ANDROID_external_memory_android_hardware_buffer. That extension is supported on newer devices like Oculus Quest but isn't supported on all Android phones that support Vulkan. If your application is intended for general Android phones, it's recommended to{(native2DSupported ? " either only use Native 2D Mode or to" : "")} change the project's selected Graphics API to OpenGLES in Player Settings.{(native2DSupported ? " If your application is already only using Native 2D Mode, then please ignore this message." : "")} For more details, see this page: <em>https://support.vuplex.com/articles/vulkan#android</em>");
                    }
                #else
                    throw new BuildFailedException(warningPrefix + " 3D WebView for Android requires Unity 2020.2 or newer in order to support Vulkan. So, please either upgrade to a newer version of Unity or change the selected Graphics API to OpenGLES in Player Settings.");
                #endif
            #endif
        }
    }
}
#endif
