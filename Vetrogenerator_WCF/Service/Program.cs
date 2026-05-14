using System;
using System.ServiceModel;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            WindTurbineService serviceInstance = new WindTurbineService();

            // TODO: pretplatiti se na događaje (logovanje na konzolu)
            // serviceInstance.OnTransferStarted   += (s, e) => Console.WriteLine(...);
            // serviceInstance.OnSampleReceived    += (s, e) => Console.WriteLine(...);
            // serviceInstance.OnTransferCompleted += (s, e) => Console.WriteLine(...);
            // serviceInstance.OnWarningRaised     += (s, e) => Console.WriteLine(...);
            // serviceInstance.OnUnderPerformance  += (s, e) => Console.WriteLine(...);
            // serviceInstance.OnYawMisalignment   += (s, e) => Console.WriteLine(...);
            // serviceInstance.OnFrequencyDeviation+= (s, e) => Console.WriteLine(...);
            // serviceInstance.OnFrequencySpike    += (s, e) => Console.WriteLine(...);

            using (ServiceHost host = new ServiceHost(serviceInstance))
            {
                host.Open();
                Console.WriteLine("[Service] Vetrogenerator WCF servis pokrenut. Pritisnite ENTER za zaustavljanje.");
                Console.ReadLine();
                host.Close();
            }
        }
    }
}
