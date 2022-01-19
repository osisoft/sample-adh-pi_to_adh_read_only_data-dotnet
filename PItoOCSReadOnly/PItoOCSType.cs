using System;
using System.Text;
using OSIsoft.Data;

namespace PItoOCSReadOnly
{
    public class PItoOCSType
    {
        [SdsMember(IsKey = true)]
        public DateTime Timestamp { get; set; }

        public float Value { get; set; }

        public bool? IsQuestionable { get; set; }

        public bool? IsSubstituted { get; set; }

        public bool? IsAnnotated { get; set; }

        public int? SystemStateCode { get; set; }

        public int? DigitalStateName { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($", Timestamp: {Timestamp}");
            sb.Append($", Value: {Value}");

            if (IsQuestionable != null) { sb.Append($", IsQuestionable: {IsQuestionable}"); }
            if (IsSubstituted != null) { sb.Append($", IsSubstituted: {IsSubstituted}"); }
            if (IsAnnotated != null) { sb.Append($", IsAnnotated: {IsAnnotated}"); }
            if (SystemStateCode != null) { sb.Append($", SystemStateCode: {SystemStateCode}"); }
            if (DigitalStateName != null) { sb.Append($", DigitalStateName: {DigitalStateName}"); }

            return sb.ToString();
        }
    }
}
