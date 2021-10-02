using System;
using SMC.Lib;

namespace FlsGliderSync
{
    public class ProffixConnection
    {
        internal string strConnectString = FlsGliderSync.Proffix.Settings.ConnectionString;
        private ADODB.Connection conn = new ADODB.Connection();

        public ProffixConnection(string connectString = "")
        {
            try
            {
                if (string.IsNullOrEmpty(connectString))
                {
                    conn.ConnectionString = strConnectString;
                }
                else
                {
                    conn.ConnectionString = connectString;
                }

                conn.Open();
                if (!(conn.State == (int)ADODB.ObjectStateEnum.adStateOpen))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Es konnte keine Verbindung hergestellt werdens");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, ex.Message + ex.StackTrace);
            }
        }

        public void close()
        {
            conn.Close();
        }

        public bool getRecord(ref ADODB.Recordset rs, string sql, ref string Fehler, bool looped = false)
        {
            if (conn.State == (int)ADODB.ObjectStateEnum.adStateOpen)
            {
                try
                {
                    rs.Open(sql, conn);
                }
                catch (Exception ex)
                {
                    if (!looped)
                    {
                        Logger.GetInstance().Log(LogLevel.Exception, ex.Message + ex.StackTrace + sql);
                    }

                    Fehler = ex.Message;
                    return false;
                }
            }
            else
            {
                Fehler = "Keine Verbindung vorhanden";
                return false;
            }

            return true;
        }
    }
}