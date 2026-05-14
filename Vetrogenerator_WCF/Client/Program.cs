using System;
using System.ServiceModel;
using Common;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Vetrogenerator WCF Klijent ===");

            // TODO:
            // 1. Prikazati korisniku listu dostupnih fajlova (Kelmarsh_1 ... Kelmarsh_6)
            // 2. Korisnik bira jedan fajl
            // 3. Parsirati izabrani CSV fajl pomoću CsvParser.Parse()
            // 4. Kreirati WCF proxy (ChannelFactory ili generisani proxy)
            // 5. Pozvati StartSession(meta) — proslediti WindTurbineMeta
            // 6. Sekvencijalno slati uzorke: PushSample(sample) red po red
            //    - Prikazivati status "prenos u toku" (npr. broj poslatih redova)
            // 7. Pozvati EndSession()
            //    - Prikazati "prenos završen"
            // 8. Pravilno zatvoriti proxy i sve resurse (Dispose pattern / using)

            Console.ReadLine();
        }
    }
}
