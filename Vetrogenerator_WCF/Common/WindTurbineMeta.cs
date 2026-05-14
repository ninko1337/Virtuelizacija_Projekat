using System.Runtime.Serialization;

namespace Common
{
    /// 
    /// Meta podaci koji se šalju jednom na početku svake sesije prenosa.
    /// TurbineId se izvlači iz naziva fajla (npr. Kelmarsh_1 → "Kelmarsh_1").
    /// 
    [DataContract]
    public class WindTurbineMeta
    {
        public WindTurbineMeta(string turbineId, string fileName, int totalRows, string schemaVersion = "1.0")
        {
            TurbineId     = turbineId;
            FileName      = fileName;
            TotalRows     = totalRows;
            SchemaVersion = schemaVersion;
        }

        [DataMember] public string TurbineId     { get; set; }
        [DataMember] public string FileName      { get; set; }
        [DataMember] public int    TotalRows     { get; set; }
        [DataMember] public string SchemaVersion { get; set; }

        public override string ToString() =>
            $"[WindTurbineMeta] TurbineId={TurbineId}, File={FileName}, Rows={TotalRows}";
    }
}
