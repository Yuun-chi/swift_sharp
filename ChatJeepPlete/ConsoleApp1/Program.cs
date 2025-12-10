using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading;
using Spectre.Console;

namespace Swift
{
    // ==========================================
    //          CORE DATA STRUCTURES
    // ==========================================

    public struct ReceiptEntry
    {
        public DateTime Timestamp { get; set; }
        public string DriverName { get; set; }
        public string PlateNumber { get; set; }
        public string PassengerName { get; set; }
        public string DestinationStop { get; set; }
        public double TotalFare { get; set; }
        public double AppCommission { get; set; }
        public double DriverEarnings { get; set; }
        public string TransactionID { get; set; }
    }

    public class VehicleInfo
    {
        public string PlateNumber { get; set; } = string.Empty;
        public string DriverUsername { get; set; } = string.Empty;
        public string OperatorUsername { get; set; } = string.Empty;
        public string Model { get; set; } = "Unknown";
        public string Color { get; set; } = "Unknown";
    }

    public class TripBooking
    {
        public string BookingID { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        public Passenger Passenger { get; set; } = new Passenger();
        public string DestinationStop { get; set; } = string.Empty;
        public DateTime BookingTime { get; set; } = DateTime.Now;
        
        public bool IsAccepted { get; set; } = false;
        public bool IsCompleted { get; set; } = false; 
        public bool IsCancelled { get; set; } = false; 

        public Driver? AcceptingDriver { get; set; }
        public double FinalFare { get; set; }
    }

    // ==========================================
    //      SYSTEM LOGGING (AUDIT TRAIL)
    // ==========================================

    public static class AuditLogger
    {
        private const string LogFile = "AccountData/SystemLogs.txt";

        public static void LogAction(string user, string action, string details)
        {
            try
            {
                Directory.CreateDirectory("AccountData");
                string logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | [{user}] {action}: {details}";
                File.AppendAllText(LogFile, logLine + Environment.NewLine);
            }
            catch { }
        }

        public static void ViewLogs()
        {
            AnsiConsole.Clear();
            if (!File.Exists(LogFile))
            {
                UI.ShowWarning("No system logs available.");
                Console.ReadLine();
                return;
            }

            var table = new Table().Border(TableBorder.Rounded).Expand();
            table.AddColumn("Timestamp");
            table.AddColumn("User");
            table.AddColumn("Action");
            table.AddColumn("Details");

            try 
            {
                string[] lines = File.ReadAllLines(LogFile).Reverse().Take(50).ToArray(); 
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 2)
                    {
                        table.AddRow(parts[0].Trim(), parts[1].Trim().EscapeMarkup(), "", ""); 
                    }
                    else
                    {
                        table.AddRow(line.EscapeMarkup(), "", "", "");
                    }
                }
            }
            catch { }

            AnsiConsole.Write(new Panel(table).Header(" SYSTEM AUDIT LOGS (Last 50) "));
            Console.ReadLine();
        }
    }

    // ==========================================
    //      UTILITIES & UI HELPERS
    // ==========================================

    public class ErrorHandling
    {
        public int handleIntegerInput(string prompt = "Enter choice: ")
        {
            while (true)
            {
                try
                {
                    return AnsiConsole.Ask<int>($"[cyan]{prompt}[/]");
                }
                catch
                {
                    AnsiConsole.MarkupLine("[red]Invalid input. Please enter a number.[/]");
                }
            }
        }

        public bool handleYesNo(string prompt)
        {
            return AnsiConsole.Confirm($"[yellow]{prompt}[/]");
        }

        public string GetValidString(string prompt, int minLength = 1)
        {
            while (true)
            {
                string input = AnsiConsole.Ask<string>($"[cyan]{prompt}[/]");
                if (!string.IsNullOrWhiteSpace(input) && input.Length >= minLength)
                {
                    return input;
                }
                AnsiConsole.MarkupLine($"[red]Input must be at least {minLength} characters long.[/]");
            }
        }
    }

    public static class UI
    {
        private static Color PrimaryColor = Color.DodgerBlue1;
        private static Color SecondaryColor = Color.SpringGreen3;
        private static Color ErrorColor = Color.Red1;
        private static Color WarningColor = Color.Gold1;

        public static void ShowHeader(string title, string subtitle = "", string color = "cyan")
        {
            var font = new FigletText(title);
            font.Color(SecondaryColor);
            font.Centered();

            var border = new Panel(font);
            border.BorderColor(PrimaryColor);
            border.PadLeft(2);
            border.PadRight(2);
            border.Expand();

            AnsiConsole.Write(border);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var rule = new Rule($"[bold {color}]{subtitle}[/]");
                rule.RuleStyle("grey");
                rule.Centered();
                AnsiConsole.Write(rule);
            }
            AnsiConsole.WriteLine();
        }

        public static void ShowFormBox(string title, string instruction)
        {
            var panel = new Panel($"[grey]{instruction}[/]");
            panel.Header($"[bold white on blue] {title} [/]");
            panel.Border(BoxBorder.Double);
            panel.BorderColor(SecondaryColor);
            panel.Expand();
            AnsiConsole.Write(panel);
        }

        public static void ShowUserStatus(string user, string role, string details)
        {
            var grid = new Grid();
            grid.AddColumn();
            string safeUser = Markup.Escape(user);
            
            grid.AddRow($"[grey]User:[/] [bold white]{safeUser}[/]");
            grid.AddRow($"[grey]Role:[/] [bold cyan]{role}[/]"); 
            grid.AddRow($"[grey]Info:[/] [italic]{details}[/]");

            var panel = new Panel(grid);
            panel.Header("[bold white on blue] DASHBOARD [/]"); 
            panel.Border(BoxBorder.Rounded);
            panel.BorderColor(PrimaryColor);
            panel.Expand();

            AnsiConsole.Write(panel);
        }

        public static void ShowWarning(string message)
        {
            var panel = new Panel($"[bold white]{message.EscapeMarkup()}[/]");
            panel.BorderColor(WarningColor);
            panel.Header("[bold black on yellow] WARNING [/]");
            panel.Padding(1, 1);
            panel.Expand();
            AnsiConsole.Write(panel);
        }

        public static void ShowSuccess(string message)
        {
            var panel = new Panel($"[bold white]{message.EscapeMarkup()}[/]");
            panel.BorderColor(SecondaryColor);
            panel.Header("[bold black on green] SUCCESS [/]");
            panel.Expand();
            AnsiConsole.Write(panel);
        }

        public static void ShowError(string message)
        {
            var panel = new Panel($"[bold white]{message.EscapeMarkup()}[/]");
            panel.BorderColor(ErrorColor);
            panel.Header("[bold white on red] ERROR [/]");
            panel.Padding(1, 1);
            panel.Expand();
            AnsiConsole.Write(panel);
        }

        public static void FakeLoading(string action)
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start($"[bold cyan]{action}...[/]", ctx => 
                {
                    Thread.Sleep(800); 
                });
        }

        public static string ShowMenu(string title, string[] choices)
        {
            AnsiConsole.WriteLine();
            var rule = new Rule($"[bold cyan]{title}[/]");
            rule.LeftJustified();
            AnsiConsole.Write(rule);
            
            var prompt = new SelectionPrompt<string>();
            prompt.HighlightStyle(new Style(Color.Black, background: Color.Cyan1)); 
            prompt.AddChoices(choices);

            return AnsiConsole.Prompt(prompt);
        }
    }

    // ==========================================
    //      FILE & LOGGING SYSTEM
    // ==========================================

    public static class ReceiptLogger
    {
        private const string BaseReceiptDirectory = "OperatorReceipts";
        private const string GlobalFile = "Global_Transactions.txt"; 

        public static void AppendReceipt(ReceiptEntry entry)
        {
            Directory.CreateDirectory(BaseReceiptDirectory);
            string globalPath = Path.Combine(BaseReceiptDirectory, GlobalFile);

            string logEntry = string.Join("|",
                entry.Timestamp.ToString(CultureInfo.InvariantCulture),
                entry.DriverName,
                entry.PlateNumber, 
                entry.PassengerName,
                entry.DestinationStop,
                entry.TotalFare.ToString(CultureInfo.InvariantCulture),
                entry.AppCommission.ToString(CultureInfo.InvariantCulture),
                entry.DriverEarnings.ToString(CultureInfo.InvariantCulture),
                Guid.NewGuid().ToString()
            ) + Environment.NewLine;

            try
            {
                File.AppendAllText(globalPath, logEntry);
            }
            catch (Exception ex) 
            { 
                UI.ShowError("File Logging Error: " + ex.Message); 
            }
        }

        public static void ShowDriverDailyEarnings(string driverName)
        {
            AnsiConsole.Clear();
            string path = Path.Combine(BaseReceiptDirectory, GlobalFile);

            if (!File.Exists(path)) 
            { 
                UI.ShowWarning("No rides completed yet."); 
                Console.ReadLine(); 
                return; 
            }

            var table = new Table().Border(TableBorder.Rounded).Expand();
            table.AddColumn("Date"); 
            table.AddColumn("Rides"); 
            table.AddColumn("Total Earnings");
            table.AddColumn("Performance");

            List<ReceiptEntry> myRides = new List<ReceiptEntry>();
            foreach(var line in File.ReadAllLines(path))
            {
                if(string.IsNullOrWhiteSpace(line)) continue;
                var p = line.Split('|');
                
                if(p.Length >= 8 && p[1].Equals(driverName, StringComparison.OrdinalIgnoreCase))
                {
                    if(DateTime.TryParse(p[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt) && 
                       double.TryParse(p[7], NumberStyles.Any, CultureInfo.InvariantCulture, out double earn))
                    {
                        myRides.Add(new ReceiptEntry { Timestamp = dt, DriverEarnings = earn });
                    }
                }
            }

            if (!myRides.Any())
            {
                UI.ShowWarning("No earnings record found.");
            }
            else
            {
                var daily = myRides.GroupBy(r => r.Timestamp.Date).OrderByDescending(g => g.Key);
                double grandTotal = 0;
                int totalRides = 0;

                foreach(var d in daily)
                {
                    double dailySum = d.Sum(x => x.DriverEarnings);
                    int dailyCount = d.Count();
                    
                    grandTotal += dailySum;
                    totalRides += dailyCount;

                    string perf = dailySum > 500 ? "[bold green]EXCELLENT[/]" : (dailySum > 200 ? "[green]GOOD[/]" : "[yellow]FAIR[/]");
                    
                    table.AddRow(
                        d.Key.ToString("MMM dd, yyyy"), 
                        dailyCount.ToString(), 
                        $"[green]P{dailySum:N2}[/]", 
                        perf
                    );
                }

                // --- TOTALS ROW (ALREADY HERE, KEEPING IT) ---
                table.AddRow(new Rule(), new Rule(), new Rule(), new Rule());
                table.AddRow("[bold]TOTALS[/]", $"[bold]{totalRides}[/]", $"[bold gold1]P{grandTotal:N2}[/]", "");

                AnsiConsole.Write(new Panel(table).Header($" EARNINGS REPORT: {driverName.EscapeMarkup()} "));
            }
            Console.ReadLine();
        }

        public static void ShowAdminAnalytics()
        {
            string path = Path.Combine(BaseReceiptDirectory, GlobalFile);

            if (!File.Exists(path)) 
            { 
                UI.ShowWarning("No data available."); 
                Console.ReadLine(); 
                return; 
            }

            var entries = new List<ReceiptEntry>();
            foreach (var line in File.ReadAllLines(path))
            {
                if(string.IsNullOrWhiteSpace(line)) continue;
                var p = line.Split('|');
                
                if(p.Length >= 8 && 
                   DateTime.TryParse(p[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt) &&
                   double.TryParse(p[5], NumberStyles.Any, CultureInfo.InvariantCulture, out double fare) && 
                   double.TryParse(p[6], NumberStyles.Any, CultureInfo.InvariantCulture, out double comm))
                {
                    entries.Add(new ReceiptEntry { Timestamp = dt, TotalFare = fare, AppCommission = comm });
                }
            }

            while(true)
            {
                AnsiConsole.Clear();
                var mode = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                    .Title("[bold cyan]Revenue Analytics[/]")
                    .AddChoices("Daily Report", "Monthly Report", "Back"));
                
                if(mode == "Back") break;

                var table = new Table().Border(TableBorder.Rounded).Expand();

                if(mode == "Daily Report")
                {
                    table.AddColumn("Period"); 
                    table.AddColumn("Total Sales"); 
                    table.AddColumn("App Revenue (20%)");
                    
                    double totalSales = 0;
                    double totalRevenue = 0;

                    foreach(var g in entries.GroupBy(x => x.Timestamp.Date).OrderByDescending(k => k.Key))
                    {
                        double groupSales = g.Sum(x => x.TotalFare);
                        double groupRev = g.Sum(x => x.AppCommission);
                        
                        totalSales += groupSales;
                        totalRevenue += groupRev;

                        table.AddRow(
                            g.Key.ToString("MMM dd, yyyy"), 
                            $"P{groupSales:N2}", 
                            $"[bold green]P{groupRev:N2}[/]"
                        );
                    }
                    // ADDED TOTALS FOR ADMIN DAILY
                    table.AddRow(new Rule(), new Rule(), new Rule());
                    table.AddRow("[bold]TOTALS[/]", $"[bold]P{totalSales:N2}[/]", $"[bold gold1]P{totalRevenue:N2}[/]");
                }
                else if(mode == "Monthly Report")
                {
                    table.AddColumn("Period"); 
                    table.AddColumn("Total Sales "); 
                    table.AddColumn("App Revenue (20%)");

                    double totalSales = 0;
                    double totalRevenue = 0;

                    foreach(var g in entries.GroupBy(x => new { x.Timestamp.Month, x.Timestamp.Year }).OrderByDescending(k => k.Key.Year).ThenByDescending(k => k.Key.Month))
                    {
                        double groupSales = g.Sum(x => x.TotalFare);
                        double groupRev = g.Sum(x => x.AppCommission);

                        totalSales += groupSales;
                        totalRevenue += groupRev;

                        string mName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month);
                        table.AddRow(
                            $"{mName} {g.Key.Year}", 
                            $"P{groupSales:N2}", 
                            $"[bold green]P{groupRev:N2}[/]"
                        );
                    }
                    // ADDED TOTALS FOR ADMIN MONTHLY
                    table.AddRow(new Rule(), new Rule(), new Rule());
                    table.AddRow("[bold]TOTALS[/]", $"[bold]P{totalSales:N2}[/]", $"[bold gold1]P{totalRevenue:N2}[/]");
                }
                
                AnsiConsole.Write(new Panel(table).Header(" FINANCIAL REPORT "));
                Console.ReadLine();
            }
        }

        public static void ShowPassengerReceipts(string passengerUsername)
        {
            AnsiConsole.Clear();
            string path = Path.Combine(BaseReceiptDirectory, GlobalFile);

            if (!File.Exists(path))
            {
                UI.ShowWarning($"No ride history found.");
                Console.ReadLine();
                return;
            }

            var table = new Table().Border(TableBorder.Rounded).Expand();
            table.AddColumn("Date");
            table.AddColumn("Driver");
            table.AddColumn("Destination");
            table.AddColumn("Total Fare");

            string[] lines = File.ReadAllLines(path);
            bool foundAny = false;

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var p = line.Split('|');
                
                if (p.Length >= 8 && p[3].Equals(passengerUsername, StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(p[5], NumberStyles.Any, CultureInfo.InvariantCulture, out double totalFare))
                    {
                        table.AddRow(
                            DateTime.Parse(p[0]).ToString("MM/dd HH:mm"),
                            p[1].EscapeMarkup(), 
                            p[4].EscapeMarkup(), 
                            $"[bold green]P{totalFare:N2}[/]"
                        );
                        foundAny = true;
                    }
                }
            }

            if (!foundAny)
            {
                UI.ShowWarning("You haven't taken any rides yet.");
            }
            else
            {
                AnsiConsole.Write(new Panel(table).Header($" RIDE HISTORY: {passengerUsername.EscapeMarkup()} "));
            }
            Console.ReadLine();
        }
    }

    // ==========================================
    //      ROUTES & PRICING ENGINE
    // ==========================================

    public static class Routes
    {
        public static double SurgeMultiplier = 1.0; 

        public static Dictionary<string, double> FaresFromCIT = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            { "Ayala Center", 75.00 },
            { "Banawa", 70.00 },
            { "Banilad", 110.00 },
            { "Basak San Nicolas", 30.00 }, 
            { "Bulacao", 70.00 },
            { "Calamba", 40.00 },
            { "Capitol Site", 65.00 },
            { "Carbon", 45.00 },
            { "Carreta", 75.00 },
            { "Cebu Doctors Hospital V Rama", 60.00 },
            { "Cebu South Bus Terminal (CSBT)", 25.00 },
            { "Colon", 45.00 },
            { "Duljo Fatima", 30.00 },
            { "Ermita", 45.00 },
            { "Fuente Osmeña", 60.00 },
            { "Guadalupe", 75.00 },
            { "Hipodromo", 80.00 },
            { "Il Corso (SRP)", 70.00 },
            { "Inayawan", 60.00 },
            { "IT Park", 95.00 },
            { "Kamputhaw", 70.00 },
            { "Labangon", 40.00 }, 
            { "Lahug", 100.00 },
            { "Mabolo", 85.00 },
            { "Mambaling", 20.00 }, 
            { "Pahina Central", 35.00 },
            { "Pahina San Nicolas", 35.00 },
            { "Pardo", 55.00 },
            { "Parian", 50.00 },
            { "Pier Area", 55.00 },
            { "Pooc", 115.00 },
            { "Punta Princesa", 35.00 },
            { "Quiot Pardo", 45.00 },
            { "Robinsons Galleria", 65.00 },
            { "San Antonio", 40.00 },
            { "San Nicolas Bukid", 35.00 },
            { "San Nicolas Proper", 35.00 },
            { "Sambag I", 45.00 },
            { "Sambag II", 45.00 },
            { "SM City Cebu", 85.00 },
            { "SM Seaside", 80.00 },
            { "Sto. Niño Basilica", 50.00 },
            { "Taboan Market", 40.00 },
            { "Tabunok", 85.00 },
            { "Talamban", 140.00 }, 
            { "Talisay", 110.00 },
            { "Tisa", 40.00 },
        };

        public static double CalculateFare(string dest)
        {
            if (!FaresFromCIT.ContainsKey(dest)) return 0.0;
            double basePrice = FaresFromCIT[dest];
            double finalPrice = basePrice * SurgeMultiplier;
            return Math.Round(finalPrice, 2);
        }
    }

    // ==========================================
    //      USER ROLES & LOGIC
    // ==========================================

    public abstract class Credential
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public abstract void Dashboard(AccountValidation dataStore);
    }

    public class Operator : Credential
    {
        public override void Dashboard(AccountValidation dataStore)
        {
            while (true)
            {
                AnsiConsole.Clear();
                UI.ShowHeader("ADMIN PANEL");
                
                string surgeStatus = Routes.SurgeMultiplier > 1.0 ? $"[bold red]{Routes.SurgeMultiplier}x (HIGH DEMAND)[/]" : "[green]Normal (1.0x)[/]";
                UI.ShowUserStatus(this.Username, "App Owner", $"Surge: {surgeStatus}");

                string[] choices = { 
                    "1. View Drivers", 
                    "2. Revenue Reports", 
                    "3. Set Price Surge", 
                    "4. Register New Driver", 
                    "5. Delete Driver", 
                    "6. Logout" 
                };
                
                var choice = UI.ShowMenu("ADMIN COMMANDS", choices);

                if (choice.StartsWith("1")) ShowDrivers(dataStore);
                else if (choice.StartsWith("2")) ReceiptLogger.ShowAdminAnalytics();
                else if (choice.StartsWith("3")) SetSurge();
                else if (choice.StartsWith("4")) dataStore.CreateAccount(nameof(Driver));
                else if (choice.StartsWith("5")) DeleteDriver(dataStore);
                else if (choice.StartsWith("6")) break;
            }
        }

        private void SetSurge()
        {
            AnsiConsole.MarkupLine("\n[grey]Enter 1.0 for normal prices. Enter 1.5 for 50% increase.[/]");
            try
            {
                double newSurge = AnsiConsole.Ask<double>("Enter new Price Multiplier: ");
                if(newSurge < 0.5 || newSurge > 5.0) 
                {
                    UI.ShowError("Surge must be between 1.0x and 5.0x");
                    Thread.Sleep(1000);
                    return;
                }
                Routes.SurgeMultiplier = newSurge;
                UI.ShowSuccess($"Surge set to {Routes.SurgeMultiplier}x");
            }
            catch
            {
                UI.ShowError("Invalid number format.");
            }
            Thread.Sleep(1500);
        }

        private void ShowDrivers(AccountValidation dataStore)
        {
            AnsiConsole.Clear();
            var table = new Table().Border(TableBorder.Rounded).Expand();
            table.AddColumn("Driver User");
            table.AddColumn("Plate");
            table.AddColumn("Wallet");
            table.AddColumn("Rating");
            table.AddColumn("Status");

            bool found = false;
            foreach (var u in dataStore.RegisteredUsers.Values)
            {
                if (u is Driver d)
                {
                    string status = dataStore.IsDriverBusy(d) ? "[bold red]ON TRIP[/]" : "[bold green]AVAILABLE[/]";
                    string ratingColor = d.GetAverageRating() >= 4.5 ? "green" : (d.GetAverageRating() >= 3.0 ? "yellow" : "red");
                    
                    table.AddRow(
                        d.Username.EscapeMarkup(), 
                        d.PlateNumber.EscapeMarkup(),
                        $"P{d.WalletBalance:N2}", 
                        $"[{ratingColor}]{d.GetRatingStar()}[/]", 
                        status
                    );
                    found = true;
                }
            }

            if (!found) UI.ShowWarning("No drivers found.");
            else AnsiConsole.Write(table);

            Console.ReadLine();
        }

        private void DeleteDriver(AccountValidation ds)
        {
            string u = AnsiConsole.Ask<string>("Enter username to delete:");
            if (ds.RegisteredUsers.ContainsKey(u) && ds.RegisteredUsers[u] is Driver)
            {
                if (AnsiConsole.Confirm($"Are you sure you want to delete [red]{u.EscapeMarkup()}[/]?"))
                {
                    ds.DeleteDriver(u);
                    UI.ShowSuccess("Driver deleted.");
                }
            }
            else UI.ShowError("Driver not found.");
            Thread.Sleep(1000);
        }
    }

    public class Driver : Credential
    {
        public string PlateNumber { get; set; } = "UNKNOWN";
        public double WalletBalance { get; set; } = 0.00;
        public bool IsOnline { get; set; } = true;

        // Rating Statistics
        public double RatingSum { get; set; } = 0;
        public int RatingCount { get; set; } = 0;

        public double GetAverageRating() => RatingCount == 0 ? 0.0 : Math.Round(RatingSum / RatingCount, 1);
        public string GetRatingStar() => $"{GetAverageRating()}★ ({RatingCount})";

        public override void Dashboard(AccountValidation dataStore)
        {
            // Force them online whenever they log in
            this.IsOnline = true; 

            while (true)
            {
                AnsiConsole.Clear();
                UI.ShowHeader("Driver Partner");

                // AUTOMATIC STATUS LOGIC:
                string status = dataStore.IsDriverBusy(this) ? "[bold red]ON TRIP[/]" : "[bold green]AVAILABLE[/]";
                
                // REMOVED WALLET DISPLAY HERE
                UI.ShowUserStatus(this.Username, "Partner", $"Plate: {PlateNumber}\nRating: [yellow]{GetRatingStar()}[/]\nStatus: {status}");

                string[] choices = {
                    "1. Job Board (Find Passengers)",
                    "2. Complete Current Trip",
                    // REMOVED "Go Online/Offline"
                    "3. View Earnings", // RENAMED
                    // REMOVED Withdraw Wallet
                    // REMOVED View Withdrawal History
                    "4. Logout"
                };
                var choice = UI.ShowMenu("COMMANDS", choices);

                if (choice.StartsWith("1")) ShowJobs(dataStore);
                else if (choice.StartsWith("2")) CompleteTrip(dataStore);
                else if (choice.StartsWith("3")) ReceiptLogger.ShowDriverDailyEarnings(this.Username);
                else if (choice.StartsWith("4")) break;
            }
        }

        private void ShowJobs(AccountValidation ds)
        {
            if (!IsOnline) { UI.ShowWarning("You must go ONLINE first."); Thread.Sleep(1500); return; }
            if (ds.IsDriverBusy(this)) { UI.ShowWarning("Please finish your current trip first."); Thread.Sleep(1500); return; }

            var pending = ds.BookedTrips.Where(t => !t.IsAccepted).ToList();
            if (!pending.Any()) { UI.ShowWarning("No passengers currently waiting."); Thread.Sleep(1500); return; }

            var opts = new Dictionary<string, TripBooking>();
            foreach (var t in pending)
            {
                double fare = Routes.CalculateFare(t.DestinationStop);
                double earn = fare * 0.80; 
                opts.Add($"{t.Passenger.Username.EscapeMarkup()} -> {t.DestinationStop.EscapeMarkup()} ([green]Earn: P{earn:N0}[/])", t);
            }
            opts.Add("Cancel", null);

            var pick = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Pick a Ride").AddChoices(opts.Keys));
            if (pick == "Cancel") return;

            var booking = opts[pick];
            booking.FinalFare = Routes.CalculateFare(booking.DestinationStop);
            booking.IsAccepted = true;
            booking.AcceptingDriver = this;

            double commission = booking.FinalFare * 0.20;
            double netEarnings = booking.FinalFare * 0.80;

            WalletBalance += netEarnings;
            ds.UpdateDriverFile(); 

            ReceiptLogger.AppendReceipt(new ReceiptEntry
            {
                Timestamp = DateTime.Now,
                DriverName = this.Username,
                PlateNumber = this.PlateNumber,
                PassengerName = booking.Passenger.Username,
                DestinationStop = booking.DestinationStop,
                TotalFare = booking.FinalFare,
                AppCommission = commission,
                DriverEarnings = netEarnings
            });

            UI.ShowSuccess($"Trip Accepted! Cash to collect: P{booking.FinalFare:N2}.");
            Thread.Sleep(2000);
        }

        private void CompleteTrip(AccountValidation ds)
        {
            var trip = ds.BookedTrips.FirstOrDefault(t => t.AcceptingDriver == this && !t.IsCompleted);

            if (trip != null)
            {
                trip.IsCompleted = true; 
                UI.ShowSuccess("Passenger dropped off. Waiting for rating...");
            }
            else
            {
                UI.ShowWarning("You don't have an active trip to complete.");
            }
            Thread.Sleep(1500);
        }
    }

    public class Passenger : Credential
    {
        public override void Dashboard(AccountValidation dataStore)
        {
            while (true)
            {
                AnsiConsole.Clear();
                UI.ShowHeader("Passenger");

                // --- NEW DYNAMIC STATUS LOGIC START ---
                var currentTrip = dataStore.BookedTrips.FirstOrDefault(t => t.Passenger.Username == this.Username);
                string statusInfo = "Ready to ride"; // Default

                if (currentTrip != null)
                {
                    if (!currentTrip.IsAccepted) statusInfo = "Waiting for driver...";
                    else if (!currentTrip.IsCompleted) statusInfo = "On trip...";
                    else statusInfo = "Arrived at destination";
                }
                
                UI.ShowUserStatus(this.Username, "Passenger", statusInfo);
                // --- NEW DYNAMIC STATUS LOGIC END ---

                CheckForRating(dataStore);

                string[] choices = { "1. Book Trip", "2. Check Status", "3. Cancel Booking", "4. View Trip History", "5. Logout" };
                var choice = UI.ShowMenu("PASSENGER COMMANDS", choices);

                if (choice.StartsWith("1")) BookTrip(dataStore);
                else if (choice.StartsWith("2")) CheckStatus(dataStore);
                else if (choice.StartsWith("3")) CancelBooking(dataStore);
                else if (choice.StartsWith("4")) ReceiptLogger.ShowPassengerReceipts(this.Username);
                else if (choice.StartsWith("5")) break;
            }
        }

        private void CheckForRating(AccountValidation ds)
        {
            var doneTrip = ds.BookedTrips.FirstOrDefault(t => t.Passenger.Username == this.Username && t.IsCompleted);

            if (doneTrip != null)
            {
                AnsiConsole.Clear();
                UI.ShowHeader("TRIP COMPLETE");
                AnsiConsole.MarkupLine($"You arrived at [cyan]{doneTrip.DestinationStop.EscapeMarkup()}[/].");
                
                if (doneTrip.AcceptingDriver != null)
                {
                    AnsiConsole.MarkupLine($"Driver: [yellow]{doneTrip.AcceptingDriver.Username.EscapeMarkup()}[/]");
                    
                    var rating = AnsiConsole.Prompt(new SelectionPrompt<int>().Title("Rate your driver (1-5):").AddChoices(5, 4, 3, 2, 1));

                    doneTrip.AcceptingDriver.RatingSum += rating;
                    doneTrip.AcceptingDriver.RatingCount++;
                    ds.UpdateDriverFile(); 
                }

                ds.BookedTrips.Remove(doneTrip); 
                UI.ShowSuccess("Thank you! Rating submitted.");
                Thread.Sleep(1500);
            }
        }

        private void BookTrip(AccountValidation ds)
        {
            if (ds.BookedTrips.Any(t => t.Passenger.Username == this.Username))
            {
                UI.ShowWarning("You already have a booking in progress.");
                Thread.Sleep(1500);
                return;
            }

            var dest = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Where to?").PageSize(10).AddChoices(Routes.FaresFromCIT.Keys));
            double fare = Routes.CalculateFare(dest);

            string surgeMsg = Routes.SurgeMultiplier > 1.0 ? " [red](Surge Active)[/]" : "";
            if (AnsiConsole.Confirm($"Fare: [green]P{fare:N2}[/]{surgeMsg}. Book now?"))
            {
                ds.BookedTrips.Add(new TripBooking { Passenger = this, DestinationStop = dest });
                UI.ShowSuccess("Request sent! Finding drivers...");
                Thread.Sleep(1500);
            }
        }

        private void CheckStatus(AccountValidation ds)
        {
            var trip = ds.BookedTrips.FirstOrDefault(t => t.Passenger.Username == this.Username);

            if (trip == null) UI.ShowWarning("No trips found.");
            else if (!trip.IsAccepted) AnsiConsole.MarkupLine("[yellow]Status: Finding a driver...[/]");
            else if (!trip.IsCompleted) 
            {
                string driverName = trip.AcceptingDriver != null ? Markup.Escape(trip.AcceptingDriver.Username) : "Unknown";
                string plate = trip.AcceptingDriver != null ? Markup.Escape(trip.AcceptingDriver.PlateNumber) : "N/A";
                AnsiConsole.MarkupLine($"[green]Status: On trip with {driverName} ({plate})...[/]");
            }
            else AnsiConsole.MarkupLine("[cyan]Status: Arrived! Please rate your driver.[/]");

            Console.ReadLine();
        }

        private void CancelBooking(AccountValidation ds)
        {
            var trip = ds.BookedTrips.FirstOrDefault(t => t.Passenger.Username == this.Username);
            if (trip == null)
            {
                UI.ShowWarning("No active booking to cancel.");
                Thread.Sleep(1000);
                return;
            }

            if (trip.IsAccepted)
            {
                UI.ShowError("Cannot cancel. Driver is already on the way!");
                Thread.Sleep(1500);
                return;
            }

            if (AnsiConsole.Confirm("Are you sure you want to cancel?"))
            {
                ds.BookedTrips.Remove(trip);
                UI.ShowSuccess("Booking cancelled.");
            }
        }
    }

    // ==========================================
    //      DATA STORE & ACCOUNT MANAGEMENT
    // ==========================================

    public class AccountValidation
    {
        private const string UsersFile = "AccountData/users.txt";
        public Dictionary<string, Credential> RegisteredUsers { get; set; } = new Dictionary<string, Credential>(StringComparer.OrdinalIgnoreCase);
        public List<TripBooking> BookedTrips { get; set; } = new List<TripBooking>();

        public AccountValidation() { Directory.CreateDirectory("AccountData"); LoadData(); }

        public bool IsDriverBusy(Driver d)
        {
            return BookedTrips.Any(t => t.AcceptingDriver == d && !t.IsCompleted);
        }

        private void LoadData()
        {
            if (File.Exists(UsersFile))
            {
                foreach (var line in File.ReadAllLines(UsersFile))
                {
                    var p = line.Split('|');
                    if (p.Length < 3) continue;

                    if (p[0] == nameof(Operator))
                    {
                        RegisteredUsers[p[1]] = new Operator { Username = p[1], Password = p[2] };
                    }
                    else if (p[0] == nameof(Passenger))
                    {
                        RegisteredUsers[p[1]] = new Passenger { Username = p[1], Password = p[2] };
                    }
                    else if (p[0] == nameof(Driver))
                    {
                        var d = new Driver { Username = p[1], Password = p[2], PlateNumber = p.Length > 3 ? p[3] : "N/A" };
                        
                        if (p.Length > 4 && double.TryParse(p[4], NumberStyles.Any, CultureInfo.InvariantCulture, out double w)) d.WalletBalance = w;
                        if (p.Length > 5 && double.TryParse(p[5], NumberStyles.Any, CultureInfo.InvariantCulture, out double rs)) d.RatingSum = rs;
                        if (p.Length > 6 && int.TryParse(p[6], NumberStyles.Any, CultureInfo.InvariantCulture, out int rc)) d.RatingCount = rc;

                        RegisteredUsers[p[1]] = d;
                    }
                }
            }
        }

        public void SaveUserToFile(Credential user)
        {
            string line = "";
            if (user is Operator) line = $"{nameof(Operator)}|{user.Username}|{user.Password}";
            else if (user is Passenger) line = $"{nameof(Passenger)}|{user.Username}|{user.Password}";
            else if (user is Driver d) line = $"{nameof(Driver)}|{d.Username}|{d.Password}|{d.PlateNumber}|{d.WalletBalance}|{d.RatingSum}|{d.RatingCount}";
            File.AppendAllText(UsersFile, line + Environment.NewLine);
        }

        public void UpdateDriverFile()
        {
            List<string> lines = new List<string>();
            foreach (var u in RegisteredUsers.Values)
            {
                if (u is Operator) lines.Add($"{nameof(Operator)}|{u.Username}|{u.Password}");
                else if (u is Passenger) lines.Add($"{nameof(Passenger)}|{u.Username}|{u.Password}");
                else if (u is Driver d) lines.Add($"{nameof(Driver)}|{d.Username}|{d.Password}|{d.PlateNumber}|{d.WalletBalance}|{d.RatingSum}|{d.RatingCount}");
            }
            try { File.WriteAllLines(UsersFile, lines); } catch { }
        }

        public void DeleteDriver(string user)
        {
            if (RegisteredUsers.ContainsKey(user))
            {
                RegisteredUsers.Remove(user);
                UpdateDriverFile();
            }
        }

        public Credential CreateAccount(string role)
        {
            AnsiConsole.Clear();
            UI.ShowFormBox("REGISTER", $"Creating new {role} account");
            
            var eh = new ErrorHandling();
            string user = eh.GetValidString("Enter Username:");
            
            if (RegisteredUsers.ContainsKey(user)) { UI.ShowError("Username taken."); Thread.Sleep(1000); return null; }
            
            string pass = AnsiConsole.Prompt(new TextPrompt<string>("Create Password: ").Secret());

            Credential newUser;
            if (role == nameof(Operator)) newUser = new Operator { Username = user, Password = pass };
            else if (role == nameof(Passenger)) newUser = new Passenger { Username = user, Password = pass };
            else
            {
                string plate = eh.GetValidString("Enter Plate Number:");
                newUser = new Driver { Username = user, Password = pass, PlateNumber = plate };
            }
            RegisteredUsers[user] = newUser;
            SaveUserToFile(newUser);
            UI.ShowSuccess("Account Created Successfully! Logging you in..."); 
            Thread.Sleep(1000);
            return newUser;
        }

        public Credential? Login(string role)
        {
            AnsiConsole.Clear();
            UI.ShowFormBox("LOGIN", "Please enter your credentials");

            string u = AnsiConsole.Ask<string>("Username: ");
            string p = AnsiConsole.Prompt(new TextPrompt<string>("Password: ").Secret());
            
            if (RegisteredUsers.TryGetValue(u, out var user) && user.Password == p && user.GetType().Name == role)
            {
                UI.FakeLoading("Logging in");
                return user;
            }
            
            UI.ShowError("Invalid credentials."); 
            Thread.Sleep(1500);
            return null;
        }
    }

    public class Program
    {
        public static void Main()
        {
            try 
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                new MainMenu().Menu();
            }
            catch (Exception ex)
            {
                Console.WriteLine("CRITICAL ERROR: " + ex.Message);
                Console.ReadLine();
            }
        }
    }

    public class MainMenu
    {
        public void Menu()
        {
            var data = new AccountValidation();
            while (true)
            {
                AnsiConsole.Clear();
                UI.ShowHeader("Swift", "The Ultimate Ride App", "springgreen3");
                string roleChoice = UI.ShowMenu("MAIN MENU", new[] { "1. Operator (Admin)", "2. Driver", "3. Passenger", "4. Exit" });
                if (roleChoice.StartsWith("4")) break;
                string role = roleChoice.Split(' ')[1];

                AnsiConsole.Clear();
                UI.ShowHeader("Swift");
                string action = UI.ShowMenu($"Welcome {role}", new[] { "1. Login", "2. Create Account", "3. Back" });
                if (action.StartsWith("3")) continue;

                Credential? user = null;
                if (action.StartsWith("1")) 
                {
                    user = data.Login(role);
                }
                else 
                {
                    user = data.CreateAccount(role);
                }

                if (user != null) user.Dashboard(data);
            }
            AnsiConsole.MarkupLine("[cyan]Goodbye![/]");
        }
    }
}