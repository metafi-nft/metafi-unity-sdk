using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using System.IO;

namespace Metafi.Unity {
    public sealed class MetafiProvider : MonoBehaviour {
        private static MetafiProvider _instance;
        private static WebViewController _webViewController;

        public static MetafiProvider Instance {
            get {
                if (_instance == null) {
                    _instance = (MetafiProvider) new GameObject("MetafiSDKPrefabDesktopNative").AddComponent<MetafiProvider>();
                    DontDestroyOnLoad(_instance.gameObject);                    
                }
                return _instance;
            }
        }

        private MetafiProvider() {
            _webViewController = WebViewController.Instance;
        }

        public async Task Init(
            string _apiKey, 
            string _secretKey, 
            dynamic _options, 
            List<Chain> _supportedChains,
            List<Token> _customTokens,
            bool _metafiSSO = false) {
            
            Debug.Log("Init");

            await _webViewController.SetupWebView();

            dynamic initParams = new ExpandoObject();
            initParams.apiKey = _apiKey;
            initParams.secretKey = _secretKey;
            initParams.options = new ExpandoObject();
            initParams.metafiSSO = _metafiSSO;

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
            
            await _webViewController.InvokeSDK("Init", initParams, "");
        }

        public async Task Login(string userIdentifier, string jwtToken, System.Action<dynamic> callback = null) {
            Debug.Log("Login");

            var loginParams = new [] { userIdentifier, jwtToken };
            await _webViewController.InvokeSDK("Login", loginParams, "callback", false, callback);  
        }

        public async Task ShowWallet(){
            Debug.Log("ShowWallet");

            await _webViewController.InvokeSDK("ShowWallet", new string[] {}, "");
        }

        public async Task TransferTokens(dynamic args, System.Action<dynamic> callback = null) {
            Debug.Log("TransferTokens");

            dynamic ttParams = new ExpandoObject();

            try {
                ttParams.to = args.to;
            } catch (System.Exception) {
                ttParams.to = "";
            }

            try {
                ttParams.amount = args.amount;
            } catch (System.Exception) {
                ttParams.amount = "";
            }

            try {
                ttParams.currency = args.currency.assetKey;
            } catch (System.Exception) {
                ttParams.currency = "";
            }

            await _webViewController.InvokeSDK("TransferTokens", ttParams, "callback", true, callback);
        }
        
        public async Task Checkout(dynamic args, System.Action<dynamic> callback = null){
            Debug.Log("Checkout");

            dynamic chkParams = new ExpandoObject();
            
            try {
                chkParams.cost = args.cost;
                chkParams.currency = args.currency.assetKey;
                chkParams.itemDescription = args.itemDescription;
                chkParams.treasuryAddress = args.treasuryAddress;
                chkParams.webhookUrl = args.webhookUrl;
            } catch (System.Exception) {
                Debug.LogWarning("incorrect arguments for checkout, please refer to the documentation");
                return;
            }

            try {
                chkParams.webhookMetadata = args.webhookMetadata;
            } catch (System.Exception) {
                chkParams.webhookMetadata = "";
            }

            await _webViewController.InvokeSDK("Checkout", chkParams, "callback", true, callback);
        }
        
        public async Task<dynamic> RetrieveUser(){
            Debug.Log("RetrieveUser");
            
            dynamic res = new {};
            await _webViewController.InvokeSDK("RetrieveUser", new string[] { }, "return", false, ((System.Action<dynamic>) (result => {
                res = result;
            })));

            return res;
        }

        public async Task<dynamic> CallGenericReadFunction(dynamic args) {
            Debug.Log("CallGenericReadFunction");
            
            dynamic cgrParams = new ExpandoObject();
            
            try {
                using (StreamReader r = new StreamReader(args.functionABIPath)) {
                    string json = r.ReadToEnd();
                    cgrParams.functionABI = JsonConvert.DeserializeObject<dynamic>(json);
                }
                cgrParams.contractAddress = args.contractAddress;
                cgrParams.functionName = args.functionName;
                cgrParams.chain = args.chain.chainKey;
                cgrParams.@params = args.@params;

            } catch (System.Exception) {
                Debug.LogWarning("incorrect arguments for CallGenericReadFunction, please refer to the documentation");
                return new {};
            }
            
            dynamic res = new {};
            await _webViewController.InvokeSDK("CallGenericReadFunction", cgrParams, "return", true, ((System.Action<dynamic>) (result => {
                res = result;
            })));
            
            return res;
        }

        public async Task CallGenericWriteFunction(dynamic args, System.Action<dynamic> callback = null) {
            Debug.Log("CallGenericWriteFunction");
                        
            dynamic cgwParams = new ExpandoObject();
            
            try {
                using (StreamReader r = new StreamReader(args.functionABIPath)) {
                    string json = r.ReadToEnd();
                    cgwParams.functionABI = JsonConvert.DeserializeObject<dynamic>(json);
                }
                cgwParams.contractAddress = args.contractAddress;
                cgwParams.functionName = args.functionName;
                cgwParams.chain = args.chain.chainKey;
                cgwParams.@params = args.@params;

            } catch (System.Exception) {
                Debug.LogWarning("incorrect arguments for CallGenericWriteFunction, please refer to the documentation");
                return;
            }
            
            await _webViewController.InvokeSDK("CallGenericWriteFunction", cgwParams, "callback", true, callback);            
        }
        
        public async Task Disconnect(){
            Debug.Log("Disconnect");
            
            await _webViewController.InvokeSDK("Disconnect", new string[] {}, "");
        }
    }
}
