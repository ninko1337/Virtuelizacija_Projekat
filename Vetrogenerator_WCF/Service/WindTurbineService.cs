using System;
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

        public void StartSession(WindTurbineMeta meta)
        {
            if (meta == null)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Meta ne sme biti null.", -1));

            _currentMeta = meta;
            _receivedCount = 0;

            OnTransferStarted?.Invoke(this, new TransferStartedEventArgs(meta));
            Console.WriteLine($"[SERVER] Prenos u toku — TurbineId={meta.TurbineId}");
        }

        public void PushSample(WindTurbineSample sample)
        {
            if (sample == null)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Uzorak ne sme biti null.", -1));

            // ── validacija  ───────────────────────────────────────
            // timestamp mora da bude pravilno parsiran
            if (sample.Timestamp == default(DateTime))
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Timestamp nije ispravno parsiran.", sample.RowIndex));

            // ── validacija  ─────────────────────────
            // brzina vetra ne sme biti negativna (logicno :d)
            if (sample.WindSpeedMs < 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault($"WindSpeed ne sme biti negativan: {sample.WindSpeedMs}", sample.RowIndex));

            // GridFrequency mora biti > 0
            if (sample.GridFrequencyHz <= 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault($"GridFrequency mora biti > 0: {sample.GridFrequencyHz}", sample.RowIndex));

            // GeneratorRpm ne sme biti negativan
            if (sample.GeneratorRpm < 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault($"GeneratorRpm ne sme biti negativan: {sample.GeneratorRpm}", sample.RowIndex));

            // ── SVE OK ───────────────────────────────────
            _receivedCount++;
            OnSampleReceived?.Invoke(this,
                new SampleReceivedEventArgs(sample, _currentMeta?.TurbineId ?? "unknown"));

            Console.WriteLine($"[SERVER] Primljen red {sample.RowIndex} | {sample.Timestamp:yyyy-MM-dd HH:mm} | Wind={sample.WindSpeedMs:F1} m/s");
        }

        public void EndSession()
        {
            var turbineId = _currentMeta?.TurbineId ?? "unknown";

            OnTransferCompleted?.Invoke(this,
                new TransferCompletedEventArgs(turbineId, _receivedCount));

            Console.WriteLine($"[SERVER] Prenos završen — primljeno {_receivedCount} uzoraka.");

            _currentMeta = null;
            _receivedCount = 0;
        }
    }
}