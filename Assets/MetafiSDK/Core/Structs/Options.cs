using System;

namespace Metafi.Unity {
    struct TextColors {
        public string primary;
        public string secondary;
    }

    struct ButtonColors {
        public string color;
        public string fontColor;
        public string disabledColor;
        public string disabledFontColor;
    }

    struct Theme {
        public TextColors fontColors;
        public string bgColor;
        public ButtonColors ctaButton;
        public ButtonColors optionButton;
        public string metafiLogoColor;
    }

    struct Options {
        public string logo;
        public Theme theme;
    }
}
