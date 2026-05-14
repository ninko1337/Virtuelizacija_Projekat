using System;
using Common;

namespace Service
{
    public class WindTurbineService : IWindTurbineService
    {
        // ── Eventi ──────────────────────────────────────────────────────────
        public event EventHandler<TransferStartedEventArgs>    OnTransferStarted;
        public event EventHandler<SampleReceivedEventArgs>     OnSampleReceived;
        public event EventHandler<TransferCompletedEventArgs>  OnTransferCompleted;
        public event EventHandler<WarningRaisedEventArgs>      OnWarningRaised;

        // Analitika 1
        public event EventHandler<UnderPerformanceEventArgs>   OnUnderPerformance;

        // Analitika 2
        public event EventHandler<YawMisalignmentEventArgs>    OnYawMisalignment;
        public event EventHandler<FrequencyDeviationEventArgs> OnFrequencyDeviation;
        public event EventHandler<FrequencySpikeEventArgs>     OnFrequencySpike;

        public void StartSession(WindTurbineMeta meta)
        {
            // TODO:
            // - Kreirati strukturu foldera: Data/<TurbineId>/<YYYY-MM-DD>/
            // - Otvoriti/kreirati session.csv (FileStream/StreamWriter)
            // - Otvoriti/kreirati rejects.csv
            // - Pokrenuti događaj OnTransferStarted
            // - Prikazati status "prenos u toku"

            throw new NotImplementedException();
        }

        public void PushSample(WindTurbineSample sample)
        {
            // TODO:
            // - Validacija podataka (Timestamp parsiranje, numeričke vrednosti > 0 gde ima smisla)
            // - NaN vrednosti → upisati u rejects.csv sa razlogom, nastaviti
            // - Upisati validan red u session.csv (FileStream/StreamWriter, nadovezati)
            // - Pokrenuti događaj OnSampleReceived
            // - Pokrenuti analitiku 1: UnderPerformance
            // - Pokrenuti analitiku 2: YawMisalignment, FrequencyDeviation, FrequencySpike
            // - Na greške baciti FaultException<DataFormatFault> ili FaultException<ValidationFault>

            throw new NotImplementedException();
        }

        public void EndSession()
        {
            // TODO:
            // - Zatvoriti StreamWriter/FileStream (Dispose pattern / using)
            // - Pokrenuti događaj OnTransferCompleted
            // - Prikazati status "prenos završen"

            throw new NotImplementedException();
        }
    }
}
