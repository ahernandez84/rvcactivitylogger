using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Timers;

using NLog;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using RVCActivityLogger.Controller;
using RVCActivityLogger.Models;
using RVCActivityLogger.Services;
using RVCActivityLogger.Views;

namespace RVCActivityLogger
{
    public struct SearchParameters
    {
        public string searchText;
        public DateTime? start;
        public DateTime? end;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private AppConfigService appConfig;
        private AppController appController;
        private Timer tmrRefreshGrid;

        private SearchParameters searchParams;

        public MainWindow()
        {
            InitializeComponent();

            tmrRefreshGrid = new Timer();
            tmrRefreshGrid.Elapsed += TmrRefreshGrid_Elapsed;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            logger.Info("Initializing application...");

            appConfig = new AppConfigService();
            appConfig.ReadSettings();

            appController = new AppController();
            appController.Initialize(appConfig.LogRowCount);

            SetAppSettings(appConfig);

            var pages = 0;
            var listOfLogs = new List<Log>();

            _ = Task.Run(() =>
            {
                pages = appController.GetLogPagingCounts();
                listOfLogs = appController.GetLogs();

            }).ContinueWith(r => {

                lblStatus.Text = "Central Database:  Online";

                logBadge.Badge = listOfLogs.Count;
                dgLogs.ItemsSource = listOfLogs;

                FormatDataGrid();

                EnableOrDisablePaging(pages);

                if (appConfig.EnableLogRefresh) tmrRefreshGrid.Start();

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Performing a search.");

            var listOfLogs = new List<Log>();

            if (string.IsNullOrEmpty(txtSearch.Text) && !startCal.SelectedDate.HasValue && !endCal.SelectedDate.HasValue)
            {
                logger.Info("Attempting a filter search.");

                EnableOrDisablePaging(appController.GetLogPagingCounts());
                listOfLogs = appController.GetLogs();
            }
            else
            {
                if ((startCal.SelectedDate.HasValue && !endCal.SelectedDate.HasValue) || (!startCal.SelectedDate.HasValue && endCal.SelectedDate.HasValue))
                {
                    await this.ShowMessageAsync("Incorrect Date Fields", "Please make sure both date fields are set and that the start date is older than or the same as the end date.");
                    return;
                }

                if ((startCal.SelectedDate.HasValue && endCal.SelectedDate.HasValue) && (startCal.SelectedDate.Value.Date > endCal.SelectedDate.Value.Date))
                {
                    await this.ShowMessageAsync("Incorrect Date Fields", "Please make sure both date fields are set and that the start date is older than or the same as the end date.");
                    return;
                }

                logger.Info("Attempting a filter search with start and end dates.");

                EnableOrDisablePaging(appController.GetLogPagingCounts(txtSearch.Text.Trim(), startCal.SelectedDate, endCal.SelectedDate));
                listOfLogs = appController.GetLogs(txtSearch.Text.Trim(), startCal.SelectedDate, endCal.SelectedDate);
            }

            searchParams.searchText = txtSearch.Text.Trim();
            searchParams.start = startCal.SelectedDate;
            searchParams.end = endCal.SelectedDate;

            logBadge.Badge = listOfLogs.Count;
            dgLogs.ItemsSource = listOfLogs;

            FormatDataGrid();
        }

        private void btnManageOfficers_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Managing Employees.");

            var employees = new Employees(appController);
            employees.ShowDialog();
        }

        private void btnManageLocations_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Managing Locations.");

            var locations = new Locations(appController);
            locations.ShowDialog();
        }

        private void btnManageIncidentTypes_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Managing Incident Types.");

            var incidentType = new IncidentTypes(appController);
            incidentType.ShowDialog();
        }

        private void dgLogs_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            logger.Info("Log was double clicked for editing.");

            var dg = sender as DataGrid;
            var selectedLog = dg.SelectedItem as Log;

            EditLog editLog = new EditLog(appController, selectedLog);
            editLog.ShowDialog();
        }

        private void TmrRefreshGrid_Elapsed(object sender, ElapsedEventArgs e)
        {
            var pages = appController.GetLogPagingCounts(logMethodCall: false);
            var listOfLogs = appController.GetLogs(logMethodCall: false);

            this.Dispatcher.Invoke((Action)delegate ()
            {
                if (!string.IsNullOrEmpty(txtSearch.Text) || startCal.SelectedDate.HasValue || endCal.SelectedDate.HasValue) return;

                logBadge.Badge = listOfLogs.Count;
                dgLogs.ItemsSource = listOfLogs;

                FormatDataGrid();

                EnableOrDisablePaging(pages);
            });
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("btnNext was clicked.");

            if (appController.Page < appController.Pages - 1)
            {
                appController.Page++;
            }

            var listOfLogs = appController.GetLogs(searchParams.searchText, searchParams.start, searchParams.end);

            if (appController.Page > 0) btnPrevious.IsEnabled = true;
            if (appController.Page == appController.Pages - 1) btnNext.IsEnabled = false;

            lblPagination.Content = $"{appController.Page + 1} of {appController.Pages}";

            logBadge.Badge = listOfLogs.Count;
            dgLogs.ItemsSource = listOfLogs;

            FormatDataGrid();
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("btnPrevious was clicked.");

            if (appController.Page > 0)
            {
                appController.Page--;
            }

            var listOfLogs = appController.GetLogs(searchParams.searchText, searchParams.start, searchParams.end);

            if (appController.Page == 0) btnPrevious.IsEnabled = false;
            if (appController.Page >= 0) btnNext.IsEnabled = true;

            lblPagination.Content = $"{appController.Page + 1} of {appController.Pages}";

            logBadge.Badge = listOfLogs.Count;
            dgLogs.ItemsSource = listOfLogs;

            FormatDataGrid();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            flyoutSettings.IsOpen = true;
        }

        private void btnReports_Click(object sender, RoutedEventArgs e)
        {
            if (CreateReportsFolder())
            {
                System.Diagnostics.Process.Start("explorer.exe", $@"{Environment.CurrentDirectory}\Reports");
            }
        }

        private void chkEnableLogRefresh_Checked(object sender, RoutedEventArgs e)
        {
            logger.Info($"EnableLogRefresh is set to {chkEnableLogRefresh.IsChecked.Value}");

            AppConfigService.SetSettingValue("EnableLogRefresh", chkEnableLogRefresh.IsChecked.Value ? "true" : "false");

            if (tmrRefreshGrid == null) return;

            tmrRefreshGrid.Enabled = chkEnableLogRefresh.IsChecked.Value;
        }

        private void numLogRefreshRate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            AppConfigService.SetSettingValue("LogRefreshRate", numLogRefreshRate.Value.ToString());

            tmrRefreshGrid.Interval = (int) numLogRefreshRate.Value * 1000;
        }

        private void numLogRowCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            AppConfigService.SetSettingValue("LogRowCount", numLogRowCount.Value.ToString());

            appController.SetLogRowCount((int)numLogRowCount.Value);
        }

        private async void btnExport_Click(object sender, RoutedEventArgs e)
        {
            var reportName = "Activity Logs";
            await this.ShowInputAsync("Generate Report", "Enter Report Name:")
                .ContinueWith(r =>
                {
                    reportName = r.Result;
                }, TaskScheduler.FromCurrentSynchronizationContext());

            if (CreateReportsFolder())
            {
                var results = appController.GetLogsForReport(txtSearch.Text.Trim(), startCal.SelectedDate, endCal.SelectedDate);
                if (results != null)
                {
                    if (ExcelService.GenerateReport($@"{Environment.CurrentDirectory}\Reports\{reportName}", reportName, results))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $@"{Environment.CurrentDirectory}\Reports");
                    }
                    else
                    {
                        await this.ShowMessageAsync("Export Logs", "Oops, something went wrong.  Please check the app log for more details.");
                    }
                }
                else
                {
                    await this.ShowMessageAsync("Export Logs", "Oops, something went wrong.  Please check the app log for more details.");
                }
            }
            else
            {
                await this.ShowMessageAsync("Export Logs", "Oops, something went wrong.  Please check the app log for more details.");
            }
        }

        #region supporting methods
        private void FormatDataGrid()
        {
            dgLogs.Columns[12].Visibility = Visibility.Collapsed;
            dgLogs.Columns[13].Visibility = Visibility.Collapsed;
            dgLogs.Columns[14].Visibility = Visibility.Collapsed;
            dgLogs.Columns[15].Visibility = Visibility.Collapsed;

            var sort1 = dgLogs.Columns[0];
            var sort2 = dgLogs.Columns[5];
            dgLogs.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(sort1.SortMemberPath, System.ComponentModel.ListSortDirection.Descending));
            dgLogs.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(sort2.SortMemberPath, System.ComponentModel.ListSortDirection.Descending));
        }

        private void dgLogs_AutoGeneratingColumn(object sender, System.Windows.Controls.DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(System.DateTime) && e.PropertyName.Equals("IncidentDate", StringComparison.OrdinalIgnoreCase))
                (e.Column as DataGridTextColumn).Binding.StringFormat = "MM/dd/yyyy";
            else if (e.PropertyType == typeof(System.DateTime))
                (e.Column as DataGridTextColumn).Binding.StringFormat = "hh:mm:ss tt";
        }

        private void EnableOrDisablePaging(int pages)
        {
            if (pages > 1)
            {
                spPagination.Visibility = Visibility.Visible;
                lblPagination.Content = $"{1} of {pages}";
                btnPrevious.IsEnabled = false;
                btnNext.IsEnabled = true;
            }
            else
            {
                spPagination.Visibility = Visibility.Collapsed;
            }
        }

        private void SetAppSettings(AppConfigService appConfig)
        {
            chkEnableLogRefresh.IsChecked = appConfig.EnableLogRefresh;
            numLogRefreshRate.Value = appConfig.LogRefreshRate / 1000;
            tmrRefreshGrid.Interval = appConfig.LogRefreshRate;
            numLogRowCount.Value = appConfig.LogRowCount;
        }

        private bool CreateReportsFolder()
        {
            try
            {
                if (!Directory.Exists($@"{Environment.CurrentDirectory}\Reports"))
                {
                    Directory.CreateDirectory($@"{Environment.CurrentDirectory}\Reports");
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "MainWindow <CreateReportsFolder> method."); return false; }
        }


        #endregion


    }
}
