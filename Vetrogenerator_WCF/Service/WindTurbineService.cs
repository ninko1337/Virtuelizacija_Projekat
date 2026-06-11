using System;
using System.Configuration;
using System.Globalization;
using System.ServiceModel;
using Common;

namespace Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
                     ConcurrencyMode = ConcurrencyMode.Single)]
    public class WindTurbineService : IWindTurbineService
    {
        // ── eventi ──────────────────────────────────────────────────────────
        public event EventHandler<TransferStartedEventArgs> OnTransferStarted;
        public event EventHandler<SampleReceivedEventArgs> OnSampleReceived;
        public event EventHandler<TransferCompletedEventArgs> OnTransferCompleted;
        public event EventHandler<WarningRaisedEventArgs> OnWarningRaised;
        public event EventHandler<UnderPerformanceEventArgs> OnUnderPerformance;
        public event EventHandler<YawMisalignmentEventArgs> OnYawMisalignment;
        public event EventHandler<FrequencyDeviationEventArgs> OnFrequencyDeviation;
        public event EventHandler<FrequencySpikeEventArgs> OnFrequencySpike;

        // ── stanje sesije  ────────────────────────────────────────────
        private WindTurbineMeta _currentMeta;
        private int _receivedCount;
        private int _rejectedCount;

        // Snimanje na disk (Kontrolna tačka 2 — zadatak 6).
        private SessionFileWriter _fileWriter;

        public void StartSession(WindTurbineMeta meta)
        {
            if (meta == null)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Meta ne sme biti null.", -1));

            _currentMeta = meta;
            _receivedCount = 0;
            _rejectedCount = 0;

            // ── Snimanje: kreiraj Data/<TurbineId>/<YYYY-MM-DD>/ i otvori session.csv + rejects.csv ──
            string dataRoot = ConfigurationManager.AppSettings["DataPath"] ?? "Data";
            _fileWriter = new SessionFileWriter(dataRoot, meta.TurbineId);

            OnTransferStarted?.Invoke(this, new TransferStartedEventArgs(meta));
            Console.WriteLine($"[SERVER] Prenos u toku — TurbineId={meta.TurbineId}");
            Console.WriteLine($"[SERVER] Snimam u: {_fileWriter.SessionFilePath}");
        }

        public void PushSample(WindTurbineSample sample)
        {
            if (sample == null)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Uzorak ne sme biti null.", -1));

            if (_fileWriter == null)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Sesija nije započeta (pozovite StartSession pre PushSample).", sample.RowIndex));

            // ── validacija ──────────────────────────────────────────────────
            // timestamp mora da bude pravilno parsiran
            if (sample.Timestamp == default(DateTime))
                RejectAndThrow(sample, "Timestamp nije ispravno parsiran.", isDataFormat: true);

            // brzina vetra ne sme biti negativna (logicno :d)
            if (sample.WindSpeedMs < 0)
                RejectAndThrow(sample, $"WindSpeed ne sme biti negativan: {sample.WindSpeedMs}", isDataFormat: false);

            // GridFrequency mora biti > 0
            if (sample.GridFrequencyHz <= 0)
                RejectAndThrow(sample, $"GridFrequency mora biti > 0: {sample.GridFrequencyHz}", isDataFormat: false);

            // GeneratorRpm ne sme biti negativan
            if (sample.GeneratorRpm < 0)
                RejectAndThrow(sample, $"GeneratorRpm ne sme biti negativan: {sample.GeneratorRpm}", isDataFormat: false);

            // ── SVE OK → upiši red u session.csv ────────────────────────────
            _fileWriter.WriteSample(sample);
            _receivedCount++;

            OnSampleReceived?.Invoke(this,
                new SampleReceivedEventArgs(sample, _currentMeta?.TurbineId ?? "unknown"));

            Console.WriteLine($"[SERVER] Primljen red {sample.RowIndex} | {sample.Timestamp:yyyy-MM-dd HH:mm} | Wind={sample.WindSpeedMs:F1} m/s");
        }

        /// <summary>
        /// Beleži odbijeni uzorak u rejects.csv (razlog + rekonstruisana originalna linija)
        /// i potom baca odgovarajući FaultException (zadatak 3 — DataFormatFault/ValidationFault).
        /// </summary>
        private void RejectAndThrow(WindTurbineSample sample, string reason, bool isDataFormat)
        {
            _rejectedCount++;
            _fileWriter.WriteReject(sample.RowIndex, reason, ReconstructLine(sample));
            Console.WriteLine($"[SERVER] ODBIJEN red {sample.RowIndex} → rejects.csv | {reason}");

            if (isDataFormat)
                throw new FaultException<DataFormatFault>(new DataFormatFault(reason, sample.RowIndex));
            else
                throw new FaultException<ValidationFault>(new ValidationFault(reason, sample.RowIndex));
        }

        /// <summary>
        /// Rekonstruiše CSV liniju iz primljenog uzorka (server ne dobija sirov tekst reda,
        /// pa se originalna linija za rejects.csv sastavlja iz vrednosti uzorka).
        /// </summary>
        private static string ReconstructLine(WindTurbineSample s)
        {
            var ci = CultureInfo.InvariantCulture;
            return string.Join(",", new[]
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
                s.GeneratorRpm.ToString(ci)
            });
        }

        public void EndSession()
        {
            var turbineId = _currentMeta?.TurbineId ?? "unknown";

            OnTransferCompleted?.Invoke(this,
                new TransferCompletedEventArgs(turbineId, _receivedCount));

            Console.WriteLine($"[SERVER] Prenos završen — primljeno {_receivedCount}, odbijeno {_rejectedCount} uzoraka.");

            // ── Snimanje: zatvori fajlove (Dispose pattern) ─────────────────
            _fileWriter?.Dispose();
            _fileWriter = null;

            _currentMeta = null;
            _receivedCount = 0;
            _rejectedCount = 0;
        }
    }
}
