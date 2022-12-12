using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public class ButtonBehaviour : MonoBehaviour
{
    string userIdentifier;

    public Button hideButton;
    public Button showButton;
    public Button loginButton;
    public Button checkoutButton;
    public Button disconnectButton;
    public Button retrieveUserButton;
    public Button transferTokensButton;
    
    GameObject webview;

    Vuplex.WebView.CanvasWebViewPrefab _canvasWebViewPrefab;

    void OnEnable() {
        hideButton.onClick.AddListener(hide);
        showButton.onClick.AddListener(show);
        loginButton.onClick.AddListener(login);
        checkoutButton.onClick.AddListener(checkout);
        disconnectButton.onClick.AddListener(disconnect);
        retrieveUserButton.onClick.AddListener(retrieveUser);
        transferTokensButton.onClick.AddListener(transferTokens);

        webview = GameObject.Find("CanvasWebViewPrefab");
        initializeProvider();
    }

    async void Start() {
        Debug.Log("Setting _canvasWebViewPrefab");
        _canvasWebViewPrefab = GameObject.Find("CanvasWebViewPrefab").GetComponent<Vuplex.WebView.CanvasWebViewPrefab>();
        // Wait for the WebViewPrefab to initialize, because the WebViewPrefab.WebView property
        // is null until the prefab has initialized.
        await _canvasWebViewPrefab.WaitUntilInitialized();
        // Send a message after the page has loaded.
        await _canvasWebViewPrefab.WebView.WaitForNextPageLoadToFinish();
    }

    void initializeProvider() {
        Debug.Log("initializeProvider");
    }

    async void invokeSDK(string _function, string _payload) {
        string json = JsonConvert.SerializeObject(new 
        {
            type = "sdkInvocation",
            message = new
            { 
                function = _function,
                payload = _payload,
            }
        } 
        );

        Debug.Log("json: " + json);

        _canvasWebViewPrefab.WebView.PostMessage(json);
        // _canvasWebViewPrefab.WebView.PostMessage("hiii");
    }

    public void hide(){
        Debug.Log("hide");
        // webview.GetComponent<Renderer>().enabled = true;
        // webview.SetActive(false);
    }

    public void show(){
        Debug.Log("show");
        // webview.GetComponent<Renderer>().enabled = true;
        // webview.SetActive(true);
        invokeSDK("ShowWallet", "");
    }

    public async void login(){
        Debug.Log("login");
        invokeSDK("Login", "");
    }
    
    public void checkout(){
        Debug.Log("checkout");
    
    }
    
    public void disconnect(){
        Debug.Log("disconnect");
    
    }
    
    public void retrieveUser(){
        Debug.Log("retrieveUser");
    
    }
    
    public void transferTokens(){
        Debug.Log("transferTokens");
    
    }
}
