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

        private SessionFileWriter _fileWriter;

        public void StartSession(WindTurbineMeta meta)
        {
            if (meta == null)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Meta ne sme biti null.", -1), new FaultReason("Meta ne sme biti null."));

            _currentMeta = meta;
            _receivedCount = 0;
            _rejectedCount = 0;

            string dataRoot = ConfigurationManager.AppSettings["DataPath"] ?? "Data";
            _fileWriter = new SessionFileWriter(dataRoot, meta.TurbineId);

            OnTransferStarted?.Invoke(this, new TransferStartedEventArgs(meta));
            Console.WriteLine($"[SERVER] Snimam u: {_fileWriter.SessionFilePath}");
        }

        public void PushSample(WindTurbineSample sample)
        {
            if (sample == null)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Uzorak ne sme biti null.", -1), new FaultReason("Uzorak ne sme biti null."));

            if (_fileWriter == null)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Sesija nije započeta (pozovite StartSession pre PushSample).", sample.RowIndex), new FaultReason("Sesija nije započeta."));

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

            // ── SVE OK ───────────────────────────────────
            _fileWriter.WriteSample(sample);
            _receivedCount++;

            OnSampleReceived?.Invoke(this,
                new SampleReceivedEventArgs(sample, _currentMeta?.TurbineId ?? "unknown"));
        }

        private void RejectAndThrow(WindTurbineSample sample, string reason, bool isDataFormat)
        {
            _rejectedCount++;
            _fileWriter.WriteReject(sample.RowIndex, reason, ReconstructLine(sample));
            OnWarningRaised?.Invoke(this,
                new WarningRaisedEventArgs(_currentMeta?.TurbineId ?? "unknown", $"Odbijen uzorak → rejects.csv: {reason}", sample.RowIndex));

            if (isDataFormat)
                throw new FaultException<DataFormatFault>(new DataFormatFault(reason, sample.RowIndex), new FaultReason(reason));
            else
                throw new FaultException<ValidationFault>(new ValidationFault(reason, sample.RowIndex), new FaultReason(reason));
        }

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

            _fileWriter?.Dispose();
            _fileWriter = null;

            _currentMeta = null;
            _receivedCount = 0;
            _rejectedCount = 0;
        }
    }
}
