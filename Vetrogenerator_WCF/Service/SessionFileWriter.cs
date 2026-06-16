using System;
using System.Globalization;
using System.IO;
using Common;

namespace Service
{
    public sealed class SessionFileWriter : IDisposable
    {
        private readonly StreamWriter _sessionWriter;
        private readonly StreamWriter _rejectsWriter;
        private bool _disposed;

        public string SessionFilePath { get; }
        public string RejectsFilePath { get; }
        public string SessionDirectory { get; }

        private const string SessionHeader =
            "Timestamp,WindSpeedMs,WindDirectionDeg,NacellePositionDeg,PowerKW," +
            "PotentialPowerDefaultKW,PowerFactor,ReactivePowerKvar,GridFrequencyHz," +
            "GeneratorRpm,RowIndex,TurbineId";

        private const string RejectsHeader = "RejectedAtUtc,RowIndex,Reason,OriginalLine";

        public SessionFileWriter(string dataRoot, string turbineId)
        {
            if (string.IsNullOrWhiteSpace(dataRoot)) dataRoot = "Data";
            if (string.IsNullOrWhiteSpace(turbineId)) turbineId = "unknown";

            string dateFolder = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            SessionDirectory = Path.Combine(dataRoot, turbineId, dateFolder);
            Directory.CreateDirectory(SessionDirectory);

            SessionFilePath = Path.Combine(SessionDirectory, "session.csv");
            RejectsFilePath = Path.Combine(SessionDirectory, "rejects.csv");

            bool sessionIsNew = !File.Exists(SessionFilePath);
            bool rejectsIsNew = !File.Exists(RejectsFilePath);

            _sessionWriter = new StreamWriter(
                new FileStream(SessionFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
            { AutoFlush = true };

            _rejectsWriter = new StreamWriter(
                new FileStream(RejectsFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
            { AutoFlush = true };

            if (sessionIsNew) _sessionWriter.WriteLine(SessionHeader);
            if (rejectsIsNew) _rejectsWriter.WriteLine(RejectsHeader);
        }

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

        public void Dispose()
        {
            if (_disposed) return;

            try { _sessionWriter?.Flush(); _sessionWriter?.Dispose(); } catch { }
            try { _rejectsWriter?.Flush(); _rejectsWriter?.Dispose(); } catch { }

            _disposed = true;
        }
    }
}
