using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Dynamic;
using Metafi.Unity;

public class ButtonBehaviour : MonoBehaviour
{
    private static readonly HttpClient client = new HttpClient();

    public Button showButton;
    public Button loginButton;
    public Button checkoutButton;
    public Button disconnectButton;
    public Button retrieveUserButton;
    public Button transferTokensButton;

    void OnEnable() {
        showButton.onClick.AddListener(show);
        loginButton.onClick.AddListener(login);
        checkoutButton.onClick.AddListener(checkout);
        disconnectButton.onClick.AddListener(disconnect);
        retrieveUserButton.onClick.AddListener(retrieveUser);
        transferTokensButton.onClick.AddListener(transferTokens);
    }

    void Start() {
        Debug.Log("Setting _canvasWebViewPrefab");
        initializeProvider();
    }

    async void initializeProvider() {
        Debug.Log("initializeProvider");

        dynamic _options = new ExpandoObject();
        _options.logo = @"Assets/Resources/logo.png";
        _options.theme = new {};

        await MetafiProvider.Instance.Init(
            "test-6371f967cea553bdd5ae3d5e-mrQsJvLklpohUWuN",
            "KkEWQkndudnJmipaSpIfD1rO",
            _options,
            new List<Chain> {Chains.GOERLI, Chains.MUMBAI},
            new List<Token> {}
        );
    }

    public async void show(){
        Debug.Log("show");
        
        await MetafiProvider.Instance.Show();
    }

    public async void login(){
        Debug.Log("login");

        var response = await client.PostAsync("https://vpa58nk2e9.execute-api.us-east-1.amazonaws.com/dev/testMerchantAuthenticate", null);
        var responseString = await response.Content.ReadAsStringAsync();
        
        dynamic responseObj = JsonConvert.DeserializeObject<dynamic>(responseString);

        string userIdentifier = responseObj.userIdentifier;
        string jwtToken = responseObj.jwtToken;

        Debug.Log("response from login: " + userIdentifier + " " + jwtToken);

        await MetafiProvider.Instance.Login(userIdentifier, jwtToken, ((System.Action<dynamic>) (result => {
            Debug.Log("Login complete, result: " + result.ToString());
        })));
    }
    
    public void checkout(){
        Debug.Log("checkout");
    }
    
    public async void disconnect(){
        Debug.Log("disconnect");
        
        await MetafiProvider.Instance.Disconnect();
    }
    
    public async void retrieveUser(){
        Debug.Log("retrieveUser");

        var res = await MetafiProvider.Instance.RetrieveUser();
        Debug.Log("retrieveUser complete, result: " + res.ToString());
    }
    
    public void transferTokens(){
        Debug.Log("transferTokens");
    }
}
