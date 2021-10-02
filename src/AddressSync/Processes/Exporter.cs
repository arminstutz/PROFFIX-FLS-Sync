using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;
using pxBook;
using SMC.Lib;

namespace FlsGliderSync
{
    public class Exporter
    {
        public enum ExporterCommitCommand
        {
            Update,
            Create
        }

        // Klassenvariablen
        private FlsConnection flsConn;
        private ProffixConnection MyConn;
        private ProffixHelper pxHelper;
        private ArticleMapper articleMapper;

        // Actions
        public Action DoProgress;
        public Action<string> Log;

        // Regex-Pattern für GUID
        private Regex pattern_GUID = new Regex(@"^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$", RegexOptions.Compiled);
        private DateTime _lastExport = default;

        public DateTime LastExport
        {
            get
            {
                return _lastExport;
            }

            set
            {
                _lastExport = value;
            }
        }

        private int _progress;

        public int Progress
        {
            get
            {
                return _progress;
            }

            private set
            {
                _progress = value;
            }
        }

        private int _count;

        public int Count
        {
            get
            {
                return _count;
            }

            private set
            {
                _count = value;
            }
        }

        public Exporter(DateTime lastExport, ref FlsConnection serviceClient, ref ProffixHelper pxHelper, ref ProffixConnection Myconn)
        {
            LastExport = lastExport;
            flsConn = serviceClient;
            this.pxHelper = pxHelper;
            MyConn = Myconn;
            articleMapper = new ArticleMapper();
        }

        public bool Export()
        {
            System.Threading.Tasks.Task<JArray> articleResult;
            string fehler = "";
            string response_FLS = "";
            bool successful = true;
            var articleList = new List<pxKommunikation.pxArtikel>();
            bool existsInFLS = false;
            string response = string.Empty;
            bool update_successful = true;
            bool create_successful = true;
            try
            {
                logComplete("Artikelexport gestartet", LogLevel.Info);
                Progress = 0;

                // **************************************************************alle FLS-Artikel: Negativbestand = 1 setzen***********************************************************
                pxHelper.SetNegativBestand(ref response);
                if (!string.IsNullOrEmpty(response))
                {
                    logComplete(response, LogLevel.Exception);
                }

                // ******************************************************************************Artikel holen*********************************************************************************
                // alle Artikel aus FLS holen
                articleResult = flsConn.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPIArticlesMethod);
                articleResult.Wait();

                // alle Artikel aus Proffix holen, die erstellt/geaendert am > lastExport haben und in Gruppe FLS sind
                articleList = pxArtikelLaden();
                Count = articleList.Count;

                // **********************************************************************Artikel vergleichen*********************************************************
                // Artikel in Proffix + FLS vergleichen (ArticleNumber)
                foreach (var proffixArticle in articleList)
                {

                    // Defaultwert = false
                    existsInFLS = false;

                    // Alle Artikel aus FLS durchgehen
                    foreach (JObject existingFLSarticle in articleResult.Result.Children())
                    {

                        // ************************************************************UPDATE IN FLS*******************************************************************
                        // --> wenn in FLS vorhanden --> update PUT
                        if ((proffixArticle.ArtikelNr ?? "") == (existingFLSarticle["ArticleNumber"].ToString() ?? ""))
                        {
                            existsInFLS = true;

                            // updated den Artikel in FLS (Flag existsInFLS
                            if (!updateInFLS(proffixArticle, existingFLSarticle))
                            {

                                // geklappt soll falsch sein (und bleiben), sobald 1 update/create nicht geklappt hat --> wenn updateInFLS true zurückgibt --> geklappt nicht verändern, da sonst vorheriger Fehler ignoriert wird
                                successful = false;
                            }
                        }
                    }

                    // *******************************************************************CREATE IN FLS******************************************************************
                    // wenn in FLS nicht vorhanden 
                    if (!existsInFLS)
                    {

                        // ... und  in Proffix nicht bereits schon wieder gelöscht wurde
                        if (proffixArticle.Geloescht == 0)
                        {

                            // ... dann: create Article in FLS + neue Id in Proffix schreiben
                            if (!createInFLS(proffixArticle))
                            {
                                // geklappt soll falsch sein, sobald 1 update/create nicht geklappt hat --> wenn updateInFLS true zurückgibt --> geklappt nicht verändern, da sonst vorheriger Fehler ignoriert wird
                                successful = false;
                            }
                        }
                    }

                    Progress += 1;
                    InvokeDoProgress();
                }

                // **************************************************************LastSync updaten*****************************************************************************
                // wenn bis herhin alles geklappt --> geklappt immer noch true
                if (successful)
                {
                    logComplete("Artikelexport erfolgreich beendet", LogLevel.Info);
                    LastExport = DateTime.Now;
                }
                else
                {
                    logComplete("Beim Artikelexport ist mindestens 1 Fehler aufgetreten. Deshalb wird das Datum des letzten Exports nicht angepasst.", LogLevel.Exception);
                    Logger.GetInstance().Log(LogLevel.Exception, "Artikelexport beendet. Mindestens 1 Fehler bei Artikelexport");
                }

                logComplete("", LogLevel.Info);
                Progress = Count;
                InvokeDoProgress();
                return successful;
            }
            catch (FaultException faultExce)
            {
                Logger.GetInstance().Log(LogLevel.Exception, faultExce.Message);
                throw faultExce;
            }
            // End If
            catch (Exception exce)
            {
                Logger.GetInstance().Log(LogLevel.Exception, exce);
                throw;
            }
        }

        private List<pxKommunikation.pxArtikel> pxArtikelLaden()
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            var articleList = new List<pxKommunikation.pxArtikel>();
            string fehler = string.Empty;
            sql = "Select artikelNrLAG, bezeichnung1, bezeichnung2, bezeichnung3, geloescht from lag_artikel " + "where Z_FLS = 1 and (erstelltam > '" + LastExport.ToString(pxHelper.dateformat + " HH:mm:ss") + "' or geaendertam > '" + LastExport.ToString(pxHelper.dateformat + " HH:mm:ss") + "')";
            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                logComplete("Fehler beim Laden der geänderten Artikel" + fehler, LogLevel.Exception);
                return null;
            }
            else
            {

                // geholte Artikel in einer Liste speichern
                var article = new pxKommunikation.pxArtikel();
                while (!rs.EOF)
                {
                    article.ArtikelNr = rs.Fields["artikelNrLAG"].ToString();
                    article.Bezeichnung1 = rs.Fields["bezeichnung1"].ToString();
                    article.Bezeichnung2 = rs.Fields["bezeichnung2"].ToString();
                    article.Bezeichnung3 = rs.Fields["bezeichnung3"].ToString();
                    article.Geloescht = Conversions.ToInteger(rs.Fields["geloescht"]);
                    articleList.Add(article);
                    rs.MoveNext();
                }
            }

            return articleList;
        }

        private bool updateInFLS(pxKommunikation.pxArtikel proffixArticle, JObject existingFLSarticle)
        {
            string response_FLS = string.Empty;
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            try
            {
                // JSON verändern
                existingFLSarticle = articleMapper.Mapp(proffixArticle, existingFLSarticle);

                // Artikel in FLS updaten 
                response_FLS = flsConn.ExportChanges(existingFLSarticle["ArticleId"].ToString(), existingFLSarticle, ExporterCommitCommand.Update);

                // Ist response_FLS keine GUID? --> update in FLS hat nicht geklappt, response_FLS enthält Fehlermeldung
                if (!pattern_GUID.IsMatch(response_FLS))
                {
                    logComplete("Fehler beim Updaten in FLS ArtikelNr: " + proffixArticle.ArtikelNr.ToString() + " " + proffixArticle.Bezeichnung1, LogLevel.Exception, response_FLS);
                    throw new Exception("Fehler beim Updaten des Artikels in FLS");
                }

                // response_FLS ist GUID = update in FLS hat geklappt --> ArticleId in Proffix updaten
                if (!updateArticleIdInProffix(response_FLS, proffixArticle.ArtikelNr, ref fehler))
                {
                    // Update ArticleId in Proffix hat nicht geklappt
                    Logger.GetInstance().Log(LogLevel.Exception, "... des bereits in FLS vorhandenen Artikels. " + " ArtikelNr:" + proffixArticle.ArtikelNr + " " + proffixArticle.Bezeichnung1);
                    throw new Exception("Fehler beim Updaten der ArticleId in Proffix ArtikelNr ");
                }

                // wenn bis hierher --> update (in Artikel in FLS und ArticleId in Proffix) hat geklappt
                logComplete("Aktualisiert in FLS: ArticleNr " + proffixArticle.ArtikelNr + " Bezeichnung " + proffixArticle.Bezeichnung1, LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                logComplete("Fehler beim Updaten des Artikels in FLS ArtikelNr: " + proffixArticle.ArtikelNr, LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return false;
            }
        }

        private bool createInFLS(pxKommunikation.pxArtikel proffixArticle)
        {
            var newFLSarticle = new JObject();
            string response_FLS = string.Empty;
            string fehler = string.Empty;
            try
            {
                // JSON für neuen Artikel erstellen
                newFLSarticle = articleMapper.Mapp(proffixArticle, newFLSarticle);
                if (FlsGliderSync.logAusfuehrlich)
                {
                    Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPIArticlesMethod);
                    Logger.GetInstance().Log(LogLevel.Info, newFLSarticle.ToString());
                }
                // Artikel in FLS erstellen
                response_FLS = flsConn.ExportChanges("", newFLSarticle, ExporterCommitCommand.Create);

                // Wenn InternalServerError und ArtikelName in FLS bereits vorhanden (unique) --> Fehlermeldung
                if (response_FLS.Contains("InternalServerError") & articleNameExistsAlreadyInFLS(newFLSarticle["ArticleName"].ToString()))
                {

                    // Anweisungen an User
                    logComplete("Fehler: In FLS besteht bereits ein Artikel mit dem Artikelnamen/Bezeichnung1 \"" + proffixArticle.Bezeichnung1 + "\" ArtikelNr: " + newFLSarticle["ArticleNumber"].ToString() + "Deshalb konnte der Artikel in FLS nicht neu erstellt werden." + Constants.vbCrLf + "Sie haben folgende Möglichkeiten:" + Constants.vbCrLf + "- Ändern Sie den Artikelnamen/Bezeichnung1 \"" + proffixArticle.Bezeichnung1 + "\"des Artikels" + Constants.vbCrLf + "- Falls der Artikel mit Artikel-Nr. " + newFLSarticle["ArticleNumber"].ToString() + " Artikelname/Bezeichnung1 " + proffixArticle.Bezeichnung1 + " gelöscht noch vorhanden ist, entfernen Sie das Häckchen bei \"gelöscht\"", LogLevel.Exception);
                    // logcomplete( "- Erstellen Sie einen neuen Artikel mit der Artikel-Nr: " + newFLSarticle("ArticleNumber").ToString + "Artikelname/Bezeichnung1: " + proffixArticle.Bezeichnung1 + " und löschen Sie den Artikel mit der Artikel-Nr: " + proffixArticle.ArtikelNr)
                    Logger.GetInstance().Log(LogLevel.Exception, "In FLS besteht bereits ein Artikel mit dem Artikelnamen/Bezeichnung \"" + proffixArticle.Bezeichnung1 + "\" ArtikelNr: " + newFLSarticle["ArticleNumber"].ToString() + " Anweisungen an Kunde im Log");
                    return false;
                }

                // Ist response_FLS GUID? --> create hat geklappt, ansonsten enthält response_FLS die Fehlermeldung
                if (!pattern_GUID.IsMatch(response_FLS))
                {
                    logComplete("Fehler beim Erstellen in FLS: " + proffixArticle.ArtikelNr + " " + proffixArticle.Bezeichnung1, LogLevel.Exception, response_FLS);
                    return false;
                }

                // response_FLS ist GUID --> in Proffix updaten
                if (!updateArticleIdInProffix(response_FLS, proffixArticle.ArtikelNr, ref fehler))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "... des soeben in FLS neu erstellten Artikels. " + " ArtikelNr:" + proffixArticle.ArtikelNr + " " + proffixArticle.Bezeichnung1);
                    return false;
                }

                // wenn bis hierher --> create Artikel in FLS und update ArticleId in Proffix hat geklappt
                logComplete("Erstellt in FLS: " + proffixArticle.ArtikelNr + " " + proffixArticle.Bezeichnung1, LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                logComplete("Fehler beim Erstellen des Artikels in FLS ArtikelNr: " + proffixArticle.ArtikelNr, LogLevel.Exception, response_FLS + " " + ex.Message);
                return default;
            }
        }

        // prüft, ob Artikelname/Bezeichnung1 bereits in FLS in Artikeltabelle vorhanden ist (Feld ist unique)
        private bool articleNameExistsAlreadyInFLS(string articlename)
        {
            System.Threading.Tasks.Task<JArray> articleResult;

            // alle Artikel aus Artikeltabelle in FLS holen
            articleResult = flsConn.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPIArticlesMethod);
            articleResult.Wait();

            // ist der Artikelname bereits in FLS vorhanden?
            foreach (var article in articleResult.Result.Children())
            {

                // Artikelname/Bezeichnung in FLS bereits vorhanden
                if ((articlename ?? "") == (article["ArticleName"].ToString() ?? ""))
                {
                    return true;
                }
            }

            // wenn bis hierher --> Artikelname in FLS in Artikeltabelle noch nicht vorhanden
            return false;
        }

        private bool updateArticleIdInProffix(string FLSarticleId, string pxArtikelNr, ref string fehler)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();

            // Artikel mit neuer ArtikelId in Proffix schreiben
            sql = "Update lag_artikel set Z_ArticleId = '" + FLSarticleId + "', " + "geaendertVon = '" + Assembly.GetExecutingAssembly().GetName().Name + "', geaendertAm = '" + DateAndTime.Now.ToString(pxHelper.dateformat + " HH:mm:ss") + "' " + "where ArtikelNrLAG = '" + pxArtikelNr + "'";

            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Updaten der ArticleId " + FLSarticleId + " in Proffix");
                return false;
            }
            else
            {
                return true;
            }
        }

        private void InvokeDoProgress()
        {
            if (DoProgress is object)
                DoProgress.Invoke();
        }


        // schreibt in Log und in Logger (File)
        private void logComplete(string logString, LogLevel loglevel, string zusatzloggerString = "")
        {
            if (Log is object)
                Log.Invoke((loglevel != LogLevel.Info ? Constants.vbTab : "") + logString);
            Logger.GetInstance().Log(loglevel, logString + " " + zusatzloggerString);
        }

        /// <summary>
    /// Anzeigen des Synchronisationsfortschritt
    /// </summary>
        private void DoExporterProgress()
        {
            // ProgressBar aktualisieren
            My.MyProject.Forms.FrmMain.pbMain.Maximum = Count;
            My.MyProject.Forms.FrmMain.pbMain.Value = Progress;
        }
    }
}