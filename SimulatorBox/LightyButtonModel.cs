namespace SimulatorBox
{
    using System;
    using System.Threading.Tasks;

    using Caliburn.Micro;
    using System.Drawing;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using GameBox.Framework;

    public class LightyButtonModel : PropertyChangedBase, ILightableButton
    {
        private bool isPressed;

        private bool isLighted;

        private readonly Subject<bool> buttonAction;

        public LightyButtonModel(ButtonIdentifier identifier)
        {
            this.ButtonIdentifier = identifier;
            this.buttonAction = new Subject<bool>();

            this.Button = this.buttonAction.Select(value => new ButtonPressedEventArgs(this.ButtonIdentifier, value));
            this.ButtonUp = this.buttonAction.Where(value => !value).Select(v => this.ButtonIdentifier);
            this.ButtonDown = this.buttonAction.Where(value => value).Select(v => this.ButtonIdentifier);
        }


        public bool IsPressed
        {
            get
            {
                return this.isPressed;
            }
            set
            {
                if (this.isPressed == value)
                {
                    return;
                }

                this.isPressed = value;
                this.buttonAction.OnNext(value);
                this.NotifyOfPropertyChange();
            }
        }

        public bool IsLighted
        {
            get
            {
                return this.isLighted;
            }
            set
            {
                this.isLighted = value;
                this.NotifyOfPropertyChange();
            }
        }

        public void Dispose()
        {
        }

        public ButtonIdentifier ButtonIdentifier { get; }

        public Color Color => this.ButtonIdentifier.Color;

        public Player Player => this.ButtonIdentifier.Player;

        public IObservable<ButtonIdentifier> ButtonDown { get; }

        public IObservable<ButtonIdentifier> ButtonUp { get; }

        public IObservable<ButtonPressedEventArgs> Button { get; }

        public async Task SetLight(bool enabled, TimeSpan? milliseconds = null)
        {
            var oldValue = this.IsLighted;

            this.IsLighted = enabled;

            if (milliseconds.HasValue)
            {
                await Task.Delay(milliseconds.Value);
                this.IsLighted = oldValue;
            }
        }
    }
}