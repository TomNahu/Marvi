using System;
using System.Windows;

namespace Marvi
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Una sola mascota por escritorio evita que dos procesos escriban el mismo perfil.
            bool first;
            using (var instance = new System.Threading.Mutex(true, @"Local\Marvi.Desktop", out first))
            {
                if (!first) return;
                var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
                var pet = new PetWindow();
                app.MainWindow = pet;
                app.Run(pet);
            }
        }
    }
}
