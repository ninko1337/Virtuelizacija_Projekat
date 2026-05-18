using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Client
{
    public static class CsvParser
    {

        /// Parsira jedan Kelmarsh SCADA CSV fajl.
        /// - Preskače prvih 9 redova
        /// - 10. red je zaglavlje (header)
        /// - Podaci počinju od 11. reda
        /// - Koristi InvariantCulture (decimalna tačka)
        /// - NaN vrednosti → skip + log u client_errors.log
        /// - Nan vrednosti ili loš format - skip - log u clienterror.log
        /// - DisposePatern - StreamReader i StreamWriter se zatvaraju u using blokovima
        /// 
        private const string ColTimestamp = "# Date and time";
        private const string ColWindSpeed = "Wind speed (m/s)";
        private const string ColWindDirection = "Wind direction (\u00b0)";
        private const string ColNacellePosition = "Nacelle position (\u00b0)";
        private const string ColPower = "Power (kW)";
        private const string ColPotentialPower = "Potential power default PC (kW)";
        private const string ColPowerFactor = "Power factor (cosphi)";
        private const string ColReactivePower = "Reactive power (kvar)";
        private const string ColGridFrequency = "Grid frequency (Hz)";
        private const string ColGeneratorRpm = "Generator RPM (RPM)";

        /// <summary>
        /// ovo za logger samo
        /// </summary>
        private const string ErrorLogFile = "client_errors.log";

        public static List<WindTurbineSample> Parse(string path, string turbineId)
        {
            var samples = new List<WindTurbineSample>();

            using (var errorWriter = new StreamWriter(ErrorLogFile, append: true))
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(fs))
            {
                // Preskače se prvih 9 redova
                for (int skip = 0; skip < 9; skip++)
                {
                    string skipped = reader.ReadLine();
                    if (skipped == null)
                    {
                        errorWriter.WriteLine($"[FATAL] Fajl '{path}' ima manje od 9 redova komentara.");
                        return samples;
                    }
                }

                // 10. red  je heder
                string headerLine = reader.ReadLine();
                if (headerLine == null)
                {
                    errorWriter.WriteLine($"[FATAL] Fajl '{path}' nema header red (red 10).");
                    return samples;
                }

                string[] headers = SplitCsvLine(headerLine);
                var colIndex = BuildColumnMap(headers);

                // Da li su sve kolone prisutne , koje trebaju
                string[] requiredCols = {
                    ColTimestamp, ColWindSpeed, ColWindDirection, ColNacellePosition,
                    ColPower, ColPotentialPower, ColPowerFactor, ColReactivePower,
                    ColGridFrequency, ColGeneratorRpm
                };

                foreach (var col in requiredCols)
                {
                    if (!colIndex.ContainsKey(col))
                    {
                        errorWriter.WriteLine($"[FATAL] Kolona '{col}' nije pronađena u headeru fajla '{path}'.");
                        return samples;
                    }
                }

                // Čitanje podataka od reda 11
                string line;
                int rowIndex = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) { rowIndex++; continue; }

                    string[] fields = SplitCsvLine(line);

                    WindTurbineSample sample;
                    string errorReason;

                    if (TryParseSample(fields, colIndex, turbineId, rowIndex, out sample, out errorReason))
                    {
                        samples.Add(sample);
                    }
                    else
                    {
                        errorWriter.WriteLine($"[SKIP] Red {rowIndex + 11} | Razlog: {errorReason} | Sadržaj: {line.Substring(0, Math.Min(120, line.Length))}...");
                    }

                    rowIndex++;
                }
            }

            return samples;
        }

        private static bool TryParseSample(
           string[] fields,
           Dictionary<string, int> colIndex,
           string turbineId,
           int rowIndex,
           out WindTurbineSample sample,
           out string errorReason)
        {
            sample = null;
            errorReason = null;

            string tsRaw = GetField(fields, colIndex, ColTimestamp);
            if (IsNanOrEmpty(tsRaw)) { errorReason = $"Timestamp je NaN/prazan: '{tsRaw}'"; return false; }

            DateTime timestamp;
            if (!DateTime.TryParse(tsRaw, CultureInfo.InvariantCulture,
                                   DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                   out timestamp))
            {
                errorReason = $"Timestamp se ne može parsirati: '{tsRaw}'";
                return false;
            }

            double windSpeed, windDir, nacellePos, power, potPower, powerFactor, reactivePower, gridFreq, genRpm;

            if (!TryParseDouble(fields, colIndex, ColWindSpeed, out windSpeed, out errorReason)) return false;
            if (!TryParseDouble(fields, colIndex, ColWindDirection, out windDir, out errorReason)) return false;
            if (!TryParseDouble(fields, colIndex, ColNacellePosition, out nacellePos, out errorReason)) return false;
            if (!TryParseDouble(fields, colIndex, ColPower, out power, out errorReason)) return false;
            if (!TryParseDouble(fields, colIndex, ColPotentialPower, out potPower, out errorReason)) return false;
            if (!TryParseDouble(fields, colIndex, ColPowerFactor, out powerFactor, out errorReason)) return false;
            if (!TryParseDouble(fields, colIndex, ColReactivePower, out reactivePower, out errorReason)) return false;
            if (!TryParseDouble(fields, colIndex, ColGridFrequency, out gridFreq, out errorReason)) return false;
            if (!TryParseDouble(fields, colIndex, ColGeneratorRpm, out genRpm, out errorReason)) return false;

            sample = new WindTurbineSample
            {
                Timestamp = timestamp,
                WindSpeedMs = windSpeed,
                WindDirectionDeg = windDir,
                NacellePositionDeg = nacellePos,
                PowerKW = power,
                PotentialPowerDefaultKW = potPower,
                PowerFactor = powerFactor,
                ReactivePowerKvar = reactivePower,
                GridFrequencyHz = gridFreq,
                GeneratorRpm = genRpm,
                RowIndex = rowIndex,
                TurbineId = turbineId
            };

            return true;
        }

        private static bool TryParseDouble(
            string[] fields, Dictionary<string, int> colIndex, string colName,
            out double result, out string errorReason)
        {
            result = 0;
            string raw = GetField(fields, colIndex, colName);

            if (IsNanOrEmpty(raw))
            {
                errorReason = $"Kolona '{colName}' sadrži NaN/prazan: '{raw}'";
                return false;
            }

            if (!double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                errorReason = $"Kolona '{colName}' se ne može parsirati kao broj: '{raw}'";
                return false;
            }

            errorReason = null;
            return true;
        }

        private static string GetField(string[] fields, Dictionary<string, int> colIndex, string colName)
        {
            int idx = colIndex[colName];
            if (idx < 0 || idx >= fields.Length) return string.Empty;
            return fields[idx].Trim();
        }

        private static bool IsNanOrEmpty(string value) =>
            string.IsNullOrWhiteSpace(value) || value.Equals("NaN", StringComparison.OrdinalIgnoreCase);

        private static Dictionary<string, int> BuildColumnMap(string[] headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                string name = headers[i].Trim().Trim('"');
                if (!map.ContainsKey(name))
                    map[name] = i;
            }
            return map;
        }

        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (char c in line)
            {
                if (c == '\r') continue;
                if (c == '"') { inQuotes = !inQuotes; }
                else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
                else { current.Append(c); }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}
