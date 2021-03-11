
using System.Windows;
using System.Windows.Controls;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using RVCActivityLogger.Controller;
using RVCActivityLogger.Models;

namespace RVCActivityLogger.Views
{
    /// <summary>
    /// Interaction logic for Locations.xaml
    /// </summary>
    public partial class Locations : MetroWindow
    {
        private AppController appController;

        public Locations(AppController appController)
        {
            InitializeComponent();

            this.appController = appController;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GetLocations();
        }

        private async void btnSaveUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtLocCode.Text) || string.IsNullOrEmpty(txtLocDescription.Text))
            {
                await this.ShowMessageAsync("Missing Fields", "Please make sure all fields are filled in.");
                return;
            }

            var location = new LocationItem { Code = txtLocCode.Text, Description = txtLocDescription.Text, StatusNum = switchStatus.IsOn ? 1 : 0 };

            var isCreatedOrUpdated = btnSaveUpdate.Content.ToString() == "Save" ? appController.CreateLocation(location) : appController.UpdateLocation(location, (dgLocations.SelectedItem as LocationItem).RowId);

            if (isCreatedOrUpdated)
            {
                GetLocations();

                ClearFields();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void dgLocations_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var location = dgLocations.SelectedItem as LocationItem;
            if (location == null) return;

            txtLocCode.Text = location.Code;
            txtLocDescription.Text = location.Description;

            switchStatus.IsOn = location.StatusNum == 1 ? true : false;

            btnSaveUpdate.Content = "Update";
        }

        private void GetLocations()
        {
            var locations = appController.GetLocations();
            dgLocations.ItemsSource = locations;

            if (locations.Count == 0) return;

            dgLocations.Columns[3].Visibility = Visibility.Collapsed;
            dgLocations.Columns[4].Visibility = Visibility.Collapsed;
        }

        private void ClearFields()
        {
            txtLocCode.Text = txtLocDescription.Text = "";
            btnSaveUpdate.Content = "Save";
        }
    }
}
