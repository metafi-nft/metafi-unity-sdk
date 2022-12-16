using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

public class ButtonBehaviour : MonoBehaviour
{
    private static readonly HttpClient client = new HttpClient();

    public Button hideButton;
    public Button showButton;
    public Button loginButton;
    public Button checkoutButton;
    public Button disconnectButton;
    public Button retrieveUserButton;
    public Button transferTokensButton;

    GameObject _webview;
    Vuplex.WebView.CanvasWebViewPrefab _canvasWebViewPrefab;
    Dictionary<string, TaskCompletionSource<dynamic>> promises = new Dictionary<string, TaskCompletionSource<dynamic>>();


    void OnEnable() {
        hideButton.onClick.AddListener(hide);
        showButton.onClick.AddListener(show);
        loginButton.onClick.AddListener(login);
        checkoutButton.onClick.AddListener(checkout);
        disconnectButton.onClick.AddListener(disconnect);
        retrieveUserButton.onClick.AddListener(retrieveUser);
        transferTokensButton.onClick.AddListener(transferTokens);
        _webview = GameObject.Find("CanvasWebViewPrefab");
    }

    async void Start() {
        Debug.Log("Setting _canvasWebViewPrefab");
        _compressWebview();
        _canvasWebViewPrefab = _webview.GetComponent<Vuplex.WebView.CanvasWebViewPrefab>();
        await _canvasWebViewPrefab.WaitUntilInitialized();
        await _canvasWebViewPrefab.WebView.WaitForNextPageLoadToFinish();
        initializeProvider();
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

    void initializeProvider() {
        Debug.Log("initializeProvider");

        string base64logo = Resources.Load<TextAsset>("logo").text;
        Debug.Log("image data in base64=" + base64logo);

        invokeSDK("initialise", new {
            apiKey = "test-6371f967cea553bdd5ae3d5e-mrQsJvLklpohUWuN",
            secretKey = "KkEWQkndudnJmipaSpIfD1rO",
            supportedChains = new[] {"eth", "goerli", "mumbai", "matic"},
            options = new {
                logo = base64logo,
                theme = new {}
            },
        });
    }

    void invokeSDK(string _type, System.Object _payload, TaskCompletionSource<dynamic> promise = null) {
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

    public void hide(){
        Debug.Log("hide");
        // _webview.GetComponent<Renderer>().enabled = true;
        // _webview.SetActive(false);
    }

    public void show(){
        Debug.Log("show");
        // _webview.GetComponent<Renderer>().enabled = true;
        // _expandWebview();
        invokeSDK("method", new {
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

        invokeSDK("method", new {
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
        invokeSDK("method", new {
            methodName = "Disconnect",
            methodParams = new string[] { },
            methodOutput = ""
        });
    }
    
    public async void retrieveUser(){
        Debug.Log("retrieveUser");
        var promise = new TaskCompletionSource<dynamic>();
        invokeSDK("method", new {
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
