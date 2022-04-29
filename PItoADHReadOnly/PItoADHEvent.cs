using System;
using System.Globalization;
using System.Text;
using OSIsoft.Data;

namespace PItoADHReadOnly
{
    public class PItoADHEvent
    {
        [SdsMember(IsKey = true)]
        public DateTime Timestamp { get; set; }

        public float? Value { get; set; }

        public bool IsQuestionable { get; set; }

        public bool IsSubstituted { get; set; }

        public bool IsAnnotated { get; set; }

        public int? SystemStateCode { get; set; }

        public string DigitalStateName { get; set; }

        public override string ToString()
        {
            CultureInfo cultureInfo = CultureInfo.InvariantCulture;
            StringBuilder sb = new ();

            sb.Append(cultureInfo, $"Timestamp: {Timestamp}, ");

            if (Value is not null)
            {
                sb.Append(cultureInfo, $"Value: {Value}, ");
            }

            sb.Append(cultureInfo, $"IsQuestionable: {IsQuestionable}, ");
            sb.Append(cultureInfo, $"IsSubstituted: {IsSubstituted}, ");
            sb.Append(cultureInfo, $"IsAnnotated: {IsAnnotated}");

            // In case Value is null, the event will specify a SystemStateCode
            // integer with DigitalStateName as its string representation
            if (SystemStateCode is not null)
            {
                sb.Append(cultureInfo, $", SystemStateCode: {SystemStateCode}, ");
                sb.Append(cultureInfo, $"DigitalStateName: {DigitalStateName}");
            }

            return sb.ToString();
        }
    }
}
