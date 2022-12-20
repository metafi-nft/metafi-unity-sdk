using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Metafi.Unity {
    public sealed class WebViewController : MonoBehaviour {
        
        private static GameObject _webview;
        private static Vuplex.WebView.CanvasWebViewPrefab _canvasWebViewPrefab;
        private static WebViewController _instance;
        // public event System.Action<dynamic> WebViewActionHandler;
        private static Dictionary<string, TaskCompletionSource<dynamic>> promises = new Dictionary<string, TaskCompletionSource<dynamic>>();
        
        public static WebViewController Instance {
            get {
                if (_instance == null) {
                    _instance = new WebViewController();
                }
                return _instance;
            }
        }

        private WebViewController() {}

        public async Task SetupWebView() {
            Debug.Log("SetupWebView");
            _webview = GameObject.Find("CanvasWebViewPrefab");
            CompressWebview();
            _canvasWebViewPrefab = _webview.GetComponent<Vuplex.WebView.CanvasWebViewPrefab>();
            await _canvasWebViewPrefab.WaitUntilInitialized();
            await _canvasWebViewPrefab.WebView.WaitForNextPageLoadToFinish();
            _canvasWebViewPrefab.WebView.MessageEmitted += _handleWebViewMessageEmitted;
            Debug.Log("complete _setupWebView");
        }

        // void _handleWebViewMessageEmitted(object sender, Vuplex.WebView.EventArgs<string> eventArgs) {
        //     Debug.Log("_handleWebViewMessageEmitted");
        //     Debug.Log("JSON received: " + sender + ", " + eventArgs.Value);
            
        //     dynamic eventObj = JsonConvert.DeserializeObject<dynamic>(eventArgs.Value);
        //     WebViewActionHandler?.Invoke(eventObj);
        // }

        void _handleWebViewMessageEmitted(object sender, Vuplex.WebView.EventArgs<string> eventArgs) {
            Debug.Log("_handleWebViewMessageEmitted");
            Debug.Log("JSON received: " + sender + ", " + eventArgs.Value);
            
            dynamic eventObj = JsonConvert.DeserializeObject<dynamic>(eventArgs.Value);
            
            int statusCode = eventObj.statusCode;
            dynamic data = eventObj.data;
            string eventType = eventObj.eventType;
            dynamic eventMetadata = eventObj.eventMetadata;
            string error = eventObj.error;

            Debug.Log("statusCode, data, error, eventType, eventMetadata = " +  statusCode + " " + data + " " + error + " " + eventType + " " + eventMetadata);

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
                            statusCode = statusCode,
                            data = data,
                            error = error,
                        });
                    }
                    break;
                
                case "callbackResult":
                    strUuid = eventMetadata.uuid.ToString();
                    if (promises.ContainsKey(strUuid)) {
                        promises[strUuid].TrySetResult(new {
                            statusCode = statusCode,
                            data = data,
                            error = error,
                        });
                    }
                    break;

                default:
                    break;
            }
        }

        // public void SubscribeToWebViewEvents(System.Action<dynamic> handlerFunc) {
        //     WebViewActionHandler += handlerFunc;
        // }

        public void ExpandWebview() {
            Debug.Log("_expandWebview");
            _webview.transform.localScale = new Vector3(1, 1, 1);
        }

        public void CompressWebview() {
            Debug.Log("_compressWebview");
            _webview.transform.localScale = new Vector3(0, 0, 0);
        }

        public async Task InvokeSDK(string methodName, dynamic methodParams, string methodOutput, System.Action<dynamic> onComplete = null) {
            // string _type, System.Object _payload, TaskCompletionSource<dynamic> promise = null) {
            string _uuid = System.Guid.NewGuid().ToString();
            
            dynamic _payload = new {
                methodName = methodName,
                methodParams = methodParams,
                methodOutput = methodOutput,
            };

            string json = JsonConvert.SerializeObject(new {
                type = "MetafiSDKInvoke",
                props = _payload,
                uuid = _uuid
            });

            Debug.Log("json: " + json);

            var promise = new TaskCompletionSource<dynamic>();
            if (methodOutput == "callback" || methodOutput == "result") {
                promises.Add(_uuid, promise);
            }
            
            _canvasWebViewPrefab.WebView.PostMessage(json);
            
            if (methodOutput == "callback" || methodOutput == "result") {
                dynamic result = await promise.Task;
                Debug.Log("result of promise is = " + result);
                promises.Remove(_uuid);
                onComplete?.Invoke(result);
            }

            return;
        }
    }
}