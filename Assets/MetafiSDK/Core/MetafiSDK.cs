using System.Collections;
using UnityEngine;
using UnityEngine.UI;
// using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace Metafi.Unity {
    public sealed class MetafiProvider : MonoBehaviour {
        private static MetafiProvider _instance;
        private static WebViewController _webViewController;

        public static MetafiProvider Instance {
            get {
                if (_instance == null) {
                    _instance = (MetafiProvider) new GameObject("MetafiSDKPrefabDesktopNative").AddComponent<MetafiProvider>();
                    DontDestroyOnLoad(_instance.gameObject);
                    
                    // _instance = new MetafiProvider();
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
            
            await _webViewController.InvokeSDK("Init", initParams, "");
        }

        public async Task Login(string userIdentifier, string jwtToken, System.Action<dynamic> callback) {
            Debug.Log("Login");

            var loginParams = new [] { userIdentifier, jwtToken };
            await _webViewController.InvokeSDK("Login", loginParams, "callback", callback);  
        }

        public async Task Show(){
            Debug.Log("Show");

            await _webViewController.InvokeSDK("ShowWallet", new string[] {}, "");
        }
        
        // public async Task Checkout(){
        //     Debug.Log("Checkout");
        // }
        
        public async Task Disconnect(){
            Debug.Log("Disconnect");

            await _webViewController.InvokeSDK("Disconnect", new string[] {}, "");
        }
        
        public async Task<dynamic> RetrieveUser(){
            Debug.Log("RetrieveUser");
            
            dynamic res = new {};
            await _webViewController.InvokeSDK("RetrieveUser", new string[] { }, "return", ((System.Action<dynamic>) (result => {
                res = result;
            })));

            return res;
        }
        
        public void transferTokens(){
            Debug.Log("transferTokens");
        }
    }
}
