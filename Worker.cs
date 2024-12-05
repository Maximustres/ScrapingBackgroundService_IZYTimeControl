using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;

namespace ScrapingBackgroundService_IZYTimeControl
{

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string[] _scheduledTimes;
        private readonly string? _user;
        private readonly string? _password;

        public Worker(
            ILogger<Worker> logger, 
            IConfiguration configuration)
        {
            _logger = logger;
            _scheduledTimes = configuration.GetSection("ScrapingSchedule:Times").Get<string[]>() ?? ["09:00", "19:00"];
            _user = configuration.GetSection("Credential:User").Get<string>();
            _password = configuration.GetSection("Credential:Password").Get<string>();

            if (string.IsNullOrEmpty(_user)) throw new ArgumentException($"Missing value user");
            if (string.IsNullOrEmpty(_password)) throw new ArgumentException($"Missing value password");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scraping service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                if (IsWeekday(now) && IsScheduledTime(now))
                {
                    _logger.LogInformation($"Running scraping at {now:HH:mm}.");
                    await PerformScraping();
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private bool IsWeekday(DateTime date)
        {
            return date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Friday;
        }

        private bool IsScheduledTime(DateTime date)
        {
            return _scheduledTimes.Contains(date.ToString("HH:mm"));
        }

        private async Task PerformScraping()
        {
            try
            {
                var options = new ChromeOptions();
                options.AddArgument("--headless"); // Modo headless para ejecutar sin interfaz gráfica
                options.AddArgument("--disable-gpu");
                options.AddArgument("--no-sandbox");

                using var driver = new ChromeDriver(options);
                _logger.LogInformation("Starting scraping...");

                driver.Navigate().GoToUrl("https://siigroup.izytimecontrol.com/#/login");

                // Move to Employee Self-Service Portal
                var element = driver.FindElement(By.XPath("//a[contains(text(), 'Portal Autoconsulta Empleados')]"));
                element.Click();

                await Task.Delay(300);

                // Enter the values in the fields
                var rutInput = driver.FindElement(By.Id("mat-input-2"));
                var passwordInput = driver.FindElement(By.Id("mat-input-3"));

                rutInput.SendKeys(_user);
                passwordInput.SendKeys(_password);

                // send the form to login
                driver.FindElement(By.CssSelector("button[type='submit']")).Click();

                await Task.Delay(300);

                // Go to page to check in or check out
                driver.Navigate().GoToUrl("https://siigroup.izytimecontrol.com/#/auto-consulta/web-marks");

                var passwordInputMarck = driver.FindElement(By.Id("mat-input-4"));
                passwordInputMarck.SendKeys(_password);

                driver.FindElement(By.CssSelector("button[type='submit']")).Click();

                _logger.LogInformation("You have successfully clocked in or out.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during scraping: {ex.Message}");
            }

        }
    }
}
