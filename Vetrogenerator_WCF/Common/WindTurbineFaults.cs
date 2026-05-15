using System.Runtime.Serialization;

namespace Common
{
    /// Baca se kada CSV red ne može da se parsira (pogrešan broj kolona, loš format broja, itd.).
    [DataContract]
    public class DataFormatFault
    {
        public DataFormatFault(string message, int rowIndex = -1)
        {
            Message  = message;
            RowIndex = rowIndex;
        }

        [DataMember] public string Message  { get; set; }
        [DataMember] public int    RowIndex { get; set; }
    }

    /// Baca se kada uzorak ne prođe validaciju poslovnih pravila (vrednosti van opsega, negativne vrednosti gde ne treba, itd.)
    [DataContract]
    public class ValidationFault
    {
        public ValidationFault(string message, int rowIndex = -1)
        {
            Message  = message;
            RowIndex = rowIndex;
        }

        [DataMember] public string Message  { get; set; }
        [DataMember] public int    RowIndex { get; set; }
    }
}
