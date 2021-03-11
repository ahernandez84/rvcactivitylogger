using System;
using System.Collections.Generic;
using System.Timers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using NLog;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using RVCOfficerLogger.Controller;
using RVCOfficerLogger.Models;

namespace RVCOfficerLogger.Views
{
    public struct SearchParameters
    {
        public string searchText;
        public DateTime? start;
        public DateTime? end;
    }

    /// <summary>
    /// Interaction logic for ViewLogs.xaml
    /// </summary>
    public partial class ViewLogs : MetroWindow
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private AppController appController;
        private Guid officerId;
        private List<Log> listOfLogs = new List<Log>();
        private Timer tmrCheckCentralDatabaseStatus;

        private bool allowToEditDuringShift = false;
        private bool refreshOnceOnCentralDBOffline = true;

        private Log startOfShift = null;
        private SearchParameters searchParams;

        public ViewLogs(AppController appController, Guid officerId)
        {
            InitializeComponent();

            tmrCheckCentralDatabaseStatus = new Timer();
            tmrCheckCentralDatabaseStatus.Elapsed += TmrCheckCentralDatabaseStatus_Elapsed;
            tmrCheckCentralDatabaseStatus.Interval = 5000;

            this.appController = appController;
            this.officerId = officerId;

            startCal.SelectedDate = endCal.SelectedDate = DateTime.Now;

            if (appController.IsCentralDatabaseOffline) lblOffline.Visibility = Visibility.Visible;
        }

        private void ViewLogWin_Loaded(object sender, RoutedEventArgs e)
        {
            var pages = 0;
            _ = Task.Run(() =>
            {
                pages = appController.GetLogPagingCounts("", DateTime.Now, DateTime.Now);
                listOfLogs = appController.GetLogs("", DateTime.Now, DateTime.Now);

                searchParams.start = searchParams.end = DateTime.Now;

                startOfShift = listOfLogs.Find(f => f.IncidentType.Equals("10-41", StringComparison.OrdinalIgnoreCase) && f.EmployeeId == officerId && f.IncidentDate.Date == DateTime.Now.Date);
                if (startOfShift != null)
                {
                    var endOfShift = listOfLogs.Find(f => f.IncidentType.Equals("10-42", StringComparison.OrdinalIgnoreCase) && f.EmployeeId == officerId && f.IncidentDate.Date >= startOfShift.IncidentDate.Date && f.StartTime >= startOfShift.StartTime);
                    if (endOfShift == null)
                    {
                        allowToEditDuringShift = true;
                    }
                }

            }).ContinueWith(r => {

                logBadge.Badge = listOfLogs.Count;
                dgLogs.ItemsSource = listOfLogs;

                FormatDataGrid();

                EnableOrDisablePaging(pages);

                if (allowToEditDuringShift)
                {
                    lblCanEdit.Content = "Can I edit logs right now?  Yes, but only during your shift.";
                    lblCanEdit.Foreground = System.Windows.Media.Brushes.ForestGreen;
                }
                else
                {
                    lblCanEdit.Foreground = System.Windows.Media.Brushes.Red;
                }

                tmrCheckCentralDatabaseStatus.Start();

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtSearch.Text) && !startCal.SelectedDate.HasValue && !endCal.SelectedDate.HasValue)
            {
                logger.Info("Attempting a filter search.");

                EnableOrDisablePaging(appController.GetLogPagingCounts(txtSearch.Text.Trim()));
                listOfLogs = appController.GetLogs(txtSearch.Text.Trim());
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

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (appController.Page > 0)
            {
                appController.Page--;
            }

            listOfLogs = appController.GetLogs(searchParams.searchText, searchParams.start, searchParams.end);

            if (appController.Page == 0) btnPrevious.IsEnabled = false;
            if (appController.Page >= 0) btnNext.IsEnabled = true;

            lblPagination.Content = $"{appController.Page + 1} of {appController.Pages}";

            logBadge.Badge = listOfLogs.Count;
            dgLogs.ItemsSource = listOfLogs;

            FormatDataGrid();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (appController.Page < appController.Pages - 1)
            {
                appController.Page++;
            }

            listOfLogs = appController.GetLogs(searchParams.searchText, searchParams.start, searchParams.end);

            if (appController.Page > 0) btnPrevious.IsEnabled = true;
            if (appController.Page == appController.Pages - 1) btnNext.IsEnabled = false;

            lblPagination.Content = $"{appController.Page + 1} of {appController.Pages}";

            logBadge.Badge = listOfLogs.Count;
            dgLogs.ItemsSource = listOfLogs;

            FormatDataGrid();
        }

        private void cbViewOnlyMine_Checked(object sender, RoutedEventArgs e)
        {
            if (cbViewOnlyMine.IsChecked.Value)
            {
                var myLogs = listOfLogs.FindAll(f => f.EmployeeId.Equals(officerId));

                logBadge.Badge = myLogs.Count;
                dgLogs.ItemsSource = myLogs;

                FormatDataGrid();
            }
        }

        private void cbViewOnlyMine_Unchecked(object sender, RoutedEventArgs e)
        {
            logBadge.Badge = listOfLogs.Count;
            dgLogs.ItemsSource = listOfLogs;

            FormatDataGrid();
        }

        private async void dgLogs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dg = sender as DataGrid;
            var selectedLog = dg.SelectedItem as Log;

            if (!allowToEditDuringShift || selectedLog.EmployeeId != officerId || (selectedLog.IncidentDate.Date <= startOfShift.IncidentDate.Date && selectedLog.StartTime < startOfShift.StartTime))
            {
                await this.ShowMessageAsync("Edit Logs", $"You can only edit your own logs during your shift.{(allowToEditDuringShift ? "" : "  Please enter a 10-41.")}");
                return;
            }

            EditLog editLog = new EditLog(appController, selectedLog);
            editLog.ShowDialog();
        }

        private void TmrCheckCentralDatabaseStatus_Elapsed(object sender, ElapsedEventArgs e)
        {
            tmrCheckCentralDatabaseStatus.Stop();

            if (appController != null)
            {
                if (appController.IsCentralDatabaseOffline)
                {
                    if (refreshOnceOnCentralDBOffline)
                    {
                        refreshOnceOnCentralDBOffline = false;

                        var listOfLogs = appController.GetLogs("", DateTime.Now, DateTime.Now);

                        this.Dispatcher.Invoke((Action)delegate ()
                        {
                            lblOffline.Visibility = Visibility.Visible;

                            dgLogs.ItemsSource = listOfLogs;
                            FormatDataGrid();
                        });
                    }
                }
                else
                {
                    refreshOnceOnCentralDBOffline = true;

                    this.Dispatcher.Invoke((Action)delegate ()
                    {
                        lblOffline.Visibility = Visibility.Collapsed;
                    });
                }
            }

            tmrCheckCentralDatabaseStatus.Start();
        }

        #region supporting methods
        private void FormatDataGrid()
        {
            dgLogs.Columns[12].Visibility = Visibility.Collapsed;
            dgLogs.Columns[13].Visibility = Visibility.Collapsed;
            dgLogs.Columns[14].Visibility = Visibility.Collapsed;
            dgLogs.Columns[15].Visibility = Visibility.Collapsed;
            dgLogs.Columns[16].Visibility = Visibility.Collapsed;

            var sort1 = dgLogs.Columns[0];
            var sort2 = dgLogs.Columns[5];
            dgLogs.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(sort1.SortMemberPath, System.ComponentModel.ListSortDirection.Descending));
            dgLogs.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(sort2.SortMemberPath, System.ComponentModel.ListSortDirection.Descending));
        }

        private void dgLogs_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
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

        #endregion

    }
}
