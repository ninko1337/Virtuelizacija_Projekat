using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public enum ResultType
    {
        [EnumMember] Success,
        [EnumMember] Warning,
        [EnumMember] Failed
    }
}
