using System;
using System.Windows.Forms;

namespace BlackjackNet
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Meniu simplu: vrei să pornești ca Server sau ca Client?
            var result = MessageBox.Show(
                "Vrei să pornești aplicația ca SERVER?\n\nYES = Server\nNO = Client",
                "Blackjack în rețea",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // PORNEȘTE SERVERUL
                Application.Run(new fServer());
                // sau Application.Run(new fServer()); dacă ai pus namespace comun
            }
            else
            {
                // PORNEȘTE CLIENTUL
                Application.Run(new fClient());
                // sau Application.Run(new fClient()); dacă ai pus namespace comun
            }
        }
    }
}