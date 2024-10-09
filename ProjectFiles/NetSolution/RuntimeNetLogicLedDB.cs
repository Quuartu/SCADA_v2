#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.Recipe;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.SQLiteStore;
using FTOptix.S7TiaProfinet;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using System.Timers;
using FTOptix.Report;
#endregion

public class RuntimeNetLogicLedDB : BaseNetLogic
{
    private static Timer timer;
    private static Timer timer2;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        timer = new System.Timers.Timer(10000); // 10000 millisecondi = 10 secondi
        timer2 = new System.Timers.Timer(10000); 
        // Fa partire il timer
        timer.AutoReset = true;  // Imposta AutoReset a true per eseguire ripetutamente
        timer.Elapsed += OnTimedEvent;
        timer.Enabled = true;
        timer2.AutoReset = true;  // Imposta AutoReset a true per eseguire ripetutamente
        timer2.Elapsed += OnTimedEvent2;
        timer2.Enabled = true;
        // Esegui immediatamente il metodo per la prima volta
        OnTimedEvent(this, null); // Passa `this` come sorgente e `null` come argomento
        OnTimedEvent2(this, null);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    private static void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        var LedSQLite = Project.Current.GetVariable(VariablePaths.Path_LedSQLite);
        object[,] TextConnection;
        var _store = Project.Current.Get<Store>("DataStores/EmbeddedDatabase1");
        try
        {
            _store.Query($"SELECT 1", out _, out TextConnection);
            LedSQLite.Value = true;  
            Console.WriteLine("TestDBLocale eseguito. Risultato: true");
        }
        catch (Exception ex)
        {
            LedSQLite.Value = false; 
            Console.WriteLine($"TestDBLocale eseguito. Risultato: false");
            Console.WriteLine($"Errore di connessione: {ex.Message}");
        }
    }

    private static void OnTimedEvent2(Object source, ElapsedEventArgs e)
    {
        var LedSQLexpress = Project.Current.GetVariable(VariablePaths.Path_LedSQLexpress);
        object[,] TextConnection;
        var _store = Project.Current.Get<Store>("DataStores/ODBC_PRG_OPTIX");
        try
        {
            _store.Query($"SELECT 1", out _, out TextConnection);
            LedSQLexpress.Value = true;  
            Console.WriteLine("TestDBexpress eseguito. Risultato: true");
        }
        catch (Exception ex)
        {
            LedSQLexpress.Value = false; 
            Console.WriteLine($"TestDBespress eseguito. Risultato: false");
            Console.WriteLine($"Errore di connessione: {ex.Message}");
        }
    }

}
