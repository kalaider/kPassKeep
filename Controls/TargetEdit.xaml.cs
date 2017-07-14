using FirstFloor.ModernUI.Presentation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using kPassKeep.Model;
using kPassKeep.Service;

namespace kPassKeep.Controls
{
    /// <summary>
    /// Interaction logic for TargetEdit.xaml
    /// </summary>
    public partial class TargetEdit : UserControl
    {

        public Target Selected
        {
            get { return (Target)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.Register("Selected", typeof(Target), typeof(TargetEdit), new FrameworkPropertyMetadata { DefaultValue = null, BindsTwoWayByDefault = true });
        
        public ObservableCollection<Target> Targets
        {
            get { return (ObservableCollection<Target>)GetValue(TargetsProperty); }
            set { SetValue(TargetsProperty, value); }
        }

        public static readonly DependencyProperty TargetsProperty =
            DependencyProperty.Register("Targets", typeof(ObservableCollection<Target>), typeof(TargetEdit), new FrameworkPropertyMetadata { DefaultValue = new ObservableCollection<Target>() });

        private TargetDescription TargetDescription
        {
            get { return (TargetDescription)GetValue(TargetDescriptionProperty); }
            set { SetValue(TargetDescriptionProperty, value); }
        }

        private static readonly DependencyProperty TargetDescriptionProperty =
            DependencyProperty.Register("TargetDescription", typeof(TargetDescription), typeof(TargetEdit), new FrameworkPropertyMetadata { DefaultValue = null });
        
        private bool IsIconComboBoxVisible
        {
            get { return (bool)GetValue(IsIconComboBoxVisibleProperty); }
            set { SetValue(IsIconComboBoxVisibleProperty, value); }
        }

        private static readonly DependencyProperty IsIconComboBoxVisibleProperty =
            DependencyProperty.Register("IsIconComboBoxVisible", typeof(bool), typeof(TargetEdit), new PropertyMetadata(false));
        
        public TargetEdit()
        {
            InitializeComponent();
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null)
            {
                return;
            }
            TargetDescription = await HttpTargetDescriptionProvider.GetDescription(Selected);
            if (TargetDescription == null)
            {
                return;
            }
            if (TargetDescription.Icons.Count() > 1)
            {
                IsIconComboBoxVisible = true;
            }
            Selected.Title = TargetDescription.Title;
            Selected.Description = TargetDescription.Description;
            Selected.Icon = TargetDescription.Icons.FirstOrDefault();
        }

        private void AddTargetButton_Click(object sender, RoutedEventArgs e)
        {
            Target t = new Target();
            t.Title = "<New Target>";
            Targets.Add(t);
            Selected = t;
        }

        private void ChooseIconButton_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null)
            {
                return;
            }
            var dialog = new Microsoft.Win32.OpenFileDialog { Multiselect = false, Title = "Choose Icon" };
            dialog.ShowDialog(Application.Current.MainWindow);
            var file = dialog.FileName;
            byte[] bytes;
            try
            {
                bytes = System.IO.File.ReadAllBytes(file);
            }
            catch (Exception)
            {
                FirstFloor.ModernUI.Windows.Controls.ModernDialog.ShowMessage("Cannot read file", "Error", MessageBoxButton.OK, Application.Current.MainWindow);
                return;
            }
            using (var s = new System.IO.MemoryStream(bytes))
            {
                try
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = s;
                    bi.EndInit();
                    TargetDescription = null;
                    Selected.Icon = bi;
                    IsIconComboBoxVisible = false;
                }
                catch (Exception)
                {
                    FirstFloor.ModernUI.Windows.Controls.ModernDialog.ShowMessage("Unknown image format", "Error", MessageBoxButton.OK, Application.Current.MainWindow);
                }
            }
        }

        private void TargetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsIconComboBoxVisible = false;
        }
    }
}
