﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TheAirline.GraphicsModel.Converters;
using TheAirline.GraphicsModel.PageModel.GeneralModel;
using TheAirline.GraphicsModel.UserControlModel.MessageBoxModel;
using TheAirline.Model.AirlinerModel;
using TheAirline.Model.AirlinerModel.RouteModel;
using TheAirline.Model.GeneralModel;
using TheAirline.Model.GeneralModel.Helpers;
using Xceed.Wpf.Toolkit;

namespace TheAirline.GraphicsModel.UserControlModel.PopUpWindowsModel
{
    /// <summary>
    /// Interaction logic for PopUpAirlinerAutoRoutes.xaml
    /// </summary>
    public partial class PopUpAirlinerAutoRoutes : PopUpWindow
    {
        private enum FlightInterval { Daily, Weekly }
        private FlightInterval Interval;

        private FleetAirliner Airliner;
        private ListBox lbFlights;
        
        private Dictionary<Route, List<RouteTimeTableEntry>> Entries;
        private Dictionary<Route, List<RouteTimeTableEntry>> EntriesToDelete;

        private ComboBox cbRoute, cbFlightsPerDay, cbFlightsPerWeek, cbFlightCode, cbRegion, cbDelayMinutes, cbStartTime;
        private CheckBox cbBusinessRoute;

        private double maxBusinessRouteTime = new TimeSpan(2, 0, 0).TotalMinutes;

        public static object ShowPopUp(FleetAirliner airliner)
        {
            PopUpWindow window = new PopUpAirlinerAutoRoutes(airliner);
            window.ShowDialog();

            return window.Selected;
        }
        public PopUpAirlinerAutoRoutes(FleetAirliner airliner)
        {
            this.Entries = new Dictionary<Route, List<RouteTimeTableEntry>>();
            this.EntriesToDelete = new Dictionary<Route, List<RouteTimeTableEntry>>();

            this.Airliner = airliner;
       
            InitializeComponent();

            this.Title = this.Airliner.Name;

            this.Width = 1200;

            this.Height = 325;
          
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            ScrollViewer scroller = new ScrollViewer();
            //scroller.Margin = new Thickness(10, 10, 10, 10);
            scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroller.MaxHeight = this.Height;

            StackPanel mainPanel = new StackPanel();
            mainPanel.Margin = new Thickness(10, 10, 10, 10);
            scroller.Content = mainPanel;

            Grid grdFlights = UICreator.CreateGrid(2);
            grdFlights.ColumnDefinitions[1].Width = new GridLength(200);
            mainPanel.Children.Add(grdFlights);

            lbFlights = new ListBox();
            lbFlights.ItemContainerStyleSelector = new ListBoxItemStyleSelector();
            lbFlights.SetResourceReference(ListBox.ItemTemplateProperty, "QuickInfoItem");
            lbFlights.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

            Grid.SetColumn(lbFlights, 0);
            grdFlights.Children.Add(lbFlights);

            ScrollViewer panelRoutes = createRoutesPanel();

            Grid.SetColumn(panelRoutes, 1);
            grdFlights.Children.Add(panelRoutes);

            mainPanel.Children.Add(createAutoGeneratePanel());
            mainPanel.Children.Add(createButtonsPanel());

            this.Content = scroller;

            showFlights();
        }
        //creates the panel for the routes
        private ScrollViewer createRoutesPanel()
        {
            ScrollViewer scroller = new ScrollViewer();
            scroller.MaxHeight = 200;
            scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            StackPanel panelRoutes = new StackPanel();
            panelRoutes.Margin = new Thickness(5, 0, 0, 0);

            long requiredRunway = this.Airliner.Airliner.Type.MinRunwaylength;

            var routes = this.Airliner.Airliner.Airline.Routes.FindAll(r => this.Airliner.Airliner.Type.Range > r.getDistance() && !r.Banned && r.Destination1.getMaxRunwayLength() >= requiredRunway && r.Destination2.getMaxRunwayLength() >= requiredRunway);
            foreach (Route route in routes)
            {
                Border brdRoute = new Border();
                brdRoute.BorderBrush = Brushes.Black;
                brdRoute.BorderThickness = new Thickness(1);
                brdRoute.Margin = new Thickness(0, 0, 0, 2);
                brdRoute.Width = 150;
                brdRoute.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

                ContentControl ccRoute = new ContentControl();
                ccRoute.ContentTemplate = this.Resources["RouteItem"] as DataTemplate;
                ccRoute.Content = route;

                brdRoute.Child = ccRoute;

                panelRoutes.Children.Add(brdRoute);
            }

            scroller.Content = panelRoutes;

            return scroller;

        }
        //creates the panel for a route for a day
        private Border createRoutePanel(DayOfWeek day)
        {
            int hourFactor = 1;

            Border brdDay = new Border();
            brdDay.BorderThickness = new Thickness(1, 1, 1, 1);
            brdDay.BorderBrush = Brushes.Black;

            Canvas cnvFlights = new Canvas();
            cnvFlights.Width = 24 * 60 / hourFactor;
            cnvFlights.Height = 20;
            brdDay.Child = cnvFlights;

            List<RouteTimeTableEntry> entries = this.Airliner.Routes.SelectMany(r => r.TimeTable.Entries.FindAll(e => e.Airliner == this.Airliner && e.Day == day)).ToList(); //&& e.Time.Hours<12)).ToList();
            entries.AddRange(this.Entries.Keys.SelectMany(r => this.Entries[r].FindAll(e => e.Day == day)));
            entries.RemoveAll(e => this.EntriesToDelete.Keys.SelectMany(r => this.EntriesToDelete[r]).ToList().Find(te => te == e) == e);

            foreach (RouteTimeTableEntry e in entries)
            {
                double maxTime = new TimeSpan(24, 0, 0).Subtract(e.Time).TotalMinutes;

                TimeSpan flightTime = e.TimeTable.Route.getFlightTime(this.Airliner.Airliner.Type);

                ContentControl ccFlight = new ContentControl();
                ccFlight.ContentTemplate = this.Resources["FlightItem"] as DataTemplate;
                ccFlight.Content = new KeyValuePair<double, RouteTimeTableEntry>(Math.Min(flightTime.TotalMinutes / hourFactor, maxTime / hourFactor), e);
                int hours = e.Time.Hours;

                Canvas.SetLeft(ccFlight, 60 * hours / hourFactor + e.Time.Minutes / hourFactor);

                cnvFlights.Children.Add(ccFlight);
            }

            List<RouteTimeTableEntry> dayBeforeEntries = this.Airliner.Routes.SelectMany(r => r.TimeTable.Entries.FindAll(e => e.Airliner == this.Airliner && (e.Day == day - 1 || ((int)day) == 0 && ((int)e.Day) == 6))).ToList();
            dayBeforeEntries.AddRange(this.Entries.Keys.SelectMany(r => this.Entries[r].FindAll(e => e.Day == day - 1 || ((int)day) == 0 && ((int)e.Day) == 6)));
            dayBeforeEntries.RemoveAll(e => this.EntriesToDelete.Keys.SelectMany(r => this.EntriesToDelete[r]).ToList().Find(te => te == e) == e);

            foreach (RouteTimeTableEntry e in dayBeforeEntries)
            {
                TimeSpan flightTime = e.TimeTable.Route.getFlightTime(this.Airliner.Airliner.Type);

                TimeSpan endTime = e.Time.Add(flightTime);
                if (endTime.Days == 1)
                {
                    ContentControl ccFlight = new ContentControl();
                    ccFlight.ContentTemplate = this.Resources["FlightItem"] as DataTemplate;
                    ccFlight.Content = new KeyValuePair<double, RouteTimeTableEntry>(endTime.Subtract(new TimeSpan(1, 0, 0, 0)).TotalMinutes / hourFactor, e);

                    Canvas.SetLeft(ccFlight, 0);

                    cnvFlights.Children.Add(ccFlight);
                }
            }

            return brdDay;
        }
        //creates the time indicator header
        private WrapPanel createTimeHeaderPanel()
        {
            string format = System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.PMDesignator == "" ? "HH" : "hh";
            int hourFactor = 1;

            WrapPanel panelHeader = new WrapPanel();
            for (int i = 0; i < 24; i++)
            {
                Border brdHour = new Border();
                brdHour.BorderThickness = new Thickness(1, 0, 1, 0);
                brdHour.BorderBrush = Brushes.Black;

                TextBlock txtHour = new TextBlock();
                txtHour.Text = string.Format("{0}-{1}", new DateTime(2000, 1, 1, i, 0, 0).ToString(format), new DateTime(2000, 1, 1, i + 1 == 24 ? 0 : i + 1, 0, 0).ToString(format));
                txtHour.FontWeight = FontWeights.Bold;
                txtHour.Width = (60 / hourFactor) - 2;
                txtHour.TextAlignment = TextAlignment.Center;

                brdHour.Child = txtHour;
                panelHeader.Children.Add(brdHour);
            }

            return panelHeader;
        }
        //creates the buttons panel
        private WrapPanel createButtonsPanel()
        {
            WrapPanel buttonsPanel = new WrapPanel();
            buttonsPanel.Margin = new Thickness(0, 10, 0, 0);

            Button btnOk = new Button();
            btnOk.Uid = "100";
            btnOk.SetResourceReference(Button.StyleProperty, "RoundedButton");
            btnOk.Height = Double.NaN;
            btnOk.Width = Double.NaN;
            btnOk.Content = Translator.GetInstance().GetString("General", btnOk.Uid);
            btnOk.Click += new RoutedEventHandler(btnOk_Click);
            btnOk.SetResourceReference(Button.BackgroundProperty, "ButtonBrush");

            buttonsPanel.Children.Add(btnOk);

            Button btnCancel = new Button();
            btnCancel.Uid = "101";
            btnCancel.SetResourceReference(Button.StyleProperty, "RoundedButton");
            btnCancel.Height = Double.NaN;
            btnCancel.Margin = new Thickness(5, 0, 0, 0);
            btnCancel.Width = Double.NaN;
            btnCancel.Click += new RoutedEventHandler(btnCancel_Click);
            btnCancel.Content = Translator.GetInstance().GetString("General", btnCancel.Uid);
            btnCancel.SetResourceReference(Button.BackgroundProperty, "ButtonBrush");

            buttonsPanel.Children.Add(btnCancel);

            Button btnAdd = new Button();
            btnAdd.Uid = "104";
            btnAdd.SetResourceReference(Button.StyleProperty, "RoundedButton");
            btnAdd.Height = Double.NaN;
            btnAdd.Width = Double.NaN;
            btnAdd.Click += new RoutedEventHandler(btnAdd_Click);
            btnAdd.Margin = new Thickness(5, 0, 0, 0);
            btnAdd.Content = Translator.GetInstance().GetString("General", btnAdd.Uid);
            btnAdd.IsEnabled = cbRoute.Items.Count > 0;
            btnAdd.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            btnAdd.SetResourceReference(Button.BackgroundProperty, "ButtonBrush");

            buttonsPanel.Children.Add(btnAdd);

            Button btnUndo = new Button();
            btnUndo.Uid = "103";
            btnUndo.SetResourceReference(Button.StyleProperty, "RoundedButton");
            btnUndo.Height = Double.NaN;
            btnUndo.Width = Double.NaN;
            btnUndo.Margin = new Thickness(5, 0, 0, 0);
            btnUndo.Content = Translator.GetInstance().GetString("General", btnUndo.Uid);
            btnUndo.SetResourceReference(Button.BackgroundProperty, "ButtonBrush");
            btnUndo.Click += new RoutedEventHandler(btnUndo_Click);

            buttonsPanel.Children.Add(btnUndo);

            Button btnClear = new Button();
            btnClear.Uid = "108";
            btnClear.SetResourceReference(Button.StyleProperty, "RoundedButton");
            btnClear.Height = Double.NaN;
            btnClear.Width = Double.NaN;
            btnClear.Margin = new Thickness(5, 0, 0, 0);
            btnClear.Content = Translator.GetInstance().GetString("General", btnClear.Uid);
            btnClear.Click += new RoutedEventHandler(btnClear_Click);
            btnClear.SetResourceReference(Button.BackgroundProperty, "ButtonBrush");

            buttonsPanel.Children.Add(btnClear);


            Button btnTransfer = new Button();
            btnTransfer.Uid = "1000";
            btnTransfer.SetResourceReference(Button.StyleProperty, "RoundedButton");
            btnTransfer.Height = Double.NaN;
            btnTransfer.Width = Double.NaN;
            btnTransfer.Visibility = getTransferAirliners().Count > 0 && this.Airliner.Routes.Count == 0 ? Visibility.Visible : System.Windows.Visibility.Collapsed;
            btnTransfer.SetResourceReference(Button.BackgroundProperty, "ButtonBrush");
            btnTransfer.Content = Translator.GetInstance().GetString("PopUpAirlinerRoutes", btnTransfer.Uid);
            btnTransfer.Click += new RoutedEventHandler(btnTransfer_Click);
            btnTransfer.Margin = new Thickness(5, 0, 0, 0);

            buttonsPanel.Children.Add(btnTransfer);


            return buttonsPanel;
        }
        //creates the panel for auto generation of time table from a route
        private WrapPanel createAutoGeneratePanel()
        {
            WrapPanel autogeneratePanel = new WrapPanel();
            autogeneratePanel.Margin = new Thickness(0, 5, 0, 0);

            cbRegion = new ComboBox();
            cbRegion.SetResourceReference(ComboBox.StyleProperty, "ComboBoxTransparentStyle");
            cbRegion.Width = 150;
            cbRegion.DisplayMemberPath = "Name";
            cbRegion.SelectedValuePath = "Name";
            cbRegion.SelectionChanged += cbRegion_SelectionChanged;
            cbRegion.Items.Add(Regions.GetRegion("100"));
            cbRegion.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;

            List<Region> regions = GameObject.GetInstance().HumanAirline.Routes.Where(r => r.Destination1.Profile.Country.Region == r.Destination2.Profile.Country.Region).Select(r => r.Destination1.Profile.Country.Region).ToList();
            regions.AddRange(GameObject.GetInstance().HumanAirline.Routes.Where(r => r.Destination1.Profile.Country == GameObject.GetInstance().HumanAirline.Profile.Country).Select(r => r.Destination2.Profile.Country.Region));
            regions.AddRange(GameObject.GetInstance().HumanAirline.Routes.Where(r => r.Destination2.Profile.Country == GameObject.GetInstance().HumanAirline.Profile.Country).Select(r => r.Destination1.Profile.Country.Region));


            foreach (Region region in regions.Distinct())
                cbRegion.Items.Add(region);

            autogeneratePanel.Children.Add(cbRegion);

            cbRoute = new ComboBox();
            cbRoute.SetResourceReference(ComboBox.StyleProperty, "ComboBoxTransparentStyle");
            cbRoute.SelectionChanged += new SelectionChangedEventHandler(cbAutoRoute_SelectionChanged);
            cbRoute.ItemTemplate = this.Resources["SelectRouteItem"] as DataTemplate;
            cbRoute.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;

            foreach (Route route in this.Airliner.Airliner.Airline.Routes.FindAll(r => this.Airliner.Airliner.Type.Range > r.getDistance() && !r.Banned))
            {
                cbRoute.Items.Add(route);
            }

            autogeneratePanel.Children.Add(cbRoute);

            cbFlightCode = new ComboBox();
            cbFlightCode.SetResourceReference(ComboBox.StyleProperty, "ComboBoxTransparentStyle");
            cbFlightCode.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;

            foreach (string flightCode in this.Airliner.Airliner.Airline.getFlightCodes())
                cbFlightCode.Items.Add(flightCode);

            cbFlightCode.Items.RemoveAt(cbFlightCode.Items.Count - 1);

            cbFlightCode.SelectedIndex = 0;

            autogeneratePanel.Children.Add(cbFlightCode);

            StackPanel panelFlightInterval = new StackPanel();
            panelFlightInterval.Margin = new Thickness(10, 0, 0, 0);

            RadioButton rbDailyFlights = new RadioButton();
            rbDailyFlights.Content = Translator.GetInstance().GetString("PopUpAirlinerAutoRoutes", "1002");
            rbDailyFlights.GroupName = "Interval";
            rbDailyFlights.IsChecked = true;
            rbDailyFlights.Checked += rbDailyFlights_Checked;

            panelFlightInterval.Children.Add(rbDailyFlights);

            RadioButton rbWeeklyFlights = new RadioButton();
            rbWeeklyFlights.Content = Translator.GetInstance().GetString("PopUpAirlinerAutoRoutes", "1005");
            rbWeeklyFlights.GroupName = "Interval";
            rbWeeklyFlights.Checked += rbWeeklyFlights_Checked;

            panelFlightInterval.Children.Add(rbWeeklyFlights);

            autogeneratePanel.Children.Add(panelFlightInterval);
                         
            cbFlightsPerDay = new ComboBox();
            cbFlightsPerDay.SetResourceReference(ComboBox.StyleProperty, "ComboBoxTransparentStyle");
            cbFlightsPerDay.SelectionChanged += cbFlightsPerDay_SelectionChanged;
            cbFlightsPerDay.Margin = new Thickness(5, 0, 0, 0);
            cbFlightsPerDay.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;

            autogeneratePanel.Children.Add(cbFlightsPerDay);

            this.Interval = FlightInterval.Daily;  

            cbFlightsPerWeek = new ComboBox();
            cbFlightsPerWeek.SetResourceReference(ComboBox.StyleProperty, "ComboBoxTransparentStyle");
            cbFlightsPerWeek.Visibility = System.Windows.Visibility.Collapsed;
            cbFlightsPerWeek.Margin = new Thickness(5, 0, 0, 0);
            cbFlightsPerWeek.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;

            for (int i = 1; i < 7; i++)
                cbFlightsPerWeek.Items.Add(i);

            cbFlightsPerWeek.SelectedIndex = 0;

            autogeneratePanel.Children.Add(cbFlightsPerWeek);

            TextBlock txtDelayMinutes = UICreator.CreateTextBlock(Translator.GetInstance().GetString("PopUpAirlinerAutoRoutes", "1003"));
            txtDelayMinutes.Margin = new Thickness(10, 0, 0, 0);
            txtDelayMinutes.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;

            autogeneratePanel.Children.Add(txtDelayMinutes);

            cbDelayMinutes = new ComboBox();
            cbDelayMinutes.SetResourceReference(ComboBox.StyleProperty,"ComboBoxTransparentStyle");
            cbDelayMinutes.SelectionChanged += cbDelayMinutes_SelectionChanged;
            cbDelayMinutes.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Right;
            cbDelayMinutes.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            

            autogeneratePanel.Children.Add(cbDelayMinutes);

            TextBlock txtStartTime = UICreator.CreateTextBlock(Translator.GetInstance().GetString("PopUpAirlinerAutoRoutes","1004"));
            txtStartTime.Margin = new Thickness(10,0,5,0);
            txtStartTime.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;

            autogeneratePanel.Children.Add(txtStartTime);

            cbStartTime = new ComboBox();
            cbStartTime.SetResourceReference(ComboBox.StyleProperty,"ComboBoxTransparentStyle");
            cbStartTime.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            cbStartTime.SetResourceReference(ComboBox.ItemTemplateProperty, "TimeSpanItem");
                     
            autogeneratePanel.Children.Add(cbStartTime);

            cbBusinessRoute = new CheckBox();
            cbBusinessRoute.FlowDirection = System.Windows.FlowDirection.RightToLeft;
            cbBusinessRoute.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            cbBusinessRoute.Unchecked += cbBusinessRoute_Unchecked;
            cbBusinessRoute.Checked += cbBusinessRoute_Checked;
            cbBusinessRoute.Content = Translator.GetInstance().GetString("PopUpAirlinerAutoRoutes","1001");
            cbBusinessRoute.Margin = new Thickness(5, 0, 0, 0);

            autogeneratePanel.Children.Add(cbBusinessRoute);

            cbRegion.SelectedIndex = 0;

            return autogeneratePanel;


        }

        private void rbWeeklyFlights_Checked(object sender, RoutedEventArgs e)
        {
            cbFlightsPerWeek.Visibility = System.Windows.Visibility.Visible;
            cbFlightsPerDay.Visibility = System.Windows.Visibility.Collapsed;

            cbFlightsPerDay.SelectedIndex = 0;

            this.Interval = FlightInterval.Weekly;
        }

        private void rbDailyFlights_Checked(object sender, RoutedEventArgs e)
        {
            cbFlightsPerDay.Visibility = System.Windows.Visibility.Visible;
            cbFlightsPerWeek.Visibility = System.Windows.Visibility.Collapsed;

            this.Interval = FlightInterval.Daily;
        }

       
        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            ComboBox cbAirliners = new ComboBox();
            cbAirliners.SetResourceReference(ComboBox.StyleProperty, "ComboBoxTransparentStyle");
            cbAirliners.SelectedValuePath = "Name";
            cbAirliners.DisplayMemberPath = "Name";
            cbAirliners.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            cbAirliners.Width = 200;

            foreach (FleetAirliner airliner in getTransferAirliners())
                cbAirliners.Items.Add(airliner);

            cbAirliners.SelectedIndex = 0;

            if (PopUpSingleElement.ShowPopUp(Translator.GetInstance().GetString("PopUpAirlinerRoutes", "1001"), cbAirliners) == PopUpSingleElement.ButtonSelected.OK && cbAirliners.SelectedItem != null)
            {
                FleetAirliner transferAirliner = (FleetAirliner)cbAirliners.SelectedItem;

                foreach (Route route in transferAirliner.Routes)
                {
                    foreach (RouteTimeTableEntry entry in route.TimeTable.Entries.FindAll(en => en.Airliner == transferAirliner))
                    {
                        entry.Airliner = this.Airliner;
                    }
                    this.Airliner.addRoute(route);
                }
                transferAirliner.Routes.Clear();

                showFlights();
            }
        }

        private void cbRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            long requiredRunway = this.Airliner.Airliner.Type.MinRunwaylength;

            Region region = (Region)cbRegion.SelectedItem;

            cbRoute.Items.Clear();

            if (region.Uid == "100")
            {
                foreach (Route route in this.Airliner.Airliner.Airline.Routes.FindAll(r => this.Airliner.Airliner.Type.Range > r.getDistance() && !r.Banned && r.Destination1.getMaxRunwayLength() >= requiredRunway && r.Destination2.getMaxRunwayLength() >= requiredRunway).OrderBy(r => new AirportCodeConverter().Convert(r.Destination1)).ThenBy(r => new AirportCodeConverter().Convert(r.Destination2)))
                {
                    cbRoute.Items.Add(route);
                }
            }
            else
            {
                var routes = this.Airliner.Airliner.Airline.Routes.FindAll(r => this.Airliner.Airliner.Type.Range > r.getDistance() && !r.Banned && r.Destination1.getMaxRunwayLength() >= requiredRunway && r.Destination2.getMaxRunwayLength() >= requiredRunway && ((r.Destination1.Profile.Country.Region == region && r.Destination2.Profile.Country.Region == GameObject.GetInstance().HumanAirline.Profile.Country.Region) || (r.Destination2.Profile.Country.Region == region && r.Destination1.Profile.Country.Region == GameObject.GetInstance().HumanAirline.Profile.Country.Region) || (r.Destination1.Profile.Country.Region == region && r.Destination2.Profile.Country.Region == region))).OrderBy(r => new AirportCodeConverter().Convert(r.Destination1)).ThenBy(r => new AirportCodeConverter().Convert(r.Destination2));

                foreach (Route route in routes)
                    cbRoute.Items.Add(route);
            }
            cbRoute.SelectedIndex = 0;

        }
        private void cbAutoRoute_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            Route route = (Route)cbRoute.SelectedItem;

            if (route != null)
            {
                TimeSpan routeFlightTime = route.getFlightTime(this.Airliner.Airliner.Type);

                TimeSpan minFlightTime = routeFlightTime.Add(FleetAirlinerHelpers.GetMinTimeBetweenFlights(this.Airliner));

                cbDelayMinutes.Items.Clear();

                int minDelayMinutes = (int)FleetAirlinerHelpers.GetMinTimeBetweenFlights(this.Airliner).TotalMinutes;

                for (int i=minDelayMinutes;i<minDelayMinutes+120;i+=15)
                    cbDelayMinutes.Items.Add(i);

                cbDelayMinutes.SelectedIndex = 0;
                
                cbBusinessRoute.Visibility = minFlightTime.TotalMinutes <= maxBusinessRouteTime ? Visibility.Visible : System.Windows.Visibility.Collapsed;

                if (minFlightTime.TotalMinutes > maxBusinessRouteTime)
                    cbBusinessRoute.IsChecked = false;

            }


        }
        
        private void cbDelayMinutes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             Route route = (Route)cbRoute.SelectedItem;

             if (route != null && cbDelayMinutes.SelectedItem != null)
             {
                  TimeSpan routeFlightTime = route.getFlightTime(this.Airliner.Airliner.Type);

                  int delayMinutes = (int)cbDelayMinutes.SelectedItem;

                  TimeSpan minFlightTime = routeFlightTime.Add(new TimeSpan(0,delayMinutes,0));

                  int maxHours = 22 - 06; //from 06.00 to 22.00

                  int flightsPerDay = Convert.ToInt16(maxHours * 60 / (2 * minFlightTime.TotalMinutes));

                  cbFlightsPerDay.Items.Clear();

                  for (int i = 0; i < Math.Max(1, flightsPerDay); i++)
                      cbFlightsPerDay.Items.Add(i + 1);

                  cbFlightsPerDay.SelectedIndex = 0;


              }

         }
         private void cbFlightsPerDay_SelectionChanged(object sender, SelectionChangedEventArgs e)
         {
              Route route = (Route)cbRoute.SelectedItem;

              if (route != null && cbFlightsPerDay.SelectedItem != null)
              {                           
                  int latestStartTime = 22;

                  TimeSpan routeFlightTime = route.getFlightTime(this.Airliner.Airliner.Type);

                  int delayMinutes = (int)cbDelayMinutes.SelectedItem;

                  TimeSpan minFlightTime = routeFlightTime.Add(new TimeSpan(0, delayMinutes, 0));

                  int flightsPerDay = (int)cbFlightsPerDay.SelectedItem;

                  int lastDepartureHour = (int)(latestStartTime - (minFlightTime.TotalHours * flightsPerDay*2));

                  cbStartTime.Items.Clear();
                 
                  for (int i = 6; i <= Math.Max(6,lastDepartureHour); i++)
                      cbStartTime.Items.Add(new TimeSpan(i,0,0));

                  cbStartTime.SelectedIndex = cbStartTime.Items.Count / 2;
              }
         }
         //clears the time table
         private void clearTimeTable()
         {
             this.Entries.Clear();

             foreach (Route r in this.Airliner.Routes)
             {
                 foreach (RouteTimeTableEntry entry in r.TimeTable.Entries)
                 {
                     if (!this.EntriesToDelete.ContainsKey(r))
                     {
                         this.EntriesToDelete.Add(r, new List<RouteTimeTableEntry>());
                         this.EntriesToDelete[r].Add(entry);
                     }
                     else
                     {
                         if (!this.EntriesToDelete[r].Contains(entry))
                             this.EntriesToDelete[r].Add(entry);
                     }
                 }

             }
         }
         //shows the flights for the airliner
         private void showFlights()
         {
             lbFlights.Items.Clear();

             lbFlights.Items.Add(new QuickInfoValue("Day", createTimeHeaderPanel()));

             foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
             {
                 lbFlights.Items.Add(new QuickInfoValue(day.ToString(), createRoutePanel(day)));

             }
         }
         private void btnOk_Click(object sender, RoutedEventArgs e)
         {
             foreach (Route route in this.Entries.Keys)
             {
                 foreach (RouteTimeTableEntry entry in this.Entries[route])
                     route.TimeTable.addEntry(entry);

                 if (!this.Airliner.Routes.Contains(route))
                     this.Airliner.addRoute(route);
             }
             foreach (Route route in this.EntriesToDelete.Keys)
             {
                 foreach (RouteTimeTableEntry entry in this.EntriesToDelete[route])
                     route.TimeTable.removeEntry(entry);

                 if (route.TimeTable.getEntries(this.Airliner).Count == 0)
                     this.Airliner.removeRoute(route);

             }

             this.Close();
         }
         private void btnClear_Click(object sender, RoutedEventArgs e)
         {
             clearTimeTable();

             showFlights();

         }
         private void btnUndo_Click(object sender, RoutedEventArgs e)
         {
             this.Entries.Clear();

             this.EntriesToDelete.Clear();

             showFlights();
         }
         private void btnCancel_Click(object sender, RoutedEventArgs e)
         {
             this.Selected = null;
             this.Close();
         }

         private void btnAdd_Click(object sender, RoutedEventArgs e)
         {

             Route route = (Route)cbRoute.SelectedItem;

             RouteTimeTable rt;
            
            
            int flightsPerDay = (int)cbFlightsPerDay.SelectedItem;
            int flightsPerWeek = (int)cbFlightsPerWeek.SelectedItem;
             int delayMinutes = (int)cbDelayMinutes.SelectedItem;
             TimeSpan startTime = (TimeSpan)cbStartTime.SelectedItem;

             string flightcode1 = cbFlightCode.SelectedItem.ToString();
             string flightcode2 = this.Airliner.Airliner.Airline.getFlightCodes()[this.Airliner.Airliner.Airline.getFlightCodes().IndexOf(flightcode1) + 1];
            
             if (flightsPerDay > 0)
             {
                 if (cbBusinessRoute.IsChecked.Value)
                 {
                     flightsPerDay = (int)(route.getFlightTime(this.Airliner.Airliner.Type).Add(FleetAirlinerHelpers.GetMinTimeBetweenFlights(this.Airliner)).TotalMinutes / 2 / maxBusinessRouteTime);
                     rt = AIHelpers.CreateBusinessRouteTimeTable(route, this.Airliner, Math.Max(1, flightsPerDay), flightcode1, flightcode2);
                 }
                 else
                 {
                     if (this.Interval == FlightInterval.Daily)
                         rt = AIHelpers.CreateAirlinerRouteTimeTable(route, this.Airliner, flightsPerDay,true, delayMinutes, startTime, flightcode1, flightcode2);
                     else
                         rt = AIHelpers.CreateAirlinerRouteTimeTable(route, this.Airliner, flightsPerWeek,false, delayMinutes, startTime, flightcode1, flightcode2);
                 }
             }
             else
                 rt = null;

             if (!TimeTableHelpers.IsTimeTableValid(rt, this.Airliner, this.Entries))
             {
                   WPFMessageBoxResult result = WPFMessageBox.Show(Translator.GetInstance().GetString("MessageBox", "2705"), Translator.GetInstance().GetString("MessageBox", "2705", "message"), WPFMessageBoxButtons.YesNo);

                 if (result == WPFMessageBoxResult.Yes)
                 {

                     clearTimeTable();

                     if (!this.Entries.ContainsKey(route))
                         this.Entries.Add(route, new List<RouteTimeTableEntry>());


                     foreach (RouteTimeTableEntry entry in rt.Entries)
                         this.Entries[route].Add(entry);
                 }
             }
             else
             {
                 if (!this.Entries.ContainsKey(route))
                     this.Entries.Add(route, new List<RouteTimeTableEntry>());


                 foreach (RouteTimeTableEntry entry in rt.Entries)
                     this.Entries[route].Add(entry);
             }
             showFlights();
         }
         private void cbBusinessRoute_Checked(object sender, RoutedEventArgs e)
         {
             cbFlightsPerDay.IsEnabled = false;
             cbFlightsPerWeek.IsEnabled = false;
             cbStartTime.IsEnabled = false;
             cbDelayMinutes.IsEnabled = false;
         }

         private void cbBusinessRoute_Unchecked(object sender, RoutedEventArgs e)
         {
             cbFlightsPerDay.IsEnabled = true;
             cbStartTime.IsEnabled = true;
             cbDelayMinutes.IsEnabled = true;
             cbFlightsPerWeek.IsEnabled = true;
         }
         private void txtFlightEntry_MouseDown(object sender, MouseButtonEventArgs e)
         {
             if (e.RightButton == MouseButtonState.Pressed)
             {
                 /*
                 RouteTimeTableEntry entry = (RouteTimeTableEntry)((TextBlock)sender).Tag;

                 if (this.Entries.ContainsKey(entry.TimeTable.Route) && this.Entries[entry.TimeTable.Route].Find(te => te == entry) != null)
                 {
                     this.Entries[entry.TimeTable.Route].Remove(entry);
                 }
                 else
                 {
                     if (!this.EntriesToDelete.ContainsKey(entry.TimeTable.Route))
                         this.EntriesToDelete.Add(entry.TimeTable.Route, new List<RouteTimeTableEntry>());

                     this.EntriesToDelete[entry.TimeTable.Route].Add(entry);
                 }

                 showFlights();*/
             }
        }
        //returns the airliners from where the airliner can transfer schedule
        private List<FleetAirliner> getTransferAirliners()
        {
            long maxDistance = this.Airliner.Airliner.Type.Range;

            long requiredRunway = this.Airliner.Airliner.Type.MinRunwaylength;

            return GameObject.GetInstance().HumanAirline.Fleet.FindAll(a => a != this.Airliner && a.Routes.Count > 0 && a.Status == FleetAirliner.AirlinerStatus.Stopped && a.Routes.Max(r => r.getDistance()) <= maxDistance);
        }
    }
}