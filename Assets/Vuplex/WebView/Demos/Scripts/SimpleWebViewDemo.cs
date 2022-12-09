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
using UnityEngine;

namespace Vuplex.WebView.Demos {

    /// <summary>
    /// Provides a simple example of using 3D WebView's scripting APIs.
    /// </summary>
    /// <remarks>
    /// Links: <br/>
    /// - WebViewPrefab docs: https://developer.vuplex.com/webview/WebViewPrefab <br/>
    /// - How clicking works: https://support.vuplex.com/articles/clicking <br/>
    /// - Other examples: https://developer.vuplex.com/webview/overview#examples <br/>
    /// </remarks>
    class SimpleWebViewDemo : MonoBehaviour {

        WebViewPrefab _webViewPrefab;

        void Awake() {

            // Use a desktop User-Agent to request the desktop versions of websites.
            // https://developer.vuplex.com/webview/Web#SetUserAgent
            Web.SetUserAgent(false);
        }

        async void Start() {

            // Get a reference to the WebViewPrefab.
            // https://support.vuplex.com/articles/how-to-reference-a-webview
            _webViewPrefab = GameObject.Find("WebViewPrefab").GetComponent<WebViewPrefab>();

            // Wait for the prefab to initialize because its WebView property is null until then.
            // https://developer.vuplex.com/webview/WebViewPrefab#WaitUntilInitialized
            await _webViewPrefab.WaitUntilInitialized();

            // After the prefab has initialized, you can use the IWebView APIs via its WebView property.
            // https://developer.vuplex.com/webview/IWebView
            _webViewPrefab.WebView.UrlChanged += (sender, eventArgs) => {
                Debug.Log("[SimpleWebViewDemo] URL changed: " + eventArgs.Url);
            };
        }
    }
}
