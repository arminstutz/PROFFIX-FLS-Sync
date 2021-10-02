using System;
using System.Collections.Generic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;

namespace FlsGliderSync
{

    /// <summary>
/// Das Interface der MappingProperty
/// </summary>
    public interface IMappingProperty
    {
        /// <summary>
    /// Der Name des Feldes in der Quell-Klasse
    /// </summary>
        string SourcePropertyName { get; set; }
        /// <summary>
    /// Der Name des Feldes in der Ziel-Klasse
    /// </summary>
        string TargetPropertyName { get; set; }

        /// <summary>
    /// Definition der Mapp Methode
    /// </summary>
    /// <param name="source">Das Quell-Objekt</param>
    /// <param name="target">Das Ziel-Objekt</param>
    /// <returns>Das neu gemappte Ziel-Objekt</returns>
        object Mapp(object source, ref object target);

        /// <summary>
    /// Definition der DeMapp Methode
    /// </summary>
    /// <param name="source">Das Quell-Objekt</param>
    /// <param name="target">Das Ziel-Objekt</param>
    /// <returns>Das neu gemappte Ziel-Objekt</returns>
        object DeMapp(object source, ref object target);

        /// <summary>
    /// Gibt den Wert des Source-Objekts zurück
    /// </summary>
    /// <param name="source">Das Objekt</param>
    /// <param name="name">Der Name des Feldes</param>
    /// <returns>Den Wert des Feldes</returns>
        object GetValue(object source, string name);

        /// <summary>
    /// Setzt den Wert des Target-Objekts
    /// </summary>
    /// <param name="target">Das Objekts</param>
    /// <param name="name">Der Name des Feldes</param>
    /// <param name="value">Den Wert der gesetzt wird</param>
        void SetValue(ref object target, string name, object value);
    }

    /// <summary>
/// Die Generische Implementation des IMappingProperty Interfaces
/// </summary>
/// <typeparam name="TP">Der erste Typ</typeparam>
/// <typeparam name="TA">Der zweite Typ</typeparam>
    public class MappProperty<TP, TA> : IMappingProperty
    {
        private Dictionary<string, string> laender_dict = null;

        /// <summary>
    /// Der Name des Feldes in der Quell-Klasse
    /// </summary>
        public string SourcePropertyName { get; set; }
        /// <summary>
    /// Der Name des Feldes in der Ziel-Klasse
    /// </summary>
        public string TargetPropertyName { get; set; }

        /// <summary>
    /// Parst das Objekt vom Typ TP zum Typ TA
    /// </summary>
    /// <value>Das Objekt vom Typ TP</value>
    /// <returns>Das Objekt vom Typ TA</returns>
        public Func<TP, TA> ToTargetProperty { get; set; }
        /// <summary>
    /// Parst das Objekt vom Typ TA zum Typ TP
    /// </summary>
    /// <value>Das Objekt vom Typ TA</value>
    /// <returns>Das Objekt vom Typ TP</returns>
        public Func<TA, TP> ToSourceProperty { get; set; }

        /// <summary>
    /// Setzt alle Werte des ersten Objekts gleich wie die des zweiten Objekts
    /// </summary>
    /// <value>Das Source-Objekt und das Ziel-Objekt</value>
    /// <returns>Das neue Ziel-Objekt</returns>
        public Func<object, object, object> MappFunc { get; set; }
        /// <summary>
    /// Setzt alle Werte des ersten Objekts gleich wie die des zweiten Objekts
    /// </summary>
    /// <value>Das Source-Objekt und das Ziel-Objekt</value>
    /// <returns>Das neue Ziel-Objekt</returns>
        public Func<object, object, object> DeMappFunc { get; set; }

        /// <summary>
    /// Gibt den Wert des Felds des Objekts zurück
    /// </summary>
    /// <value>Das Objekt und der Name des Feldes</value>
    /// <returns>Der Wert des Felds</returns>
        public Func<object, string, object> GetValueFunc { get; set; }
        /// <summary>
    /// Setzt den Wert des Felds des Objekts
    /// </summary>
    /// <value>Das Objekt, der Name des Fels und der neue Wert</value>
    /// <returns>Das neue Objekt</returns>
        public Func<object, string, object, object> SetValueFunc { get; set; }

        /// <summary>
    /// Initialisiert die Klasse
    /// </summary>
    /// <param name="targetPropertyName">Der Name des Ziel-Felds</param>
    /// <param name="sourcePropertyName">Der Name des Quell-Felds</param>
        public MappProperty(string targetPropertyName, string sourcePropertyName) : this(sourcePropertyName, targetPropertyName, null, null)
        {
        }

        /// <summary>
    /// Initialisiert die Klasse
    /// </summary>
    /// <param name="targetPropertyName">Der Name des Ziel-Felds</param>
    /// <param name="sourcePropertyName">Der Name des Quell-Felds</param>
    /// <param name="toTargetProperty">Die Parse Funktion des Ziel Objekts</param>
    /// <param name="toSourceProperty">Die Parse Funktion des Quell Objekts</param>
        public MappProperty(string targetPropertyName, string sourcePropertyName, Func<TP, TA> toTargetProperty, Func<TA, TP> toSourceProperty)
        {
            SourcePropertyName = sourcePropertyName;
            TargetPropertyName = targetPropertyName;
            ToTargetProperty = toTargetProperty;
            ToSourceProperty = toSourceProperty;
        }

        /// <summary>
    /// Initialisiert die Klasse
    /// </summary>
    /// <param name="targetPropertyName">Der Name des Ziel-Felds</param>
    /// <param name="sourcePropertyName">Der Name des Quell-Felds</param>
    /// <param name="toTargetProperty">Die Parse Funktion des Ziel Objekts</param>
    /// <param name="toSourceProperty">Die Parse Funktion des Quell Objekts</param>
    /// <param name="mappFunc">Die Mapping Funktion</param>
    /// <param name="demappFunc">Die DeMapping Funktion</param>
        public MappProperty(string targetPropertyName, string sourcePropertyName, Func<TP, TA> toTargetProperty, Func<TA, TP> toSourceProperty, Func<object, object, object> mappFunc, Func<object, object, object> demappFunc)
        {
            SourcePropertyName = sourcePropertyName;
            TargetPropertyName = targetPropertyName;
            ToTargetProperty = toTargetProperty;
            ToSourceProperty = toSourceProperty;
            MappFunc = mappFunc;
            DeMappFunc = demappFunc;
        }

        /// <summary>
    /// Initialisiert die Klasse
    /// </summary>
    /// <param name="targetPropertyName">Der Name des Ziel-Felds</param>
    /// <param name="sourcePropertyName">Der Name des Quell-Felds</param>
    /// <param name="toTargetProperty">Die Parse Funktion des Ziel Objekts</param>
    /// <param name="toSourceProperty">Die Parse Funktion des Quell Objekts</param>
    /// <param name="mappFunc">Die Mapping Funktion</param>
    /// <param name="demappFunc">Die DeMapping Funktion</param>
    /// <param name="getValueFunc">Die Funtkion um den Wert eines Felds zu lesen</param>
    /// <param name="setValueFunc">Die Funktion um den Wert eines Felds zu schreiben</param>
        public MappProperty(string targetPropertyName, string sourcePropertyName, Func<TP, TA> toTargetProperty, Func<TA, TP> toSourceProperty, Func<object, object, object> mappFunc, Func<object, object, object> demappFunc, Func<object, string, object> getValueFunc, Func<object, string, object, object> setValueFunc)
        {
            SourcePropertyName = sourcePropertyName;
            TargetPropertyName = targetPropertyName;
            ToTargetProperty = toTargetProperty;
            ToSourceProperty = toSourceProperty;
            MappFunc = mappFunc;
            DeMappFunc = demappFunc;
            GetValueFunc = getValueFunc;
            SetValueFunc = setValueFunc;
        }

        /// <summary>
    /// Mappt die Werte des Source-Objekts zum Target-Objekt
    /// </summary>
    /// <param name="source">Das Quell-Objekt</param>
    /// <param name="target">Das Ziel-Objekt</param>
    /// <returns>Das neue Objekt</returns>
        public object Mapp(object source, ref object target)
        {
            // Wenn eine spezielle Mapping Funtkion übergeben wurde wird diese verwendet
            if (MappFunc is object)
            {
                return MappFunc(source, target);
            }
            else
            {
                // Anderenfalls wird die "GetValue" Mehtode aufgerufen
                var data = GetValue(source, SourcePropertyName);


                // Wenn der Wert in einen anderen Typ geparst werden muss wird das hier gemacht
                if (ToSourceProperty is object)
                {
                    data = ToSourceProperty.Invoke((TA)data);
                }

                // Als letztes wird der Wert auf das Ziel-Objekt gesetzt ()
                SetValue(ref target, TargetPropertyName, data);
                return target;
            }
        }

        /// <summary>
    /// DeMappt die Werte des Source-Objekt zum Target-Objekt
    /// </summary>
    /// <param name="source">Das Quell-Objekt</param>
    /// <param name="target">Das Ziel-Objekt</param>
    /// <returns>Das neue Objekt</returns>
        public object DeMapp(object source, ref object target)
        {
            // Wenn eine spezielle DeMapping Funtkion übergeben wurde wird diese verwendet
            if (DeMappFunc is object)
            {
                return DeMappFunc(source, target);
            }
            else
            {
                // Anderenfalls wird die "GetValue" Mehtode aufgerufen
                var data = GetValue(source, TargetPropertyName);
                // Wenn der Wert in einen anderen Typ geparst werden muss wird das hier gemacht
                if (ToTargetProperty is object)
                {
                    data = ToTargetProperty.Invoke((TP)data);
                }
                // Als letztes wird der Wert auf das Ziel-Objekt gesetzt
                SetValue(ref target, SourcePropertyName, data);
                return target;
            }
        }

        /// <summary>
    /// Gibt den Wert des Feldes zurück
    /// </summary>
    /// <param name="source">Das Quell-Objekt</param>
    /// <param name="name">Der Name des Fels</param>
    /// <returns>Der Wert des Fels</returns>
        public object GetValue(object source, string name)
        {
            // Wenn eine spezielle 'GetValue' Funktion definiert wurde wird diese verwendet
            if (GetValueFunc is object)
                return GetValueFunc(source, name);

            // Anderenfalls wird geprüft ob es ein JSON Objekt ist sich dieses anders verhält als eine gewönliche Klasse
            object data;
            if (ReferenceEquals(source.GetType(), typeof(JObject)))
            {
                // Lesen der Daten wenn es ein JSON Objekt ist
                data = ((JObject)source)[name];
                if (data is object)
                {
                    data = data.ToString();
                }
            }
            else
            {
                // Es ist kein JSON Objekt
                // Der Wert wird als Property oder Feld gelesen
                data = source.GetType().GetProperty(name);
                if (data is null)
                {
                    data = source.GetType().GetField(name).GetValue(source);
                }
                else
                {
                    data = ((System.Reflection.PropertyInfo)data).GetValue(source, null);
                }
            }

            return data;
        }


        /// <summary>
    /// Setzt den Wert des Feldes
    /// </summary>
    /// <param name="target">Das Ziel-Objekt</param>
    /// <param name="name">Der Name des Felds</param>
    /// <param name="value">Der Wert des Felds</param>
        public void SetValue(ref object target, string name, object value)
        {
            bool isNull = false;

            // Wenn eine spezielle 'SetValue' Funktion definiert wurde wird diese verwendet
            if (SetValueFunc is object)
            {
                target = SetValueFunc.Invoke(target, name, value);
                return;
            }

            // Wenn das auszufüllende Objekt ein JSON Objekt ist
            if (ReferenceEquals(target.GetType(), typeof(JObject)))
            {

                // wenn kein value keinen Wert hat
                if (value is null)
                {
                    // FLS verlangt einen Vornamen
                    if (name == "Firstname")
                    {
                        value = ".";
                        ((JObject)target)[name] = value.ToString();
                    }
                    else
                    {
                        isNull = true;
                    }
                }

                // wenn value einen Wert hat
                // ist es ein Datum?
                else if (ReferenceEquals(value.GetType(), typeof(DateTime)) | ReferenceEquals(value.GetType(), typeof(DateTime)))
                {
                    // In einer als Datetime definierten Variable speichern (erst dann können datetimefunktionen aufgerufen werden)
                    DateTime datum_datetime = Conversions.ToDate(value);

                    // wenn kein Datum eingefügt ist --> nichts hineinschreiben (bei keinem Datum ist day, month und year = 1 und hour, minute and second = 0)
                    if (datum_datetime.Day + datum_datetime.Month + datum_datetime.Year == 3)
                    {
                        isNull = true;
                    }

                    // es soll ein gültiges Date/Datetime hineingeschrieben werden
                    else
                    {
                        ((JObject)target)[name] = datum_datetime.ToString("o");
                    }
                }

                // es ist kein Datum einzufügen sondern String oder Boolean

                // wenn in Proffix Defaultwerte für Ort + PLZ --> in FLS nichts übertragen
                else if (name == "ZipCode" & value.ToString() == "9999" | name == "City" & value.ToString() == "Ort unbekannt")
                {
                    isNull = true;
                }
                else
                {

                    // wenn Land --> CountryId einfügen (auch in MappingProperties einfügen
                    if (name == "CountryId")
                    {
                        value = landToCountryId(value.ToString());

                        // wenn bis hierher gekommen --> value für name normal einfügen
                    } ((JObject)target)[name] = value.ToString();
                }

                // null, damit vorher ausgefüllte Felder leerbar
                if (isNull)
                {
                    ((JObject)target)[name] = null;
                }
            }
            else
            {
                // CountryId in Landeskürzel umwandeln 
                if (name == "Land")
                {
                    if (value is object)
                    {
                        value = CountryIdToLand(value.ToString());
                    }
                }

                // Es ist kein JSON Objekt
                object data = target.GetType().GetProperty(name);
                if (data is null)
                {
                    ValueType objTarget = (ValueType)target;
                    target.GetType().GetField(name).SetValue(objTarget, value);
                    target = objTarget;
                }
                else
                {

                    // wenn bis hierher gekommen --> value für name normal einfügen
                    ((JObject)target)[name] = value.ToString();
                    ((System.Reflection.PropertyInfo)data).SetValue(target, value, null);
                }
            }
        }

        // gibt für einen Ländernamen die entsprechende CountryId zurück
        private string landToCountryId(string value)
        {
            laender_dict = GeneralDataLoader.GetLaender;
            // entsprechende Werte aus Dict auslesen (muss in
            if (laender_dict.ContainsKey(value.ToString()))
            {
                value = laender_dict[value.ToString()];
            }
            else
            {
                value = null;
            }

            return value;
        }

        // CountryId in Land umwandeln 
        private string CountryIdToLand(string value)
        {
            foreach (var pair in GeneralDataLoader.GetLaender)
            {
                if ((pair.Value ?? "") == (value ?? ""))
                {
                    value = pair.Key;
                }
            }

            return value;
        }


        // ' Spezielle Werte abfangen, die bei der Synchronisation nicht richtig übertragen würden
        // Private Function isSpecialValue(ByVal name As String, ByVal value As Object) As Boolean

        // ' wenn ein Wert leer ist --> nichts in JSON schreiben
        // If value Is Nothing Then
        // Return True
        // ElseIf value.ToString = "" Then
        // Return True
        // ' wenn in Proffix kein Datum --> würde 1.1.01 in FLS schreiben --> abfangen --> nichts in JSON schreiben
        // ' ElseIf value.ToString.Contains("01.01.0001") Or value.ToString.Contains("0001-01-01") Then
        // '    Return True
        // ' wenn in Proffix Defaultwerte für Ort + PLZ --> in FLS nichts übertragen
        // ElseIf (name = "ZipCode" And value.ToString = "9999") Or (name = "City" And value.ToString = "Ort unbekannt") Then
        // Return True
        // End If

        // ' wenn bis hierher gekommen --> kein Spezialfall --> false zurückgeben = kein Spezialfall
        // Return False
        // End Function

    }

    /// <summary>
/// Nicht generische Implementation des IMappingProperty Interface
/// </summary>
    public class MappingProperty : MappProperty<object, object>
    {

        /// <summary>
    /// Initialisiert die Klasse
    /// </summary>
    /// <param name="personPropertyName">Der Name des Ziel-Felds</param>
    /// <param name="addressPropertyName">Der Name des Quell-Felds</param>
        public MappingProperty(string personPropertyName, string addressPropertyName) : base(personPropertyName, addressPropertyName, null, null)
        {
        }

        /// <summary>
    /// Initialisiert die Klasse
    /// </summary>
    /// <param name="personPropertyName">Der Name des Ziel-Felds</param>
    /// <param name="addressPropertyName">Der Name des Quell-Felds</param>
    /// <param name="toAdressProperty">Die Parse Funktion des Ziel Objekts</param>
    /// <param name="toPersonProperty">Die Parse Funktion des Quell Objekts</param>
        public MappingProperty(string personPropertyName, string addressPropertyName, Func<object, object> toAdressProperty, Func<object, object> toPersonProperty) : base(personPropertyName, addressPropertyName, toAdressProperty, toPersonProperty)
        {
        }

        /// <summary>
    /// Initialisiert die Klasse
    /// </summary>
    /// <param name="personPropertyName">Der Name des Ziel-Felds</param>
    /// <param name="addressPropertyName">Der Name des Quell-Felds</param>
    /// <param name="toAdressProperty">Die Parse Funktion des Ziel Objekts</param>
    /// <param name="toPersonProperty">Die Parse Funktion des Quell Objekts</param>
    /// <param name="mappFunc">Die Mapping Funktion</param>
    /// <param name="demappFunc">Die DeMapping Funktion</param>
        public MappingProperty(string personPropertyName, string addressPropertyName, Func<object, object> toAdressProperty, Func<object, object> toPersonProperty, Func<object, object, object> mappFunc, Func<object, object, object> demappFunc) : base(personPropertyName, addressPropertyName, toAdressProperty, toPersonProperty, mappFunc, demappFunc)
        {
        }

        /// <summary>
    /// Initialisiert die Klasse
    /// </summary>
    /// <param name="personPropertyName">Der Name des Ziel-Felds</param>
    /// <param name="addressPropertyName">Der Name des Quell-Felds</param>
    /// <param name="toAdressProperty">Die Parse Funktion des Ziel Objekts</param>
    /// <param name="toPersonProperty">Die Parse Funktion des Quell Objekts</param>
    /// <param name="mappFunc">Die Mapping Funktion</param>
    /// <param name="demappFunc">Die DeMapping Funktion</param>
    /// <param name="getValueFunc">Die Funtkion um den Wert eines Felds zu lesen</param>
    /// <param name="setValueFunc">Die Funktion um den Wert eines Felds zu schreiben</param>
        public MappingProperty(string personPropertyName, string addressPropertyName, Func<object, object> toAdressProperty, Func<object, object> toPersonProperty, Func<object, object, object> mappFunc, Func<object, object, object> demappFunc, Func<object, string, object> getValueFunc, Func<object, string, object, object> setValueFunc) : base(personPropertyName, addressPropertyName, toAdressProperty, toPersonProperty, mappFunc, demappFunc, getValueFunc, setValueFunc)
        {
        }
    }
}

// alt:
// wenn das Datum keine Zeit enthält --> + 2h, damit bei Umwandlung in UTC (rechnet in Sommerzeit -2h) nicht in Vortag rutscht
// If datum_datetime.Hour + datum_datetime.Minute + datum_datetime.Second = 0 Then
// datum_datetime.AddHours(3)
// End If

// in UTC-Format in FLS schreiben
// CType(target, JObject)(name) = datum_datetime.ToUniversalTime.ToString("o")
