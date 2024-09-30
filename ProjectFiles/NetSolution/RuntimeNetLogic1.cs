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
using FTOptix.System;
using System.IO;
using System.Timers;
#endregion

public class RuntimeNetLogic1 : BaseNetLogic
{
    private static Timer timer; // Timer per eseguire l'operazione ogni 10 secondi

    public override void Start()
    {
        // Imposta e avvia il timer
        timer = new Timer(10000); // 10000 millisecondi = 10 secondi
        timer.Elapsed += ControllaFile; // Associa l'evento Elapsed all'handler
        timer.AutoReset = true; // Ripeti l'operazione ogni 10 secondi
        timer.Enabled = true; // Abilita il timer

        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void GetValue(string ProductID )
    {
        // Get the Database from the current project
        var myStore = Project.Current.Get<Store>("DataStores/EmbeddedDatabase1");
        // Create the output to get the result (mandatory)
        Object[,] ResultSet;
        String[] Header;
        // Perform the query
        myStore.Query("SELECT * FROM RecipeSchema1 WHERE Name = '" + ProductID + "'", out Header, out ResultSet);
        //Applico Product_ID
        Project.Current.GetVariable("Recipes/RecipeSchema1/EditModel/Product_ID").Value = ProductID;
        //Applico ID
        Project.Current.GetVariable("Recipes/RecipeSchema1/EditModel/ID").Value = (long)ResultSet[0, 1];
        //Applico Descr
        Project.Current.GetVariable("Recipes/RecipeSchema1/EditModel/Descr").Value = (string)ResultSet[0, 2];
        //Applico Robot_Program
        Project.Current.GetVariable("Recipes/RecipeSchema1/EditModel/Robot_Program").Value = (long)ResultSet[0, 3];
    }

    // Metodo per controllare e leggere il file
    private static void ControllaFile(Object source, ElapsedEventArgs e)
    {
        // Specifica la cartella e il nome del file
        string cartella = @"C:\Users\davide.quartucci\Desktop\InserimentoAnagraficaSuDB";
        string nomeFile = "dati.txt";

        // Cerca il file nella cartella specificata
        string percorsoFile = Path.Combine(cartella, nomeFile);

        if (File.Exists(percorsoFile))
        {
            try
            {
                // Leggi il contenuto del file
                string[] righe = File.ReadAllLines(percorsoFile);

                foreach (string riga in righe)
                {
                    try
                    {
                        // Dividi la riga in tre parti usando il separatore ';'
                        string[] campi = riga.Split(';');

                        if (campi.Length != 3)
                        {
                            throw new FormatException("La riga non contiene esattamente 3 campi.");
                        }

                        // Assegna i campi a variabili locali
                        string Product_ID = campi[0];
                        string Descr = campi[1];
                        int Robot_Program;

                        // Prova a convertire il terzo campo in un intero
                        if (!int.TryParse(campi[2], out Robot_Program))
                        {
                            throw new FormatException("Il campo Robot_Program non è un intero valido.");
                        }

                        //Esegue l'inserimento nel DB di una nuova anagrafica
                        var myStore = Project.Current.Get<Store>("DataStores/EmbeddedDatabase1");
                        var myTable = myStore.Tables.Get<Table>("RecipeSchema1");
                        string[] columns = { "Name", "/Descr", "/Robot_Program", "/Product_ID" };
                        var values = new object[1, 4];
                        values[0, 0] = Product_ID;
                        values[0, 1] = Descr;
                        values[0, 2] = Robot_Program;
                        values[0, 3] = Product_ID;
                        myTable.Insert(columns, values);
                    }
                    catch (FormatException ex)
                    {
                        Console.WriteLine($"Errore: formato dei dati non valido nella riga '{riga}'.");
                    }
                }

                // Elimina il file dopo la lettura
                File.Delete(percorsoFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Si è verificato un errore durante la lettura del file: {ex.Message}");
            }
        }
        else
        {
            // Se il file non esiste, ignora semplicemente e continua
            Console.WriteLine($"File non trovato: {percorsoFile}. Verrà controllato di nuovo.");
        }
    }
}
