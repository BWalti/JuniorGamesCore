namespace SimulatorBox
{
    using System.Windows.Media;

    using Caliburn.Micro;

    public class LightyButtonViewModel : PropertyChangedBase
    {
        private readonly LightyButtonModel lightableButton;

        public Brush Color { get; }

        public LightyButtonViewModel(Color color, LightyButtonModel lightableButton)
        {
            this.lightableButton = lightableButton;

            lightableButton.PropertyChanged += (sender, args) =>
            {
                this.NotifyOfPropertyChange(() => this.GlowIsVisible);
                this.NotifyOfPropertyChange(() => this.DarkIsVisible);
            };

            this.Color = new SolidColorBrush(color);
        }

        public bool GlowIsVisible => this.lightableButton.IsLighted;

        public bool DarkIsVisible => !this.lightableButton.IsLighted;

        public void OnMouseDown()
        {
            this.lightableButton.IsPressed = true;
        }

        public void OnMouseUp()
        {
            this.lightableButton.IsPressed = false;
        }
    }
}