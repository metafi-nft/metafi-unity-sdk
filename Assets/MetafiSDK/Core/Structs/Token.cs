using System;

namespace Metafi.Unity {
    public readonly struct Token {
        public readonly string name;
        public readonly string symbol;
        public readonly Chain chain;
        public readonly string image;
        public readonly string contractAddress;
        public readonly int decimals;

        public Token (string name, string symbol, Chain chain, string image, string contractAddress, int decimals) {
            this.name = name;
            this.symbol = symbol;
            this.chain = chain;
            this.image = image;
            this.contractAddress = contractAddress;
            this.decimals = decimals;
        }
    }
}
