using System;
using System.Windows;
using System.Windows.Controls;

using HelperClasses;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using RVCActivityLogger.Controller;
using RVCActivityLogger.Models;

namespace RVCActivityLogger.Views
{
    /// <summary>
    /// Interaction logic for Officers.xaml
    /// </summary>
    public partial class Employees : MetroWindow
    {
        private AppController appController;

        public Employees(AppController appController)
        {
            InitializeComponent();

            this.appController = appController;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GetOfficers();
        }

        private async void btnSaveUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtEmpId.Text) || string.IsNullOrEmpty(txtFirstName.Text) || string.IsNullOrEmpty(txtLastName.Text))
            {
                await this.ShowMessageAsync("Missing Fields", "Please make sure all fields are filled in.");
                return;
            }

            var employee = new Employee { 
                EmployeeID = txtEmpId.Text, 
                FirstName = txtFirstName.Text, 
                LastName = txtLastName.Text, 
                StatusNum = switchStatus.IsOn ? 1 : 0, 
                Password = UserManagement.CreateHash(txtPassword.Password) 
            };
            
            var isCreatedOrUpdated = btnSaveUpdate.Content.ToString() == "Save" ? appController.CreateEmployee(employee) : appController.UpdateEmployee(employee, (dgOfficers.SelectedItem as Employee).EmployeeID);
            if (isCreatedOrUpdated)
            {
                GetOfficers();

                ClearFields();
            }
        }

        private void btnDisable_Click(object sender, RoutedEventArgs e)
        {
            ////var officer = dgOfficers.SelectedItem as Officer;
            ////if (officer == null) return;

            ////var isDisabled = appController.DisableOfficer(officer.EmployeeID);

            ////if (isDisabled)
            ////{
            ////    var officers = appController.GetOfficers();
            ////    dgOfficers.ItemsSource = officers;

            ////    ClearFields();
            ////}
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void dgOfficers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var officer = dgOfficers.SelectedItem as Employee;
            if (officer == null) return;

            txtEmpId.Text = officer.EmployeeID.ToString();
            txtFirstName.Text = officer.FirstName;
            txtLastName.Text = officer.LastName;
            txtPassword.Password = "DoNotShow";

            btnSaveUpdate.Content = "Update";

            switchStatus.IsOn = officer.StatusNum == 1 ? true : false;
        }

        private void GetOfficers()
        {
            var officers = appController.GetEmployees();
            dgOfficers.ItemsSource = officers;

            dgOfficers.Columns[1].Visibility = Visibility.Collapsed;
            dgOfficers.Columns[5].Visibility = Visibility.Collapsed;
            dgOfficers.Columns[6].Visibility = Visibility.Collapsed;
            dgOfficers.Columns[7].Visibility = Visibility.Collapsed;
        }

        private void ClearFields()
        {
            txtEmpId.Text = txtFirstName.Text = txtLastName.Text = txtPassword.Password = "";
            btnSaveUpdate.Content = "Save";
            switchStatus.IsOn = true;
        }

    }
}
