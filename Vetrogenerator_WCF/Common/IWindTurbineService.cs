using System.ServiceModel;

namespace Common
{
    /// 
    /// WCF Service Contract za streaming SCADA podataka vetrogeneratora.
    ///
    /// Protokol:
    ///   1. Klijent poziva StartSession(meta)    → server otvara/kreira CSV fajl
    ///   2. Klijent poziva PushSample(sample)    → server validira + upisuje red
    ///   3. Klijent poziva EndSession()          → server zatvara fajl, okida OnTransferCompleted
    /// 
    [ServiceContract]
    public interface IWindTurbineService
    {
        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        void StartSession(WindTurbineMeta meta);

        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        void PushSample(WindTurbineSample sample);

        [OperationContract]
        void EndSession();
    }
}
