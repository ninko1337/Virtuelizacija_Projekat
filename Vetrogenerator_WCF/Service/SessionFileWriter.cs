using System;
using System.Globalization;
using System.IO;
using Common;

namespace Service
{
    /// <summary>
    /// Upravlja snimanjem podataka jedne sesije na disk (Kontrolna tačka 2 — zadatak 6).
    ///
    /// Pri kreiranju (StartSession) pravi strukturu:
    ///     Data/&lt;TurbineId&gt;/&lt;YYYY-MM-DD&gt;/session.csv
    ///     Data/&lt;TurbineId&gt;/&lt;YYYY-MM-DD&gt;/rejects.csv
    ///
    /// - Valjani uzorci se nadovezuju (append) u session.csv.
    /// - Odbijeni redovi se beleže u rejects.csv (razlog + originalna linija).
    /// - Resursi (FileStream/StreamWriter) se oslobađaju kroz Dispose pattern.
    /// </summary>
    public sealed class SessionFileWriter : IDisposable
    {
        private readonly StreamWriter _sessionWriter;
        private readonly StreamWriter _rejectsWriter;
        private bool _disposed;

        public string SessionFilePath { get; }
        public string RejectsFilePath { get; }
        public string SessionDirectory { get; }

        // CSV header za session.csv — odgovara poljima WindTurbineSample DataContract-a.
        private const string SessionHeader =
            "Timestamp,WindSpeedMs,WindDirectionDeg,NacellePositionDeg,PowerKW," +
            "PotentialPowerDefaultKW,PowerFactor,ReactivePowerKvar,GridFrequencyHz," +
            "GeneratorRpm,RowIndex,TurbineId";

        private const string RejectsHeader = "RejectedAtUtc,RowIndex,Reason,OriginalLine";

        /// <summary>
        /// Kreira strukturu Data/&lt;TurbineId&gt;/&lt;YYYY-MM-DD&gt;/ i otvara fajlove za dopisivanje.
        /// Datum se uzima iz trenutka pokretanja sesije (datum servera).
        /// </summary>
        public SessionFileWriter(string dataRoot, string turbineId)
        {
            if (string.IsNullOrWhiteSpace(dataRoot)) dataRoot = "Data";
            if (string.IsNullOrWhiteSpace(turbineId)) turbineId = "unknown";

            string dateFolder = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            SessionDirectory = Path.Combine(dataRoot, turbineId, dateFolder);
            Directory.CreateDirectory(SessionDirectory);

            SessionFilePath = Path.Combine(SessionDirectory, "session.csv");
            RejectsFilePath = Path.Combine(SessionDirectory, "rejects.csv");

            // Header se upisuje samo ako fajl još ne postoji (da se ne dupliraju kod append-a).
            bool sessionIsNew = !File.Exists(SessionFilePath);
            bool rejectsIsNew = !File.Exists(RejectsFilePath);

            // append: true → nadovezivanje na postojeći fajl (FileStream/StreamWriter).
            _sessionWriter = new StreamWriter(
                new FileStream(SessionFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
            { AutoFlush = true };

            _rejectsWriter = new StreamWriter(
                new FileStream(RejectsFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
            { AutoFlush = true };

            if (sessionIsNew) _sessionWriter.WriteLine(SessionHeader);
            if (rejectsIsNew) _rejectsWriter.WriteLine(RejectsHeader);
        }

        /// <summary>Dopisuje jedan valjan uzorak u session.csv.</summary>
        public void WriteSample(WindTurbineSample s)
        {
            ThrowIfDisposed();

            var ci = CultureInfo.InvariantCulture;
            string line = string.Join(",", new[]
            {
                s.Timestamp.ToString("o", ci),
                s.WindSpeedMs.ToString(ci),
                s.WindDirectionDeg.ToString(ci),
                s.NacellePositionDeg.ToString(ci),
                s.PowerKW.ToString(ci),
                s.PotentialPowerDefaultKW.ToString(ci),
                s.PowerFactor.ToString(ci),
                s.ReactivePowerKvar.ToString(ci),
                s.GridFrequencyHz.ToString(ci),
                s.GeneratorRpm.ToString(ci),
                s.RowIndex.ToString(ci),
                Csv(s.TurbineId)
            });

            _sessionWriter.WriteLine(line);
        }

        /// <summary>Beleži odbijeni red u rejects.csv: razlog odbijanja + originalna linija.</summary>
        public void WriteReject(int rowIndex, string reason, string originalLine)
        {
            ThrowIfDisposed();

            string line = string.Join(",", new[]
            {
                DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                rowIndex.ToString(CultureInfo.InvariantCulture),
                Csv(reason),
                Csv(originalLine)
            });

            _rejectsWriter.WriteLine(line);
        }

        /// <summary>Escapuje polje za CSV (navodnici ako sadrži , " ili novi red).</summary>
        private static string Csv(string value)
        {
            if (value == null) return string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SessionFileWriter));
        }

        // ── Dispose pattern ─────────────────────────────────────────────────
        public void Dispose()
        {
            if (_disposed) return;

            try { _sessionWriter?.Flush(); _sessionWriter?.Dispose(); } catch { /* ignore */ }
            try { _rejectsWriter?.Flush(); _rejectsWriter?.Dispose(); } catch { /* ignore */ }

            _disposed = true;
        }
    }
}
