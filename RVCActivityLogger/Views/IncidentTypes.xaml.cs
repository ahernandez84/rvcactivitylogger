
using System.Windows;
using System.Windows.Controls;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using RVCActivityLogger.Controller;
using RVCActivityLogger.Models;

namespace RVCActivityLogger.Views
{
    /// <summary>
    /// Interaction logic for IncidentTypes.xaml
    /// </summary>
    public partial class IncidentTypes : MetroWindow
    {
        private AppController appController;

        public IncidentTypes(AppController appController)
        {
            InitializeComponent();

            this.appController = appController;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GetIncidentTypes();
        }

        private async void btnSaveUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtCode.Text) || string.IsNullOrEmpty(txtDescription.Text))
            {
                await this.ShowMessageAsync("Missing Fields", "Please make sure all fields are filled in.");
                return;
            }

            var incident = new IncidentItem { Code = txtCode.Text, Description = txtDescription.Text, StatusNum = switchStatus.IsOn ? 1 : 0 };

            var isCreatedOrUpdated = btnSaveUpdate.Content.ToString() == "Save" ? appController.CreateIncidentType(incident) : appController.UpdateIncidentType(incident, (dgIncidents.SelectedItem as IncidentItem).RowId);

            if (isCreatedOrUpdated)
            {
                GetIncidentTypes();

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

        private void dgIncidents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var incident = dgIncidents.SelectedItem as IncidentItem;
            if (incident == null) return;

            txtCode.Text = incident.Code;
            txtDescription.Text = incident.Description;

            switchStatus.IsOn = incident.StatusNum == 1 ? true : false;

            btnSaveUpdate.Content = "Update";
        }

        private void GetIncidentTypes()
        {
            var incidents = appController.GetIncidentTypes();
            dgIncidents.ItemsSource = incidents;

            if (incidents.Count == 0) return;

            dgIncidents.Columns[3].Visibility = Visibility.Collapsed;
            dgIncidents.Columns[4].Visibility = Visibility.Collapsed;
        }

        private void ClearFields()
        {
            txtCode.Text = txtDescription.Text = "";
            btnSaveUpdate.Content = "Save";
        }

    }
}
