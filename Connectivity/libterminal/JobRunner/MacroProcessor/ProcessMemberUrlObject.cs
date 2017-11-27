using libterminal.JobRunner.MacroParser;
using System;

namespace libterminal.JobRunner.MacroProcessor
{
    public class ProcessMemberUrlObject : ProcessMemberObject
    {
        public Uri Value { get; set; }

        string UserInfo
        {
            get
            {
                return Value.UserInfo;
            }
        }

        string Username
        {
            get
            {
                var parts = UserInfo.Split(':');
                if (parts.Length != 2)
                {
                    // TODO: Throw?
                    return string.Empty;
                }
                return parts[0];
            }
        }

        string Password
        {
            get
            {
                var parts = UserInfo.Split(':');
                if (parts.Length != 2)
                {
                    // TODO: Throw?
                    return string.Empty;
                }
                return parts[1];
            }
        }

        public override ProcessMemberObject ProcessMember(PoValue value)
        {
            if (value == null)
                return new ProcessMemberUrlObject { Value = Value };

            // TODO : Handle function call in this context
            if (value.Member is PoFunctionCall)
                return null;

            if (value.Member is PoLiteralMember)
            {
                var literalMember = value.Member as PoLiteralMember;
                switch (literalMember.Literal.Text)
                {
                    case "Host":
                        return new ProcessMemberStringObject
                        {
                            Value = Value.Host
                        };
                    case "StripUserInfo":
                        return new ProcessMemberUrlObject
                        {
                            Value = new Uri(Value.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.UserInfo, UriFormat.UriEscaped))
                        };
                    case "Username":
                        return new ProcessMemberStringObject
                        {
                            Value = Username
                        };
                    case "Password":
                        return new ProcessMemberStringObject
                        {
                            Value = Password
                        };
                    default:
                        // TODO : Handle unknown member property 
                        return null;
                }
            }

            // TODO : Handle unknown value type
            return null;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}