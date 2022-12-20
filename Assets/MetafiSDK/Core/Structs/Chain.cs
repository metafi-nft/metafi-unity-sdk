namespace Metafi.Unity {
    public readonly struct Chain {
        public readonly string chainKey;

        public Chain(string chainKey) {
            this.chainKey = chainKey;
        }
    }

    public static class Chains {
        // Mainnets
        public static Chain ETH = new Chain("eth");
        public static Chain MATIC = new Chain("matic");
        
        // Testnets
        public static Chain GOERLI = new Chain("goerli");
        public static Chain MUMBAI = new Chain("mumbai");
    }
}
