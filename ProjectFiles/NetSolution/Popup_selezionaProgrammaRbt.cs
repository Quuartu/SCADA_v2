#region Using directives
using System;
using System.Net.NetworkInformation;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.S7TiaProfinet;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
using FTOptix.Core;
using System.Net;
using System.Collections.Generic;
using System.IO;
#endregion

public class Popup_selezionaProgrammaRbt : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    [ExportMethod]
    public void sp_ConfirmSelection(string program)
    {
        if (program.Length == 0) 
        {
            PopUpNetLogic popup = new PopUpNetLogic();
            popup.OpenPopUp("Attenzione: seleziona un programma valido!", 0);
            return;
        }

        int dot = program.IndexOf(".");
        if (dot != -1) 
        {
            program = program.Substring(0, dot);
        }

        Project.Current.GetVariable(VariablePaths.PathAnagraficaProdottoRobot_Program).Value = program;
    }

    [ExportMethod]
    public void sp_Load(NodeId ftpNodeId)
    {

        Project.Current.GetVariable(VariablePaths.Pathsp_ProgramSelected).Value = "";

        var ftp = InformationModel.GetObject(ftpNodeId);

        var  nomeCartella = ftp.GetVariable("ftp_NomeCartella");
        var indirizzoIP = ftp.GetVariable("ftp_IndirizzoIP");
        var utente = ftp.GetVariable("ftp_Utente");
        var password = ftp.GetVariable("ftp_Password");
        var estensioneFile = ftp.GetVariable("ftp_EstensioneFile");

        //controlla che l'estensione abbia un "."
        if ( (estensioneFile.Value.ToString()).Substring(0,1) != ".")
        {
            estensioneFile.Value = "." + estensioneFile.Value;
        }

        //se i campi sono validi, recupera i file
        if (sp_ValidateFTPFields(nomeCartella.Value,indirizzoIP.Value,utente.Value,password.Value))
        {
            Project.Current.GetVariable(VariablePaths.Pathsp_ProgramList).Value = EnumFilesPipedList(indirizzoIP.Value, utente.Value, password.Value, nomeCartella.Value);
        }
    }

    private bool sp_ValidateFTPFields(string folder, string ip, string user, string password)
    {
        PopUpNetLogic popup = new PopUpNetLogic();
        
        if (!sp_IpAddressValidation(ip))
        {
            popup.OpenPopUp("Indirizzo ip non corretto o non inserito!", 0);
            return false;
        }

        if (!sp_PingIp(ip))
        {
            popup.OpenPopUp("Indirizzo ip non raggiungibile", 0);
            return false;
        }

        if (user.Length == 0)
        {
            popup.OpenPopUp("User FTP non inserito!", 0);
            return false;
        }

        if (password.Length == 0)
        {
            popup.OpenPopUp("Password FTP non inserita!", 0);
            return false;
        }

        if (folder.Length == 0)
        {
            popup.OpenPopUp("Folder FTP non inserito!", 0);
            return false;
        }

        return true;
    }

    private bool sp_IpAddressValidation(string ip) 
    {
        return IPAddress.TryParse(ip, out _);
    }

    private bool sp_PingIp(string ip)
    {
        try
        {
            using (Ping ping = new Ping())
            {
                PingReply reply = ping.Send(ip);
                if (reply.Status == IPStatus.Success)
                    return true;
                else
                    return false;
            }
        }
        catch (Exception ex)
        {
            Log.Warning("sp_PingIp: ping fallito -> " + ex.Message);
            return false;
        }
    }

    private string EnumFilesPipedList(string ftp_server,string ftp_user, string ftp_password, string folder)
    {
        List<string> files = new List<string>();

        try
        {
            string ftpUrl = $"ftp://{ftp_server}/{folder}";

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(ftp_user, ftp_password);

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    files.Add(line);
                    line = reader.ReadLine();
                }
            }

            if (files.Count == 0)
                return "";

            return String.Join("|", files);

        }
        catch (Exception ex)
        {
            PopUpNetLogic popup = new PopUpNetLogic();
            popup.OpenPopUp("Attenzione: connessione ftp non riuscita!", 0);
            Log.Warning("Error: EnumFilesPipedList ->" + ex.Message);
            return "";
        }

    }

}
