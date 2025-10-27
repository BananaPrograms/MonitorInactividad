using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

namespace MonitorInactividad
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private bool monitoresApagados = false;
        private TimeSpan tiempoLimite = TimeSpan.FromMinutes(1);
        private AudioDetector audioDetector = new AudioDetector();
        private TrayIcon trayIcon;

        //Nota.- para linux debo tener instalado en linux
        // sudo apt install xprintidle x11-xserver-utils

        // --- Windows: P/Invoke para inactividad ---
        #region Windows Inactividad y Monitores
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        const int HWND_BROADCAST = 0xFFFF;
        const int WM_SYSCOMMAND = 0x0112;
        const int SC_MONITORPOWER = 0xF170;

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(int hWnd, int hMsg, int wParam, int lParam);
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // Icono de la bandeja (puede ser .ico o .png)

            // Icono de la ventana (barra de tareas + ventana)
            this.Icon = new WindowIcon("Assets/icono.ico");
            // Crear tray icon en código
            trayIcon = new TrayIcon
            {
                ToolTipText = "Monitor de Inactividad",
                Icon = new WindowIcon("Assets/icono.ico"), // Debes tener este icono en tu proyecto
                IsVisible = true
            };

            // Menú contextual
            var menu = new NativeMenu();
            var abrir = new NativeMenuItem("Abrir");
            abrir.Click += (_, __) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };

            var salir = new NativeMenuItem("Salir");
            salir.Click += (_, __) =>
            {
                trayIcon.IsVisible = false; // ocultar icono
                    // Cerrar completamente la aplicación
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    lifetime.Shutdown();
                }
            };

            menu.Items.Add(abrir);
            menu.Items.Add(salir);

            trayIcon.Menu = menu;

            trayIcon.Clicked += (_, __) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };

            // --- Aquí agregamos el evento Closing ---
            /*this.Closing += (sender, e) =>
            {
                e.Cancel = true; // Cancela el cierre real
                this.Hide();     // Oculta la ventana
                trayIcon.IsVisible = true; // Asegúrate de que el icono se vea
            };*/

            // Detectar cuando la ventana se minimiza
            this.PropertyChanged += (sender, e) =>
            {
                if (e.Property == Window.WindowStateProperty)
                {
                    if (this.WindowState == WindowState.Minimized)
                    {
                        // 🔹 Cuando se minimiza, ocultarla de la barra de tareas
                        this.Hide();
                        trayIcon.IsVisible = true; // Asegúrate de que el icono se vea
                    }
                    else if (this.WindowState == WindowState.Normal)
                    {
                        // 🔹 Cuando se restaura, puedes hacer algo si lo necesitas
                        // Por ejemplo: this.Activate();
                    }
                }
            };
            

            // Conectar eventos de botones
            btnIniciar.Click += BtnIniciar_Click;
            btnDetener.Click += BtnDetener_Click;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += Timer_Tick;
        }
        private void BtnIniciar_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (int.TryParse(txtTiempoLimite.Text, out int minutos))
                tiempoLimite = TimeSpan.FromMinutes(minutos);

            monitoresApagados = false;
            timer.Start();
            txtStatus.Text = $"Monitoreo iniciado. Tiempo límite: {tiempoLimite.TotalMinutes} min";

            // 🔹 Deshabilitar botón Iniciar
            btnIniciar.IsEnabled = false;

            // 🔹 Habilitar botón Detener
            btnDetener.IsEnabled = true;

            this.Hide();
            trayIcon.IsVisible = true; // Asegúrate de que el icono se vea
        }

        private void BtnDetener_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            timer.Stop();
            txtStatus.Text = "Monitoreo detenido.";

            // 🔹 Habilitar botón Iniciar nuevamente
            btnIniciar.IsEnabled = true;

            // 🔹 Deshabilitar botón Detener
            btnDetener.IsEnabled = false;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            TimeSpan inactivo = ObtenerTiempoInactividad();
            txtStatus.Text = $"Tiempo inactivo: {inactivo.Minutes}m {inactivo.Seconds}s";
            bool hayAudio = audioDetector.HayAudioActivo();

            if (inactivo > tiempoLimite && !monitoresApagados && !hayAudio)
            {
                ApagarMonitores();
                monitoresApagados = true;
            }
            else if ((inactivo < TimeSpan.FromSeconds(5) || hayAudio) && monitoresApagados)
            {
                EncenderMonitores();
                monitoresApagados = false;
            }
        }

        private TimeSpan ObtenerTiempoInactividad()
        {
            if (OperatingSystem.IsWindows())
            {
                LASTINPUTINFO info = new LASTINPUTINFO();
                info.cbSize = (uint)Marshal.SizeOf(info);
                GetLastInputInfo(ref info);
                uint tiempoActual = (uint)Environment.TickCount;
                uint tiempoInactivo = tiempoActual - info.dwTime;
                return TimeSpan.FromMilliseconds(tiempoInactivo);
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    // Ejecuta xprintidle para obtener milisegundos de inactividad
                    var psi = new ProcessStartInfo("xprintidle")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    };
                    var proceso = Process.Start(psi);
                    string output = proceso!.StandardOutput.ReadToEnd();
                    proceso.WaitForExit();
                    if (uint.TryParse(output.Trim(), out uint ms))
                        return TimeSpan.FromMilliseconds(ms);
                }
                catch
                {
                    // Si falla, asumimos 0
                }
                return TimeSpan.Zero;
            }
            else
            {
                return TimeSpan.Zero;
            }
        }

        private void ApagarMonitores()
        {
            if (OperatingSystem.IsWindows())
                SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, 2);
            else if (OperatingSystem.IsLinux())
                EjecutarShell("xset dpms force off");
        }

        private void EncenderMonitores()
        {
            if (OperatingSystem.IsWindows())
                SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, -1);
            else if (OperatingSystem.IsLinux())
                EjecutarShell("xset dpms force on");
        }

        private void EjecutarShell(string comando)
        {
            try
            {
                var psi = new ProcessStartInfo("bash", $"-c \"{comando}\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var proceso = Process.Start(psi);
                proceso!.WaitForExit();
            }
            catch
            {
                // Ignorar errores
            }
        }
    }
}
