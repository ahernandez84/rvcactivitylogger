using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

using NLog;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using RVCOfficerLogger.Utility;
using RVCOfficerLogger.Controller;
using RVCOfficerLogger.Models;
using RVCOfficerLogger.Services;
using RVCOfficerLogger.Views;
using UserManagement = HelperClasses.UserManagement;

namespace RVCOfficerLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private AppConfigService appConfig;
        private AppController appController;
        private Timer tmrCheckForDBConnectivity;

        bool isAdornerAdded = false;
        bool isLoggedOn = false;

        public MainWindow()
        {
            InitializeComponent();

            HideControls();

            tmrCheckForDBConnectivity = new Timer();
            tmrCheckForDBConnectivity.Elapsed += TmrCheckForDBConnectivity_Elapsed;

            dpIncidentDate.DisplayDateStart = DateTime.Now.AddDays(-1);
        }

        private void MainWin_Loaded(object sender, RoutedEventArgs e)
        {
            logger.Info("Application is loading and initializing.");

            lblStatus.Text = "Initializing System...";

            AddAdornerToControls(new List<UIElement> { cmbOfficers, dpIncidentDate, cmbIncidentTypes });

            appConfig = new AppConfigService();
            appConfig.ReadSettings();

            tmrCheckForDBConnectivity.Interval = appConfig.CheckCentralDatabaseTimer;

            appController = new AppController();

            var officers = new List<Employee>();
            var incidentTypes = new List<IncidentItem>();
            var locations = new List<LocationItem>(); 

            var systemCheck = 0;

            _ = Task.Run(() =>
            {
                systemCheck = appController.Initialize(appConfig.LogRowCount);

                officers = appController.GetEmployees();
                incidentTypes = appController.GetIncidentTypes();
                locations = appController.GetLocations();

            }).ContinueWith(r =>
            {
                if (systemCheck == -2)
                {
                    lblStatus.Text = "System failed to initialize. Please contact your administrator.";
                    return;
                }

                cmbOfficers.ItemsSource = officers;
                cmbOfficers.DisplayMemberPath = $"FullName";

                cmbIncidentTypes.ItemsSource = incidentTypes;
                cmbIncidentTypes.DisplayMemberPath = "Code";

                cmbLocations.ItemsSource = locations;
                cmbLocations.DisplayMemberPath = "Code";

                //Verify central and local database statuses
                if ((systemCheck & 1) != 1 && (systemCheck & 2) != 2)
                {
                    lblStatus.Text = "Both databases, local and central, are offline.  Please notify the system administrator.";
                    return;
                } 
                else if ((systemCheck & 1) != 1 && (systemCheck & 2) == 2 && (systemCheck & 8) != 8)
                {
                    lblStatus.Text = "The central database is offline and the local database is not initialized with data.  Please notify the system administrator.";
                    return;
                }
                else if ((systemCheck & 1) != 1 && (systemCheck & 2) == 2 && (systemCheck & 8) == 8)
                {
                    lblStatus.Text = $"Central Database:  {((systemCheck & 1) == 1 ? "Online" : "Offline")} | Local Database:  {((systemCheck & 2) == 2 ? "Online" : "Offline")}";

                    lblOffline.Visibility = Visibility.Visible;
                }
                else if ((systemCheck & 1) == 1 && (systemCheck & 2) == 2 && ((systemCheck & 8) == 8 || (systemCheck & 8) != 8))
                {
                    lblStatus.Text = $"Central Database:  {((systemCheck & 1) == 1 ? "Online" : "Offline")} | Local Database:  {((systemCheck & 2) == 2 ? "Online" : "Offline")}";

                    SynchronizeDBTables(officers, locations, incidentTypes);
                }

                tmrCheckForDBConnectivity.Start();

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void hlViewLogs_Click(object sender, RoutedEventArgs e)
        {
            if (cmbOfficers.SelectedIndex < 0) return;

            ViewLogs viewLog = new ViewLogs(appController, (cmbOfficers.SelectedItem as Employee).RowId);
            viewLog.Show();
        }

        private async void cmbOfficers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || cmbOfficers.SelectedIndex == -1) return;

            var status = lblStatus.Text;

            LoginDialogData result = await this.ShowLoginAsync("Authentication", "Enter Password:", new LoginDialogSettings { ColorScheme = this.MetroDialogOptions.ColorScheme, ShouldHideUsername = true, EnablePasswordPreview = true });
            if (result == null)
            {
                cmbOfficers.SelectedIndex = -1;
            }
            else
            {
                if (UserManagement.ValidatePassword(result.Password, (cmbOfficers.SelectedItem as Employee).Password))
                {
                    lblOfficer.Text = $"Welcome Back, {(cmbOfficers.SelectedItem as Employee).FirstName[0]}. {(cmbOfficers.SelectedItem as Employee).LastName}";

                    ShowControlsWithAnimation(new List<UIElement> { lblOfficer, lblLogText, grid1, txtDescription, grid2, grid3, stackPanel1 });

                    if (!isAdornerAdded)
                    {
                        AddAdornerToControls(new List<UIElement> { cmbLocations });
                        isAdornerAdded = true;
                    }

                    isLoggedOn = true;
                }
                else
                {
                    cmbOfficers.SelectedIndex = -1;

                    RemoveAdornerToControls(new List<UIElement> { cmbLocations });
                    HideControlsWithAnimation(new List<UIElement> { lblOfficer, lblLogText, grid1, txtDescription, grid2, grid3, stackPanel1 });

                    lblStatus.Text = "Incorrect Password.";
                    await Task.Delay(2000);
                    lblStatus.Text = status;
                }
            }
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
                Vehicle = string.IsNullOrEmpty(txtVehicle.Text) ? "" : txtVehicle.Text
            };

            prSaveLog.Visibility = Visibility.Visible;  // activate progress ring during the save
            prSaveLog.IsActive = true;

            var isCreated = appController.CreateLog(log);

            var status = lblStatus.Text;
            if (isCreated) 
            {
                ClearFields();
                lblStatus.Text = "Activity log successfully saved.";
            }
            else
            {
                lblStatus.Text = "Failed to save activity log.";
            }

            await Task.Delay(1750);

            prSaveLog.IsActive = false;
            prSaveLog.Visibility = Visibility.Collapsed;

            lblStatus.Text = status;
        }

        private void TmrCheckForDBConnectivity_Elapsed(object sender, ElapsedEventArgs e)
        {
            tmrCheckForDBConnectivity.Stop();

            if (appController.CheckStatusOfCentralDB())
            {
                this.Dispatcher.Invoke((Action)delegate ()
                {
                    lblStatus.Text = $"Central Database:  Offline | Local Database:  Online & Tables are Synced";
                    lblOffline.Visibility = Visibility.Visible;
                });
            }
            else
            {
                this.Dispatcher.Invoke((Action)delegate ()
                {
                    lblOffline.Visibility = Visibility.Collapsed;
                    lblStatus.Text = $"Central Database:  Online | Local Database:  Online & Tables are Synced";
                });

                appController.InsertOrUpdateLogsToCentralDatabase();
            }

            tmrCheckForDBConnectivity.Start();
        }

        private async void btnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (!isLoggedOn) return;

            var status = lblStatus.Text;

            LoginDialogData result = await this.ShowLoginAsync("Change Password", "Enter New Password:", new LoginDialogSettings { ColorScheme = this.MetroDialogOptions.ColorScheme, ShouldHideUsername = true, EnablePasswordPreview = true });
            if (result == null)
            {
                return;
            }
            else
            {
                if (string.IsNullOrEmpty(result.Password))
                {
                    lblStatus.Text = "Password cannot be blank!";
                    await Task.Delay(2000);
                    lblStatus.Text = status;
                }
                else
                {
                    (cmbOfficers.SelectedItem as Employee).Password = UserManagement.CreateHash(result.Password);

                    var isUpdated = appController.UpdateEmployeePassword((cmbOfficers.SelectedItem as Employee));

                    lblStatus.Text = isUpdated ? "Password was successfully updated." : "Failed to change Password.  Please check logs for more details.";

                    await Task.Delay(2000);
                    lblStatus.Text = status;
                }
            }
        }

        #region ui business logic for one off required fields
        private void cmbIncidentTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            if ((cmbIncidentTypes.SelectedItem as IncidentItem).Code.Equals("mt", StringComparison.OrdinalIgnoreCase))
            {
                AddAdornerToControls(new List<UIElement> { txtMoneyTransport });

                RemoveAdornerToControls(new List<UIElement> { txtVehicle});
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
        private void HideControls()
        {
            lblOfficer.Opacity = 0;
            lblLogText.Opacity = 0;
            grid1.Opacity = 0;
            txtDescription.Opacity = 0;
            grid2.Opacity = 0;
            grid3.Opacity = 0;
            stackPanel1.Opacity = 0;
        }

        private void ShowControlsWithAnimation(List<UIElement> uiElements)
        {
            DoubleAnimation fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(2));
            DoubleAnimation fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(2));

            startUpIcon.BeginAnimation(StackPanel.OpacityProperty, fadeOutAnimation);
            startUpIcon.Visibility = Visibility.Collapsed;

            foreach (var c in uiElements)
            {
                c.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
            }
        }

        private void HideControlsWithAnimation(List<UIElement> uiElements)
        {
            DoubleAnimation fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(2));
            DoubleAnimation fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(2));

            startUpIcon.Visibility = Visibility.Visible;
            startUpIcon.BeginAnimation(StackPanel.OpacityProperty, fadeInAnimation);

            foreach (var c in uiElements)
            {
                c.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
            }
        }

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
                    foreach(var r in toRemove)
                        adornerLayer.Remove(r);
                }
            }
        }

        private void ClearFields()
        {
            cmbIncidentTypes.SelectedIndex = -1;
            cmbLocations.SelectedIndex = -1;
            txtDescription.Document.Blocks.Clear();
            txtMoneyTransport.Text = null;
            txtVehicle.Text = null;
            numStartMileage.Value = null;
            numEndMileage.Value = null;
            numFuel.Value = null;

            startTime.SelectedDateTime = endTime.SelectedDateTime = DateTime.Now;
        }

        #endregion

        #region local database
        private void SynchronizeDBTables(List<Employee> officers, List<LocationItem> locations, List<IncidentItem> incidentTypes)
        {
            logger.Info("Synchronizing local database with central SQL Server.");

            int count = 0;
            _ = Task.Run(() =>
            {
                appController.DeleteLocalTables();

                officers.ForEach(o =>
                { 
                    if (appController.CreateOfficerInLocalDB(o)) count++;
                });

                locations.ForEach(l =>
                {
                    if (appController.CreateLocationInLocalDB(l)) count++;
                });

                incidentTypes.ForEach(i =>
                {
                    if (appController.CreateIncidentTypeInLocalDB(i)) count++;
                });
            }).ContinueWith(r2 =>
            {
                if (count == officers.Count + locations.Count + incidentTypes.Count) lblStatus.Text += " & Tables are Synced";

                logger.Info($"Local database {((count == officers.Count + locations.Count + incidentTypes.Count) ? "is" : "is not")} synchronized.");

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        #endregion


    }
}
