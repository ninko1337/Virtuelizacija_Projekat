using System;

namespace Common
{
    // ── Životni ciklus prenosa ───────────────────────────────────────────────

    public class TransferStartedEventArgs : EventArgs
    {
        public TransferStartedEventArgs(WindTurbineMeta meta) { Meta = meta; }
        public WindTurbineMeta Meta { get; }
    }

    public class SampleReceivedEventArgs : EventArgs
    {
        public SampleReceivedEventArgs(WindTurbineSample sample, string turbineId)
        {
            Sample    = sample;
            TurbineId = turbineId;
        }
        public WindTurbineSample Sample    { get; }
        public string            TurbineId { get; }
    }

    public class TransferCompletedEventArgs : EventArgs
    {
        public TransferCompletedEventArgs(string turbineId, int totalReceived)
        {
            TurbineId     = turbineId;
            TotalReceived = totalReceived;
        }
        public string TurbineId     { get; }
        public int    TotalReceived { get; }
    }

    // ── Upozorenja ──────────────────────────────────────────────────────────

    public class WarningRaisedEventArgs : EventArgs
    {
        public WarningRaisedEventArgs(string turbineId, string message, int rowIndex = -1)
        {
            TurbineId = turbineId;
            Message   = message;
            RowIndex  = rowIndex;
            RaisedAt  = DateTime.Now;
        }
        public string   TurbineId { get; }
        public string   Message   { get; }
        public int      RowIndex  { get; }
        public DateTime RaisedAt  { get; }
    }

    // ── Analitika 1 — Snaga (Power vs Potential) ────────────────────────────

    public class UnderPerformanceEventArgs : EventArgs
    {
        public UnderPerformanceEventArgs(string turbineId, DateTime timestamp, int rowIndex,
                                         double powerKW, double potentialKW, double deltaPct)
        {
            TurbineId   = turbineId;
            Timestamp   = timestamp;
            RowIndex    = rowIndex;
            PowerKW     = powerKW;
            PotentialKW = potentialKW;
            DeltaPct    = deltaPct;
        }
        public string   TurbineId   { get; }
        public DateTime Timestamp   { get; }
        public int      RowIndex    { get; }
        public double   PowerKW     { get; }
        public double   PotentialKW { get; }
        /// Procentualni pad u odnosu na potencijalnu snagu.
        public double   DeltaPct    { get; }
    }

    // ── Analitika 2 — Orijentacija (Yaw) ────────────────────────────────────

    public class YawMisalignmentEventArgs : EventArgs
    {
        public YawMisalignmentEventArgs(string turbineId, DateTime timestamp, int rowIndex,
                                        double windDirectionDeg, double nacellePositionDeg, double misalignDeg)
        {
            TurbineId          = turbineId;
            Timestamp          = timestamp;
            RowIndex           = rowIndex;
            WindDirectionDeg   = windDirectionDeg;
            NacellePositionDeg = nacellePositionDeg;
            MisalignDeg        = misalignDeg;
        }
        public string   TurbineId          { get; }
        public DateTime Timestamp          { get; }
        public int      RowIndex           { get; }
        public double   WindDirectionDeg   { get; }
        public double   NacellePositionDeg { get; }
        public double   MisalignDeg        { get; }
    }

    // ── Analitika 2 — Frekvencija mreže ─────────────────────────────────────

    public class FrequencyDeviationEventArgs : EventArgs
    {
        public FrequencyDeviationEventArgs(string turbineId, DateTime timestamp, int rowIndex,
                                           double frequencyHz, double deviationHz)
        {
            TurbineId     = turbineId;
            Timestamp     = timestamp;
            RowIndex      = rowIndex;
            FrequencyHz   = frequencyHz;
            DeviationHz   = deviationHz;
        }
        public string   TurbineId   { get; }
        public DateTime Timestamp   { get; }
        public int      RowIndex    { get; }
        public double   FrequencyHz { get; }
        public double   DeviationHz { get; }
    }

    public class FrequencySpikeEventArgs : EventArgs
    {
        public FrequencySpikeEventArgs(string turbineId, DateTime timestamp, int rowIndex,
                                       double frequencyBefore, double frequencyAfter, double deltaHz)
        {
            TurbineId       = turbineId;
            Timestamp       = timestamp;
            RowIndex        = rowIndex;
            FrequencyBefore = frequencyBefore;
            FrequencyAfter  = frequencyAfter;
            DeltaHz         = deltaHz;
        }
        public string   TurbineId       { get; }
        public DateTime Timestamp       { get; }
        public int      RowIndex        { get; }
        public double   FrequencyBefore { get; }
        public double   FrequencyAfter  { get; }
        public double   DeltaHz         { get; }
        public string   Direction       => DeltaHz > 0 ? "UP" : "DOWN";
    }
}
