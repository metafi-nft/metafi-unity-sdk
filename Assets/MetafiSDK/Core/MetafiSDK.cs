using System.Collections;
using UnityEngine;
using UnityEngine.UI;
// using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Dynamic;

namespace Metafi.Unity {
    public sealed class MetafiProvider : MonoBehaviour {
        private static MetafiProvider _instance;
        private static WebViewController _webViewController;
        // Dictionary<string, TaskCompletionSource<dynamic>> promises = new Dictionary<string, TaskCompletionSource<dynamic>>();
        
        public static MetafiProvider Instance {
            get {
                if (_instance == null) {
                    _instance = new MetafiProvider();
                }
                return _instance;
            }
        }

        private MetafiProvider() {
            _webViewController = WebViewController.Instance;
            // _webViewController.SubscribeToWebViewEvents(_handleSDKMessage);
        }

        // void _handleSDKMessage(dynamic eventObj) {
        //     Debug.Log("_handleSDKMessage");
        //     Debug.Log("eventObj received: " + eventObj);
        //     // dynamic eventObj = JsonConvert.DeserializeObject<dynamic>(eventArgs.Value);
            
        //     int statusCode = eventObj.statusCode;
        //     dynamic data = eventObj.data;
        //     string eventType = eventObj.eventType;
        //     dynamic eventMetadata = eventObj.eventMetadata;
        //     string error = eventObj.error;

        //     Debug.Log("statusCode, data, error, eventType, eventMetadata = " +  statusCode + " " + data + " " + error + " " + eventType + " " + eventMetadata);

        //     switch(eventType) {
        //         case "modalStatus":
        //             if (data.open == false) {
        //                 _webViewController.CompressWebview();
        //             } else if (data.open == true) {
        //                 _webViewController.ExpandWebview();
        //             }
        //             break;
        //         case "returnResult":
        //             string strUuid = eventMetadata.uuid.ToString();
        //             if (promises.ContainsKey(strUuid)) {
        //                 promises[strUuid].TrySetResult(data);
        //             }
        //             break;

        //         default:
        //             break;
        //     }
        // }

        public async void Init(
            string _apiKey, 
            string _secretKey, 
            dynamic _options, 
            List<Chain> _supportedChains,
            List<Token> _customTokens) {
            
            Debug.Log("Init");

            await _webViewController.SetupWebView();

            dynamic initParams = new ExpandoObject();
            initParams.apiKey = _apiKey;
            initParams.secretKey = _secretKey;
            initParams.options = new ExpandoObject();

            var _optionsDict = (IDictionary<string,object>)_options;
            
            try {

                byte[] logoBytes = System.IO.File.ReadAllBytes((string)_optionsDict["logo"]);  
                string base64logo = "data:image/png;base64," + System.Convert.ToBase64String(logoBytes);
                initParams.options.logo = base64logo;
            } catch (System.Exception e) {
                Debug.Log("exception while reading image from path, e=" + e.ToString());
            }

            try {
                initParams.options.theme = (dynamic)_optionsDict["theme"];
            } catch (System.Exception e) {
                Debug.Log("exception while appending theme to init params, e=" + e.ToString());
            }

            try {
                List<string> parsedSupportedChains = new List<string>();
                foreach (Chain chain in _supportedChains) {
                    parsedSupportedChains.Add(chain.chainKey);
                }
                initParams.supportedChains = parsedSupportedChains;
            } catch (System.Exception e) {
                Debug.Log("exception while appending supportedChains to init params, e=" + e.ToString());
            }

            try {
                List<dynamic> parsedCustomTokens = new List<dynamic>();
                foreach (Token token in _customTokens) {
                    parsedCustomTokens.Add(new {
                        name = token.name,
                        symbol = token.symbol,
                        chain = token.chain.chainKey,
                        image = token.image,
                        contractAddress = token.contractAddress,
                        decimals = token.decimals
                    });
                }
                //
                initParams.customTokens = parsedCustomTokens;
            } catch (System.Exception e) {
                Debug.Log("exception while appending custom tokens to init params, e=" + e.ToString());
            }

            // _webViewController.InvokeSDK("Init", initParams, "", ((System.Action<dynamic>) (result => {
            //     Debug.Log("Init complete, result: " + result.ToString());
            // })));            
            
            await _webViewController.InvokeSDK("Init", initParams, "");
        }

        // void _invokeSDK(string _type, System.Object _payload, TaskCompletionSource<dynamic> promise = null) {
        //     string _uuid = System.Guid.NewGuid().ToString();
        //     string json = JsonConvert.SerializeObject(new {
        //         type = _type,
        //         props = _payload,
        //         uuid = _uuid
        //     });

        //     Debug.Log("json: " + json);

        //     if (promise != null) {
        //         Debug.Log("Appending promise to dict");
        //         promises[_uuid] = promise;
        //         Debug.Log("Appended = " + promises[_uuid]);
        //     }

        //     _webViewController.PostMessage(json);
        // }

        // public void show(){
        //     Debug.Log("show");
        //     // _webview.GetComponent<Renderer>().enabled = true;
        //     // _expandWebview();
        //     _invokeSDK("method", new {
        //         methodName = "ShowWallet",
        //         methodParams = new string[] {},
        //         methodOutput = ""
        //     });
        // }

        public async void Login(string userIdentifier, string jwtToken, System.Action<dynamic> callback) {
            Debug.Log("Login");

            var loginParams = new [] { userIdentifier, jwtToken };

            await _webViewController.InvokeSDK("Login", loginParams, "callback", callback);  
        }
        
        public void checkout(){
            Debug.Log("checkout");
        }
        
        // public void disconnect(){
        //     Debug.Log("disconnect");
        //     _invokeSDK("method", new {
        //         methodName = "Disconnect",
        //         methodParams = new string[] { },
        //         methodOutput = ""
        //     });
        // }
        
        // public async void retrieveUser(){
        //     Debug.Log("retrieveUser");
        //     var promise = new TaskCompletionSource<dynamic>();
        //     _invokeSDK("method", new {
        //         methodName = "RetrieveUser",
        //         methodParams = new string[] { },
        //         methodOutput = "return"
        //     }, promise);
        //     var result = await promise.Task;
        //     Debug.Log("result of promise is = " + result);
        // }
        
        public void transferTokens(){
            Debug.Log("transferTokens");
        }
    }
}
