using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using System.IO;

namespace Metafi.Unity {
    public sealed class MetafiProvider : MonoBehaviour {
        private static MetafiProvider _instance;
        private WebViewController _webViewController;
        private static bool isInitialized = false;
        private static int instanceID;
        private static string logFileName;

        public static MetafiProvider Instance {
            get {                
                if (_instance == null) {
                    _instance = GameObject.FindWithTag("MetafiSDK").AddComponent<MetafiProvider>();

                    DontDestroyOnLoad(_instance);

                    instanceID = _instance.gameObject.GetInstanceID();
                }
                return _instance;
            }
        }

        private MetafiProvider() {
            _webViewController = new WebViewController();
        }

        public void Awake() {
            logFileName = Application.persistentDataPath + "/logs.txt";
            // Debug.Log("MetafiProvider Awake, instanceID = " + this.gameObject.GetInstanceID());
            if (MetafiProvider.instanceID != 0 && this.gameObject.GetInstanceID() != instanceID) {
                // Debug.Log("destroying duplicate instance with ID = " + this.gameObject.GetInstanceID());
                Destroy(this.gameObject);
            }
        }
    
        void OnEnable() {
            Application.logMessageReceived += Log;  
        }

        void OnDisable() {
            Application.logMessageReceived -= Log; 
        }

        public void Log(string logString, string stackTrace, LogType type)
        {
            if (logFileName == "")
            {
                string d = System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.Desktop) + "/YOUR_LOGS";
                System.IO.Directory.CreateDirectory(d);
                logFileName = d + "/my_happy_log.txt";
            }
    
            try {
                System.IO.File.AppendAllText(logFileName, logString + "\n----------------\n");
            }
            catch { }
        }

        public async Task Init(
            string _apiKey, 
            string _secretKey, 
            dynamic _options, 
            List<Chain> _supportedChains,
            List<Token> _customTokens,
            bool _metafiSSO = false) {
                        
            if (isInitialized) {
                // Debug.Log("MetafiProvider already initialized, skipping");
                return;
            }

            Debug.Log("[Metafi SDK] Init");

            await _webViewController.SetupWebView(this.gameObject.transform.GetChild(0).gameObject);

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
                Debug.LogError("[Metafi SDK] exception while reading image from path, e=" + e.ToString());
            }

            try {
                initParams.options.theme = (dynamic)_optionsDict["theme"];
            } catch (System.Exception e) {
                Debug.LogError("[Metafi SDK] exception while appending theme to init params, e=" + e.ToString());
            }

            try {
                List<string> parsedSupportedChains = new List<string>();
                foreach (Chain chain in _supportedChains) {
                    parsedSupportedChains.Add(chain.chainKey);
                }
                initParams.supportedChains = parsedSupportedChains;
            } catch (System.Exception e) {
                Debug.LogError("[Metafi SDK] exception while appending supportedChains to init params, e=" + e.ToString());
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
                Debug.LogError("[Metafi SDK] exception while appending custom tokens to init params, e=" + e.ToString());
            }         
            
            await _webViewController.InvokeSDK("Init", initParams, "");
            isInitialized = true;
        }

        public async Task Login(string userIdentifier, string jwtToken, System.Action<dynamic> callback = null) {
            Debug.Log("[Metafi SDK] Login");

            var loginParams = new [] { userIdentifier, jwtToken };
            await _webViewController.InvokeSDK("Login", loginParams, "callback", false, callback);  
        }

        public async Task ShowWallet(){
            Debug.Log("[Metafi SDK] ShowWallet");

            await _webViewController.InvokeSDK("ShowWallet", new string[] {}, "");
        }

        public async Task HideWallet(){
            Debug.Log("[Metafi SDK] HideWallet");

            await _webViewController.InvokeSDK("HideWallet", new string[] {}, "");
        }


        public async Task TransferTokens(dynamic args, System.Action<dynamic> callback = null) {
            Debug.Log("[Metafi SDK] TransferTokens");

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
            Debug.Log("[Metafi SDK] Checkout");

            dynamic chkParams = new ExpandoObject();
            
            try {
                chkParams.cost = args.cost;
                chkParams.currency = args.currency.assetKey;
                chkParams.itemDescription = args.itemDescription;
                chkParams.treasuryAddress = args.treasuryAddress;
                chkParams.webhookUrl = args.webhookUrl;
            } catch (System.Exception e) {
                Debug.LogError("[Metafi SDK] incorrect arguments for checkout, please refer to the documentation, exception = " + e.ToString());
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
            Debug.Log("[Metafi SDK] CallGenericReadFunction");
            
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

            } catch (System.Exception e) {
                Debug.LogError("[Metafi SDK] incorrect arguments for CallGenericReadFunction, please refer to the documentation, exception = " + e.ToString());
                return new {};
            }
            
            dynamic res = new {};
            await _webViewController.InvokeSDK("CallGenericReadFunction", cgrParams, "return", true, ((System.Action<dynamic>) (result => {
                res = result;
            })));
            
            return res;
        }

        public async Task CallGenericWriteFunction(dynamic args, System.Action<dynamic> callback = null) {
            Debug.Log("[Metafi SDK] CallGenericWriteFunction");
                        
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
                cgwParams.value = args.value;

            } catch (System.Exception e) {
                Debug.LogError("[Metafi SDK] incorrect arguments for CallGenericWriteFunction, please refer to the documentation, exception = " + e.ToString());
                return;
            }
            
            await _webViewController.InvokeSDK("CallGenericWriteFunction", cgwParams, "callback", true, callback);            
        }

        public async Task CallGaslessFunction(dynamic args, System.Action<dynamic> callback = null) {
            Debug.Log("[Metafi SDK] CallGaslessFunction");
                        
            dynamic cgsParams = new ExpandoObject();
            
            try {
                using (StreamReader r = new StreamReader(args.functionABIPath)) {
                    string json = r.ReadToEnd();
                    cgsParams.functionABI = JsonConvert.DeserializeObject<dynamic>(json);
                }
                cgsParams.contractAddress = args.contractAddress;
                cgsParams.functionName = args.functionName;
                cgsParams.chain = args.chain.chainKey;
                cgsParams.@params = args.@params;
                cgsParams.value = args.value;
                
            } catch (System.Exception e) {
                Debug.LogError("[Metafi SDK] incorrect arguments for CallGaslessFunction, please refer to the documentation, exception = " + e.ToString());
                return;
            }
            
            await _webViewController.InvokeSDK("CallGaslessFunction", cgsParams, "callback", true, callback);            
        }
        
        public async Task Disconnect(){
            Debug.Log("[Metafi SDK] Disconnect");
            
            await _webViewController.InvokeSDK("Disconnect", new string[] {}, "");
        }
    }
}
