using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using RVCActivityLogger.Controller;
using RVCActivityLogger.Models;
using RVCActivityLogger.Utility;

namespace RVCActivityLogger.Views
{
    /// <summary>
    /// Interaction logic for EditLog.xaml
    /// </summary>
    public partial class EditLog : MetroWindow
    {
        private AppController appController;
        private Log logToEdit;

        public EditLog(AppController appController, Log log)
        {
            InitializeComponent();

            this.appController = appController;
            this.logToEdit = log;
        }

        private void EditLogWin_Loaded(object sender, RoutedEventArgs e)
        {
            AddAdornerToControls(new List<UIElement> { cmbOfficers, dpIncidentDate, cmbIncidentTypes, cmbLocations });

            cmbOfficers.ItemsSource = new List<Employee> { new Employee { FullName = logToEdit.Employee, RowId = logToEdit.EmployeeId } };
            cmbOfficers.DisplayMemberPath = "FullName";
            cmbOfficers.SelectedIndex = 0;

            dpIncidentDate.DisplayDate = logToEdit.IncidentDate;

            var locations = new List<LocationItem>();
            var incidentTypes = new List<IncidentItem>();

            _ = Task.Run(() =>
            {
                locations = appController.GetLocations();
                incidentTypes = appController.GetIncidentTypes();

            }).ContinueWith(r =>
            {
                cmbIncidentTypes.ItemsSource = incidentTypes;
                cmbLocations.ItemsSource = locations;

                cmbIncidentTypes.DisplayMemberPath = "Code";
                cmbLocations.DisplayMemberPath = "Code";

                cmbIncidentTypes.SelectedIndex = incidentTypes.FindIndex(f => f.RowId == logToEdit.IncidentTypeId);
                cmbLocations.SelectedIndex = locations.FindIndex(f => f.RowId == logToEdit.LocationId);

                startTime.SelectedDateTime = logToEdit.StartTime;
                endTime.SelectedDateTime = logToEdit.EndTime;

                if (!string.IsNullOrEmpty(logToEdit.Description))
                {
                    txtDescription.Document.Blocks.Clear();
                    txtDescription.Document.Blocks.Add(new Paragraph(new Run(logToEdit.Description.Trim())));
                }

                txtMoneyTransport.Text = logToEdit.Money;
                txtVehicle.Text = logToEdit.Vehicle;
                numStartMileage.Value = logToEdit.StartMileage == 0 ? null : (double?)logToEdit.StartMileage;
                numEndMileage.Value = logToEdit.EndMileage == 0 ? null : (double?)logToEdit.EndMileage;
                numFuel.Value = logToEdit.Fuel == 0 ? null : (double?)logToEdit.Fuel;

            }, TaskScheduler.FromCurrentSynchronizationContext());

        }

        private async void btnSaveLog_Click(object sender, RoutedEventArgs e)
        {
            if (cmbIncidentTypes.SelectedIndex < 0 || cmbLocations.SelectedIndex < 0)
            {
                await this.ShowMessageAsync("Required Fields", "Please make sure the required fields are filled in and try again.");
                return;
            }

            if ((cmbIncidentTypes.SelectedItem as IncidentItem).Code.Equals("MT", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(txtMoneyTransport.Text))
            {
                await this.ShowMessageAsync("Required Fields", "Please make sure the money transport field is filled in and try again.");
                return;
            }

            if ((cmbIncidentTypes.SelectedItem as IncidentItem).Code.Equals("VMS", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(txtVehicle.Text))
            {
                await this.ShowMessageAsync("Required Fields", "Please make sure the vehicle field is filled in and try again.");
                return;
            }

            if (numFuel.Value.HasValue && string.IsNullOrEmpty(txtVehicle.Text))
            {
                await this.ShowMessageAsync("Required Fields", "Please make sure the vehicle field is filled in and try again.");
                return;
            }

            var log = new Log
            {
                Description = string.IsNullOrEmpty(new TextRange(txtDescription.Document.ContentStart, txtDescription.Document.ContentEnd).Text) ? "" : new TextRange(txtDescription.Document.ContentStart, txtDescription.Document.ContentEnd).Text.Trim(),
                EndMileage = numEndMileage.Value.HasValue ? Convert.ToInt32(numEndMileage.Value) : 0,
                EndTime = endTime.SelectedDateTime.HasValue ? endTime.SelectedDateTime.Value : DateTime.MinValue,
                Fuel = numFuel.Value.HasValue ? Convert.ToInt32(numFuel.Value) : 0,
                IncidentDate = dpIncidentDate.DisplayDate,
                IncidentType = cmbIncidentTypes.Text,
                IncidentTypeId = (cmbIncidentTypes.SelectedItem as IncidentItem).RowId,
                Location = cmbLocations.Text,
                LocationId = (cmbLocations.SelectedItem as LocationItem).RowId,
                Money = string.IsNullOrEmpty(txtMoneyTransport.Text) ? "" : txtMoneyTransport.Text,
                EmployeeId = (cmbOfficers.SelectedItem as Employee).RowId,
                StartMileage = numStartMileage.Value.HasValue ? Convert.ToInt32(numStartMileage.Value) : 0,
                StartTime = startTime.SelectedDateTime.HasValue ? startTime.SelectedDateTime.Value : DateTime.MinValue,
                Vehicle = string.IsNullOrEmpty(txtVehicle.Text) ? "" : txtVehicle.Text,
                RowId = logToEdit.RowId,
            };

            prSaveLog.Visibility = Visibility.Visible;
            prSaveLog.IsActive = true;

            var isCreated = appController.EditLog(log);

            await Task.Delay(1750);
            prSaveLog.IsActive = false;
            prSaveLog.Visibility = Visibility.Collapsed;

            if (isCreated)
            {
                this.Close();
            }
        }

        #region ui business logic for required fields
        private void cmbIncidentTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            if ((cmbIncidentTypes.SelectedItem as IncidentItem).Code.Equals("mt", StringComparison.OrdinalIgnoreCase))
            {
                AddAdornerToControls(new List<UIElement> { txtMoneyTransport });

                RemoveAdornerToControls(new List<UIElement> { txtVehicle });
            }
            else if ((cmbIncidentTypes.SelectedItem as IncidentItem).Code.Equals("vms", StringComparison.OrdinalIgnoreCase))
            {
                AddAdornerToControls(new List<UIElement> { txtVehicle });

                RemoveAdornerToControls(new List<UIElement> { txtMoneyTransport });
            }
            else
            {
                RemoveAdornerToControls(new List<UIElement> { txtVehicle, txtMoneyTransport });
            }
        }

        private void numFuel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (e.NewValue == null || e.NewValue < 1)
            {
                RemoveAdornerToControls(new List<UIElement> { txtVehicle });
            }
            else
            {
                AddAdornerToControls(new List<UIElement> { txtVehicle });
            }
        }
        #endregion

        #region local methods
        private void AddAdornerToControls(List<UIElement> uiElements)
        {
            AdornerLayer adornerLayer = null;

            foreach (var c in uiElements)
            {
                adornerLayer = AdornerLayer.GetAdornerLayer(c as Visual);
                if (adornerLayer == null) continue;

                adornerLayer.Add(new RequiredAdorner(c));
            }
        }

        private void RemoveAdornerToControls(List<UIElement> uiElements)
        {
            AdornerLayer adornerLayer = null;

            foreach (var c in uiElements)
            {
                if (numFuel.Value.HasValue && c == txtVehicle) continue;

                adornerLayer = AdornerLayer.GetAdornerLayer(c as Visual);
                if (adornerLayer == null) continue;

                var toRemove = adornerLayer.GetAdorners(c);
                if (toRemove != null)
                {
                    foreach (var r in toRemove)
                        adornerLayer.Remove(r);
                }
            }
        }

        #endregion


    }
}
