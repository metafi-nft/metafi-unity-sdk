using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace Metafi.Unity {
    public class SDK : MonoBehaviour {
        
        private static readonly HttpClient client = new HttpClient();

        GameObject _webview;

        Vuplex.WebView.CanvasWebViewPrefab _canvasWebViewPrefab;

        Dictionary<string, TaskCompletionSource<dynamic>> promises = new Dictionary<string, TaskCompletionSource<dynamic>>();

        async void Start() {
            Debug.Log("Setting _canvasWebViewPrefab");
            _compressWebview();
            _webview = GameObject.Find("CanvasWebViewPrefab");
            _canvasWebViewPrefab = _webview.GetComponent<Vuplex.WebView.CanvasWebViewPrefab>();
            await _canvasWebViewPrefab.WaitUntilInitialized();
            await _canvasWebViewPrefab.WebView.WaitForNextPageLoadToFinish();
            _initializeProvider();
            _canvasWebViewPrefab.WebView.MessageEmitted += _handleWebViewMessageEmitted;
        }

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

            switch(eventType) {
                case "modalStatus":
                    if (data.open == false) {
                        _compressWebview();
                    } else if (data.open == true) {
                        _expandWebview();
                    }
                    break;
                case "returnResult":
                    string strUuid = eventMetadata.uuid.ToString();
                    if (promises.ContainsKey(strUuid)) {
                        promises[strUuid].TrySetResult(data);
                    }
                    break;

                default:
                    break;
            }
        }

        void _expandWebview() {
            Debug.Log("_expandWebview");
            _webview.transform.localScale = new Vector3(1, 1, 1);
        }

        void _compressWebview() {
            Debug.Log("_compressWebview");
            _webview.transform.localScale = new Vector3(0, 0, 0);
        }

        void _initializeProvider() {
            Debug.Log("_initializeProvider");

            string base64logo = Resources.Load<TextAsset>("logo").text;
            Debug.Log("image data in base64=" + base64logo);

            _invokeSDK("initialise", new {
                apiKey = "test-6371f967cea553bdd5ae3d5e-mrQsJvLklpohUWuN",
                secretKey = "KkEWQkndudnJmipaSpIfD1rO",
                supportedChains = new[] {"eth", "goerli", "mumbai", "matic"},
                options = new {
                    logo = base64logo,
                    theme = new {}
                },
            });
        }

        void _invokeSDK(string _type, System.Object _payload, TaskCompletionSource<dynamic> promise = null) {
            string _uuid = System.Guid.NewGuid().ToString();
            string json = JsonConvert.SerializeObject(new {
                type = _type,
                props = _payload,
                uuid = _uuid
            });

            Debug.Log("json: " + json);

            if (promise != null) {
                Debug.Log("Appending promise to dict");
                promises[_uuid] = promise;
                Debug.Log("Appended = " + promises[_uuid]);
            }

            _canvasWebViewPrefab.WebView.PostMessage(json);
            // _canvasWebViewPrefab.WebView.PostMessage("hiii");
        }

        public void show(){
            Debug.Log("show");
            // _webview.GetComponent<Renderer>().enabled = true;
            // _expandWebview();
            _invokeSDK("method", new {
                methodName = "ShowWallet",
                methodParams = new string[] {},
                methodOutput = ""
            });
        }

        public async void login(){
            Debug.Log("login");

            var response = await client.PostAsync("https://vpa58nk2e9.execute-api.us-east-1.amazonaws.com/dev/testMerchantAuthenticate", null);
            var responseString = await response.Content.ReadAsStringAsync();
            
            dynamic responseObj = JsonConvert.DeserializeObject<dynamic>(responseString);

            string userIdentifier = responseObj.userIdentifier;
            string jwtToken = responseObj.jwtToken;

            Debug.Log("response from login: " + userIdentifier + " " + jwtToken);

            _invokeSDK("method", new {
                methodName = "Login",
                methodParams = new [] { userIdentifier, jwtToken },
                methodOutput = "callback"
            });
        }
        
        public void checkout(){
            Debug.Log("checkout");
        }
        
        public void disconnect(){
            Debug.Log("disconnect");
            _invokeSDK("method", new {
                methodName = "Disconnect",
                methodParams = new string[] { },
                methodOutput = ""
            });
        }
        
        public async void retrieveUser(){
            Debug.Log("retrieveUser");
            var promise = new TaskCompletionSource<dynamic>();
            _invokeSDK("method", new {
                methodName = "RetrieveUser",
                methodParams = new string[] { },
                methodOutput = "return"
            }, promise);
            var result = await promise.Task;
            Debug.Log("result of promise is = " + result);
        }
        
        public void transferTokens(){
            Debug.Log("transferTokens");
        }
    }
}
