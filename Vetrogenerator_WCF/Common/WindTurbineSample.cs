using System;
using System.Runtime.Serialization;

namespace Common
{
    /// 
    /// Predstavlja jedan red iz Kelmarsh SCADA CSV fajla.
    /// Zaglavlje je u 10. redu, podaci počinju od 11. reda.
    ///
    /// Izdvojeni kanali:
    ///   Date and time, Wind speed (m/s), Wind direction (°),
    ///   Nacelle position (°), Power (kW), Potential power default PC (kW),
    ///   Power factor (cosphi), Reactive power (kvar),
    ///   Grid frequency (Hz), Generator RPM (RPM)
    /// 
    [DataContract]
    public class WindTurbineSample
    {
        // ── Vreme ───────────────────────────────────────────────────────────
        [DataMember] public DateTime Timestamp                { get; set; }

        // ── Vetar ───────────────────────────────────────────────────────────
        [DataMember] public double   WindSpeedMs              { get; set; }
        [DataMember] public double   WindDirectionDeg         { get; set; }
        [DataMember] public double   NacellePositionDeg       { get; set; }

        // ── Snaga ───────────────────────────────────────────────────────────
        [DataMember] public double   PowerKW                  { get; set; }
        [DataMember] public double   PotentialPowerDefaultKW  { get; set; }
        [DataMember] public double   PowerFactor              { get; set; }
        [DataMember] public double   ReactivePowerKvar        { get; set; }

        // ── Mreža i generator ───────────────────────────────────────────────
        [DataMember] public double   GridFrequencyHz          { get; set; }
        [DataMember] public double   GeneratorRpm             { get; set; }

        // ── Sekvenca ────────────────────────────────────────────────────────
        /// 0-baziran indeks reda unutar trenutne sesije.
        [DataMember] public int      RowIndex                 { get; set; }

        /// ID turbine (npr. "Kelmarsh_1"), prosleđen iz meta podataka sesije.
        [DataMember] public string   TurbineId               { get; set; }

        public override string ToString() =>
            $"[Row {RowIndex}] {Timestamp:O} | Wind={WindSpeedMs:F2} m/s  Power={PowerKW:F1} kW  Freq={GridFrequencyHz:F3} Hz";
    }
}
