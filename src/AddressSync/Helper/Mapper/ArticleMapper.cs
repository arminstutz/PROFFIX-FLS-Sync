using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using pxBook;

namespace FlsGliderSync
{
    public class ArticleMapper
    {

        // Dictionary der Felder, die im FLS <-> Proffix entsprechen
        private Dictionary<string, string> MappingProperties
        {
            get
            {
                var articleProperties_dict = new Dictionary<string, string>();
                articleProperties_dict.Add("ArticleNumber", "ArtikelNr");
                articleProperties_dict.Add("ArticleName", "Bezeichnung1");
                articleProperties_dict.Add("ArticleInfo", "Bezeichnung2");
                articleProperties_dict.Add("Description", "Bezeichnung3");
                articleProperties_dict.Add("IsActive", "Geloescht");
                return articleProperties_dict;
            }
        }

        private Exporter Exporter { get; set; }



        // updatet target mit Werten aus source
        public JObject Mapp(pxKommunikation.pxArtikel source, JObject target)
        {
            object data;

            // holt für jede Artikeleigenschaft den Wert aus pxArtikel und setzt sie in JSON ein
            foreach (var item in MappingProperties)
            {
                data = GetValue(source, item.Value.ToString());
                object argtarget = target;
                SetValue(ref argtarget, item.Key.ToString(), data);
            }

            // gibt geupdatetes JSON zurück
            return target;
        }

        /// <summary>
    /// Liest Wert aus pxArtikel
    /// </summary>
    /// <param name="source"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        public object GetValue(object source, string name)
        {
            object data;

            // Feld wird aus pxArtikel gelesen
            data = source.GetType().GetProperty(name);
            if (data is null)
            {
                data = source.GetType().GetField(name).GetValue(source);
            }
            else
            {
                data = ((System.Reflection.PropertyInfo)data).GetValue(source, null);
            }

            return data;
        }


        /// <summary>
    /// Schreibt wert in ein JSON
    /// </summary>
    /// <param name="target"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <remarks></remarks>
        public void SetValue(ref object target, string name, object value)
        {

            // wenn value keinen Wert hat --> null in JSON
            if (value is null)
            {
                ((JObject)target)[name] = null;
            }

            // wenn value einen Wert hat --> Wert für name einfügen
            else if (name == "IsActive")
            {
                JValue jValue = (JValue)false;
                if (value.ToString() == "0")
                {
                    jValue = (JValue)true;
                }
                else
                {
                    jValue = (JValue)false;
                } ((JObject)target)[name] = jValue;
            }
            else
            {
                ((JObject)target)[name] = value.ToString();
            }
        }
    }
}