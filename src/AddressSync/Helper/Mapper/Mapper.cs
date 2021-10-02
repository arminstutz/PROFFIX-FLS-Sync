using System.Collections.Generic;
using System.Linq;

namespace FlsGliderSync
{

    /// <summary>
/// Die Basisklasse des Mappers der generisch mehere Felder von zwei verschiedenen Typen verbindet
/// </summary>
/// <typeparam name="TS">Der erste Typ</typeparam>
/// <typeparam name="TT">Der zweite Typ</typeparam>
    public abstract class Mapper<TS, TT>
    {

        /// <summary>
    /// Die Einstellungen der Mappings
    /// </summary>
        public static List<IMappingProperty> MappingProperies
        {
            get
            {
                return null;
            }
        }

        /// <summary>
    /// Die Werte der Felder des 'TS' Objekts werden in die Felder des 'TT' Objekts geschrieben
    /// </summary>
    /// <param name="source">Das Objekt aus dem die Felder gelesen werden</param>
    /// <param name="target">Das Objekt in das die Felder gespeichert werden</param>
    /// <returns>Das neu gemappte TT Objekt</returns>
        public TT Mapp(TS source, TT target)
        {
            // Lesen der Mappingproperties
            object objTarget = target;
            List<IMappingProperty> mp = (List<IMappingProperty>)GetType().GetProperty("MappingProperies").GetValue(this, null);

            // Iteration durch die Properties (--> PersonMapper bzw. ClubMapper) und ausführung deren 'Mapp' funktion
            target = (TT)mp.Aggregate(objTarget, (current, mapping) => mapping.Mapp(source, ref current));
            return target;
        }

        /// <summary>
    /// Die Werte der Felder des 'TT' Objekts werden in die Felder des 'TS' Objekts geschrieben
    /// </summary>
    /// <param name="target">Das Objekt in das die Felder gespeichert werden</param>
    /// <param name="source">Das Objekt aus dem die Felder gelesen werden</param>
    /// <returns>Das neu gemappte TS Objekt</returns>
        public TS DeMapp(TS target, TT source)
        {
            // Lesen der Mappingproperties
            object objTarget = target;
            var objSource = source;

            // Holt MappingPropertys
            List<IMappingProperty> mp = (List<IMappingProperty>)GetType().GetProperty("MappingProperies").GetValue(this, null);

            // Iteration durch die Properties (--> PersonMapper bzw. ClubMapper) und ausführung deren 'DeMapp' funktion
            target = (TS)mp.Aggregate(objTarget, (current, mapping) => mapping.DeMapp(objSource, ref current));
            return target;
        }
    }
}