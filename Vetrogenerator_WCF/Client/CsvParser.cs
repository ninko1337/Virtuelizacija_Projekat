using System.Collections.Generic;
using Common;

namespace Client
{
    public static class CsvParser
    {
        /// <summary>
        /// Parsira jedan Kelmarsh SCADA CSV fajl.
        /// - Preskače prvih 9 redova
        /// - 10. red je zaglavlje (header)
        /// - Podaci počinju od 11. reda
        /// - Koristi InvariantCulture (decimalna tačka)
        /// - NaN vrednosti → skip + log u client_errors.log
        /// </summary>
        /// <param name="path">Putanja do CSV fajla (npr. Data/Kelmarsh_1.csv)</param>
        /// <param name="turbineId">ID turbine koji se upisuje u svaki uzorak</param>
        /// <returns>Lista parsiranih uzoraka</returns>
        public static List<WindTurbineSample> Parse(string path, string turbineId)
        {
            // TODO:
            // - Otvoriti fajl sa StreamReader (Dispose pattern / using)
            // - Preskočiti prvih 9 redova
            // - Pročitati 10. red kao header
            // - Od 11. reda parsirati samo 10 izabranih kanala:
            //     Timestamp, WindSpeed, WindDirection, NacellePosition,
            //     Power, PotentialPower, PowerFactor, ReactivePower,
            //     GridFrequency, GeneratorRpm
            // - Problematične redove (NaN, loš format) upisati u client_errors.log
            // - Svaki uzorak dobija RowIndex (0-baziran) i TurbineId

            return new List<WindTurbineSample>();
        }
    }
}
