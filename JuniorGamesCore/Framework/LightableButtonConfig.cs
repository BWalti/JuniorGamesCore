namespace JuniorGames.Core.Framework
{
    using System.Drawing;

    public class LightableButtonConfig
    {
        public LightableButtonConfig(int ledPin, int buttonPin, ButtonIdentifier buttonIdentifier)
        {
            this.Id = buttonIdentifier;
            this.LedPin = ledPin;
            this.ButtonPin = buttonPin;
        }

        public ButtonIdentifier Id { get; set; }

        public ButtonIdentifier Identifier => new ButtonIdentifier(this.Player, this.Color);

        public int LedPin { get; }
        public int ButtonPin { get; }
        public Color Color => this.Id.Color;
        public Player Player => this.Id.Player;
    }
}