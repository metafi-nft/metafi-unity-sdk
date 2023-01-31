# Metafi Unity SDK

## Getting Started

<br/>

> **_INFO:_** First, you will need an API key to get started - sign up for a key [here](https://developer-test.usemeta.fi/).

<br/>

## Description

To get started with the Unity SDK, please follow the steps outlined below. If you face any issues, feel free to post your questions on our [Discord support channel](https://discord.gg/mjpyh64zEW).

<br/>

## Step 1: Import Packages

You can download the latest version of our unity package from our Github link. Add this to your project under the `Assets` folder.

You will also need to purchase the Vuplex 3D WebView package for the relevant platform you require (Windows + Mac / iOS / Android), and place this package under the `Assets` folder of your project.

<br/>

> **_INFO:_** You can get a 20% discount while purchasing the WebView from the Vuplex store by using the code `metafi` during checkout. Additionally will reimburse an additional $100 of your Vuplex purchase if you require; to claim this, you can book a call with us via this link.

<br/>

## Step 2: Import Prefab

Import the Prefab named `MetafiSDKPrefabDesktopNative` to your Hierarchy. Set the `sort order` of this Component to a high value so that it rests above your other UI elements. Adjust the reference resolution of this Component if required.

<br/>

## Step 3: Initialize the Provider

Under the `Start()` script of your first scene, Import the Metafi namespace and initialise the provider as shown below. More information on the Provider Initialization can be found [here](https://docs.usemeta.fi/unity-sdk/sdk-reference/provider-initialisation).

```csharp
using Metafi.Unity;
using System.Dynamic;

public class GameManager : MonoBehaviour {
    async void Start() {
        Debug.Log("Initializing Metafi Provider");

        dynamic _options = new ExpandoObject();
        _options.logo = @"Assets/Resources/logo-2.png";
        _options.theme = new {
            fontColors = new {
                primary = "#FFFFFF",
                secondary = "#e8e8e8"
            },
            bgColor = "#29327F",
            ctaButton = new {
                color = "#F19B28",
                fontColor = "#FFFFFF"
            },
            optionButton = new {
                color = "rgba(255,255,255,0.1)",
                fontColor = "#FFFFFF"
            },
            metafiLogoColor = "light",
        };
        
        Token wethToken = new Token(
            "Wrapped Ethereum",
            "goerliWETH",
            Chains.GOERLI,
            "https://d2qdyxy3mxzsfv.cloudfront.net/images/logo/ethereum.png",
            "0xb4fbf271143f4fbf7b91a5ded31805e42b2208d6",
            18
        );

        await MetafiProvider.Instance.Init(
            "apiKey",
            "secretKey",
            _options,
            new List<Chain> {Chains.ETH, Chains.MATIC, Chains.GOERLI, Chains.MUMBAI},
            new List<Token> {wethToken},
            false
        );
        
        Debug.Log("Metafi Provider initialized");
    }
}
```
<br/>

## Step 4: Function Invocation

You're done! You can now invoke the functions that you need.

```csharp
using Metafi.Unity;

public class StartButton : MonoBehaviour {
    public async void ShowWallet() {
        await MetafiProvider.Instance.ShowWallet();
    }
}
```

<br/>

## API Reference
The full API reference can be found in our [docs](https://docs.usemeta.fi/unity-sdk/get-started).

<br/>

## Bug Reports
Let us know about any bugs on our [Discord](https://discord.gg/mjpyh64zEW). We will respond at the soonest!

<br/>

## Related Links
* [Website](https://www.usemeta.fi/)
* [Documentation](https://docs.usemeta.fi/)
* [Twitter](https://twitter.com/metafi_wallet)
* [Discord](https://discord.gg/yaxvxEmuKn)
