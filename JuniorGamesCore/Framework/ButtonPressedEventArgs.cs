namespace JuniorGames.Core.Framework
{
    using System;

    public class ButtonPressedEventArgs : EventArgs
    {
        public ButtonPressedEventArgs(ButtonIdentifier identifier, bool isPressed)
        {
            this.Identifier = identifier;
            this.IsPressed = isPressed;
        }

        public ButtonIdentifier Identifier { get; }
        public bool IsPressed { get; }
    }
}