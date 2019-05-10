namespace SimulatorBox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;

    using Caliburn.Micro;

    public class GameBoxBootstrapper : BootstrapperBase
    {
        public GameBoxBootstrapper()
        {
            ConventionManager.AddElementConvention<UIElement>(UIElement.VisibilityProperty,
                                                              "Visibility",
                                                              "VisibilityChanged");

            var baseBindProperties = ViewModelBinder.BindProperties;
            ViewModelBinder.BindProperties = (elements, viewModel) =>
            {
                var frameworkElements = elements.ToList();

                this.BindVisiblityProperties(frameworkElements, viewModel);
                return baseBindProperties(frameworkElements, viewModel);
            };

            // Need to override BindActions as well, as it's called first and filters out anything it binds to before
            // BindProperties is called.
            var baseBindActions = ViewModelBinder.BindActions;
            ViewModelBinder.BindActions = (elements, viewModel) =>
            {
                var frameworkElements = elements.ToList();

                this.BindVisiblityProperties(frameworkElements, viewModel);
                return baseBindActions(frameworkElements, viewModel);
            };

            this.Initialize();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            this.DisplayRootViewFor<ShellViewModel>();
        }

        private void BindVisiblityProperties(IEnumerable<FrameworkElement> frameWorkElements, Type viewModel)
        {
            foreach (var frameworkElement in frameWorkElements)
            {
                var propertyName = frameworkElement.Name + "IsVisible";
                var property = viewModel.GetPropertyCaseInsensitive(propertyName);
                if (property != null)
                {
                    var convention = ConventionManager.GetElementConvention(typeof(FrameworkElement));
                    ConventionManager.SetBindingWithoutBindingOverwrite(viewModel,
                                                                        propertyName,
                                                                        property,
                                                                        frameworkElement,
                                                                        convention,
                                                                        convention
                                                                        .GetBindableProperty(frameworkElement));
                }
            }
        }
    }
}