namespace Metafi.Unity {
    public readonly struct Asset {
        public readonly string assetKey;

        public Asset(string assetKey) {
            this.assetKey = assetKey;
        }
    }

    public static class Assets {
        // Mainnets
        public static Asset ETH_ETH = new Asset("eth_eth");
        public static Asset MATIC_MATIC = new Asset("matic_matic");
        
        // Testnets
        public static Asset GOERLI_ETH = new Asset("goerli_eth");
        public static Asset MUMBAI_MATIC = new Asset("mumbai_matic");
    }
}
