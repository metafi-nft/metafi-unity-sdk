using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Metafi.Unity {
    public class WebViewController {
        private GameObject _webview;
        private Vuplex.WebView.CanvasWebViewPrefab _canvasWebViewPrefab;
        private WebViewController _instance;
        private Dictionary<string, TaskCompletionSource<dynamic>> promises = new Dictionary<string, TaskCompletionSource<dynamic>>();

        public WebViewController() {}

        public async Task SetupWebView(GameObject webview) {
            // Debug.Log("SetupWebView");
            _webview = webview;
            _canvasWebViewPrefab = _webview.GetComponent<Vuplex.WebView.CanvasWebViewPrefab>();

            await _canvasWebViewPrefab.WaitUntilInitialized();
            
            CompressWebview();
            await _canvasWebViewPrefab.WebView.WaitForNextPageLoadToFinish();
            _canvasWebViewPrefab.WebView.MessageEmitted += _handleWebViewMessageEmitted;

            // Debug.Log("complete _setupWebView");
        }

        void _handleWebViewMessageEmitted(object sender, Vuplex.WebView.EventArgs<string> eventArgs) {
            // Debug.Log("_handleWebViewMessageEmitted");
            // Debug.Log("JSON received: " + sender + ", " + eventArgs.Value);
            
            dynamic eventObj = JsonConvert.DeserializeObject<dynamic>(eventArgs.Value);
            
            dynamic data = eventObj.data;
            string eventType = eventObj.eventType;
            dynamic eventMetadata = eventObj.eventMetadata;
            string error = eventObj.error;

            // Debug.Log("data, error, eventType, eventMetadata = " + data + " " + error + " " + eventType + " " + eventMetadata);

            string strUuid;
            switch(eventType) {
                case "modalStatus":
                    if (data.open == false) {
                        CompressWebview();
                    } else if (data.open == true) {
                        ExpandWebview();
                    }
                    break;
                case "returnResult":
                    strUuid = eventMetadata.uuid.ToString();
                    if (promises.ContainsKey(strUuid)) {
                        promises[strUuid].TrySetResult(new {
                            // statusCode = statusCode,
                            data = data,
                            error = error,
                        });
                    }
                    break;
                
                case "callbackResult":
                    strUuid = eventMetadata.uuid.ToString();
                    if (promises.ContainsKey(strUuid)) {
                        promises[strUuid].TrySetResult(new {
                            // statusCode = statusCode,
                            data = data,
                            error = error,
                        });
                    }
                    break;

                default:
                    break;
            }
        }

        public void ExpandWebview() {
            // Debug.Log("ExpandWebview");
            _webview.transform.localScale = new Vector3(1, 1, 1);
        }

        public void CompressWebview() {
            // Debug.Log("CompressWebview");
            _webview.transform.localScale = new Vector3(0, 0, 0);
        }

        public async Task InvokeSDK(string methodName, dynamic methodParams, string methodOutput, bool isArgsObject = false, System.Action<dynamic> onComplete = null) {
            // Debug.Log("InvokeSDK");

            string _uuid = System.Guid.NewGuid().ToString();
            
            dynamic _payload = new {
                methodName = methodName,
                methodParams = methodParams,
                methodOutput = methodOutput,
                isArgsObject = isArgsObject,
            };

            string json = JsonConvert.SerializeObject(new {
                type = "MetafiSDKInvoke",
                props = _payload,
                uuid = _uuid
            });

            // Debug.Log("json: " + json);

            var promise = new TaskCompletionSource<dynamic>();
            if (methodOutput == "callback" || methodOutput == "return") {
                promises.Add(_uuid, promise);
            }

            // Debug.Log("_canvasWebViewPrefab" + _canvasWebViewPrefab);
            
            _canvasWebViewPrefab.WebView.PostMessage(json);
            
            if (methodOutput == "callback" || methodOutput == "return") {
                dynamic result = await promise.Task;
                promises.Remove(_uuid);
                onComplete?.Invoke(result);
            }

            // Debug.Log("InvokeSDK complete");

            return;
        }
    }
}