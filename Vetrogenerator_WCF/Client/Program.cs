using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.ServiceModel;

namespace Client
{
    internal class Program
    {

        private static readonly (string TurbineId, string FileName)[] TurbineFiles =
        {
            ("Kelmarsh_1", "Turbine_Data_Kelmarsh_1_2018-01-01_-_2019-01-01_228.csv"),
            ("Kelmarsh_2", "Turbine_Data_Kelmarsh_2_2018-01-01_-_2019-01-01_229.csv"),
            ("Kelmarsh_3", "Turbine_Data_Kelmarsh_3_2018-01-01_-_2019-01-01_230.csv"),
            ("Kelmarsh_4", "Turbine_Data_Kelmarsh_4_2018-01-01_-_2019-01-01_231.csv"),
            ("Kelmarsh_5", "Turbine_Data_Kelmarsh_5_2018-01-01_-_2019-01-01_232.csv"),
            ("Kelmarsh_6", "Turbine_Data_Kelmarsh_6_2018-01-01_-_2019-01-01_233.csv"),
        };
        static void Main(string[] args)
        {
            Console.WriteLine("=== Vetrogenerator WCF Klijent ===");
            Console.WriteLine();

            //Izbor fajla
            Console.WriteLine("Izaberite turbinu (1-6):");
            for (int i = 0; i < TurbineFiles.Length; i++)
                Console.WriteLine($"  [{i + 1}] {TurbineFiles[i].TurbineId}  ({TurbineFiles[i].FileName})");

            Console.Write("> ");
            int choice = 0;
            if (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > 6)
            {
                Console.WriteLine("[GREŠKA] Neispravan izbor. Prekidam.");
                return;
            }

            var (turbineId, fileName) = TurbineFiles[choice - 1];
            string dataFolder = ConfigurationManager.AppSettings["ScadaDataPath"] ?? "Data";
            string csvPath = Path.Combine(dataFolder, fileName);

            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"[GREŠKA] Fajl nije pronađen: {csvPath}");
                return;
            }

            // Parsiranje csva
            Console.WriteLine($"[KLIJENT] Učitavam fajl: {csvPath}");
            List<WindTurbineSample> samples = CsvParser.Parse(csvPath, turbineId);
            Console.WriteLine($"[KLIJENT] Uspešno parsirano {samples.Count} uzoraka.");
            Console.WriteLine($"[KLIJENT] Odbačeni redovi su upisani u: client_errors.log");

            if (samples.Count == 0)
            {
                Console.WriteLine("[UPOZORENJE] Nema valjanih uzoraka za slanje. Prekidam.");
                return;
            }

            ChannelFactory<IWindTurbineService> factory = null;
            IWindTurbineService proxy = null;

            try
            {
                factory = new ChannelFactory<IWindTurbineService>("WindTurbineService");
                proxy = factory.CreateChannel();

                var meta = new WindTurbineMeta(turbineId, fileName, samples.Count);
                Console.WriteLine($"[KLIJENT] Šaljem StartSession → {meta}");
                proxy.StartSession(meta);

                // Sekv. streaming
                Console.WriteLine("[KLIJENT] Prenos u toku...");
                int sent = 0;

                foreach (WindTurbineSample sample in samples)
                {
                    try
                    {
                        proxy.PushSample(sample);
                        sent++;

                        if (sent % 100 == 0 || sent == samples.Count)
                            Console.Write($"\r[KLIJENT] Prenos u toku: {sent}/{samples.Count} ({100.0 * sent / samples.Count:F1}%)   ");
                    }
                    catch (FaultException<DataFormatFault> dfEx)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"[UPOZORENJE] Server odbio uzorak {sample.RowIndex} (DataFormatFault): {dfEx.Detail.Message}");
                    }
                    catch (FaultException<ValidationFault> valEx)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"[UPOZORENJE] Server odbio uzorak {sample.RowIndex} (ValidationFault): {valEx.Detail.Message}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"[KLIJENT] Prenos završen. Poslato {sent}/{samples.Count} uzoraka.");

                proxy.EndSession();
                Console.WriteLine("[KLIJENT] Sesija zatvorena.");

                // Normalno zatvaranje
                CloseChannelSafely((IClientChannel)proxy);
                proxy = null;
                factory.Close();
                factory = null;
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"[GREŠKA] ValidationFault pri StartSession: {ex.Detail.Message}");
                AbortChannelSafely((IClientChannel)proxy);
                proxy = null;
            }
            catch (CommunicationException ex)
            {
                
                Console.WriteLine($"[GREŠKA] Mrežni problem: {ex.Message}");
                Console.WriteLine("[KLIJENT] Prekid prenosa — čistim resurse...");
                AbortChannelSafely((IClientChannel)proxy);
                proxy = null;
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"[GREŠKA] Timeout: {ex.Message}");
                AbortChannelSafely((IClientChannel)proxy);
                proxy = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GREŠKA] Neočekivana greška: {ex.Message}");
                AbortChannelSafely((IClientChannel)proxy);
                proxy = null;
            }
            finally
            {
                
                if (proxy != null)
                {
                    AbortChannelSafely((IClientChannel)proxy);
                    proxy = null;
                }

                if (factory != null)
                {
                    try
                    {
                        if (factory.State == CommunicationState.Faulted) factory.Abort();
                        else factory.Close();
                    }
                    catch { factory.Abort(); }
                    factory = null;
                }

                Console.WriteLine("[KLIJENT] Svi resursi su oslobođeni (proxy, factory).");
            }

            Console.WriteLine();
            Console.WriteLine("Pritisnite ENTER za izlaz...");
            Console.ReadLine();
        }

        private static void CloseChannelSafely(IClientChannel channel)
        {
            if (channel == null) return;
            try
            {
                if (channel.State == CommunicationState.Faulted)
                    channel.Abort();
                else
                    channel.Close();
            }
            catch (CommunicationException) { channel.Abort(); }
            catch (TimeoutException) { channel.Abort(); }
            catch (Exception) { channel.Abort(); }
        }

        /// Abort bez čekanja (za prekide i exceptione)
        private static void AbortChannelSafely(IClientChannel channel)
        {
            if (channel == null) return;
            try { channel.Abort(); }
            catch { /* ništa?? */ }
        }
    }
}
