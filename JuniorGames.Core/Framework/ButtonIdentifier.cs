namespace JuniorGames.Core.Framework
{
    using System;
    using System.Drawing;

    public struct ButtonIdentifier : IEquatable<ButtonIdentifier>
    {
        public Player Player { get; }
        public Color Color { get; }

        public ButtonIdentifier(Player player, Color color)
        {
            this.Player = player;
            this.Color = color;
        }

        public bool Equals(ButtonIdentifier other)
        {
            return (this.Player == other.Player) && this.Color.Equals(other.Color);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is ButtonIdentifier && this.Equals((ButtonIdentifier) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) this.Player * 397) ^ this.Color.GetHashCode();
            }
        }
    }
}