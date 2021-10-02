using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using static FlsGliderSync.Exporter;
using Newtonsoft.Json.Linq;

namespace FlsGliderSync
{

    /// <summary>
/// Den HTTP Request Typ
/// </summary>
    public enum HttpRequestType
    {
        POST,
        PUT,
        DELETE
    }

    /// <summary>
/// Der Client des FLS Services
/// </summary>
    public class FlsConnection
    {
        private const string WebMethodOverride = "X-HTTP-Method-Override";

        /// <summary>
    /// Das private HttpClient Objekt
    /// </summary>
        private HttpClient pClient { get; set; }
        /// <summary>
    /// Boolean der angibt ob der Benutzer Authentifiziert ist
    /// </summary>
        private bool pIsAuthenticated { get; set; }

        /// <summary>
    /// Boolean der angibt ob der Benutzer Authentifiziert ist und von ausserhalb zugreifbar ist
    /// </summary>
        public bool IsAuthenticated
        {
            get
            {
                return pIsAuthenticated;
            }
        }

        /// <summary>
    /// Initialisiert den FLS Client
    /// </summary>
        public FlsConnection()
        {
            // Initialisiert den Http Client
            pClient = new HttpClient();
        }


        // ************************************************************nur für Testbetrieb********************************************************************************
        // gibt die erfassten Flüge aus der Testdatenbank für die Verrechnung frei
        public void testDeliveriesErstellen()
        {
            var response = pClient.GetAsync("https://test.glider-fls.ch/api/v1/flights/validate");
            response.Wait();
            var respons2 = pClient.GetAsync("https://test.glider-fls.ch/api/v1/flights/lock/force");
            respons2.Wait();
            var response3 = pClient.GetAsync("https://test.glider-fls.ch/api/v1/deliveries/create");
            response3.Wait();
        }
        // *************************************************************************************************************************************************************

        /// <summary>
    /// Einloggen auf dem FLS Service
    /// </summary>
    /// <param name="userName">Der Benutzername</param>
    /// <param name="password">Das Passwort</param>
    /// <param name="tokenAddress">Den Pfad zur Webmehthode</param>
    /// <returns>Ein Boolean der angibt ob die Authentifizierung erfolgreich war</returns>
        public async System.Threading.Tasks.Task<bool> Login(string userName, string password, string tokenAddress)
        {

            // Header leeren, damit bei Änderung der Logindaten nicht weiterhin Zugang
            pClient.DefaultRequestHeaders.Authorization = null;
            try
            {
                // Aufruf der Loggin Mehtode auf dem Service
                string token = await GetToken(userName, password, tokenAddress);
                var json = JObject.Parse(token);
                string accessToken = json["access_token"].ToString();

                // Den Authorization Header setzten
                pClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                return true;
            }
            catch
            {
                pIsAuthenticated = false;
            }

            return IsAuthenticated;
        }

        // Public Function loadPerson(ByVal adressnr As Integer) As JObject
        // Dim response As Threading.Tasks.Task(Of HttpResponseMessage) = pClient.GetAsync(My.Settings.ServiceAPIPersonsMemberNrMethod + adressnr.ToString)
        // response.Wait()
        // End Function

        /// <summary>
    /// Laden des Token für die Authentifikation
    /// </summary>
    /// <param name="userName">Der Benutzername</param>
    /// <param name="password">Das Passwort</param>
    /// <param name="tokenAddress">Die Adresse der Webmethode</param>
    /// <returns>Der geladene Token</returns>
        private System.Threading.Tasks.Task<string> GetToken(string userName, string password, string tokenAddress)
        {

            // Definiert den Parameter der Webmethode
            var pairs = new List<KeyValuePair<string, string>>(new[] { new KeyValuePair<string, string>("grant_type", "password"), new KeyValuePair<string, string>("username", userName), new KeyValuePair<string, string>("Password", password) });
            var content = new FormUrlEncodedContent(pairs);

            // Aufruf der Webmethode
            using (var client = new HttpClient())
            {
                HttpResponseMessage response;
                response = client.PostAsync(tokenAddress, content).Result;
                return response.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
    /// Eine Webmehtode Asynchron laden
    /// </summary>
    /// <param name="path">Der Pfad der Methode</param>
    /// <returns>Das Resultat des Aufrufs</returns>
        public async System.Threading.Tasks.Task<JArray> CallAsyncAsJArray(string path)
        {
            // Ruft die Webmethode auf
            var response = pClient.GetAsync(path);
            response.Wait();
            string result = await response.Result.Content.ReadAsStringAsync();
            // Parst das resultat und gibt es zurück
            return JArray.Parse(result);
        }

        /// <summary>
    /// Eine Webmehtode Asynchron laden
    /// </summary>
    /// <param name="path">Der Pfad der Methode</param>
    /// <returns>Das Resultat des Aufrufs</returns>
        public async System.Threading.Tasks.Task<JObject> CallAsyncAsJObject(string path)
        {
            // Ruft die Webmethode auf
            var response = pClient.GetAsync(path);
            response.Wait();
            string result = await response.Result.Content.ReadAsStringAsync();
            // Parst das resultat und gibt es zurück
            return JObject.Parse(result);
        }


        /// <summary>
    /// Macht WebRequest um Daten in FLS zu schreiben (delete, update und create)
    /// </summary>
    /// <param name="id"></param>
    /// <param name="obj"></param>
    /// <param name="operation"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        public string SubmitChanges(string id, JObject obj, SyncerCommitCommand operation)
        {
            try
            {
                string newPersonId = "";
                HttpResponseMessage result = null;

                // Header leeren
                if (pClient.DefaultRequestHeaders.Contains(WebMethodOverride))
                {
                    pClient.DefaultRequestHeaders.Remove(WebMethodOverride);
                }

                // delete (in Proffix gelöschte Adressen)
                if (operation == SyncerCommitCommand.Delete)
                {
                    pClient.DefaultRequestHeaders.Add(WebMethodOverride, HttpRequestType.DELETE.ToString());
                    result = pClient.PostAsync(My.MySettingsProperty.Settings.ServiceAPIPersonMethod + id, new StringContent(obj.ToString(), Encoding.UTF8, "application/json")).Result;
                }

                // update (Änderungen + neu erstellte AdressNr in Proffix --> MemberNumber)
                else if (operation == SyncerCommitCommand.Update)
                {
                    pClient.DefaultRequestHeaders.Add(WebMethodOverride, HttpRequestType.PUT.ToString());
                    result = pClient.PostAsync(My.MySettingsProperty.Settings.ServiceAPIPersonMethod + id, new StringContent(obj.ToString(), Encoding.UTF8, "application/json")).Result;
                }

                // create (in Proffix neu erstellte Adresse in FLS erstellen)
                else if (operation == SyncerCommitCommand.Create)
                {
                    pClient.DefaultRequestHeaders.Add(WebMethodOverride, HttpRequestType.POST.ToString());
                    result = pClient.PostAsync(My.MySettingsProperty.Settings.ServiceAPIPersonMethod + id, new StringContent(obj.ToString(), Encoding.UTF8, "application/json")).Result;
                }

                // ***********************************************************RESPONSE AUSWERTEN + JE NACH RESULTAT ANDERER RÜCKGABEWERT****************************************
                // Response auswerten (gibt Fehler-bzw. Erfolgsstring zurück oder newPersonId wenn create)
                if ((int)result.StatusCode != 200) // 200 = erfolgreich --> alles andere = Fehler
                {
                    return result.StatusCode.ToString() + " " + result.ReasonPhrase;
                }

                // neu erstellte PersonId bei create auslesen, um in Proffix schreiben zu können
                else if (operation == SyncerCommitCommand.Create)
                {
                    var jobj = JObject.Parse(result.Content.ReadAsStringAsync().Result);
                    newPersonId = jobj["PersonId"].ToString();
                    return newPersonId;
                }
                // wenn update (NICHT create) --> Statuscode 200 zurückgeben
                else
                {
                    return result.StatusCode.ToString();
                }
            }
            catch (Exception ex)
            {
                return "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + obj.ToString();
            }
        }

        // exportiert die Artikel aus Proffix in FLS
        public string ExportChanges(string id, JObject obj, ExporterCommitCommand operation)
        {
            string newarticleId = "";
            HttpResponseMessage result = null;
            try
            {

                // Header leeren
                if (pClient.DefaultRequestHeaders.Contains(WebMethodOverride))
                {
                    pClient.DefaultRequestHeaders.Remove(WebMethodOverride);
                }

                // update Artikel in FLS (auch InActive = 1 ist ein update, entspricht dem Löschen des Artikels)
                if (operation == ExporterCommitCommand.Update)
                {
                    pClient.DefaultRequestHeaders.Add(WebMethodOverride, HttpRequestType.PUT.ToString());
                    result = pClient.PostAsync(My.MySettingsProperty.Settings.ServiceAPIArticlesMethod + id, new StringContent(obj.ToString(), Encoding.UTF8, "application/json")).Result;
                }

                // create Artikel in FLS
                else if (operation == ExporterCommitCommand.Create)
                {
                    pClient.DefaultRequestHeaders.Add(WebMethodOverride, HttpRequestType.POST.ToString());

                    // MsgBox("vor")
                    result = pClient.PostAsync(My.MySettingsProperty.Settings.ServiceAPIArticlesMethod, new StringContent(obj.ToString(), Encoding.UTF8, "application/json")).Result;
                    // MsgBox("nach")

                }

                // ***********************************************************RESPONSE AUSWERTEN + JE NACH RESULTAT ANDERER RÜCKGABEWERT****************************************
                // Response auswerten (gibt Fehler bzw. ArticleId
                if ((int)result.StatusCode != 200) // 200 = erfolgreich --> alles andere = Fehler
                {
                    return result.StatusCode.ToString() + " " + result.ReasonPhrase;
                }
                else
                {
                    // ArticleId auslesen, um in Proffix schreiben/updaten zu können
                    var jobj = JObject.Parse(result.Content.ReadAsStringAsync().Result);
                    newarticleId = jobj["ArticleId"].ToString();
                    return newarticleId;
                }
            }
            catch (Exception ex)
            {
                return "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message + " " + result.ReasonPhrase;
            }
        }

        // exportiert die Artikel aus Proffix in FLS
        public string submitFlag(string path, JObject obj)
        {
            HttpResponseMessage result = null;

            // Header leeren
            if (pClient.DefaultRequestHeaders.Contains(WebMethodOverride))
            {
                pClient.DefaultRequestHeaders.Remove(WebMethodOverride);
            }

            pClient.DefaultRequestHeaders.Add(WebMethodOverride, HttpRequestType.POST.ToString());
            result = pClient.PostAsync(path, new StringContent(obj.ToString(), Encoding.UTF8, "application/json")).Result;

            // ***********************************************************RESPONSE AUSWERTEN + JE NACH RESULTAT ANDERER RÜCKGABEWERT****************************************
            // Response auswerten (gibt Fehler-bzw. Erfolgsstring zurück oder newPersonId wenn create)
            if ((int)result.StatusCode != 200) // 200 = erfolgreich --> alles andere = Fehler
            {
                return result.StatusCode.ToString() + " " + result.ReasonPhrase;
            }
            else
            {
                return result.StatusCode.ToString();
            }
        }
    }
}